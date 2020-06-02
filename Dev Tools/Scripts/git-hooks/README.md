# Git Hooks for Rock

# About
This folder contains hook scripts for Git that may be useful to Rock developers.

To activate these scripts, follow the Installation instructions in this document.

## Installation
To enable these scripts for your local Git client, ensure that the folder containing the scripts exists in the repository in which you intend to use them.

The default location for these scripts in a Rock repository is:
"./Dev Tools/Scripts/git-hooks"

To enable these scripts for Git using the SmartGit client, do the following:
1. Open SmartGit.
2. Select "Tools/Open Git-Shell" from the main menu.
3. At the command prompt, execute the following command:

**git config core.hooksPath "./Dev Tools/Scripts/git-hooks"**
 
This will set the search path for Git hook scripts to the location of the script files in the active repository.

## Scripts
Git hook scripts are executed if a file matching the hook name exists in the git-hooks folder.

The initial hook is a bash script that can be executed by any Git client, and this is used to execute one or more PowerShell scripts that perform the actual work.

### Hook: commit-msg
This hook is executed by the git client when a local commit is started.

#### Script: commit-msg-verify-rock-message-format.ps1
This is a PowerShell script that validates the pending commit message to ensure it conforms to the Rock requirements for commit messages set out in the Developer Codex.

If the commit message doesn't meet one of the requirements, the commit will fail with a (hopefully!) helpful message on how to fix the problem.



