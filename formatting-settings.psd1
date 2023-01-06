@{
    IncludeRules = @(
        'PSUseBOMForUnicodeEncodedFile',
        'PSMisleadingBacktick',
        'PSAvoidUsingCmdletAliases',
        'PSAvoidTrailingWhitespace',
        'PSAvoidSemicolonsAsLineTerminators',
        'PSUseCorrectCasing',
        'PSPlaceOpenBrace',
        'PSPlaceCloseBrace',
        'PSAlignAssignmentStatement',
        'PSUseConsistentWhitespace',
        'PSUseConsistentIndentation'
    )

    Rules        = @{

        <#
        PSAvoidUsingCmdletAliases          = @{
            'allowlist' = @('')
        }#>

        PSAvoidSemicolonsAsLineTerminators = @{
            Enable = $true
        }


        PSUseCorrectCasing                 = @{
            Enable = $true
        }

        PSPlaceOpenBrace                   = @{
            Enable             = $true
            OnSameLine         = $true
            NewLineAfter       = $true
            IgnoreOneLineBlock = $false
        }

        PSPlaceCloseBrace                  = @{
            Enable             = $true
            NewLineAfter       = $true
            IgnoreOneLineBlock = $false
            NoEmptyLineBefore  = $true
        }

        PSAlignAssignmentStatement         = @{
            Enable         = $true
            CheckHashtable = $true
        }

        PSUseConsistentIndentation         = @{
            Enable              = $true
            Kind                = 'space'
            PipelineIndentation = 'IncreaseIndentationForFirstPipeline'
            IndentationSize     = 4
        }

        PSUseConsistentWhitespace          = @{
            Enable                                  = $true
            CheckInnerBrace                         = $true
            CheckOpenBrace                          = $true
            CheckOpenParen                          = $true
            CheckOperator                           = $true
            CheckPipe                               = $true
            CheckPipeForRedundantWhitespace         = $false
            CheckSeparator                          = $true
            CheckParameter                          = $false
            IgnoreAssignmentOperatorInsideHashTable = $true
        }
    }
}