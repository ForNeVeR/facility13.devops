wsl --distribution Ubuntu-24.04 ansible-playbook --inventory hosts.ini @args -e 'ansible_ssh_pipelining=True'
