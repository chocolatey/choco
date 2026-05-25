# .NET 10 Migration Plan — Remove .NET Framework 4.8 from Chocolatey CLI

> ## ⚠️ Maintenance instructions — READ FIRST
>
> **This is a living document. Keep it updated as work progresses.**
>
> - Update the **Status** column the moment a task changes state.
> - A task is **✅ DONE only when it is implemented _and_ its tests pass on CI** (the GitHub
>   `windows-latest` runner). Code that compiles but isn't proven by a test is **not** done —
>   *NOT TESTED = NOT WORKING.*
> - Put the **short git commit hash** that implements a task in the **Commit** column when it
>   lands. If several commits implement one task, list the last/primary one.
> - **Add new rows** as tasks are discovered. **Never delete** a row — if a task is dropped or
>   superseded, mark it **⏯️ DEFERRED** and note why.
> - Commit updates to this file **alongside** the code they describe, on the feature branch.
> - When the whole table is ✅, the PR is ready to move from draft to review.

## Status legend

| Symbol | Meaning |
|---|---|
| ✅ DONE | Implemented **and** tested (CI green) |
| 🔧 IN PROGRESS | Partially implemented or underway |
| ❌ OPEN | Not yet addressed |
| ⏯️ DEFERRED | On hold until a dependency completes, or out of current scope |

---

## Goal & rationale

Installing Chocolatey on fresh Windows servers frequently triggers a **.NET Framework 4.8
install that demands a reboot** (the bootstrap downloads `ndp48-x86-x64-allos-enu.exe`; installer
exit codes `1641`/`3010` force a restart — see upstream
[#3880](https://github.com/chocolatey/choco/issues/3880)).

**This plan removes Chocolatey CLI's .NET Framework 4.8 dependency completely and rebuilds it on
the latest .NET LTS (.NET 10, GA Nov 2025), Windows-only.** After this work, installing choco
installs no framework and never reboots.

This is **not** about making Chocolatey cross-platform — only modernizing the runtime. It aligns
with the project's own intended direction: upstream [#2147](https://github.com/chocolatey/choco/issues/2147)
("Migrate to .NET Core") is milestoned **3.0.0**; the current `net48` target was a deliberate
stepping stone (PR #2739). PowerShell-Core hosting is tracked upstream in
[#3590](https://github.com/chocolatey/choco/issues/3590) and
[#3667](https://github.com/chocolatey/choco/issues/3667).

> *Terminology:* .NET Framework 4.x cannot be removed from Windows itself (it ships in the OS).
> The achievable goal is: **choco never installs/upgrades FX, and choco's assemblies don't target
> FX.** The optional WinPS 5.1 fallback below uses only what Windows already ships.

## Key decisions

| Topic | Decision | Rationale |
|---|---|---|
| Target framework | `net10.0-windows`, **hard cut** (no `net48` anywhere) | Latest LTS, no Framework legacy. |
| Runtime delivery | **Self-contained, single-file, Windows Desktop runtime**, per-RID (start: `win-x64`) | Nothing installed on target → no reboot. Desktop flavor keeps WinForms/WPF/Drawing available for package `Add-Type`. |
| PowerShell hosting | **Hybrid:** PowerShell 7 in-process by default (`Microsoft.PowerShell.SDK`) **+** out-of-process in-box Windows PowerShell 5.1 fallback (per-package opt-in) | PS7 is the modern default; the in-box 5.1 fallback installs nothing and rescues the long tail. |

> ⚠️ **Dominant risk:** by default, package `chocolateyInstall.ps1` scripts run under **PowerShell
> 7, not Windows PowerShell 5.1** — an ecosystem behavior shift. Mitigated by helper hardening +
> compat shims + the 5.1 fallback, and **measured** by the Pester E2E suite and the package-corpus
> CI job (≥80% bar).

---

## Tasks

### Phase 0 — Setup, branch & CI scaffolding

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-00 | Create feature branch `feature/net10-migration` off `develop` | ✅ DONE | |
| DM-01 | Add this migration plan (`docs/DOTNET_MIGRATION_PLAN.md`) to the repo | ✅ DONE | 4a942f91 |
| DM-02 | Open a **draft PR** within the fork (`fdcastel/choco`, base `develop`) so push-triggered CI runs on every commit — [PR #1](https://github.com/fdcastel/choco/pull/1) | ✅ DONE | |
| DM-03 | Add `actions/setup-dotnet` `10.0.x` to `.github/workflows/build.yml` and `test.yml` | ❌ OPEN | |
| DM-04 | Trim CI to Windows-only — remove/disable the Mono Ubuntu/macOS/Docker jobs | ❌ OPEN | |
| DM-05 | Run NUnit unit **and** integration on every PR push (not just nightly); upload all result artifacts | ❌ OPEN | |

### Phase 1 — Solution compiles on .NET 10

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-10 | Flip every `*.csproj` `TargetFramework` `net48` → `net10.0-windows` (Windows Desktop SDK) | ❌ OPEN | |
| DM-11 | Replace the `lib\PowerShell\System.Management.Automation.dll` references with `Microsoft.PowerShell.SDK` (in `chocolatey.csproj` and `Chocolatey.PowerShell.csproj`) | ❌ OPEN | |
| DM-12 | Remove binding-redirect props; move `choco.exe.manifest` to `<ApplicationManifest>`; drop `Microsoft.Bcl.HashCode`; set modern `<LangVersion>` | ❌ OPEN | |
| DM-13 | Dependency audit: confirm net-compatible builds of `Chocolatey.NuGet.PackageManagement`, `Rhino.Licensing`, `log4net`, `SimpleInjector`, `System.Reactive` | ❌ OPEN | |
| DM-14 | Resolve remaining compile errors until `build.bat --target=CI` builds the solution. **Gate: solution builds** | ❌ OPEN | |

### Phase 2 — NUnit unit tests green

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-20 | Replace `BinaryFormatter` in `ObjectExtensions.DeepCopy` (`src/chocolatey/ObjectExtensions.cs`); audit all `DeepCopy()` callers | ❌ OPEN | |
| DM-21 | Replace `AlphaFS` with `System.IO` (+ targeted P/Invoke for junctions/hardlinks/ADS if used) | ❌ OPEN | |
| DM-22 | Migrate `AppDomain.AssemblyResolve` → `AssemblyLoadContext.Default.Resolving` (`AssemblyResolution.cs`, `GetChocolatey.cs`, `PowershellService.cs`) | ❌ OPEN | |
| DM-23 | Migrate `chocolatey.tests` to `net10.0-windows`; fix unit failures. **Gate: NUnit unit suite green** | ❌ OPEN | |

### Phase 3 — NUnit integration tests green

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-30 | Migrate `chocolatey.tests.integration` to `net10.0-windows`; update `*.exe.config` `supportedRuntime` fixtures | ❌ OPEN | |
| DM-31 | Validate the PS7 in-process host: rewrite/remove the private-field output-redirection hack (`PowershellService.cs`); fix the `WindowsPowerShell\` profile-path assumption | ❌ OPEN | |
| DM-32 | Fix integration scenario failures. **Gate: `--testExecutionType=all --shouldRunOpenCover=false` green** | ❌ OPEN | |

### Phase 4 — Pester E2E suite green under PowerShell 7

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-40 | Port the Chocolatey helper module (`chocolateyInstaller.psm1` & helpers) to be PS7-clean | ❌ OPEN | |
| DM-41 | Add compat shims in the choco module: `Get-WmiObject`→`Get-CimInstance` proxy; restore `curl`/`wget` aliases; 5.1-style default encoding within choco's execution scope | ❌ OPEN | |
| DM-42 | Provide the `Import-Module -UseWindowsPowerShell` / `Import-WinModule` path for WinPS-only modules | ❌ OPEN | |
| DM-43 | Add a **Pester E2E CI job**: `pwsh -File Invoke-Tests.ps1` against the built `chocolatey.*.nupkg` (excluding `Licensed`/`CCM`/`VMOnly` tags as today) | ❌ OPEN | |
| DM-44 | Make the full Pester suite pass under PS7. **Gate: Pester E2E green under PS7** | ❌ OPEN | |

### Phase 5 — Self-contained publish + clean-environment smoke

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-50 | Replace the ILMerge step in `recipe.cake` with `dotnet publish` self-contained single-file (`-r win-x64 --self-contained -p:PublishSingleFile=true`, Desktop runtime) | ❌ OPEN | |
| DM-51 | Rework the "ILMerged into chocolatey" branch in `AssemblyResolution.cs` (nothing is merged now) | ❌ OPEN | |
| DM-52 | Update all hardcoded `/net48/` paths in `recipe.cake`; `chocolatey.lib` `lib/net48` → `lib/net10.0` | ❌ OPEN | |
| DM-53 | Add a **clean-container smoke CI job**: run the self-contained `choco.exe` in a `servercore` Windows container with **no .NET 10 runtime installed** (`choco --version`/`list`/`source list`) | ❌ OPEN | |
| DM-54 | Assert no `net48` artifacts remain. **Gate: smoke green — choco runs with nothing installed** | ❌ OPEN | |

### Phase 6 — Real-world package compatibility (≥80% bar)

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-60 | Curate a corpus of ~30-50 silently-installable top community packages (exclude known reboot/GUI packages) | ❌ OPEN | |
| DM-61 | Add a **package-corpus CI job**: install + uninstall the corpus under PS7; emit a pass/fail triage report (buckets: WMI / `Add-Type` / WinPS-module / encoding / alias) | ❌ OPEN | |
| DM-62 | Fix high-value failures or route un-fixable ones to the 5.1 fallback. **Gate: corpus ≥ 80%** | ❌ OPEN | |

### Phase 7 — Windows PowerShell 5.1 fallback

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-70 | Implement the out-of-process in-box WinPS 5.1 runner (stream capture + exit-code propagation) | ❌ OPEN | |
| DM-71 | Add a per-package opt-in directive selecting 5.1 (default remains PS7) | ❌ OPEN | |
| DM-72 | Validate the fallback via a tagged Pester subset. **Gate: fallback path green** | ❌ OPEN | |

### Phase 8 — Bootstrap, installer & docs

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-80 | Delete `Install-DotNet48IfMissing` + the reboot path in `chocolateysetup.psm1`; update `chocolateyInstall.ps1`, `init.ps1`, community `install.ps1` | ❌ OPEN | |
| DM-81 | Remove WiX `src/chocolatey.install/NetFx48.wxs` and the `WIX_IS_NETFRAMEWORK_48_OR_LATER_INSTALLED` condition in `chocolatey.wxs` | ❌ OPEN | |
| DM-82 | Update `README.md` requirements (.NET FX 4.8 → none / self-contained) and build prerequisites (.NET 10 SDK) | ❌ OPEN | |
| DM-83 | Full pipeline green end-to-end; bootstrap no longer references `ndp48`; mark PR ready for review. **Gate: all green** | ❌ OPEN | |

### Deferred / out of current scope

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-90 | Self-contained `win-x86` build | ⏯️ DEFERRED | |
| DM-91 | Self-contained `win-arm64` build | ⏯️ DEFERRED | |
| DM-92 | Size optimization (ReadyToRun / trimming) | ⏯️ DEFERRED | |
| DM-93 | Lockstep migration of the wider ecosystem (Licensed extension, Agent, GUI, Boxstarter) — separate repos | ⏯️ DEFERRED | |

---

## Testing strategy

All tests run on the GitHub **`windows-latest`** runner (Server 2022 — ships **both** Windows
PowerShell 5.1 and PowerShell 7, so both host paths are testable). The existing harness is reused;
new jobs are added only for gaps.

| Layer | What it proves | How it runs |
|---|---|---|
| **NUnit unit** (`chocolatey.tests`) | Core logic on net10 | `build.bat --target=test-nunit --exclusive` |
| **NUnit integration** (`chocolatey.tests.integration`) | Real install/upgrade/uninstall scenarios | `--testExecutionType=all --shouldRunOpenCover=false` |
| **Pester E2E** (`tests/pester-tests/`, via `Invoke-Tests.ps1`) | The real `choco.exe` + helper functions under **PS7** | new CI job: `pwsh -File Invoke-Tests.ps1` |
| **Package corpus** (new) | Real-world package compatibility ≥ 80% | new CI job: install+uninstall curated corpus under PS7 |
| **Clean-container smoke** (new) | choco runs with **no runtime installed, no reboot** | new CI job: `servercore` container, no .NET 10 runtime |
| **Script format** (`ScriptFormat.Tests.ps1`) | PowerShell style | existing Pester check |

## Branch / PR workflow

- All work lands on **`feature/net10-migration`** (off `develop`).
- A **draft PR within the fork** (`fdcastel/choco`, base `develop`) is opened early so
  push-triggered CI validates every commit.
- Each phase's **Gate** task must be **green on `windows-latest`** before advancing. Never merge red.
- The eventual PR to upstream `chocolatey/choco` is a **separate, explicit step** once everything
  is complete — not part of this fork-internal workflow.
