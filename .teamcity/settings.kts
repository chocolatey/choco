import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.script
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.v2019_2.Dependencies
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.pullRequests
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.vcs
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.finishBuildTrigger
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.ScheduleTrigger
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.schedule
import jetbrains.buildServer.configs.kotlin.v2019_2.vcs.GitVcsRoot

project {
    buildType(Chocolatey)
    buildType(ChocolateySchd)
    buildType(ChocolateyQA)
    buildType(ChocolateySign)
    buildType(ChocolateyDockerWin)
    buildType(ChocolateyPosix)
}

object Chocolatey : BuildType({
    id = AbsoluteId("Chocolatey")
    name = "Chocolatey CLI (Built with Unit Tests)"

    templates(AbsoluteId("SlackNotificationTemplate"))

    artifactRules = """
    """.trimIndent()

    params {
        param("env.vcsroot.branch", "%vcsroot.branch%")
        param("env.Git_Branch", "%teamcity.build.vcs.branch.Chocolatey_ChocolateyVcsRoot%")
        param("teamcity.git.fetchAllHeads", "true")
        password("env.GITHUB_PAT", "%system.GitHubPAT%", display = ParameterDisplay.HIDDEN, readOnly = true)
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

        script {
            name = "Call Cake"
            scriptContent = """
                build.official.bat --verbosity=diagnostic --target=CI --testExecutionType=unit --shouldRunOpenCover=false --shouldBuildMsi=true
            """.trimIndent()
        }
    }

    triggers {
        vcs {
            branchFilter = ""
        }
    }

    features {
        pullRequests {
            provider = github {
                authType = token {
                    token = "%system.GitHubPAT%"
                }
            }
        }
    }

    requirements {
        doesNotExist("docker.server.version")
        doesNotContain("teamcity.agent.name", "Docker")
    }
})

object ChocolateySchd : BuildType({
    id = AbsoluteId("ChocolateySchd")
    name = "Chocolatey CLI (Scheduled Integration Testing)"

    templates(AbsoluteId("SlackNotificationTemplate"))

    artifactRules = """
    """.trimIndent()

    params {
        param("env.vcsroot.branch", "%vcsroot.branch%")
        param("env.Git_Branch", "%teamcity.build.vcs.branch.Chocolatey_ChocolateyVcsRoot%")
        param("teamcity.git.fetchAllHeads", "true")
        password("env.GITHUB_PAT", "%system.GitHubPAT%", display = ParameterDisplay.HIDDEN, readOnly = true)
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

        script {
            name = "Call Cake"
            scriptContent = """
                build.official.bat --verbosity=diagnostic --target=CI --testExecutionType=all --shouldRunOpenCover=false --shouldRunAnalyze=false --shouldRunIlMerge=false --shouldObfuscateOutputAssemblies=false --shouldRunChocolatey=false --shouldRunNuGet=false --shouldAuthenticodeSignMsis=false --shouldAuthenticodeSignOutputAssemblies=false --shouldAuthenticodeSignPowerShellScripts=false
            """.trimIndent()
        }
    }

    triggers {
        schedule {
            schedulingPolicy = daily {
                hour = 2
                minute = 0
            }
            branchFilter = """
                +:<default>
            """.trimIndent()
            triggerBuild = always()
			withPendingChangesOnly = false
        }
    }

    requirements {
        doesNotExist("docker.server.version")
        doesNotContain("teamcity.agent.name", "Docker")
    }
})

object ChocolateyQA : BuildType({
    id = AbsoluteId("ChocolateyQA")
    name = "Chocolatey CLI (SonarQube)"

    templates(AbsoluteId("SlackNotificationTemplate"))

    artifactRules = """
    """.trimIndent()

    params {
        param("env.vcsroot.branch", "%vcsroot.branch%")
        param("env.Git_Branch", "%teamcity.build.vcs.branch.Chocolatey_ChocolateyVcsRoot%")
        param("env.SONARQUBE_ID", "choco")
        param("teamcity.git.fetchAllHeads", "true")
        password("env.GITHUB_PAT", "%system.GitHubPAT%", display = ParameterDisplay.HIDDEN, readOnly = true)
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

                    choco install windows-sdk-7.1 netfx-4.0.3-devpack dotnet-6.0-runtime --confirm --no-progress
                    exit ${'$'}LastExitCode
                """.trimIndent()
            }
        }

        script {
            name = "Call Cake"
            scriptContent = """
                build.official.bat --verbosity=diagnostic --target=CI --testExecutionType=none --shouldRunAnalyze=false --shouldRunIlMerge=false --shouldObfuscateOutputAssemblies=false --shouldRunChocolatey=false --shouldRunNuGet=false --shouldRunSonarQube=true --shouldRunDependencyCheck=true --shouldAuthenticodeSignMsis=false --shouldAuthenticodeSignOutputAssemblies=false --shouldAuthenticodeSignPowerShellScripts=false
            """.trimIndent()
        }
    }

    triggers {
        schedule {
            schedulingPolicy = weekly {
                dayOfWeek = ScheduleTrigger.DAY.Saturday
                hour = 2
                minute = 45
            }
            branchFilter = """
                +:<default>
            """.trimIndent()
            triggerBuild = always()
			withPendingChangesOnly = false
        }
    }

    requirements {
        doesNotExist("docker.server.version")
        doesNotContain("teamcity.agent.name", "Docker")
    }
})

object ChocolateySign : BuildType({
    id = AbsoluteId("ChocolateySign")
    name = "Chocolatey CLI (Script Signing)"

    templates(AbsoluteId("SlackNotificationTemplate"))

    artifactRules = """
    """.trimIndent()

    params {
        param("env.vcsroot.branch", "%vcsroot.branch%")
        param("env.Git_Branch", "%teamcity.build.vcs.branch.Chocolatey_ChocolateyVcsRoot%")
        param("env.FORCE_OFFICIAL_AUTHENTICODE_SIGNATURE", "true")
        param("teamcity.git.fetchAllHeads", "true")
        password("env.GITHUB_PAT", "%system.GitHubPAT%", display = ParameterDisplay.HIDDEN, readOnly = true)
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

                    choco install windows-sdk-7.1 netfx-4.0.3-devpack dotnet-6.0-runtime --confirm --no-progress
                    exit ${'$'}LastExitCode
                """.trimIndent()
            }
        }

        step {
            name = "Include Signing Keys"
            type = "PrepareSigningEnvironment"
        }

        script {
            name = "Call Cake"
            scriptContent = """
                build.official.bat --verbosity=diagnostic --target=Sign-PowerShellScripts --exclusive
            """.trimIndent()
        }
    }

    triggers {
        vcs {
            triggerRules = """
                +:nuspec/**/*.ps1
                +:nuspec/**/*.psm1
                +:nuspec/**/*.psd1
                +:src/chocolatey.resources/**/*.ps1
                +:src/chocolatey.resources/**/*.psm1
                +:src/chocolatey.resources/**/*.psd1
            """.trimIndent()
            branchFilter = "+:develop"
        }
    }

    requirements {
        doesNotExist("docker.server.version")
        doesNotContain("teamcity.agent.name", "Docker")
    }
})

object ChocolateyDockerWin : BuildType({
    id = AbsoluteId("ChocolateyDockerWin")
    name = "Docker (Windows)"

    templates(AbsoluteId("SlackNotificationTemplate"))

    params {
        // TeamCity has suggested "${Chocolatey.depParamRefs.buildNumber}"
        param("env.CHOCOLATEY_VERSION", "%dep.Chocolatey.build.number%")
        param("env.vcsroot.branch", "%vcsroot.branch%")
        param("env.Git_Branch", "%teamcity.build.vcs.branch.Chocolatey_ChocolateyVcsRoot%")
        param("teamcity.git.fetchAllHeads", "true")
        password("env.DOCKER_USER", "%system.DockerUsername%", display = ParameterDisplay.HIDDEN, readOnly = true)
        password("env.DOCKER_PASSWORD", "%system.DockerPassword%", display = ParameterDisplay.HIDDEN, readOnly = true)
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        script {
            name = "Call Cake"
            scriptContent = "call build.official.bat --verbosity=diagnostic --target=Docker"
        }
    }

    triggers {
        finishBuildTrigger {
            buildType = "Chocolatey"
            successfulOnly = true
            branchFilter = """
                +:tags/*
            """.trimIndent()
        }
    }

    dependencies {
        dependency(AbsoluteId("Chocolatey")) {
            snapshot {
                onDependencyFailure = FailureAction.FAIL_TO_START
                synchronizeRevisions = false
            }

            artifacts {
                artifactRules = "chocolatey.%env.CHOCOLATEY_VERSION%.nupkg=>%system.teamcity.build.checkoutDir%\\code_drop\\Packages\\Chocolatey"
            }
        }
    }

    requirements {
        contains("docker.server.osType", "windows")
        exists("docker.server.version")
        contains("teamcity.agent.name", "Docker")
    }
})

object ChocolateyPosix : BuildType({
    id = AbsoluteId("ChocolateyPosix")
    name = "Docker (Linux)"

    templates(AbsoluteId("SlackNotificationTemplate"))

    params {
        param("env.CAKE_NUGET_SOURCE", "") // The Cake version we use has issues with authing to our private source on Linux
        param("env.PRIMARY_NUGET_SOURCE", "") // As above there are issues with authing to our private source on Linux
        param("env.CHOCOLATEY_VERSION", "%dep.Chocolatey.build.number%")
        param("env.CHOCOLATEY_OFFICIAL_KEY", "%system.teamcity.build.checkoutDir%/chocolatey.official.snk")
        password("env.GITHUB_PAT", "%system.GitHubPAT%", display = ParameterDisplay.HIDDEN, readOnly = true)
        param("env.vcsroot.branch", "%vcsroot.branch%")
        param("env.Git_Branch", "%teamcity.build.vcs.branch.Chocolatey_ChocolateyVcsRoot%")
        param("teamcity.git.fetchAllHeads", "true")
        password("env.DOCKER_USER", "%system.DockerUsername%", display = ParameterDisplay.HIDDEN, readOnly = true)
        password("env.DOCKER_PASSWORD", "%system.DockerPassword%", display = ParameterDisplay.HIDDEN, readOnly = true)
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    artifactRules = """
    """.trimIndent()

    steps {
        step {
            name = "Load Key"
            conditions {
                doesNotContain("teamcity.serverUrl", "-dev")
            }
            type = "StrongNameKeyLinux"
        }

        script {
            name = "Install Prerequisites"
            scriptContent = """
                sudo apt update
                sudo apt install dirmngr gnupg apt-transport-https ca-certificates software-properties-common
                sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
                sudo apt-add-repository 'deb https://download.mono-project.com/repo/ubuntu stable-focal main'
                sudo apt install mono-complete -y

                # Import Cert Bundle for Mono, allowing for knowledge of existing cert chains
                cert-sync /etc/ssl/certs/ca-certificates.crt
            """.trimIndent()
        }

        script {
            name = "Build Chocolatey"
            scriptContent = """
                ./build.official.sh --verbosity=diagnostic
            """.trimIndent()
        }

        // Please note that this method will need to be changed to some form of CD after we lock agents down
        script {
            name = "Publish TarGz to GitHub Release"
            conditions {
                exists("env.GITHUB_PAT")
                startsWith("teamcity.build.branch", "tags")
            }
            scriptContent = """
                curl \
                    -X POST \
                    -H "Accept: application/vnd.github+json" \
                    -H "Authorization: Bearer %env.GITHUB_PAT%"\
                    -H "X-GitHub-Api-Version: 2022-11-28" \
                    -H "Content-Type: application/octet-stream" \
                    https://uploads.github.com/repos/chocolatey/choco/releases/%env.CHOCOLATEY_VERSION%/assets?name=chocolatey.v%env.CHOCOLATEY_VERSION%.tar.gz \
                    --data-binary "@code_drop/Packages/Chocolatey/chocolatey.v%env.CHOCOLATEY_VERSION%.tar.gz"
            """.trimIndent()
        }

        script {
            name = "Build Docker Image"
            scriptContent = "./build.official.sh --verbosity=diagnostic --target=Docker"
        }

        script {
            name = "Create Docker Manifest"
            conditions {
                exists("env.DOCKER_USER")
                exists("env.DOCKER_PASSWORD")
                startsWith("teamcity.build.branch", "tags")
            }
            scriptContent = "./build.official.sh --verbosity=diagnostic --target=DockerManifest"
        }
    }

    triggers {
        finishBuildTrigger {
            buildType = "Chocolatey"
            successfulOnly = true
            branchFilter = """
                +:tags/*
            """.trimIndent()
        }
    }

    dependencies {
        snapshot(AbsoluteId("ChocolateyDockerWin")) {
            onDependencyFailure = FailureAction.FAIL_TO_START
            synchronizeRevisions = false
        }
    }

    requirements {
        contains("docker.server.osType", "linux")
        exists("docker.server.version")
        contains("teamcity.agent.name", "Docker")
    }
})
