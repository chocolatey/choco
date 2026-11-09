function Assert-FolderAccessRules {
    <#
        .SYNOPSIS
        Throws if any of the given folders do not have exactly the expected access rules.

        .DESCRIPTION
        Checks every folder and throws once with all the problems found, so a failure reports everything wrong at once.

        .EXAMPLE
        Assert-FolderAccessRules -Path "$env:ProgramData/ChocolateyHttpCache" -Owner 'BUILTIN\Administrators' -ExpectedRules @(
            @{ Identity = 'BUILTIN\Administrators'; Rights = 'FullControl' }
            @{ Identity = 'NT AUTHORITY\SYSTEM'; Rights = 'FullControl' }
            @{ Identity = 'BUILTIN\Users'; Rights = 'ReadAndExecute, Synchronize' }
        )
    #>
    [CmdletBinding()]
    param(
        # The folders to check.
        [Parameter(Mandatory)]
        [string[]]
        $Path,

        # The only access rules each folder should have. Each is a hashtable with:
        # Identity (required), Rights (required), Type (defaults to 'Allow'),
        # and InheritanceFlags (only checked when given).
        [Parameter(Mandatory)]
        [hashtable[]]
        $ExpectedRules,

        # The owner each folder should have. Not checked when not given.
        [string]
        $Owner,

        # Check that each folder does not inherit permissions from its parent folder.
        [switch]
        $Protected
    )

    $observedErrors = @(
        foreach ($folder in $Path) {
            if (-not (Test-Path -Path $folder -PathType Container)) {
                "The $folder folder does not exist"
                continue
            }

            $acl = Get-Acl -Path $folder
            if ($Owner -and $acl.Owner -ne $Owner) {
                "The $folder folder is owned by $($acl.Owner), not $Owner"
            }

            if ($Protected -and -not $acl.AreAccessRulesProtected) {
                "The $folder folder inherits permissions from its parent folder"
            }

            $rules = @($acl.Access)
            $unexpectedRules = $rules.Where{ $_.IdentityReference.Value -notin $ExpectedRules.Identity }
            if ($unexpectedRules) {
                "Unexpected access rules are present on the $folder folder:`n$($unexpectedRules | Format-List | Out-String)"
            }

            foreach ($rule in $ExpectedRules) {
                $type = if ($rule.Type) { $rule.Type } else { 'Allow' }
                $actual = @($rules.Where{ $_.IdentityReference.Value -eq $rule.Identity })

                if ($actual.Count -eq 0) {
                    "There is no access rule for $($rule.Identity) on the $folder folder"
                    continue
                }

                if ($actual.Count -gt 1) {
                    "There are $($actual.Count) access rules for $($rule.Identity) on the $folder folder, but there should be 1:`n$($actual | Format-List | Out-String)"
                    continue
                }

                $actual = $actual[0]
                if ($actual.AccessControlType -ne $type) {
                    "Access control type for $($rule.Identity) rule on the $folder folder should have '$type', but has '$($actual.AccessControlType)'"
                }

                if ($actual.FileSystemRights -ne $rule.Rights) {
                    "File system rights for $($rule.Identity) rule on the $folder folder should have '$($rule.Rights)', but has '$($actual.FileSystemRights)'"
                }

                if ($rule.InheritanceFlags -and $actual.InheritanceFlags -ne $rule.InheritanceFlags) {
                    "Inheritance flags for $($rule.Identity) rule on the $folder folder should have '$($rule.InheritanceFlags)', but has '$($actual.InheritanceFlags)'"
                }
            }
        }
    ) -join "`n"

    $observedErrors = $observedErrors.Trim()
    if ($observedErrors) {
        throw $observedErrors
    }
}
