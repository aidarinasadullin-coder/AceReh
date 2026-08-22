# Phase 3.1 Task 11 Final Dossier and Protected Preimage Comparison

- Date: 2026-08-20
- Scope: factual evidence synchronization after Tasks 7-10 plus F1 remediation of the public-load regression, post-load public User edit coverage, and protected test/helper attribution.
- Result: `GREEN` for the remediated Task 11 documentation and protected-boundary gate. Final F1-F4 and owner result acceptance remain separate pending gates.
- Workflow transition: `executing` -> `final-verification` only.

## Implementation Receipt

Worker-owned Phase 3.1 production changes are confined to the approved six-file ceiling:

- `ClimateMutationOrigin.cs`: distinguishes `UserReset` from `ProjectLoadReset`.
- `ProjectSessionClimateState.cs`: keeps `CompleteMutation` as policy boundary; only `User` and `UserReset` publish compatibility `DataChanged` and dirty state, while changed lifecycle origins still synchronize `CalculationContext`.
- `ClimateData.cs`: keeps one `ApplyProjection` path and makes compatibility publication explicit.
- `ProjectLoadOrchestrator.cs`: routes pre-load reset to `ProjectLoadReset`; its other Construction-related hunks are pre-existing Phase 3 Construction content and are not attributed to Phase 3.1.
- `ClimateViewModel.cs`: routes both user reset entrypoints to `UserReset`.
- `MainViewModel.cs`: routes new-calculation reset to `ProjectLoadReset`; its other Construction-related hunks are pre-existing Phase 3 Construction content and are not attributed to Phase 3.1.

`src/ViewModels/Thermal/ThermalViewModel.cs` is read-only and unchanged. No path outside the six-file ceiling is part of the worker implementation. No package, installer, publish output, formula, UI design, persistence schema, shared model, or widget was changed by Task 11.

## Executable Gates Reconciled

Task 9's historical receipt remains the authority for its original `76/76` runs. The F1 remediation added one focused test, so the current authoritative focused Debug and Release artifacts are `phase-3.1-f1-final-focused-debug.trx` and `phase-3.1-f1-final-focused-release.trx`: each reports `total=77`, `executed=77`, `passed=77`, `failed=0`, aggregate `notExecuted=0`, and no explicit `NotExecuted` result rows. User resets use `UserReset`; pre-load and new-calculation resets use `ProjectLoadReset`; restore uses silent `Load`.

Task 10 receipt `task-10-executable-gates.md` records:

- Debug build and Release build: exit `0`, `0` warnings, `0` errors each.
- F1 remediation supersedes those historical totals with `phase-3.1-f1-final-affected-release.trx`: `total=343`, `executed=342`, `passed=342`, `failed=0`, aggregate `notExecuted=0`; accepted explicit `NotExecuted`: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.
- F1 remediation supersedes those historical totals with `phase-3.1-f1-final-full-release.trx`: `total=1739`, `executed=1736`, `passed=1736`, `failed=0`, aggregate `notExecuted=0`; accepted explicit `NotExecuted`: `RegenerateCircuitsBaseline`, `RegenerateBaseline`, `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.
- TRX aggregate `notExecuted=0` and explicit result rows are retained as two adapter representations without normalization.

## Binary-Safe Protected Comparison

Boundary: `task-7-baseline/preimage-metadata.txt`. Commands actually run were PowerShell `Get-FileHash -Algorithm SHA256` on exact bytes, `$env:GIT_MASTER='1'; git status --porcelain=v1`, `$env:GIT_MASTER='1'; git diff --name-only`, and `$env:GIT_MASTER='1'; git diff --binary --no-ext-diff --exit-code -- src/ViewModels/Thermal/ThermalViewModel.cs`. No staging, reset, checkout, clean, revert, or commit was performed.

| Protected path | Task 7 preimage SHA-256 | Final SHA-256 | Classification |
|---|---|---|---|
| `src/Services/Project/ClimateMutationOrigin.cs` | `A27791CB886335A56403E94A402FC49C5E2DC5583E833327DDC9E7AFF7FBA691` | `E5A0DC1D1BF7603C96ADBE5FD9D01B20BD9F08D1F28AC5B49A23CF7AE8F5E472` | worker-owned Phase 3.1 |
| `src/Services/Project/ProjectSessionClimateState.cs` | `BCCF7A6D18DC3D08F1A7369EF3590E89E1932C56A6C82C250296B101E65D0FED` | `D468ABD5E613B1A06B91677556E490EB582F59ED539788D707B7A0B0C43C57F5` | worker-owned Phase 3.1 |
| `src/Models/Climate/ClimateData.cs` | `ED5CDD0B88A92FFE3449AAEBA0A835C2FF9771B3D190D0D78E0FE9227740EB6D` | `4BF2E3E84895C41BA2061E97D39B544A1404025F1E89A631DB4AD6794B3E89F6` | worker-owned Phase 3.1 |
| `src/Services/Project/ProjectLoadOrchestrator.cs` | `4EE41EF1BFABA4D84B604063FA7366F32F625AA3D6BCD4CBCE3F63819F9B9549` | `68F331862D5081B8870F25B3F18B9A36EEA5436DDDE56BB4ADC3B27D14876A3A` | Climate delta plus pre-existing Construction hunks |
| `src/ViewModels/Climate/ClimateViewModel.cs` | `D14A7C69555169E1BEEE70EC44BF984F0F034651FB34888F364D59E4E4B9370A` | `98EDC97FC62D3BC4680D67FE84B5E5B9C17F263CA35FA76BCC7CD8E070CF92D6` | worker-owned Phase 3.1 |
| `src/ViewModels/Shell/MainViewModel.cs` | `7EFC382D4C8CA8D962DD9CE98E8CF97010F439AECE54617AD4D5F3A8093AAD57` | `8DDDC9BF3915261706855B88FC90266C6F70490DECF6C3BB75653CE5DB43ADBC` | Climate delta plus pre-existing Construction hunks |
| `src/ViewModels/Thermal/ThermalViewModel.cs` | `27334159C03405747F7488116D23ED7FDF24F5769FC44F202C4B7622FF4411D2` | `27334159C03405747F7488116D23ED7FDF24F5769FC44F202C4B7622FF4411D2` | byte-identical protected reference |

The changed production paths are a subset of the six-file ceiling; Thermal has no binary diff. Existing dirty files and unrelated hunks were neither overwritten nor claimed as Phase 3.1 output.

## Protected Test and Helper Preimage Reconciliation

The comparison below uses exact Task 7 SHA-256 entries where captured. For the helper category on plan line 90, Task 7 did not capture a helper hash; the binary-safe `status.manifest` and `unstaged.manifest` instead prove that `ResultsViewModelTestHelpers.cs` was already modified before Phase 3.1 RED work. `staged.manifest` is exactly zero bytes. This limitation is stated rather than inventing a preimage hash.

| Protected test/helper path | Task 7 evidence | Final SHA-256 | Attribution |
|---|---|---|---|
| `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateThermalInvalidationRegressionTests.cs` | `MISSING` | `0B45931890189E96216D99C12C7E25448B98B66F2C59A20063C8265162051768` | Entire file is Phase 3.1-owned. F1 remediation changes the primary regression to `ResultsViewModel.LoadProjectDataAsync()` and adds repeated public `ClimateViewModel.WindSpeed` User edits after successful loads, asserting one `User` completion, one compatibility publication, one context update, one Thermal invalidation, and dirty state per edit. |
| `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateStateTests.cs` | `57F9209697D6E957FA1B8D597955AC77BF0BE35F5D91C81CE6C76E3B01CFBC33` | `4AEF27EF5A315CF7CA9ED23BED2EE1B1615B88A98251C0448535041AF5753A5B` | Phase 3.1-owned enum-routing delta: lifecycle reset assertions use `ProjectLoadReset`. |
| `tests/SnowMeltingCalculator.Tests/Climate/ClimateMultiplicityCharacterizationTests.cs` | `CAE51A120DC2A3FAD2010E6F8BDBC66AB3870166BDB47441A2B1282B051D7969` | `19DEB1F853064321A6A7FA7B26394DC1F6E5B91845B7583A4328D4DB0AAD2C9B` | Phase 3.1-owned characterization delta for completion counts/origins, compatibility multiplicity, and user-reset dirty semantics. |
| `tests/SnowMeltingCalculator.Tests/Climate/ClimateDataProjectionTests.cs` | `F1F254CC5BE83CC3FB88CD144768DBA146CFF2985D25CBE08A1F5FA6C6890195` | `F1F254CC5BE83CC3FB88CD144768DBA146CFF2985D25CBE08A1F5FA6C6890195` | Byte-identical, unchanged. |
| `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs` | `F5FAC3B687B939264202B27F437FB62053141720418E61B36357689FD47D2FF6` | `ACF606131D30DE19B8076A9C0E87093A2076EB2180D993FAB2B50A09A9447E5C` | Mixed. The renamed user-reset test and `MarkDirty()` expectation are Phase 3.1-owned. Construction initializer/session wiring hunks were already present in Task 7 `status.manifest`/`unstaged.manifest` and are pre-existing Phase 3 Construction work. |
| `tests/SnowMeltingCalculator.Tests/ViewModels/MainViewModelTests.cs` | `CA58706C476C6F0C3F4BD53ACACD58C5F88B8700E94471123329B1CCB09C3630` | `328150C253E6798658EA93D8F5F9FD22CD5FC4685071FE6EBB250EC94A8EDCB4` | Mixed. `NewCalculation_ChangedClimateReset_SynchronizesOnceWithoutCompatibilityThermalOrDirty` is Phase 3.1-owned. Construction default-reset test, DI imports, constructor/helper wiring, and material setup are pre-existing Phase 3 Construction hunks, with the path already dirty in both Task 7 manifests. |
| `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs` | `9BAE304ACA2DA23E6AB8682C225A927FB34CDB1C918C2101C1948CABEAC7EA84` | `9BAE304ACA2DA23E6AB8682C225A927FB34CDB1C918C2101C1948CABEAC7EA84` | Byte-identical relative to Task 7. Its Construction-related git hunks predate Task 7 and were already present in both manifests; Phase 3.1 added no delta. |
| `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs` | helper category, no Task 7 hash; path present in `status.manifest` and `unstaged.manifest` | `B72EB9E9D9DB02F0E16B33B3FCB5361A52E28D54F5C5A90C4299CD10522A040A` | Pre-existing unrelated Phase 3 Construction helper hunks. F1 remediation did not edit this helper; the real Results graph was constructed inside the new Phase 3.1 regression fixture. |

Reproducible patch-level separation for the two mixed production paths comes from `$env:GIT_MASTER='1'; git diff --unified=3 -- src/Services/Project/ProjectLoadOrchestrator.cs src/ViewModels/Shell/MainViewModel.cs`: in `ProjectLoadOrchestrator.cs`, only `ClimateMutationOrigin.Reset` to `ProjectLoadReset` is Phase 3.1-owned; Construction state fields, initializer constructor parameter, default application, adapter projection, snapshot builder, and legacy loader rename are pre-existing Construction hunks. In `MainViewModel.cs`, only that same Climate origin replacement is Phase 3.1-owned; Construction state fields, initializer constructor parameter, default application, and adapter projection are pre-existing Construction hunks.

The protected-path check is therefore preimage-relative and hunk-specific. It does not infer ownership from the repository's global dirty state.

## F1 Remediation Executable Evidence

Commands were run after the final C# write:

- Focused Debug/Release: exact plan filter for `ClimateThermalInvalidationRegressionTests|ClimateStateTests|ClimateMultiplicityCharacterizationTests|ClimateDataProjectionTests|ResetOrchestrationTests|MainViewModelTests`; both `77/77` passed.
- Affected Release: exact plan filter for `Climate|ThermalViewModelTests|CalculationStateServiceTests|ResultsViewModelOpenProjectTests|ProjectLifecycleFlowCharacterizationTests|ResetOrchestrationTests|MainViewModelTests`; `343` total, `342` executed/passed, zero failed, one accepted explicit `NotExecuted` identity.
- Full Release: `1739` total, `1736` executed/passed, zero failed, the three accepted explicit `NotExecuted` identities listed above.
- `dotnet build src/SnowMeltingCalculator.csproj -c Debug --nologo --no-restore` and Release equivalent: exit `0`, zero warnings, zero errors.
- C# LSP diagnostics were attempted with absolute and workspace-relative paths. The tool failed before analysis with `LSP file path must be inside request cwd` and then `Working directory does not exist: C:\Users\Admin\tests\...`; compiler and executable gates are the authoritative diagnostics.

## Approved Plan Identity

Canonical and tracking copies remain exact: `docs/architecture-migration/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md` and `.omo/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md` are each `53357` bytes with SHA-256 `355A81BD354EF3E3F0A4636C154DA27EB2C596FFA9F14BA4EBE1757FCAD4D0C9`. The stale `.omo/plans/phase-3-construction-state.md` was not edited.

## Six-View and Workflow Result

The six views remain separate filters. Compile-time records origin/publication type references; DI/runtime records registrations and lifetimes verified unchanged; state ownership records canonical Climate ownership and projection-only compatibility; reactive records origin-aware publication and one context path; persistence records unchanged `.smc` fields/version; user flow records reset/load/second-load behavior and gates. No shared model or deterministic widget regeneration was required because no model/schema input changed.

Task 11 evidence is recorded, but Phase 3.1 is not claimed complete or owner-accepted. The next action is F1-F4 in parallel; owner result acceptance remains pending.
