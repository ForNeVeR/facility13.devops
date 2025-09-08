# SPDX-FileCopyrightText: 2025 facility13.devops contributors <https://github.com/ForNeVeR/facility13.devops>
#
# SPDX-License-Identifier: MIT

wsl --distribution Ubuntu ansible-playbook --inventory hosts.ini @args -e 'ansible_ssh_pipelining=True'
