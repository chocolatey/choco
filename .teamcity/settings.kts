import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.script
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.pullRequests
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.xmlReport
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.XmlReport
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.nuGetPublish
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.vcs
import jetbrains.buildServer.configs.kotlin.v2019_2.vcs.GitVcsRoot

version = "2021.2"

project {
    buildType(Chocolatey)
}

object Chocolatey : BuildType({
    id = AbsoluteId("Chocolatey")
    name = "Build"

    artifactRules = """
        build_output/build_artifacts/compile/msbuild-net-4.0-results.xml
        build_output/build_artifacts/tests/index.html
        build_output/build_artifacts/codecoverage/Html/index.htm
        build_output/_BuildInfo.xml
        build_output/build.log
        code_drop/build_artifacts/ilmerge/ilmerge.log
        code_drop/build_artifacts/ilmerge/ilmergedll.log
        code_drop/nuget/*.nupkg
        src/SolutionVersion.cs
    """.trimIndent()

    params {
        param("env.TEAMCITY_GIT_TAG", "")
    }

    vcs {
        root(DslContext.settingsRoot)

        branchFilter = """
            +:*
        """.trimIndent()
    }

    steps {
        powerShell {
            name = "Prerequisites"
            scriptMode = script {
                content = """
                    if ((Get-WindowsFeature -Name NET-Framework-Features).InstallState -ne 'Installed') {
                        Install-WindowsFeature -Name NET-Framework-Features
                    }

                    choco install windows-sdk-7.1 netfx-4.0.3-devpack --confirm --no-progress
                    exit ${'$'}LastExitCode
                """.trimIndent()
            }
        }

        step {
            name = "Include Signing Keys"
            type = "PrepareSigningEnvironment"
        }

        powerShell {
            name = "Find tag if one exists"
            scriptMode = script {
                content = """
                    ${'$'}tagName = git tag -l --points-at HEAD

                    if (${'$'}tagName) {
                        Write-Host "Found tag ${'$'}tagName"
                        Write-Host "##teamcity[setParameter name='env.TEAMCITY_GIT_TAG' value='${'$'}tagName']"
                    } else {
                        Write-Host "No tag found for current commit"
                    }
                """.trimIndent()
            }
        }

        script {
            name = "Run Build"
            scriptContent = "call build.official.bat"
        }

        nuGetPublish {
            name = "Publish Packages"

            conditions {
                matches("teamcity.build.branch", "^(develop|release/.*|hotfix/.*|tags/.*)${'$'}")
            }

            toolPath = "%teamcity.tool.NuGet.CommandLine.DEFAULT%"
            packages = "code_drop/nuget/*.nupkg"
            serverUrl = "%env.NUGETDEV_SOURCE%"
            apiKey = "%env.NUGETDEV_API_KEY%"
        }
    }

    triggers {
        vcs {
            branchFilter = ""
        }
    }

    features {
        xmlReport {
            reportType = XmlReport.XmlReportType.NUNIT
            rules = "build_output/build_artifacts/tests/test-results.xml"
        }

        pullRequests {
            provider = github {
                authType = token {
                    token = "%system.GitHubPAT%"
                }
            }
        }
    }
})