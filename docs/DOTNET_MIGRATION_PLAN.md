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
| DM-02 | Open the PR to **upstream** `chocolatey/choco` — deferred until the migration is complete (CI already runs on every push, so no fork-internal PR is needed) | ⏯️ DEFERRED | |
| DM-03 | Add `actions/setup-dotnet` `10.0.x` to `.github/workflows/build.yml` and `test.yml` | ✅ DONE | 47ba9420 |
| DM-04 | Trim CI to Windows-only — remove/disable the Mono Ubuntu/macOS/Docker jobs | ✅ DONE | 059768c7 |
| DM-05 | Run NUnit unit **and** integration on every PR push (not just nightly); upload all result artifacts | ✅ DONE | 94140374 |

### Phase 1 — Solution compiles on .NET 10

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-10 | Flip every `*.csproj` `TargetFramework` `net48` → `net10.0-windows` (Windows Desktop SDK) | ✅ DONE | 7e031458 |
| DM-11 | Replace the `lib\PowerShell\System.Management.Automation.dll` references with `Microsoft.PowerShell.SDK` (in `chocolatey.csproj` and `Chocolatey.PowerShell.csproj`) | ✅ DONE | 379453ea |
| DM-12 | Remove binding-redirect props; move `choco.exe.manifest` to `<ApplicationManifest>`; drop `Microsoft.Bcl.HashCode`; set modern `<LangVersion>` | ✅ DONE | 36d395e9 |
| DM-13 | Dependency audit: confirm net-compatible builds of `Chocolatey.NuGet.PackageManagement`, `Rhino.Licensing`, `log4net`, `SimpleInjector`, `System.Reactive` | ✅ DONE | 60650026 |
| DM-14 | Resolve remaining compile errors until `build.bat --target=CI` builds the solution. **Gate: solution builds** | ✅ DONE | a2f6227d |

#### Phase 0–1 implementation notes

- **Per-push CI target.** The `CI` Cake target runs the net48-specific ILMerge + MSI
  packaging (Phases 5 & 8). Until that is reworked, the per-push job (`build.yml`) runs
  `--target=test` (build + unit + integration) so the Phase 1–4 gates can be **green**.
  `--target=CI` is restored with the self-contained publish in **DM-50**.
- **DM-11 module reference.** The `Chocolatey.PowerShell` binary module references the
  compile-only `PowerShellStandard.Library` (not the full SDK) so it binds to whatever
  `System.Management.Automation` the host loads it into (PS7 in-process, or WinPS 5.1 for
  the Phase 7 fallback). The full `Microsoft.PowerShell.SDK` is on the host `chocolatey.dll`.
- **DM-14 net10 API fixes.** Removed a stray `using System.Web.UI`; replaced FIPS-only
  `SHA*Cng` with `SHA*.Create()` (OS/CNG-backed and FIPS-compliant on .NET); switched the
  static `Directory.Get/SetAccessControl` to `DirectoryInfo` (`FileSystemAclExtensions`);
  removed obsolete CAS attributes (`HostProtection`/`SecurityPermission`) from the benchmark.
- **Deferred-but-compiling.** `ObjectExtensions.DeepCopy` still uses `BinaryFormatter` with
  `SYSLIB0011` suppressed — it **compiles but throws at runtime**; real fix is **DM-20**.
  (`AlphaFS` was the last net48 NU1701 dependency; removed in **DM-21** — now resolved.)
- **First CI result (run `26418337603`, `workflow_dispatch` on the fork).** `DotNetBuild`
  reported **`Build succeeded. 324 Warning(s) 0 Error(s)`** on `windows-latest` — the Phase 1
  gate is **green**. The job is red only in `DotNetTest` (Phase 2/3 content): unit run was
  986 passed / 64 failed before the **test host crashed** (DM-25). Failure buckets:
  `FieldAccessException` (DM-24), `PlatformNotSupportedException` (DM-20 BinaryFormatter +
  `Thread.Abort`), `InvalidOperationException` (DM-25). Note: the fork needed a one-time
  manual **Actions enable** before `push`/`workflow_dispatch` would run.

### Phase 2 — NUnit unit tests green

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-20 | Replace `BinaryFormatter` in `ObjectExtensions.DeepCopy` (`src/chocolatey/ObjectExtensions.cs`); audit all `DeepCopy()` callers | ✅ DONE | 7d9b0eb9 |
| DM-21 | Replace `AlphaFS` with `System.IO` (+ targeted P/Invoke for junctions/hardlinks/ADS if used) — AlphaFS was only a net48 long-path fallback; .NET 10 System.IO + `longPathAware` manifest cover it, so all 16 sites collapsed to System.IO and the package was dropped. No junctions/hardlinks/ADS were used, so no P/Invoke needed. Validated: unit 1188/0, integration 1463/0 | ✅ DONE | 8d9a7657 |
| DM-22 | Migrate `AppDomain.AssemblyResolve` → `AssemblyLoadContext.Default.Resolving` (`AssemblyResolution.cs`, `GetChocolatey.cs`, `PowershellService.cs`) — *not required for the unit gate; driven by Phase 3* | ❌ OPEN | |
| DM-23 | Migrate `chocolatey.tests` to `net10.0-windows`; fix unit failures. **Gate: NUnit unit suite green** | ✅ DONE | 23616cb8 |
| DM-24 | Replace reflection writes to `initonly` static fields in unit-test setup (e.g. `ApplicationParameters.AllowPrompts`) — .NET throws `FieldAccessException` (74 failures in the first CI run) | ✅ DONE | 25cf334c |
| DM-25 | Make `ReadKeyTimeout`/`ReadLineTimeout` safe under a headless/redirected console — `Console.ReadKey` throws `InvalidOperationException` and **crashes the test host**, aborting the run | ✅ DONE | e701686e |
| DM-26 | Register `CodePagesEncodingProvider` so `Encoding.GetEncoding(1252)` works (the non-ASCII password workaround in `ChocolateyNugetCredentialProvider` fell back to UTF-8 on .NET) | ✅ DONE | 7ca709ac |

### Phase 3 — NUnit integration tests green

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-30 | Migrate `chocolatey.tests.integration` to `net10.0-windows`; update `*.exe.config` `supportedRuntime` fixtures. Unblocked the suite: `NUnitSetup.FixApplicationParameterVariables` wrote ~12 `initonly` `ApplicationParameters.*` location fields by reflection → `FieldAccessException` failed **all 1567** tests; made the fields settable + direct-assign (integration analog of DM-24) | ✅ DONE | a3848e5b |
| DM-31 | Validate the PS7 in-process host: rewrite/remove the private-field output-redirection hack (`PowershellService.cs`); fix the `WindowsPowerShell\` profile-path assumption — *Install/Upgrade scenarios pass on PS7, so this isn't blocking the suite; revisit as cleanup* | ❌ OPEN | |
| DM-32 | Fix integration scenario failures. **Gate: `--testExecutionType=all` green** — **CI green (run `26423531366`): unit 1188/0, integration 1463/0, 104 skipped, build 0 errors.** Root cause of the last 20: `ConfigurationBuilderSpecs` proxy tests set real `HTTP_PROXY`/`HTTPS_PROXY` env vars (via `EnvironmentSettings.SetEnvironmentVariables`, value `EnvironmentVariableSet`) and never restored them; **.NET honors those env vars in `HttpClient.DefaultProxy`/NuGet `ProxyCache`** (.NET Framework did not), so later NuGet HTTP routed through a bogus proxy → `FatalProtocolException`. Fixed by saving/restoring the proxy env vars around those specs (test isolation). Not a product bug | ✅ DONE | f0360c3f |

### Phase 4 — Pester E2E suite green under PowerShell 7

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-40 | Port the Chocolatey helper module (`chocolateyInstaller.psm1` & helpers) to be PS7-clean. **Partial:** fixed `Test-ByteOrderMark.ps1` (was using `Get-Content -Encoding Byte`, removed in PS6+) — cleared 41 BOM-related failures. Rest of the helper-module PS7 cleanup is part of the long tail | 🔧 IN PROGRESS | 9802a4fa |
| DM-41 | Seed in-repo `tests/pester-tests/commands/testpackages/*` into the `hermes` source via `Invoke-Tests.ps1` (parity with Test Kitchen). **CI run `26457532325` confirms:** total grew 3966 → 4126 (+160 newly-runnable test cases that used to be skipped with "package not found"), failures 154 → 148 (net −6). The seeded contexts now exercise real install/uninstall paths on the migrated net10 choco.exe. *(Old DM-41 — `wget`/`curl` alias shims — folded into DM-44.)* | ✅ DONE | 7b69c686 |
| DM-42 | Provide the `Import-Module -UseWindowsPowerShell` / `Import-WinModule` path for WinPS-only modules | ❌ OPEN | |
| DM-43 | Add a **Pester E2E CI job**: `pwsh -File Invoke-Tests.ps1` against the built `chocolatey.*.nupkg` (excluding `Licensed`/`CCM`/`VMOnly` tags as today). Run under PowerShell **7.6.2** (.NET 10) — matches the host's `Microsoft.PowerShell.SDK 7.6.2`; the runner's pre-installed PS 7.4 (.NET 8) cannot load `Chocolatey.PowerShell.dll`. Runner parses `testResults.xml` and exits non-zero on Pester failures | ✅ DONE | e2e31e8e |
| DM-44 | Make the full Pester suite pass under PS7. **Gate: Pester E2E green under PS7**. **CI baseline after DM-41 (run `26457532325`, PS 7.6.2 / .NET 10): 3170 passed / 148 failed / 367 skipped / 441 not-run of 4126** (effective pass rate excluding skipped/not-run: 95.5%). No test files broken (`FailedContainers: 0`). Refined categorization below | 🔧 IN PROGRESS | |

#### Phase 4 remaining clusters (local baseline run, 2026-05-26)

Failures (115 logged `[-]` lines, 154 in `testResults.xml`) break down as:

| Bucket | Approx. count | Root cause | Resolution path |
|---:|---:|---|---|
| **Source seeding (in-repo)** | ~10 | `zip-log-disable-test`, `packagewithscript`, `chocolatey-dummy-package`, `too-long-*` had nuspecs in the repo but were never packed by `Invoke-Tests.ps1` outside Test Kitchen | **Cleared by DM-41** (`7b69c686`) — also unlocked +160 new test cases |
| **Source seeding (external)** | ~30–40 | `test-environment`, `test-params`, `wget`, `uninstallfailure` are *not* in the repo — supplied by the Test Kitchen `chocolatey-test-environment` provisioning. Tag audit: **9 Describe/Context blocks** in `choco-info`, `choco-install`, `choco-uninstall`, `choco-upgrade`, `EnvironmentVariables.Tests.ps1` install these packages but are tagged `CCRExcluded` / `Arguments, CCRExcluded` / untagged — none are `Internal` (which `Invoke-Tests.ps1` excludes). 5 sibling blocks *are* tagged `Internal` for the same packages, so this is upstream tagging inconsistency | Either (a) seed those packages into `tests/packages` for local/CI runs, or (b) add `Internal` tag to the 9 blocks — defer pending upstream-policy decision |
| `Should be appropriately signed` | 3 | Unsigned dev build — pre-existing; not a PS7 bug | Skip or scenario-gate on `IsOfficialBuild` |
| `Should not have been able to delete the rollback` | 3 | File-lock semantics during rollback | Real PS7/.NET 10 investigation |
| **Long tail** | ~65–95 | Mix of install/upgrade scenario behaviors (output shape, exit code, env-var visibility, path resolution) — most likely PS7 helper-module fixes (DM-40 follow-up) | Pattern-by-pattern locally — the iteration loop is now ~minutes per fix, not ~30 min per CI cycle |

The biggest single insight: ~40 of the ~148 failures (~27%) are **test-corpus
seeding gaps**, not product bugs. DM-41 closes the in-repo half (proven by CI);
the external half is a deliberate Test Kitchen dependency that the migration
shouldn't own. The remaining "real" failures are the actual Phase 4 work — no
single dominant root cause, so each fix is its own per-pattern investigation.

### Phase 5 — Self-contained publish + clean-environment smoke

| ID | Task | Status | Commit |
|---|---|---|---|
| DM-50 | Replace the ILMerge step in `recipe.cake` with `dotnet publish` self-contained single-file (`-r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true`, Desktop runtime). ILMerge disabled; `InstallLocation` rewritten to `Environment.ProcessPath` for single-file (`e1b0adae`). **CI run `26426612145` published the self-contained choco.exe and packaged `chocolatey.2.4.0-net10migra-11.nupkg`.** | ✅ DONE | 45511a51 |
| DM-51 | Rework the "ILMerged into chocolatey" branch in `AssemblyResolution.cs` (nothing is merged now) | ✅ DONE | 09aa1bf8 |
| DM-52 | Update all hardcoded `/net48/` paths in `recipe.cake`; `chocolatey.lib` `lib/net48` → `lib/net10.0` (no `net48` left outside the Cake-recipe tool package) | ✅ DONE | 45511a51 |
| DM-53 | Add a **clean-container smoke CI job**: run the self-contained `choco.exe` in a `servercore` Windows container with **no .NET 10 runtime installed** (`choco --version`/`list`/`source list`) | ✅ DONE | bdaea5f2 |
| DM-54 | Assert no `net48` artifacts remain. **Gate: smoke green — choco runs with nothing installed** — **GREEN: smoke runs `choco` in a no-runtime Server Core container; `--target=CI` builds net10 nupkgs only (no `net48`/no ILMerge).** | ✅ DONE | 45511a51 |

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
- **No fork-internal PR** — CI runs automatically on every push (`build.yml` triggers on `push`),
  so the branch alone gets full validation.
- Each phase's **Gate** task must be **green on `windows-latest`** before advancing. Never merge red.
- A PR to upstream `chocolatey/choco` is a **separate, explicit step** to take only once everything
  is complete and only when the user asks for it.
