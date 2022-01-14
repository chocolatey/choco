import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.xmlReport
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.XmlReport
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.commitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.nuGetPublish
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.vcs
import jetbrains.buildServer.configs.kotlin.v2019_2.vcs.GitVcsRoot

/*
The settings script is an entry point for defining a TeamCity
project hierarchy. The script should contain a single call to the
project() function with a Project instance or an init function as
an argument.
VcsRoots, BuildTypes, Templates, and subprojects can be
registered inside the project using the vcsRoot(), buildType(),
template(), and subProject() methods respectively.
To debug settings scripts in command-line, run the
    mvnDebug org.jetbrains.teamcity:teamcity-configs-maven-plugin:generate
command and attach your debugger to the port 8000.
To debug in IntelliJ Idea, open the 'Maven Projects' tool window (View
-> Tool Windows -> Maven Projects), find the generate task node
(Plugins -> teamcity-configs -> teamcity-configs:generate), the
'Debug' option is available in the context menu for the task.
*/

version = "2021.2"

project {
    buildType(Chocolatey)
}

object Chocolatey : BuildType({
    name = "Build"

    artifactRules = "code_drop/nuget/*.nupkg"

    params {
        text("env.CHOCOLATEY_SOURCE", "https://hermes.chocolatey.org:8443/repository/choco-internal-testing/", readOnly = true, allowEmpty = false)
        password("env.CHOCOLATEY_API_KEY", "credentialsJSON:c0c84719-2f46-595e-b40b-e545c83c8e9b", display = ParameterDisplay.HIDDEN, readOnly = true)
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        powerShell {
            name = "Prerequisites"
            scriptMode = script {
                content = """
                    if ((Get-WindowsFeature -Name NET-Framework-Features).InstallState -ne 'Installed') {
                        Install-WindowsFeature -Name NET-Framework-Features
                    }

                    choco install windows-sdk-7.1 --confirm --no-progress
                    choco install netfx-4.0.3-devpack --confirm --no-progress
                """.trimIndent()
            }
        }

        step {
            name = "Include Signing Keys"
            type = "PrepareSigningEnvironment"
        }

        powerShell {
            name = "Build"
            scriptMode = script {
                content = """
                    .\\build.official.bat -D:version.fix=%build.counter%
                """.trimIndent()
            }
        }

        nuGetPublish {
            name = "Publish Packages"

            conditions {
                matches("teamcity.build.branch", "^(develop|release/.*|hotfix/.*)${'$'}")
            }

            toolPath = "%teamcity.tool.NuGet.CommandLine.DEFAULT%"
            packages = "code_drop/nuget/*.nupkg"
            serverUrl = "%env.CHOCOLATEY_SOURCE%"
            apiKey = "%env.CHOCOLATEY_API_KEY%"
        }
    }

    triggers {
        vcs {
        }
    }

    features {
        xmlReport {
            reportType = XmlReport.XmlReportType.NUNIT
            rules = "code_drop/build_artifacts/tests/test-results.xml"
        }
    }
})