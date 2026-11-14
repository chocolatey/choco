#Requires -PSEdition Core

<#
    Functions behind the Update 7-Zip workflow (.github/workflows/update-7zip.yml). Each one is one
    step of the workflow, takes parameters (defaulting to chocolatey/choco and the latest 7-Zip
    release), and returns an object the workflow turns into step outputs. They can also be run by
    hand:

        Import-Module ./.github/scripts/7zipUpdate.psm1
        Find-7zipUpdateItem -Line 2.x
        Get-Help Set-7zipUpdateIssue -Full

    Every function that changes something supports -WhatIf. Most need the GitHub CLI (gh), signed in
    or with GH_TOKEN set; Get-7zipLatestVersion does not.
#>

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function Get-7zipLatestVersion {
    <#
    .SYNOPSIS
        Gets the version of the latest 7-Zip release on GitHub.

    .DESCRIPTION
        Reads the latest release of the ip7z/7zip repository through the GitHub API, and returns its
        tag, for example 26.06. When the GH_TOKEN or GITHUB_TOKEN environment variable is set, the
        token is sent with the request to avoid the lower rate limit for anonymous requests. Needs
        no GitHub CLI, so Update-7zipVersion.ps1 can use it on its own.

    .EXAMPLE
        Get-7zipLatestVersion
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param()

    $headers = @{}
    $token = if ($env:GH_TOKEN) { $env:GH_TOKEN } else { $env:GITHUB_TOKEN }

    if ($token) {
        $headers.Authorization = "Bearer $token"
    }

    $latestRelease = Invoke-RestMethod -Uri 'https://api.github.com/repos/ip7z/7zip/releases/latest' -Headers $headers
    $latestRelease.tag_name
}

function Get-7zipReleaseNotes {
    <#
    .SYNOPSIS
        Gets the release notes of a 7-Zip release on GitHub.

    .DESCRIPTION
        Reads the description of the ip7z/7zip release for the version, and returns its lines without
        the "### 7-Zip <version>" heading it starts with. Returns nothing, with a warning, when the
        release cannot be read, so a pull request is still created without them.

    .PARAMETER Version
        The 7-Zip version, for example 26.06.

    .EXAMPLE
        Get-7zipReleaseNotes -Version 26.06
    #>
    [CmdletBinding()]
    [OutputType([string[]])]
    param(
        [Parameter(Mandatory)]
        [string] $Version
    )

    try {
        $body = gh api "repos/ip7z/7zip/releases/tags/$Version" --jq '.body'
    } catch {
        Write-Warning "Unable to read the release notes of 7-Zip $Version, so they are left out: $($_.Exception.Message)"
        return
    }

    @($body -split "`r?`n" | Where-Object { $_ -notmatch '^#+\s*7-Zip\b' } | ForEach-Object { $_.TrimEnd() }) |
        Where-Object { $_ }
}

function Get-7zipBundledVersion {
    <#
    .SYNOPSIS
        Gets the 7-Zip version bundled on a branch of a GitHub repository.

    .DESCRIPTION
        Reads tests/pester-tests/BundledApplications.Tests.ps1 from the branch through the GitHub API,
        without a checkout, and returns the version of its 7z entry.

    .PARAMETER Repository
        The repository, as owner/name.

    .PARAMETER Branch
        The branch, tag or commit to read the file from.

    .EXAMPLE
        Get-7zipBundledVersion -Branch release/2.8.0
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [string] $Repository = 'chocolatey/choco',

        [Parameter(Mandatory)]
        [string] $Branch
    )

    $testFile = (gh api --header 'Accept: application/vnd.github.raw' "repos/$Repository/contents/tests/pester-tests/BundledApplications.Tests.ps1?ref=$Branch") -join "`n"

    if ($testFile -notmatch "Name\s*=\s*'7z'\s*;\s*Version\s*=\s*'(?<bundled>[^']*)'") {
        throw "Unable to find the 7-Zip version in BundledApplications.Tests.ps1 on the '$Branch' branch."
    }

    $Matches.bundled
}

function Get-7zipUpdateStatus {
    <#
    .SYNOPSIS
        Checks whether a branch bundles the latest 7-Zip release.

    .DESCRIPTION
        Compares the latest 7-Zip release on GitHub with the 7-Zip version bundled on the branch, and
        returns both versions and whether an update is needed. Refuses the 1.x branches (support/1.x,
        hotfix/1* and release/1*), because the 7-Zip update workflow only runs from 2.x related
        branches.

    .PARAMETER Repository
        The repository, as owner/name.

    .PARAMETER Branch
        The branch to check. The workflow passes the branch it runs on.

    .EXAMPLE
        Get-7zipUpdateStatus -Branch develop
    #>
    [CmdletBinding()]
    param(
        [string] $Repository = 'chocolatey/choco',

        [string] $Branch = 'develop'
    )

    if ($Branch -match '^(support/1\.x$|hotfix/1|release/1)') {
        throw "The workflow cannot run from the '$Branch' branch. Run it from develop or another 2.x branch instead."
    }

    $latestVersion = Get-7zipLatestVersion
    Write-Host "The latest 7-Zip release on GitHub is Version $latestVersion."

    $bundledVersion = Get-7zipBundledVersion -Repository $Repository -Branch $Branch
    Write-Host "The '$Branch' branch bundles 7-Zip Version $bundledVersion."

    $updateNeeded = [version] $latestVersion -gt [version] $bundledVersion

    if ($updateNeeded) {
        Write-Host "7-Zip Version $latestVersion is newer, so the bundled 7-Zip needs updating."
    } else {
        Write-Host "The bundled 7-Zip is up to date."
    }

    [pscustomobject] @{
        Version        = $latestVersion
        BundledVersion = $bundledVersion
        UpdateNeeded   = $updateNeeded
    }
}

function Get-7zipTargetBranch {
    <#
    .SYNOPSIS
        Finds the branch a product line's 7-Zip update should target.

    .DESCRIPTION
        Picks the line's target branch: the highest hotfix/<major>.* branch, otherwise the highest
        release/<major>.* branch, otherwise the fallback branch. Only names that are exactly a version
        after the prefix count, so hotfix/2.8.0-beta or release/2.x are ignored. Then reads the 7-Zip
        version bundled on the target branch, and returns the branch, that version, and whether it is
        up to date.

    .PARAMETER Repository
        The repository, as owner/name.

    .PARAMETER Line
        The product line, 1.x or 2.x.

    .PARAMETER FallbackBranch
        The branch to use when there is no hotfix or release branch for the line. Defaults to
        support/1.x for 1.x and develop for 2.x. The workflow passes the branch it runs on for 2.x.

    .PARAMETER Version
        The 7-Zip version to compare with. Defaults to the latest release on GitHub.

    .EXAMPLE
        Get-7zipTargetBranch -Line 1.x
    #>
    [CmdletBinding()]
    param(
        [string] $Repository = 'chocolatey/choco',

        [Parameter(Mandatory)]
        [ValidateSet('1.x', '2.x')]
        [string] $Line,

        [string] $FallbackBranch,

        [string] $Version
    )

    $major = $Line.Split('.')[0]

    if (-not $FallbackBranch) {
        $FallbackBranch = if ($Line -eq '1.x') { 'support/1.x' } else { 'develop' }
    }

    if (-not $Version) {
        $Version = Get-7zipLatestVersion
    }

    $branches = gh api --paginate "repos/$Repository/branches" --jq '.[].name'
    $targetBranch = $null

    foreach ($prefix in 'hotfix', 'release') {
        $candidates = @($branches | Where-Object { $_ -match "^$prefix/$major(\.\d+){1,3}$" })

        if ($candidates.Count -gt 0) {
            $targetBranch = $candidates | Sort-Object { [version] ($_ -replace '^[^/]+/') } -Descending | Select-Object -First 1
            break
        }
    }

    if (-not $targetBranch) {
        if ($branches -notcontains $FallbackBranch) {
            throw "There is no hotfix or release branch for $Line, and the fallback branch '$FallbackBranch' does not exist."
        }

        $targetBranch = $FallbackBranch
    }

    Write-Host "The target branch is '$targetBranch'."

    $bundledVersion = Get-7zipBundledVersion -Repository $Repository -Branch $targetBranch
    Write-Host "The '$targetBranch' branch bundles 7-Zip Version $bundledVersion."

    $upToDate = [version] $bundledVersion -ge [version] $Version

    if ($upToDate) {
        Write-Host "The '$targetBranch' branch is up to date, so nothing more is needed for this line."
    } else {
        Write-Host "The '$targetBranch' branch needs 7-Zip Version $Version."
    }

    [pscustomobject] @{
        Line           = $Line
        Branch         = $targetBranch
        BundledVersion = $bundledVersion
        UpToDate       = $upToDate
    }
}

function Find-7zipUpdateItem {
    <#
    .SYNOPSIS
        Finds the 7-Zip update issue and pull request for a product line.

    .DESCRIPTION
        Looks through the open issues and open pull requests for the hidden marker the 7-Zip update
        workflow puts in their descriptions, for example <!-- 7zip-update line=2.x version=26.06 -->.
        Returns, for the issue and the pull request separately, the number, the version it names, and
        what needs doing: create when none is open, none when it names the version, update when it
        names another version. Fails when the line has more than one marked open issue, or more than
        one marked open pull request.

        When there is no open issue, a closed issue with the marker still counts until it has the
        5 - Released label (it can be closed before its release ships): naming the version it needs
        nothing, naming another version it is reopened and updated. IssueState says which (open or
        closed).

        When a pull request needs creating, it also returns the latest marked pull request from the
        line's work branch that was closed without being merged, so the new one can link to it.

        With -TargetBranch, an open issue or pull request that names the latest version but another
        branch (the issue's "bundles 7-Zip ... on the `<branch>` branch" sentence, or the pull
        request's base) gives retarget: a release or hotfix branch appeared after it was created. A
        closed issue is finished work, so it stays none.

    .PARAMETER Repository
        The repository, as owner/name.

    .PARAMETER Line
        The product line, 1.x or 2.x.

    .PARAMETER Version
        The 7-Zip version the issue and pull request should name. Defaults to the latest release on
        GitHub.

    .PARAMETER TargetBranch
        The branch the update targets now, from Get-7zipTargetBranch. Leave it out to skip the
        retarget check.

    .EXAMPLE
        Find-7zipUpdateItem -Line 2.x -TargetBranch release/2.8.0
    #>
    [CmdletBinding()]
    param(
        [string] $Repository = 'chocolatey/choco',

        [Parameter(Mandatory)]
        [ValidateSet('1.x', '2.x')]
        [string] $Line,

        [string] $Version,

        [string] $TargetBranch
    )

    if (-not $Version) {
        $Version = Get-7zipLatestVersion
    }

    $markerPattern = "<!--\s*7zip-update\s+line=$([regex]::Escape($Line))\s+version=(?<version>[\d.]+)\s*-->"

    # The branch an issue's description names, in its "bundles 7-Zip ... on the `<branch>` branch"
    # sentence. A pull request's branch is its base instead.
    function Get-IssueBranch {
        param(
            [string] $Body
        )

        if ($Body -match 'bundles 7-Zip [\d.]+ on the `(?<branch>[^`]+)` branch') {
            return $Matches.branch
        }
    }

    function Find-MarkedItem {
        param(
            [string] $Kind,
            [object[]] $Items
        )

        $found = @($Items | Where-Object { $_.body -match $markerPattern })

        if ($found.Count -gt 1) {
            throw "Found $($found.Count) open $($Kind)s for $Line (#$($found.number -join ', #')). Close all but one, then run the workflow again."
        }

        if ($found.Count -eq 0) {
            Write-Host "There is no open $Kind for $Line."
            return $null
        }

        $null = $found[0].body -match $markerPattern
        $itemVersion = $Matches.version
        $item = [pscustomobject] @{
            Number  = $found[0].number
            Version = $itemVersion
            Base    = if ($found[0].base) { $found[0].base } else { Get-IssueBranch -Body $found[0].body }
        }

        Write-Host "Found the open $Kind #$($item.Number) for $Line, naming 7-Zip Version $($item.Version)$(if ($item.Base) { " on '$($item.Base)'" })."
        return $item
    }

    function Get-Action {
        param(
            [object] $Item
        )

        if (-not $Item) {
            return 'create'
        }

        if ([version] $Item.Version -ne [version] $Version) {
            return 'update'
        }

        if ($TargetBranch -and $Item.Base -and $Item.Base -ne $TargetBranch) {
            return 'retarget'
        }

        return 'none'
    }

    $issues = gh api --paginate "repos/$Repository/issues?state=open&per_page=100" --jq '.[] | select(.pull_request == null) | {number, body}' | ForEach-Object { $_ | ConvertFrom-Json }
    $pullRequests = gh api --paginate "repos/$Repository/pulls?state=open&per_page=100" --jq '.[] | {number, body, base: .base.ref}' | ForEach-Object { $_ | ConvertFrom-Json }

    $issue = Find-MarkedItem -Kind 'issue' -Items $issues
    $issueState = if ($issue) { 'open' } else { $null }
    $pullRequest = Find-MarkedItem -Kind 'pull request' -Items $pullRequests

    # An issue can be closed (fixed) before its release ships. Until it has the 5 - Released label it
    # still counts: one naming the latest version is done, one naming an older version is reopened
    # and moved to the new version. Search, because the repository has thousands of closed issues.
    if (-not $issue) {
        $closedIssues = gh api -X GET search/issues -f q="repo:$Repository is:issue is:closed -label:`"5 - Released`" `"7-Zip`" in:title" -f sort=created -f order=desc --jq '.items[] | {number, body}' | ForEach-Object { $_ | ConvertFrom-Json }
        $closedIssue = $closedIssues | Where-Object { $_.body -match $markerPattern } | Select-Object -First 1

        if ($closedIssue) {
            $null = $closedIssue.body -match $markerPattern
            $issue = [pscustomobject] @{
                Number  = $closedIssue.number
                Version = $Matches.version
                Base    = Get-IssueBranch -Body $closedIssue.body
            }
            $issueState = 'closed'
            Write-Host "Found the closed, unreleased issue #$($issue.Number) for $Line, naming 7-Zip Version $($issue.Version)."
        }
    }

    $issueAction = Get-Action -Item $issue
    $pullRequestAction = Get-Action -Item $pullRequest

    # A closed issue on the latest version is finished work, even when the target has moved since
    if ($issueAction -eq 'retarget' -and $issueState -eq 'closed') {
        $issueAction = 'none'
    }

    $nothingToDo = $issueAction -eq 'none' -and $pullRequestAction -eq 'none'

    # A closed pull request cannot be reused, so a new one is opened. Find the latest one that was
    # closed without being merged, so the new pull request can link to it. Search, rather than
    # listing every closed pull request, because the repository has thousands of them.
    $closedPullRequestNumber = $null

    if ($pullRequestAction -eq 'create') {
        $closed = gh api -X GET search/issues -f q="repo:$Repository is:pr is:closed is:unmerged head:update-7zip-$Line" -f sort=created -f order=desc --jq '.items[] | {number, body}' | ForEach-Object { $_ | ConvertFrom-Json }
        $closedPullRequestNumber = @($closed | Where-Object { $_.body -match $markerPattern } | Select-Object -First 1).number

        if ($closedPullRequestNumber) {
            Write-Host "The latest pull request for $Line, #$closedPullRequestNumber, was closed without being merged; the new one links to it."
        }
    }

    if ($nothingToDo) {
        Write-Host "The open issue and pull request already name 7-Zip Version $Version, so nothing more is needed for $Line."
    } else {
        Write-Host "Issue: $issueAction. Pull request: $pullRequestAction."
    }

    [pscustomobject] @{
        Line                    = $Line
        IssueNumber             = $issue.Number
        IssueVersion            = $issue.Version
        IssueBranch             = $issue.Base
        IssueAction             = $issueAction
        IssueState              = $issueState
        PullRequestNumber       = $pullRequest.Number
        PullRequestVersion      = $pullRequest.Version
        PullRequestBase         = $pullRequest.Base
        PullRequestAction       = $pullRequestAction
        ClosedPullRequestNumber = $closedPullRequestNumber
        NothingToDo             = $nothingToDo
    }
}

function Set-7zipUpdateIssueStatus {
    <#
    .SYNOPSIS
        Sets the status label, assignee, label and milestone of a 7-Zip update issue.

    .DESCRIPTION
        With -Status, switches the issue to one status label: 2 - Working while the update is being
        prepared, or 3 - Review once its pull request is open. Any other numbered status label (for
        example 0 - Backlog or 3 - Review) is removed, so the issue only ever has one. Without
        -Status, the status labels are left alone.

        It always assigns the issue to the account behind the GitHub token, through the REST API (gh
        issue and pr edits can need the read:org scope on a classic token), keeping other assignees,
        and adds the Dependency Change label when it is missing.

        With -TargetBranch and -Line, it also keeps the milestone in step with the target branch: the
        version for a hotfix or release branch (for example 2.8.0 for release/2.8.0), otherwise the
        line (1.x or 2.x). It sets that milestone when the issue has none, and moves a line milestone
        to the version milestone once a hotfix or release branch is the target. Any other milestone,
        such as one a maintainer picked, is left alone.

        A label or milestone that does not exist in the repository is skipped with a warning. Nothing
        is changed when the issue already matches.

    .PARAMETER Repository
        The repository, as owner/name.

    .PARAMETER IssueNumber
        The issue to update.

    .PARAMETER Status
        Working (2 - Working) or Review (3 - Review). Leave it out to keep the current status.

    .PARAMETER TargetBranch
        The branch the update targets, for the milestone. Needs -Line.

    .PARAMETER Line
        The product line, 1.x or 2.x, for the milestone. Needs -TargetBranch.

    .EXAMPLE
        Set-7zipUpdateIssueStatus -IssueNumber 3976 -Status Review -TargetBranch hotfix/1.4.9 -Line 1.x -WhatIf
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string] $Repository = 'chocolatey/choco',

        [Parameter(Mandatory)]
        [int] $IssueNumber,

        [ValidateSet('Working', 'Review')]
        [string] $Status,

        [string] $TargetBranch,

        [ValidateSet('1.x', '2.x')]
        [string] $Line
    )

    $current = gh issue view $IssueNumber --repo $Repository --json labels,assignees,milestone | ConvertFrom-Json
    $currentLabels = @($current.labels.name)
    $currentMilestone = $current.milestone.title
    $login = gh api user --jq '.login'

    if (@($current.assignees.login) -notcontains $login) {
        if ($PSCmdlet.ShouldProcess("$Repository#$IssueNumber", "Assign the issue to $login")) {
            @{ assignees = @($login) } | ConvertTo-Json -Compress | gh api --method POST "repos/$Repository/issues/$IssueNumber/assignees" --input - | Out-Null
            Write-Host "Assigned issue #$IssueNumber to $login."
        }
    }

    $repositoryLabels = gh api --paginate "repos/$Repository/labels?per_page=100" --jq '.[].name'
    $toAdd = @()
    $toRemove = @()
    $milestone = $null

    # Returns the label when the issue is missing it and the repository has it
    function Get-MissingLabel {
        param(
            [string] $Name
        )

        if ($currentLabels -contains $Name) {
            return
        }

        if ($repositoryLabels -contains $Name) {
            return $Name
        }

        Write-Warning "The '$Name' label does not exist in $Repository, so it is not added to issue #$IssueNumber."
    }

    if ($Status) {
        $statusLabel = @{ Working = '2 - Working'; Review = '3 - Review' }[$Status]
        $toRemove = @($currentLabels | Where-Object { $_ -match '^\d+ - ' -and $_ -ne $statusLabel })
        $toAdd += @(Get-MissingLabel -Name $statusLabel)
    }

    $toAdd += @(Get-MissingLabel -Name 'Dependency Change')

    if ($TargetBranch -and $Line) {
        $wantedMilestone = if ($TargetBranch -match '^(hotfix|release)/(?<version>\d+(\.\d+)+)$') { $Matches.version } else { $Line }

        # Fill in a missing milestone, or move the line milestone to a version milestone. Never
        # replace any other milestone, so one a maintainer picked stays.
        $replaceable = -not $currentMilestone -or ($currentMilestone -eq $Line -and $wantedMilestone -ne $Line)

        if ($replaceable -and $wantedMilestone -ne $currentMilestone) {
            $milestones = gh api --paginate "repos/$Repository/milestones?state=open&per_page=100" --jq '.[].title'

            if ($milestones -contains $wantedMilestone) {
                $milestone = $wantedMilestone
            } else {
                Write-Warning "The '$wantedMilestone' milestone does not exist in $Repository, so it is not set on issue #$IssueNumber."
            }
        }
    }

    if ($toAdd.Count -eq 0 -and $toRemove.Count -eq 0 -and -not $milestone) {
        Write-Host "Issue #$IssueNumber already has the right labels and milestone."
        return
    }

    $arguments = @($IssueNumber, '--repo', $Repository)

    foreach ($name in $toRemove) {
        $arguments += '--remove-label', $name
    }

    foreach ($name in $toAdd) {
        $arguments += '--add-label', $name
    }

    if ($milestone) {
        $arguments += '--milestone', $milestone
    }

    $change = @(
        "adds '$(if ($toAdd) { $toAdd -join "', '" } else { '(none)' })'"
        "removes '$(if ($toRemove) { $toRemove -join "', '" } else { '(none)' })'"
        "milestone '$(if ($milestone) { $milestone } else { '(unchanged)' })'"
    ) -join ', '

    if ($PSCmdlet.ShouldProcess("$Repository#$IssueNumber", "Update the issue: $change")) {
        gh issue edit @arguments | Out-Null
        Write-Host "Updated issue #$($IssueNumber): $change."
    }
}

function Set-7zipUpdateIssue {
    <#
    .SYNOPSIS
        Creates or updates the 7-Zip update issue for a product line.

    .DESCRIPTION
        Without -IssueNumber, creates the line's issue, with the Dependency Change label and the
        milestone that matches the target branch: the version for a hotfix or release branch (for
        example 2.8.0 for release/2.8.0), otherwise the line (1.x or 2.x). A label or milestone that
        does not exist in the repository is skipped with a warning.

        With -IssueNumber, moves that issue to the new version: a new title, the previous version
        replaced by the new one in its description (except in the problem section, which describes
        the situation when the issue was opened), the marker moved to the new version, and a comment
        saying which version it moved from. The rest of the description, including any edits, is
        kept. A closed issue (one fixed but not released yet) is reopened first.

        When the target branch has moved (a release or hotfix branch appeared), the problem section's
        "bundles 7-Zip ... on the `<branch>` branch" sentence gets the new branch name (its version
        stays as it was), and the comment says so. This works with the same version too: pass the
        current version as both -Version and -PreviousVersion.

        Either way, Set-7zipUpdateIssueStatus then sets the issue's status label (2 - Working by
        default), assigns it to the account behind the GitHub token, adds a missing Dependency Change
        label, and keeps the milestone in step with the target branch.

        The description carries the hidden marker Find-7zipUpdateItem looks for.

    .PARAMETER Repository
        The repository, as owner/name.

    .PARAMETER Line
        The product line, 1.x or 2.x.

    .PARAMETER TargetBranch
        The branch the update targets, from Get-7zipTargetBranch.

    .PARAMETER BundledVersion
        The 7-Zip version bundled on the target branch today.

    .PARAMETER Version
        The 7-Zip version to update to. Defaults to the latest release on GitHub.

    .PARAMETER IssueNumber
        The issue to update: an open one, or a closed one without the 5 - Released label, which is
        reopened. Leave it out to create a new issue.

    .PARAMETER PreviousVersion
        The version the issue names today. Needed with -IssueNumber: it is the version replaced in
        the description, and named in the comment. The same as -Version when only the target branch
        moved.

    .PARAMETER Status
        The status label to set: Working (2 - Working, the default) while the pull request still
        needs creating or updating, or Review (3 - Review) when it is already open for this version.

    .EXAMPLE
        Set-7zipUpdateIssue -Line 2.x -TargetBranch develop -BundledVersion 26.05 -WhatIf
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string] $Repository = 'chocolatey/choco',

        [Parameter(Mandatory)]
        [ValidateSet('1.x', '2.x')]
        [string] $Line,

        [Parameter(Mandatory)]
        [string] $TargetBranch,

        [Parameter(Mandatory)]
        [string] $BundledVersion,

        [string] $Version,

        [int] $IssueNumber,

        [string] $PreviousVersion,

        [ValidateSet('Working', 'Review')]
        [string] $Status = 'Working'
    )

    if (-not $Version) {
        $Version = Get-7zipLatestVersion
    }

    $title = "Update the bundled version of 7-Zip to $Version"

    $creditsCriteria = if ($Line -eq '1.x') {
        '- [ ] `docs/legal/CREDITS.md` lists the new version and its license text.'
    } else {
        '- [ ] `docs/legal/CREDITS.json` lists the new version, and `CREDITS.md` and `CREDITS.pdf` are regenerated with the Credits Generator.'
    }

    $body = @(
        '### Is Your Feature Request Related To A Problem? Please describe.'
        ''
        "Chocolatey CLI $Line bundles 7-Zip $BundledVersion on the ``$TargetBranch`` branch."
        ''
        '### Describe The Solution. Why is it needed?'
        ''
        "Update the bundled 7-Zip to [$Version](https://github.com/ip7z/7zip/releases/tag/$Version), the latest 7-Zip release, so Chocolatey CLI includes its fixes and improvements."
        ''
        '### User Story'
        ''
        '_No response_'
        ''
        '### Additional Context'
        ''
        'The scheduled Update 7-Zip workflow opened this issue, and opens the pull request with the update.'
        ''
        '### Acceptance Criteria'
        ''
        "- [ ] The bundled 7-Zip (``7z.exe`` and ``7z.dll``) is updated to $Version."
        $creditsCriteria
        "- [ ] ``tests/pester-tests/BundledApplications.Tests.ps1`` expects 7-Zip $Version."
        ''
        '### Related Issues'
        ''
        '_No response_'
        ''
        "<!-- 7zip-update line=$Line version=$Version -->"
    ) -join "`n"

    if ($IssueNumber) {
        if (-not $PreviousVersion) {
            throw '-PreviousVersion is needed with -IssueNumber, to know which version to replace in the issue description.'
        }

        $current = gh issue view $IssueNumber --repo $Repository --json body,state | ConvertFrom-Json
        $versionChanged = [version] $PreviousVersion -ne [version] $Version

        # A closed issue that is not released yet is reopened, so the newer version is tracked on it.
        # Only for a new version: a closed issue on the latest version is finished work.
        $reopen = $current.state -eq 'CLOSED' -and $versionChanged

        # The problem section keeps its wording, except the branch name in the sentence naming it:
        # when the target has moved (a release or hotfix branch appeared), only the branch name
        # changes, and the version it names stays as it was.
        $branchSentence = '(?<before>Chocolatey CLI \S+ bundles 7-Zip [\d.]+ on the `)(?<branch>[^`]+)(?<after>` branch\.)'
        $previousBranch = if ($current.body -match $branchSentence) { $Matches.branch } else { $null }
        $retargeted = $previousBranch -and $previousBranch -ne $TargetBranch

        $commentParts = @()

        if ($versionChanged) {
            $commentParts += if ($reopen) {
                "7-Zip $Version was released, so this issue is reopened and now covers $Version instead of $PreviousVersion."
            } else {
                "7-Zip $Version was released, so this issue now covers $Version instead of $PreviousVersion."
            }
        }

        if ($retargeted) {
            $commentParts += "The update now targets the ``$TargetBranch`` branch instead of ``$previousBranch``."
        }

        if ($commentParts.Count -eq 0) {
            Write-Host "Issue #$IssueNumber already names 7-Zip Version $Version and the '$TargetBranch' branch."
            Set-7zipUpdateIssueStatus -Repository $Repository -IssueNumber $IssueNumber -Status $Status -TargetBranch $TargetBranch -Line $Line -WhatIf:$WhatIfPreference

            return [pscustomobject] @{
                Number = $IssueNumber
                Action = 'none'
            }
        }

        $comment = $commentParts -join ' '

        # Keep the description as it is, so the problem statement and any edits stay. Only the
        # previous version is replaced, outside the problem section, and the marker moves to the
        # new version.
        $problemHeading = '### Is Your Feature Request Related To A Problem?'
        $previousVersionPattern = "(?<!\d|\d\.)$([regex]::Escape($PreviousVersion))(?!\d|\.\d)"
        $sections = [regex]::Split($current.body, '(?m)^(?=### )')

        $body = ($sections | ForEach-Object {
            if ($_.StartsWith($problemHeading)) {
                $_
            } else {
                $_ -replace $previousVersionPattern, $Version
            }
        }) -join ''

        $body = $body -replace '<!--\s*7zip-update\s+line=\S+\s+version=[\d.]+\s*-->', "<!-- 7zip-update line=$Line version=$Version -->"

        if ($retargeted) {
            $body = [regex]::Replace($body, $branchSentence, { param($match) $match.Groups['before'].Value + $TargetBranch + $match.Groups['after'].Value })
        }

        $arguments = @($IssueNumber, '--repo', $Repository, '--title', $title, '--body-file', '-')

        if ($PSCmdlet.ShouldProcess("$Repository#$IssueNumber", "$(if ($reopen) { 'Reopen and update' } else { 'Update' }) the issue to 7-Zip $Version")) {
            if ($reopen) {
                @{ state = 'open' } | ConvertTo-Json -Compress | gh api --method PATCH "repos/$Repository/issues/$IssueNumber" --input - | Out-Null
                Write-Host "Reopened issue #$IssueNumber."
            }

            $body | gh issue edit @arguments | Out-Null
            gh issue comment $IssueNumber --repo $Repository --body $comment | Out-Null
            Write-Host "Updated issue #$IssueNumber to 7-Zip Version $Version."
        } else {
            Write-Host "Reopen: $reopen`nTitle: $title`n`n$body`n`nComment: $comment"
        }

        # Status, assignee, Dependency Change label and milestone
        Set-7zipUpdateIssueStatus -Repository $Repository -IssueNumber $IssueNumber -Status $Status -TargetBranch $TargetBranch -Line $Line -WhatIf:$WhatIfPreference

        return [pscustomobject] @{
            Number = $IssueNumber
            Action = 'updated'
        }
    }

    $label = 'Dependency Change'
    $milestone = if ($TargetBranch -match '^(hotfix|release)/(?<version>\d+(\.\d+)+)$') { $Matches.version } else { $Line }

    $labels = gh api --paginate "repos/$Repository/labels?per_page=100" --jq '.[].name'
    $milestones = gh api --paginate "repos/$Repository/milestones?state=open&per_page=100" --jq '.[].title'

    if ($labels -notcontains $label) {
        Write-Warning "The '$label' label does not exist in $Repository, so the issue is created without it."
        $label = $null
    }

    if ($milestones -notcontains $milestone) {
        Write-Warning "The '$milestone' milestone does not exist in $Repository, so the issue is created without one."
        $milestone = $null
    }

    $arguments = @('--repo', $Repository, '--title', $title, '--body-file', '-')

    if ($label) {
        $arguments += '--label', $label
    }

    if ($milestone) {
        $arguments += '--milestone', $milestone
    }

    if (-not $PSCmdlet.ShouldProcess($Repository, "Create the $Line issue for 7-Zip $Version")) {
        Write-Host "Title: $title`nLabel: $(if ($label) { $label } else { '(none)' })`nMilestone: $(if ($milestone) { $milestone } else { '(none)' })`nAssignee: the token's account`nStatus: $Status`n`n$body"

        return [pscustomobject] @{
            Number = $null
            Action = 'created'
        }
    }

    $url = $body | gh issue create @arguments
    $number = [int] ($url -split '/')[-1]
    Write-Host "Created issue #$number for 7-Zip Version $Version ($url)."

    Set-7zipUpdateIssueStatus -Repository $Repository -IssueNumber $number -Status $Status -TargetBranch $TargetBranch -Line $Line

    [pscustomobject] @{
        Number = $number
        Action = 'created'
    }
}

function Update-7zipWorkBranch {
    <#
    .SYNOPSIS
        Prepares a product line's 7-Zip update on its work branch.

    .DESCRIPTION
        In a checkout of the target branch, creates the work branch update-7zip-<line> from the
        current commit (resetting it when it already exists), runs that branch's own
        Update-7zipVersion.ps1 with the version, and stages every change. Returns the work branch,
        whether anything changed, and the changed files. Nothing is committed or pushed.

        Refuses a checkout with uncommitted changes, so nothing is lost. Needs 7-Zip (7z) on the
        PATH, like Update-7zipVersion.ps1 does.

    .PARAMETER Path
        The checkout of the target branch.

    .PARAMETER Line
        The product line, 1.x or 2.x. Names the work branch.

    .PARAMETER Version
        The 7-Zip version to update to. Defaults to the latest release on GitHub. It is always passed
        to Update-7zipVersion.ps1 explicitly, because older copies of that script have no latest
        lookup.

    .EXAMPLE
        Update-7zipWorkBranch -Path ../choco-support -Line 1.x -WhatIf
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string] $Path = '.',

        [Parameter(Mandatory)]
        [ValidateSet('1.x', '2.x')]
        [string] $Line,

        [string] $Version
    )

    if (-not $Version) {
        $Version = Get-7zipLatestVersion
    }

    $workBranch = "update-7zip-$Line"
    $Path = (Resolve-Path -LiteralPath $Path).Path

    if (-not (Test-Path -LiteralPath (Join-Path $Path 'Update-7zipVersion.ps1'))) {
        throw "There is no Update-7zipVersion.ps1 in '$Path'."
    }

    Push-Location -LiteralPath $Path

    try {
        if (git status --porcelain) {
            throw "The checkout in '$Path' has uncommitted changes. Commit or remove them first."
        }

        $baseCommit = git rev-parse HEAD
        $baseBranch = git branch --show-current

        if (-not $PSCmdlet.ShouldProcess($Path, "Reset the '$workBranch' branch to '$baseBranch' and run Update-7zipVersion.ps1 -Version $Version")) {
            return [pscustomobject] @{
                WorkBranch   = $workBranch
                BaseCommit   = $baseCommit
                Changed      = $false
                ChangedFiles = @()
            }
        }

        git switch --quiet --force-create $workBranch
        Write-Host "The '$workBranch' branch now starts at '$baseBranch' ($($baseCommit.Substring(0, 8)))."

        # Its output (including 7z's) goes to the log, so only the result object is returned
        & ./Update-7zipVersion.ps1 -Version $Version | Out-Host

        git add --all
        $changedFiles = @(git diff --cached --name-only)

        if ($changedFiles.Count -eq 0) {
            Write-Host "Running Update-7zipVersion.ps1 changed no files."
        } else {
            Write-Host "Changed files:`n$(($changedFiles | ForEach-Object { "  $_" }) -join "`n")"
        }

        [pscustomobject] @{
            WorkBranch   = $workBranch
            BaseCommit   = $baseCommit
            Changed      = $changedFiles.Count -gt 0
            ChangedFiles = $changedFiles
        }
    } finally {
        Pop-Location
    }
}

function New-7zipUpdateCommit {
    <#
    .SYNOPSIS
        Commits the staged 7-Zip update on the work branch.

    .DESCRIPTION
        Commits what Update-7zipWorkBranch staged, with a message in the house format: the subject
        (#<issue>) Update 7-Zip to Version <version>, a description, a Key Changes list of the
        changed files, and a Reason of the change list.

        The commit is authored by the account behind the GitHub token (GH_TOKEN, or the GitHub CLI's
        own login), with that account's GitHub no-reply address, so it matches the account that
        pushes it. Nothing is pushed.

    .PARAMETER Path
        The checkout of the work branch, with the update staged.

    .PARAMETER IssueNumber
        The issue the commit belongs to, for the subject prefix.

    .PARAMETER BundledVersion
        The 7-Zip version the target branch bundled before the update.

    .PARAMETER Version
        The 7-Zip version the update moves to. Defaults to the latest release on GitHub.

    .EXAMPLE
        New-7zipUpdateCommit -Path ../choco-support -IssueNumber 3976 -BundledVersion 26.05 -WhatIf
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string] $Path = '.',

        [Parameter(Mandatory)]
        [int] $IssueNumber,

        [Parameter(Mandatory)]
        [string] $BundledVersion,

        [string] $Version
    )

    if (-not $Version) {
        $Version = Get-7zipLatestVersion
    }

    $Path = (Resolve-Path -LiteralPath $Path).Path
    $account = gh api user --jq '{login, id}' | ConvertFrom-Json
    $authorName = $account.login
    $authorEmail = "$($account.id)+$($account.login)@users.noreply.github.com"

    Push-Location -LiteralPath $Path

    try {
        $changedFiles = @(git diff --cached --name-only)

        if ($changedFiles.Count -eq 0) {
            throw "Nothing is staged in '$Path', so there is nothing to commit."
        }

        $message = @(
            "(#$IssueNumber) Update 7-Zip to Version $Version"
            ''
            "Update the bundled 7-Zip from version $BundledVersion to $Version, using"
            'the Update-7zipVersion.ps1 script.'
            ''
            'Key Changes:'
            $changedFiles | ForEach-Object { "- Update $_" }
            ''
            'Reason of the change:'
            "- 7-Zip $Version is the latest 7-Zip release."
        ) -join "`n"

        if (-not $PSCmdlet.ShouldProcess($Path, "Commit the 7-Zip $Version update as $authorName")) {
            Write-Host "Author: $authorName <$authorEmail>`n`n$message"

            return [pscustomobject] @{
                Commit  = $null
                Subject = ($message -split "`n")[0]
                Author  = "$authorName <$authorEmail>"
            }
        }

        $message | git -c "user.name=$authorName" -c "user.email=$authorEmail" commit --quiet --file -
        $commit = git rev-parse HEAD
        Write-Host "Committed $($commit.Substring(0, 8)) as $authorName on '$(git branch --show-current)'."

        [pscustomobject] @{
            Commit  = $commit
            Subject = ($message -split "`n")[0]
            Author  = "$authorName <$authorEmail>"
        }
    } finally {
        Pop-Location
    }
}

function Set-7zipUpdatePullRequest {
    <#
    .SYNOPSIS
        Pushes the 7-Zip update work branch, and creates or updates its pull request.

    .DESCRIPTION
        Force-pushes the checked-out work branch (update-7zip-<line>) to origin, because it is
        rebuilt from the target branch every time. Then, without -PullRequestNumber, creates a pull
        request from it into the target branch. With -PullRequestNumber, moves that pull request to
        the new version: new title and description, its base set to the target branch, and a comment
        saying which version it moved from. The update uses the REST API rather than gh pr edit, so a
        classic token needs no read:org scope.

        Either way, the pull request is then assigned to the account behind the GitHub token, and the
        issue's status label moves to 3 - Review with Set-7zipUpdateIssueStatus.

        The description follows the Chocolatey pull request template, lists the files changed in the
        work branch's last commit, closes the issue with Fixes #<issue>, and carries the hidden marker
        Find-7zipUpdateItem looks for. On 2.x, when CREDITS.json changed, it also asks for CREDITS.md
        and CREDITS.pdf to be regenerated with the Credits Generator.

    .PARAMETER Repository
        The repository, as owner/name.

    .PARAMETER Path
        The checkout of the work branch, with the update committed.

    .PARAMETER Line
        The product line, 1.x or 2.x.

    .PARAMETER TargetBranch
        The branch the pull request merges into, from Get-7zipTargetBranch.

    .PARAMETER IssueNumber
        The issue the pull request fixes.

    .PARAMETER Version
        The 7-Zip version the update moves to. Defaults to the latest release on GitHub.

    .PARAMETER PullRequestNumber
        The open pull request to update. Leave it out to create a new pull request.

    .PARAMETER PreviousVersion
        The version the open pull request names today, for the comment added when it is updated. The
        same as -Version when only the target branch moved.

    .PARAMETER PreviousBase
        The open pull request's base branch today. When it differs from -TargetBranch (a release or
        hotfix branch appeared), the comment says the pull request now targets the new branch.

    .PARAMETER ReplacesPullRequestNumber
        A pull request from the same work branch that was closed without being merged. A new pull
        request links to it, since a closed one is not reused.

    .EXAMPLE
        Set-7zipUpdatePullRequest -Path ../choco-support -Line 1.x -TargetBranch support/1.x -IssueNumber 3976 -WhatIf
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string] $Repository = 'chocolatey/choco',

        [string] $Path = '.',

        [Parameter(Mandatory)]
        [ValidateSet('1.x', '2.x')]
        [string] $Line,

        [Parameter(Mandatory)]
        [string] $TargetBranch,

        [Parameter(Mandatory)]
        [int] $IssueNumber,

        [string] $Version,

        [int] $PullRequestNumber,

        [string] $PreviousVersion,

        [string] $PreviousBase,

        [int] $ReplacesPullRequestNumber
    )

    if (-not $Version) {
        $Version = Get-7zipLatestVersion
    }

    if ($PullRequestNumber -and -not $PreviousVersion) {
        throw '-PreviousVersion is needed with -PullRequestNumber, for the comment added to the pull request.'
    }

    $workBranch = "update-7zip-$Line"
    $Path = (Resolve-Path -LiteralPath $Path).Path

    Push-Location -LiteralPath $Path

    try {
        $currentBranch = git branch --show-current

        if ($currentBranch -ne $workBranch) {
            throw "The checkout in '$Path' is on '$currentBranch', not on the work branch '$workBranch'."
        }

        $changedFiles = @(git show --name-only --format= HEAD | Where-Object { $_ })
    } finally {
        Pop-Location
    }

    $title = "(#$IssueNumber) Update 7-Zip to Version $Version"
    $changeList = $changedFiles | ForEach-Object { "- ``$_``" }

    # The release notes of the version updated to, quoted, followed by who opened the pull request
    $releaseNotes = @(Get-7zipReleaseNotes -Version $Version)
    $releaseLink = "[$Version](https://github.com/ip7z/7zip/releases/tag/$Version)"
    $openedBy = "The scheduled Update 7-Zip workflow opened this pull request.$(if ($ReplacesPullRequestNumber -and -not $PullRequestNumber) { " It replaces #$ReplacesPullRequestNumber, which was closed without being merged." })"

    $motivation = if ($releaseNotes.Count -gt 0) {
        @(
            "7-Zip $releaseLink is the latest 7-Zip release. Its release notes:"
            ''
            $releaseNotes | ForEach-Object { "> $_" }
            ''
            $openedBy
        )
    } else {
        "7-Zip $releaseLink is the latest 7-Zip release. $openedBy"
    }

    $creditsNote = @()

    if ($changedFiles -contains 'docs/legal/CREDITS.json') {
        $creditsNote = @(
            ''
            '**Before merging:** `CREDITS.md` and `CREDITS.pdf` are generated from `CREDITS.json` by the Credits Generator, so they still list the previous 7-Zip version. Regenerate them and add them to this pull request.'
        )
    }

    $body = @(
        '## Description Of Changes'
        ''
        "Updates the 7-Zip files bundled with Chocolatey CLI $Line to version $Version, by running ``Update-7zipVersion.ps1`` on ``$TargetBranch``. Changed files:"
        ''
        $changeList
        $creditsNote
        ''
        '## Motivation and Context'
        ''
        $motivation
        ''
        '## Testing'
        ''
        'N/A: Opened automatically by the Update 7-Zip workflow, which does not test the change. The build and test workflows run on this pull request.'
        ''
        '### Operating Systems Testing'
        ''
        'N/A: Opened automatically, and not tested on any operating system yet.'
        ''
        '## Change Types Made'
        ''
        '* [ ] Bug fix (non-breaking change).'
        '* [x] Feature / Enhancement (non-breaking change).'
        '* [ ] Breaking change (fix or feature that could cause existing functionality to change).'
        '* [x] Documentation changes.'
        '* [ ] PowerShell code changes.'
        ''
        '## Change Checklist'
        ''
        '* [x] Requires a change to the documentation.'
        '* [x] Documentation has been updated.'
        '* [ ] Tests to cover my changes, have been added.'
        '* [ ] All new and existing tests passed?'
        '* [ ] PowerShell code changes: PowerShell v3 compatibility checked?'
        '* [ ] Significant design decisions: [ADR](https://github.com/chocolatey/choco/blob/master/CONTRIBUTING.md#architecture-decision-records-adrs) has been created by a Chocolatey team member?'
        '* [x] All items are complete on the [Definition of Done](https://github.com/chocolatey/home/blob/main/definition-of-done.md).'
        ''
        '## Related Issue'
        ''
        "Fixes #$IssueNumber"
        ''
        "<!-- 7zip-update line=$Line version=$Version -->"
    ) -join "`n"

    # TODO: Request a review from the people who maintain Chocolatey CLI, once it is decided who
    # that is (named maintainers or a team), for example with the REST API
    # (POST repos/<owner>/<repo>/pulls/<number>/requested_reviewers). Avoid gh pr edit
    # --add-reviewer, which needs the read:org scope on a classic token.

    # Once the pull request is open: assign it to the token's account (REST, for the same scope
    # reason), and move the issue to 3 - Review.
    function Complete-PullRequest {
        param(
            [int] $Number
        )

        $login = gh api user --jq '.login'
        @{ assignees = @($login) } | ConvertTo-Json -Compress | gh api --method POST "repos/$Repository/issues/$Number/assignees" --input - | Out-Null
        Write-Host "Assigned pull request #$Number to $login."

        Set-7zipUpdateIssueStatus -Repository $Repository -IssueNumber $IssueNumber -Status Review -TargetBranch $TargetBranch -Line $Line
    }

    if ($PSCmdlet.ShouldProcess("$Repository $workBranch", 'Force-push the work branch')) {
        Push-Location -LiteralPath $Path

        try {
            git push --force origin "HEAD:refs/heads/$workBranch" 2>&1 | Out-Host
            Write-Host "Pushed '$workBranch' to origin."
        } finally {
            Pop-Location
        }
    }

    if ($PullRequestNumber) {
        $commentParts = @()

        if ([version] $PreviousVersion -ne [version] $Version) {
            $commentParts += "7-Zip $Version was released, so this pull request now updates to $Version instead of $PreviousVersion."
        }

        if ($PreviousBase -and $PreviousBase -ne $TargetBranch) {
            $commentParts += "The update now targets the '$TargetBranch' branch instead of '$PreviousBase'."
        }

        $commentParts += "The '$workBranch' branch was rebuilt from '$TargetBranch'."
        $comment = $commentParts -join ' '

        if (-not $PSCmdlet.ShouldProcess("$Repository#$PullRequestNumber", "Update the pull request to 7-Zip $Version")) {
            Write-Host "Title: $title`nBase: $TargetBranch`nAssignee: the token's account`n`n$body`n`nComment: $comment"
            Set-7zipUpdateIssueStatus -Repository $Repository -IssueNumber $IssueNumber -Status Review -TargetBranch $TargetBranch -Line $Line -WhatIf

            return [pscustomobject] @{
                Number = $PullRequestNumber
                Action = 'updated'
            }
        }

        # The REST API, not gh pr edit: gh pr edit also reads the reviewer teams, which needs the
        # read:org scope on a classic token. These two calls only need access to the repository.
        $update = @{
            title = $title
            body  = $body
            base  = $TargetBranch
        }

        $update | ConvertTo-Json -Compress | gh api --method PATCH "repos/$Repository/pulls/$PullRequestNumber" --input - | Out-Null
        @{ body = $comment } | ConvertTo-Json -Compress | gh api --method POST "repos/$Repository/issues/$PullRequestNumber/comments" --input - | Out-Null
        Write-Host "Updated pull request #$PullRequestNumber to 7-Zip Version $Version."
        Complete-PullRequest -Number $PullRequestNumber

        return [pscustomobject] @{
            Number = $PullRequestNumber
            Action = 'updated'
        }
    }

    if (-not $PSCmdlet.ShouldProcess($Repository, "Create the $Line pull request for 7-Zip $Version into '$TargetBranch'")) {
        Write-Host "Title: $title`nBase: $TargetBranch`nHead: $workBranch`nAssignee: the token's account`n`n$body"
        Set-7zipUpdateIssueStatus -Repository $Repository -IssueNumber $IssueNumber -Status Review -TargetBranch $TargetBranch -Line $Line -WhatIf

        return [pscustomobject] @{
            Number = $null
            Action = 'created'
        }
    }

    $url = $body | gh pr create --repo $Repository --base $TargetBranch --head $workBranch --title $title --body-file -
    $number = [int] ($url -split '/')[-1]
    Write-Host "Created pull request #$number for 7-Zip Version $Version ($url)."
    Complete-PullRequest -Number $number

    [pscustomobject] @{
        Number = $number
        Action = 'created'
    }
}

Export-ModuleMember -Function @(
    'Get-7zipLatestVersion'
    'Get-7zipReleaseNotes'
    'Get-7zipBundledVersion'
    'Get-7zipUpdateStatus'
    'Get-7zipTargetBranch'
    'Find-7zipUpdateItem'
    'Set-7zipUpdateIssueStatus'
    'Set-7zipUpdateIssue'
    'Update-7zipWorkBranch'
    'New-7zipUpdateCommit'
    'Set-7zipUpdatePullRequest'
)
