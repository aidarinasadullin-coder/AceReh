# Slice 3 — Full regression after the DEC-006 save-wire change

Phase 11 (`phase-11-migration-tails-closure`). Write-set: evidence only.

## Command (plan-exact) and result

```
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --logger "trx;LogFileName=slice-3-full-regression.trx" --results-directory "docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs"
```

Result: **2040 passed / 0 failed / 1 skipped** in the counted totals (TRX
carries 2043 entries: the counted tests plus the two `[Explicit]` tooling
entries `RegenerateBaseline`/`RegenerateCircuitsBaseline`, unchanged from
prior phases). Duration 41 s. TRX: `logs/slice-3-full-regression.trx`.

## Acceptance checks

- **0 failed.**
- **Exactly 1 counted skip — the known RR-004 external fixture**
  (`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`,
  message: «F5 smoke fixture не найден: `D:\IA\ace\Тест\тест 40.smc`»).
  Recorded as a skip, never as a pass; RR-004 stays a preserved limitation.
- **Test-count delta vs Phase 10 (2050 → 2040, −10), fully explained by
  DEC-006's owner-blessed removal of outdated tests** (per-file `[Test]`
  counts, git HEAD vs worktree):

  | File | Δ | Reason |
  |---|---|---|
  | `ProjectSnapshotContractTests` | 21 → 13 (−8) | catalog-record contract tests removed together with the removed snapshot members; DEC-006 absence guards added |
  | `ConstructionServiceTests` | 34 → 32 (−2) | two catalog-embedding round-trip tests removed (save no longer embeds catalogs) |
  | `ResultsViewModelOpenProjectTests` | 45 → 44 (−1) | two catalog-open tests merged into one DEC-006 guard |
  | `ProjectSnapshotFactoryTests` | 2 → 3 (+1) | rewritten read-once test + null test kept + new hash-pin test |
  | `ProjectPersistenceMapperTests` / `ProjectSaveServiceTests` | 0 | replacement 1:1 / signature-only |

  Net: **−10** — matches the run exactly. No unexplained drift.
- **No `.smc` fixture changed** — `git diff --name-only -- '*.smc'` empty.
- Old `.smc` files carrying `custom_materials`/`custom_templates` JSON keep
  loading (unknown members ignored; re-pinned in
  `ConstructionServiceTests.ProjectData_DeserializesOldFileWithoutCustomTemplates`).

**SLICE 3: PASS**
