import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.DockerCommandStep
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.dockerCommand
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.script
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.v2019_2.Dependencies
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.pullRequests
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.vcs
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.finishBuildTrigger
import jetbrains.buildServer.configs.kotlin.v2019_2.vcs.GitVcsRoot

project {
    buildType(Chocolatey)
    buildType(ChocolateyDockerWin)
    buildType(ChocolateyPosix)
    buildType(ChocolateyDockerManifest)
}

object Chocolatey : BuildType({
    id = AbsoluteId("Chocolatey")
    name = "Build"

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
            scriptContent = "call build.official.bat --verbosity=diagnostic --target=CI --testExecutionType=all --shouldRunOpenCover=false"
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
    }
})

object ChocolateyDockerWin : BuildType({
    id = AbsoluteId("ChocolateyDockerWin")
    name = "Docker (Windows)"

    params {
        // TeamCity has suggested "${Chocolatey.depParamRefs.buildNumber}"
        param("env.CHOCOLATEY_VERSION", "%dep.Chocolatey.build.number%")
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        step {
            name = "Login Docker"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            type = "DockerLogin"
        }

        dockerCommand {
            name = "Build Docker Image"
            commandType = build {
                source = file {
                    path = "docker/Dockerfile.windows"
                }
                contextDir = "."
                platform = DockerCommandStep.ImagePlatform.Windows
                namesAndTags = "chocolatey/choco:latest-windows"
            }
        }

        // powerShell {
        //     name = "Test Docker Image"
        //     scriptMode = script {
        //         content = """
        //             docker run --rm chocolatey/choco:latest-windows choco.exe --version
        //             exit ${'$'}LastExitCode
        //         """.trimIndent()
        //     }
        // }

        dockerCommand {
            name = "Push Docker Image"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            commandType = push {
                namesAndTags = "chocolatey/choco:latest-windows"
                removeImageAfterPush = false
            }
        }

        dockerCommand {
            name = "Tag Docker Image with Version"
            commandType = other {
                subCommand = "tag"
                commandArgs = "chocolatey/choco:latest-windows chocolatey/choco:v%env.CHOCOLATEY_VERSION%-windows"
            }
        }

        dockerCommand {
            name = "Push Versioned Tag"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            commandType = push {
                namesAndTags = "chocolatey/choco:v%env.CHOCOLATEY_VERSION%-windows"
            }
        }
    }

    triggers {
        finishBuildTrigger {
            buildType = "Chocolatey"
            successfulOnly = true
            branchFilter = """
                +:release/*
                +:hotfix/*
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
    }
})

object ChocolateyPosix : BuildType({
    id = AbsoluteId("ChocolateyPosix")
    name = "Docker (Linux)"

    params {
        param("env.CHOCOLATEY_VERSION", "%dep.Chocolatey.build.number%")
        param("env.CHOCOLATEY_OFFICIAL_KEY", "%system.teamcity.build.checkoutDir%/chocolatey.official.snk")
        password("env.GITHUB_PAT", "%system.GitHubPAT%", display = ParameterDisplay.HIDDEN, readOnly = true)
        param("env.vcsroot.branch", "%vcsroot.branch%")
        param("env.Git_Branch", "%teamcity.build.vcs.branch.Chocolatey_ChocolateyVcsRoot%")
        param("teamcity.git.fetchAllHeads", "true")
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    artifactRules = """
        chocolatey.*.tar.gz
    """.trimIndent()

    steps {
        step {
            name = "Load Key"
            conditions {
                doesNotContain("teamcity.serverUrl", "-dev")
            }
            type = "StrongNameKeyLinux"
        }

        step {
            name = "Login Docker"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            type = "DockerLogin"
        }

        script {
            name = "Install Prerequisites"
            scriptContent = """
                sudo amazon-linux-extras install mono
            """.trimIndent()
        }

        script {
            name = "Build Chocolatey"
            scriptContent = """
                ./build.official.sh
                cd ./code_drop/chocolatey/console
                tar -czvf ../../../chocolatey.v%env.CHOCOLATEY_VERSION%.tar.gz .
            """.trimIndent()
        }

        // Please note that this method will need to be changed to some form of CD after we lock agents down
        script {
            name = "Publish TarGz to GitHub Release"
            conditions {
                exists("env.GITHUB_PAT")
            }
            scriptContent = """
                curl \
                    -X POST \
                    -H "Accept: application/vnd.github+json" \
                    -H "Authorization: Bearer %env.GITHUB_PAT%"\
                    -H "X-GitHub-Api-Version: 2022-11-28" \
                    -H "Content-Type: application/octet-stream" \
                    https://uploads.github.com/repos/chocolatey/choco/releases/%env.CHOCOLATEY_VERSION%/assets?name=chocolatey.v%env.CHOCOLATEY_VERSION%.tar.gz \
                    --data-binary "@chocolatey.v%env.CHOCOLATEY_VERSION%.tar.gz"
            """.trimIndent()
        }

        dockerCommand {
            name = "Build Docker Image"
            commandType = build {
                source = file {
                    path = "docker/Dockerfile.linux"
                }
                contextDir = "."
                platform = DockerCommandStep.ImagePlatform.Linux
                namesAndTags = "chocolatey/choco:latest-linux"
                commandArgs = "--build-arg buildscript=build.official.sh"
            }
        }

        dockerCommand {
            name = "Push Docker Image"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            commandType = push {
                namesAndTags = "chocolatey/choco:latest-linux"
                removeImageAfterPush = false
            }
        }

        dockerCommand {
            name = "Tag Docker Image with Version"
            commandType = other {
                subCommand = "tag"
                commandArgs = "chocolatey/choco:latest-linux chocolatey/choco:v%env.CHOCOLATEY_VERSION%-linux"
            }
        }

        dockerCommand {
            name = "Push Versioned Tag"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            commandType = push {
                namesAndTags = "chocolatey/choco:v%env.CHOCOLATEY_VERSION%-linux"
            }
        }
    }

    triggers {
        finishBuildTrigger {
            buildType = "Chocolatey"
            successfulOnly = true
            branchFilter = """
                +:release/*
                +:hotfix/*
                +:tags/*
            """.trimIndent()
        }
    }

    dependencies {
        snapshot(AbsoluteId("Chocolatey")) {
            onDependencyFailure = FailureAction.FAIL_TO_START
            synchronizeRevisions = false
        }
    }

    requirements {
        contains("docker.server.osType", "linux")
        exists("docker.server.version")
    }
})

object ChocolateyDockerManifest : BuildType({
    id = AbsoluteId("ChocolateyDockerManifest")
    name = "Docker Manifest"

    params {
        param("env.CHOCOLATEY_VERSION", "%dep.Chocolatey.build.number%")
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        step {
            name = "Login Docker"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            type = "DockerLogin"
        }

        dockerCommand {
            name = "Create Combined Manifest"
            commandType = other {
                subCommand = "manifest"
                commandArgs = "create chocolatey/choco:latest chocolatey/choco:latest-linux chocolatey/choco:latest-windows"
            }
        }

        dockerCommand {
            name = "Push Combined Manifest"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            commandType = other {
                subCommand = "manifest"
                commandArgs = "push chocolatey/choco:latest"
            }
        }

        dockerCommand {
            name = "Create Versioned Manifest"
            commandType = other {
                subCommand = "manifest"
                commandArgs = "create chocolatey/choco:v%env.CHOCOLATEY_VERSION% chocolatey/choco:v%env.CHOCOLATEY_VERSION%-linux chocolatey/choco:v%env.CHOCOLATEY_VERSION%-windows"
            }
        }

        dockerCommand {
            name = "Push Versioned Manifest"
            conditions {
                exists("system.DockerUsername")
                exists("system.DockerPassword")
            }
            commandType = other {
                subCommand = "manifest"
                commandArgs = "push chocolatey/choco:v%env.CHOCOLATEY_VERSION%"
            }
        }

    }

    triggers {
        finishBuildTrigger {
            buildType = "Chocolatey"
            successfulOnly = true
            branchFilter = """
                +:release/*
                +:hotfix/*
                +:tags/*
            """.trimIndent()
        }
    }

    dependencies {
        snapshot(AbsoluteId("ChocolateyDockerWin")) {
            onDependencyFailure = FailureAction.FAIL_TO_START
            synchronizeRevisions = false
        }
        snapshot(AbsoluteId("ChocolateyPosix")) {
            onDependencyFailure = FailureAction.FAIL_TO_START
            synchronizeRevisions = false
        }
    }

    requirements {
        exists("docker.server.version")
    }
})