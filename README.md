<!--
SPDX-FileCopyrightText: 2025 facility13.devops contributors <https://github.com/ForNeVeR/facility13.devops>

SPDX-License-Identifier: MIT
-->

facility13 host
===============

How to Use
----------
1. Copy `hosts.example.ini` to `hosts.ini`, fix the host connection details if needed.
2. Copy `vars/vars.example.yml` to `vars/vars.yml` and adjust it accordingly.
3. Copy `vars/secrets.example.yml` to `vars/secrets.yml` and adjust it accordingly.
4. `ansible encrypt vars/secrets.yml`
5. Run `ansible-playbook --ask-vault-pass default.yml`.

If on Windows, feel free to use scripts `ansible-playbook.ps1` and `ansible-vault.ps1` as substitutes to use Ansible from WSL.

License
-------
The project is distributed under the terms of [the MIT license][docs.license].

The license indication in the project's sources is compliant with the [REUSE specification v3.3][reuse.spec].

[docs.license]: LICENSE.txt
