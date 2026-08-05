# Phase 1 Task 10 - Shared architecture dossier and widget update

## Scope

Update the shared architecture model, all six filtered views, coverage overlays,
and the architecture widget after the Task 9 implementation gates. This receipt
does not run or approve Final Verification Wave F1-F4.

## Commands and results

### 2026-08-05 re-check after nested restore lease correction

Task 10 artifacts were re-checked after the blocking
`ProjectSession.BeginProjectRestore()` nested lease defect was fixed. The model,
maps, generator and generated widget remained factually correct; no rewrite was
required beyond this stale-evidence update.

```bash
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-1-project-session-shell/task10-model-v2-recheck.json
```

Exit `0`: `PASS`, 33 assertions, 21 mutations.

```bash
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-1-project-session-shell/task10-runtime-v2-recheck.json
```

Exit `0`: `PASS`, 47 assertions (37 positive + 10 negative), 20 mutations.

```bash
node docs/architecture-migration/widget/generate-widget.mjs --check
```

Exit `0`: 14/14 checks pass. Canonical/generated widget SHA-256 remains
`9C5188BAC257D9CAC51045C0D2186D03A4A2E6B92AFFBF0519B3B3737BBCED9F`.

Correction gates that revalidated Tasks 4-9 before this Task 10 re-check:

- Targeted owner/guard lane: 43 passed, 0 failed, 0 skipped.
- Affected lifecycle lane: 83 passed, 0 failed, 1 pre-existing skipped test.
- Persistence lane: 18 passed, 0 failed, 0 skipped.
- Debug production build: 0 warnings, 0 errors.
- Release production build: 0 warnings, 0 errors.
- Full Release suite: 1568 passed, 0 failed, 1 pre-existing skipped test.

```bash
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output $env:TEMP\phase1-task10-model-validation.md
```

Exit `0`: `PASS`, 33 assertions, 21 mutations.

```bash
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output $env:TEMP\phase1-task10-runtime-validation.md
```

Exit `0`: `PASS`, 47 assertions (37 positive + 10 negative), 20 mutations.

```bash
node docs/architecture-migration/widget/generate-widget.mjs
node docs/architecture-migration/widget/generate-widget.mjs --check
```

Exit `0`: generated `docs/architecture-migration/architecture-widget.html`;
`--check` passed all 14 checks including `overview-projectsession-migration`,
`lifecycle-and-four-slices`, and `projectsession-lifecycle-implemented`.

```bash
dotnet build "src/SnowMeltingCalculator.csproj" -c Debug
```

Exit `0`: `0 warnings, 0 errors`.

```bash
git diff --check
```

Exit `0`: no whitespace errors. Git printed only CRLF conversion warnings for
the pre-existing dirty worktree.

## Updated artifacts

Production-code changes (unchanged since Task 9):

- `src/Services/Project/IProjectSession.cs`
- `src/Services/Project/ProjectSession.cs`
- `src/Configuration/ServiceCollectionExtensions.cs`
- `src/Services/Navigation/CalculationStateService.cs`
- `src/Services/Results/ProjectStateService.cs`
- `src/ViewModels/Results/ResultsViewModel.cs`

Dossier and widget updates:

- `docs/architecture-migration/maps/architecture-model.json`
- `docs/architecture-migration/maps/compile-time.md`
- `docs/architecture-migration/maps/di-runtime.md`
- `docs/architecture-migration/maps/state-ownership.md`
- `docs/architecture-migration/maps/reactive.md`
- `docs/architecture-migration/maps/persistence.md`
- `docs/architecture-migration/maps/user-flow.md`
- `docs/architecture-migration/maps/state-inventory.md`
- `docs/architecture-migration/maps/characterization-tests.md`
- `docs/architecture-migration/maps/persistence-compatibility.md`
- `docs/architecture-migration/maps/target-invariants.md`
- `docs/architecture-migration/widget/generate-widget.mjs`
- `docs/architecture-migration/architecture-widget.html`
- `docs/architecture-migration/TASK_CONTEXT.md`

## Hashes

| File | SHA-256 |
| --- | --- |
| `maps/architecture-model.json` | `0D2314C5CD5EEFA6B2DCA224256B7EA359F0115AEA37F070BE98C2E3B9FF4F87` |
| `maps/architecture-model.widget.schema.json` | `8A0BC79C00FBD8F1D2C2E52E70085DF0472E3675B99B9C5EC9209FA8EEB4C97B` |
| `widget/architecture-widget.mjs` | `F2A4F047156810FC23BC32F1F23496E5F0495ABCCBEBF969453D4DF825F12E7B` |
| `widget/verify-widget.mjs` | `2D281D7C46ADFD987F26227BBF8E2EDD15B1DBC1EDBEC213BB98DEAD7E7C5314` |
| `widget/generate-widget.mjs` | `8CF9A20EB4B3EB864C15916C0882BF5BBAF65E00941A81242D0FA8EC8297A065` |
| `architecture-widget.html` | `9C5188BAC257D9CAC51045C0D2186D03A4A2E6B92AFFBF0519B3B3737BBCED9F` |
| `maps/compile-time.md` | `C4E314844D1BF94B16763358B11E53826CAF614706DA38E42B4958C2E2F892A6` |
| `maps/di-runtime.md` | `F4DC2DCC84C563152EEE90898AE7AB85AD1FB34EB3B49A0A786CAFE8DA6F5D34` |
| `maps/state-ownership.md` | `A2E6386D6A69EC64CC6430864B2A4FDBDAA3D06B5FCA32A7B2D9C25C6F2C86F9` |
| `maps/reactive.md` | `010F464007B0CEBEA2C64A65BCF3C70D3E2B0D46CD572D07C5E8F56361F5C787` |
| `maps/persistence.md` | `E65B023AF0B2C20259AB0C117F7961F3A2E24115BC1B330DCC78529C9C16910C` |
| `maps/user-flow.md` | `080F0E45E1EF9A7BD94BC977F0676BEE32C2DA48C68926D9B6C3423B232F6577` |
| `maps/state-inventory.md` | `32B6A5739C7B0F076CE931EFD7A6BCE89223AFD7665953FF20945EF767834156` |
| `maps/characterization-tests.md` | `2A61D41A570D00DA8A2CA3905653A5804E803B2FC8846DF53FB172D45566E764` |
| `maps/persistence-compatibility.md` | `DAABB35A944624F6ACA1B30966E66AD7205A17C4ECC5DAEEA50252FD54933CEF` |
| `maps/target-invariants.md` | `4B2216FD2812C4D86A13D75759DEB4E120A9750D6C16A0FA57EEDCDF1E2CB06D` |

## Model changes

- Updated metadata to `phase-1-project-session-shell` with HEAD
  `021d4abd159aa71c4a19c7a6536851264e5a58ca`.
- Added 10 Phase 1 evidence entries (`EV-P1-*`).
- Added `INV-P1-001` for verified lifecycle shell ownership.
- Migrated state records `ST-001`, `ST-002`, `ST-004`, `ST-005` current owner to
  `ProjectSession` with `migrated` status and `covered` coverage.
- Added node records `DRN-PS` (`ProjectSession`) and `DRN-IPS` (`IProjectSession`).
- Added 9 Phase 1 edges for DI registration, resolution, lifecycle state
  read/write, and `ProjectSession.PropertyChanged`.
- Updated `LIM-003` to note that the lifecycle shell is implemented while module
  slices remain target-only.

## Widget check output

```text
PASS html-non-empty-utf8
PASS one-embedded-payload
PASS accepted-identity
PASS overview-projectsession-migration
PASS current-target-diff
PASS six-accepted-views
PASS diff-stable-id-and-direction
PASS projectsession-lifecycle-implemented
PASS lifecycle-and-four-slices
PASS eight-core-flow-groups
PASS displayed-references-resolve
PASS no-external-network-runtime-dependencies
PASS two-in-memory-builds-byte-identical
PASS check-does-not-change-canonical-html
PASS count 14
```

## Remaining limitations

- Climate/Construction/Thermal/Hydraulics state slices remain in their existing
  owners; a later separately approved slice must migrate them into
  `ProjectSession.*State` slices.
- `CalculationContext` is unchanged and still mediates module interactions.
- Transactional restore, rollback, `.bak` recovery, schema/version policy changes,
  and UI/package/installer changes remain out of scope.
- `lsp_diagnostics` could not run for `widget/generate-widget.mjs` because this
  harness rejected the absolute documentation path as outside its request cwd.
  Node executed the generator and both validator suites successfully instead.
- The first generator call using explicit `--model`, `--schema`, and `--output`
  arguments was rejected by its documented no-argument/`--check` interface; the
  supported generation and deterministic check commands above were then used.

## Next step

The parent orchestrator may independently verify Task 10 and then decide whether
to start F1-F4. This task did not start, mark, or approve that final wave.
