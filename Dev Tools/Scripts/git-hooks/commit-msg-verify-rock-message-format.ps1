#
# Powershell script to be called from Git hook: commit-msg
# Checks the syntax of a Rock commit message prior to commit.
#
# Last Modified: [DL] 2020-06-01
#
param($commitFile)

$commitText = Get-Content -Path "$commitFile"

$fixesPattern='.*(?i)Fix(es|ed) #([0-9]+)'
$issuePattern='^\+.*\(Fixes #([0-9]*)\)(\.)?$'
$newLinePattern='(\r\n|\r|\n)'
$releaseCommitPattern='\+ \(.*\) (Improved|Updated|Added|Fixed) .*\.'

write-host "Validating Rock commit message format..."

# Check for correct prefix format.
$isDailyCommit = $commitText -like '-*'
$isReleaseCommit = $commitText -like '+*'

if ($isDailyCommit -eq $false -and $isReleaseCommit -eq $false)
{
  write-host "Invalid Commit Message Format. Valid formats are:"
  write-host "- {Daily Commit Description}"
  write-host "+ [{Module}] {Release Commit Description}."
  exit 1
}

$isIssueFix = $commitText -match $fixesPattern

# Verify Message Format for Release Commit.
if ($isReleaseCommit -eq $true -and $isIssueFix -eq $false )
{
	# Check that the message is confined to a single line - required for processing by the "readme" generator.
	$hasNewLines = $commitText -match $newLinePattern
	if ($hasNewLines -eq $true)
	{
	  write-host "Invalid Commit Message Format. A Release Commit message should not contain any newline characters."
	  exit 1
	}

	# Check format and required keywords.
	$verifiedReleaseFormat = $commitText -match $releaseCommitPattern
	if ($verifiedReleaseFormat -eq $false)
	{
	  write-host "Invalid Commit Message Format. Valid Release Commit message format is:"
	  write-host "+ ({Module}) {Improved|Updated|Added|Fixed} {Release Commit Description}."
	  exit 1
	}
}

# Verify Message Format for Issue Fix.
if ($isIssueFix -eq $true)
{
	$hasCorrectIssueFormat = $commitText -match $issuePattern
	if ($hasCorrectIssueFormat -eq $false)
	{  
	  write-host "Invalid Commit Message Format. Valid Issue Fix format is:"
	  write-host "+ {Change Commit Message} (Fixes #{IssueNumber})"
	  exit 1
	}
}

exit 0
