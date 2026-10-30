// SPDX-FileCopyrightText: 2026 facility13.devops contributors <https://github.com/ForNeVeR/facility13.devops>
//
// SPDX-License-Identifier: MIT

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open Fabricator.Core
open Fabricator.Secrets.KeePass
open TruePath
open TruePath.SystemIo

let private repositoryRoot = AbsolutePath(__SOURCE_DIRECTORY__) / ".."

let private configFileName =
    LocalPath(
        if Environment.GetEnvironmentVariable "CI" <> null
        then "settings.example.json"
        else "settings.json"
    )
let private configFile = repositoryRoot / configFileName
let private config =
    if not <| configFile.ExistsFile() then failwith $"Missing file \"{configFile.Value}\"."
    use stream = configFile.OpenRead()
    stream
    |> JsonSerializer.Deserialize<Dictionary<string, JsonElement>>
    |> nonNull

let private configString(key: string) = nonNull <| config[key].GetString()

/// The command to run ansible-playbook, e.g. ["wsl", "--", "ansible-playbook"].
let private ansibleCommand =
    config["ansible-playbook.command"].EnumerateArray()
    |> Seq.map(fun element -> nonNull <| element.GetString())
    |> Seq.toList

let private secretStore = KeePassSecretStore(AbsolutePath(configString "kdbx.path"))
let private becomePassword = { Store = secretStore; Name = SecretName(configString "kdbx.become-password-name") }
let private vaultPassword = { Store = secretStore; Name = SecretName(configString "kdbx.vault-password-name") }

/// The environment variable passing the become password to Ansible.
let private becomePasswordVariable = "FACILITY13_BECOME_PASSWORD"

let private commonArgs = [
    "-i"; "hosts.ini"
    "--vault-password-file"; "/bin/cat"
    "--extra-vars"; "ansible_become_password={{ lookup('env', '" + becomePasswordVariable + "') }}"
]
let private playbook = "default.yml"

let private runAnsible (ctx: ResourceContext) (args: string list): Async<string[]> = async {
    let! ct = Async.CancellationToken
    let exe, args =
        match ansibleCommand with
        | exe :: baseArgs -> exe, baseArgs @ args
        | [] -> failwith "The \"ansible-playbook.command\" setting is empty."
    ctx.Reporter.Log $"""> {String.Join(" ", exe :: args)}"""

    let startInfo =
        ProcessStartInfo(
            exe,
            WorkingDirectory = repositoryRoot.Value,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        )
    args |> List.iter startInfo.ArgumentList.Add
    startInfo.Environment["WSLENV"] <-
        match startInfo.Environment.TryGetValue "WSLENV" with
        | true, existing when not(String.IsNullOrEmpty existing) -> $"{existing}:{becomePasswordVariable}"
        | _ -> becomePasswordVariable

    use proc = new Process(StartInfo = startInfo)
    try
        do
            use password = ctx.Secrets[becomePassword]
            startInfo.Environment[becomePasswordVariable] <- Secrets.Materialize password
        proc.Start() |> ignore
    finally
        startInfo.Environment.Remove becomePasswordVariable |> ignore
    use _ = ct.Register(fun () -> try proc.Kill true with _ -> ())

    try
        do
            use password = ctx.Secrets[vaultPassword]
            proc.StandardInput.Write(Secrets.Materialize password)
        proc.StandardInput.Close()
    with
    | :? IOException as ex -> ctx.Reporter.Log $"Failed to pass the vault password: {ex.Message}"

    let readLines (reader: StreamReader) = async {
        let lines = ResizeArray()
        let mutable finished = false
        while not finished do
            match! Async.AwaitTask(reader.ReadLineAsync()) with
            | null -> finished <- true
            | line ->
                ctx.Reporter.Log line
                lines.Add line
        return lines.ToArray()
    }
    let! output = Async.Parallel [| readLines proc.StandardOutput; readLines proc.StandardError |]
    do! Async.AwaitTask(proc.WaitForExitAsync ct)

    if proc.ExitCode <> 0 then failwith $"{exe} exited with code {proc.ExitCode}."
    return output[0]
}

/// A host line of the play recap, e.g. "facility13 : ok=12 changed=1 unreachable=0 failed=0 …".
let private recapLine = Regex @"^\S+\s+:\s+ok=\d+\s+changed=(\d+)\s"

/// Converts the play recap from the output to the change: no changes if no host has changed.
let private toChange(output: string[]) =
    let recap =
        output
        |> Array.choose(fun line ->
            let m = recapLine.Match line
            if m.Success then Some(line.Trim(), int m.Groups[1].Value) else None
        )
    if recap |> Array.forall(fun (_, changed) -> changed = 0)
    then NoChanges
    else NamedChange(recap |> Seq.map fst |> String.concat "\n")

let private ansiblePlaybook = {
    PresentableName = $"Ansible: {playbook}"
    DependsOn = Resource.NoDependencies
    Lock = None
    Secrets = ImmutableArray.Create(
        { Secret = becomePassword; Mode = SecretRequestMode.CheckAndApply },
        { Secret = vaultPassword; Mode = SecretRequestMode.CheckAndApply }
    )
    AlreadyApplied = fun ctx -> async {
        ctx.Reporter.Status "Running the playbook in check mode"
        let! output = runAnsible ctx (commonArgs @ [ "--check"; "--diff"; playbook ])
        return toChange output
    }
    Apply = fun ctx -> async {
        ctx.Reporter.Status "Running the playbook"
        let! output = runAnsible ctx (commonArgs @ [ playbook ])
        return toChange output
    }
}

let private resources = [
    ansiblePlaybook
]

[<EntryPoint>]
let main(args: string[]): int =
    Fabricator.Console.EntryPoint.main args resources
