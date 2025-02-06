<!--
SPDX-FileCopyrightText: 2025 facility13.devops contributors <https://github.com/ForNeVeR/facility13.devops>

SPDX-License-Identifier: MIT
-->

Contributor Guide
=================
<!-- REUSE-IgnoreStart -->

License Automation
------------------
If the CI asks you to update the file licenses, follow one of these:
1. Update the headers manually (look at the existing files), something like this:
   ```
   # SPDX-FileCopyrightText: %year% %your name% <%your contact info, e.g. email%>
   #
   # SPDX-License-Identifier: MIT
   ```
   (accommodate to the file's comment style if required).
2. Alternately, use [REUSE][reuse] tool:
   ```console
   $ reuse annotate --license MIT --copyright '%your name% <%your contact info, e.g. email%>' %file names to annotate%
   ```

(Feel free to attribute the changes to "facility13.devops contributors <https://github.com/ForNeVeR/facility13.devops>" instead of your name in a multi-author file, or if you don't want your name to be mentioned in the project's source: this doesn't mean you'll lose the copyright.)

[reuse]: https://reuse.software/

<!-- REUSE-IgnoreEnd -->
