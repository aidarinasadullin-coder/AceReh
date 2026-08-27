# Phase 6 Task 7 - Architecture Dossier Refresh

Date: 2026-08-26
Canonical plan: `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md`
Canonical plan SHA-256: `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`

## Scope

Refreshed the six architecture views and canonical widget model for the proven
save boundary only. The overlay is source-backed by the live snapshot, mapper,
save-service, DI and Results save adapter sources, plus Task 5/6 evidence.

## Proven chain

`ProjectSession -> ProjectSnapshot -> ProjectPersistenceMapper -> ProjectData -> IProjectFileService/ProjectFileService`.

The model records this as `PE-P6-SESSION-SNAPSHOT`, `PE-P6-SNAPSHOT-MAPPER`,
`PE-P6-MAPPER-DATA` and `PE-P6-SERVICE-DATA`. `ProjectData` remains a DTO;
`ProjectFileService` remains serializer/file I/O. Save failure returns the
existing failed result or exception behavior, and the Results adapter retains
the existing clean transition only after successful persistence.

## Six-view disposition

- `compile-time`: snapshot, mapper, DTO and save-service types are represented;
  service source has no ViewModel/WPF dependency.
- `di-runtime`: `IProjectSnapshotPersistenceInputs`, factory and save service
  registrations are source-backed in `AddResultsModule`.
- `state-ownership`: canonical module snapshots are read from `ProjectSession`;
  lifecycle path/dirty and restore ownership remain outside this save overlay.
- `reactive`: no new event, invalidation, recalculation or subscription edge is
  claimed; save is an action boundary.
- `persistence`: immutable snapshot assembly, pure mapping, unchanged `1.1`
  wire DTO and file-service delegation are represented.
- `user-flow`: save success/failure and clean-transition behavior is partial;
  headless environment prevents manual WPF button/dialog QA.

## Negative evidence and residual risks

Task 6 recorded `124 passed / 1 skipped / 0 failed / 125 total`, 28 tracked
`.smc` fixtures with valid hashes, and `STATUS=NOT_PRESENT` for a standalone
missing-edge process probe. Existing invalid-file/result tests pass. The
missing external fixture is `D:\IA\ace\Тест\тест 40.smc`.

`ProjectSnapshotPersistenceInputs.Templates` uses sync-over-async via
`GetAwaiter().GetResult()` and remains a residual risk. Restore migration,
transactional restore, calculation redesign, broad legacy-owner removal,
exports and Markdown removal are not claimed complete.

## Widget/model gates

The model keeps `contract_version: 2.0.0`, exactly six required views and the
existing `baseline/current/target` vocabulary. Required commands for this task:

```text
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
node docs/architecture-migration/widget/generate-widget.mjs --check
```

Invalid-ID and missing-evidence-edge fixtures are expected to reject with
nonzero exit codes. The generated widget must be byte-identical across two
clean generations.

Write-set: six maps, `architecture-model.json`, this dossier, and append-only
Phase 6 notepads. No production code, tests, fixtures, frozen plan, schemas,
workflow state or unrelated dirty paths were changed.

## Executed Gates

| Gate | Exit | Result |
| --- | ---: | --- |
| `verify-widget.mjs --suite model-v2` with explicit schema/model/output | 0 | PASS; 33 assertions, 21 mutations |
| `verify-widget.mjs --suite runtime-v2` with explicit schema/model/output | 0 | PASS; 47 assertions, 20 mutations |
| `generate-widget.mjs --check` | 0 | PASS; 14/14 checks; canonical/generated SHA-256 `2b9d48ed6dc3e15ff6622f3d56737ab31c2b3e67f20f2f95af061c0ebd472c3b` |
| two sequential `generate-widget.mjs` runs | 0, 0 | PASS; both outputs 15,945,248 bytes and byte-identical |

The first attempted `--check` ran concurrently with generation and failed while
the canonical HTML was being rewritten; the isolated rerun above is the gate
result.

## Negative Fixtures

Validator negative mutations passed in the mandatory suites. Standalone
invalid-ID and missing-evidence-edge process probes are not present:
`STATUS=NOT_PRESENT`; no fixture or test was created. Reused Task 6 evidence:
124 passed / 1 skipped / 0 failed / 125 total; 28 tracked `.smc` fixtures
unchanged.

## Correction Note — 2026-08-26

Independent review found that the initial Task 7 patch appended the Phase 6
Save-Boundary Overlay twice to each of the six required maps and carried one
unrelated duplicated `user-flow.md` line. The correction removed only the
second overlay block from each map and restored that unrelated line to its
pre-task single occurrence. Exact changed map count: 6. The first valid overlay,
model/widget content, and all unrelated user changes were preserved.

Fresh correction gates: explicit `model-v2` and `runtime-v2` validators exited
0 with 33/21 and 47/20 assertions/mutations; `generate-widget.mjs --check`
exited 0 with 14/14 checks; two sequential generations exited 0 and produced
15,945,248 bytes each with SHA-256
`2b9d48ed6dc3e15ff6622f3d56737ab31c2b3e67f20f2f95af061c0ebd472c3b`; and
`git diff --check` exited 0. No model, widget, schema, plan, production, test,
fixture, or unrelated path was changed by this correction.

## Correction Verification Note — 2026-08-26 (second pass)

A follow-up independent verification pass over the first correction found the
six maps already reduced to exactly one `## Phase 6 Save-Boundary Overlay`
section each (duplicate blocks removed; exact changed map count for Task 7
remains 6), but the unrelated `user-flow.md` line was not yet restored: it
still carried the patch's encoding substitution — UTF-8 bytes `D0 A7`
(Cyrillic "Ч") in place of the pre-task single byte `0x97` (em dash) in
"nine numbered steps <em dash> all PASS". This pass restored that byte exactly
(single occurrence, byte offset 12232; `D0 A7` -> `0x97`), so `git diff` for
`user-flow.md` now shows only the appended overlay block and no other delta.
The first valid overlay in every map, all model/widget content, and all
unrelated user changes were preserved.

Fresh gates rerun in this second pass with explicit arguments:

```text
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-0.5-acceptance-v2.json
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-0.5-acceptance-v2.json
node docs/architecture-migration/widget/generate-widget.mjs --check
node docs/architecture-migration/widget/generate-widget.mjs  (twice, sequential)
```

| Gate | Exit | Result |
| --- | ---: | --- |
| `verify-widget.mjs --suite model-v2` | 0 | PASS; 33 assertions, 21 mutations |
| `verify-widget.mjs --suite runtime-v2` | 0 | PASS; 47 assertions, 20 mutations |
| `generate-widget.mjs --check` | 0 | PASS; 14/14 checks; canonical SHA unchanged across check |
| two sequential `generate-widget.mjs` runs | 0, 0 | PASS; both outputs 15,945,248 bytes, byte-identical, SHA-256 `2b9d48ed6dc3e15ff6622f3d56737ab31c2b3e67f20f2f95af061c0ebd472c3b` |

`git diff --check` exited 0 after the restore. The canonical widget HTML was
byte-identical before and after this pass (SHA-256 above). The only working
tree deltas introduced by this pass are the `user-flow.md` byte restore, this
receipt section, and the Phase 6 notepad appends; no model, widget, schema,
plan, production, test, fixture, or unrelated path was changed.

## Independent Re-verification Note — 2026-08-26 (third pass)

A separate independent lane re-read the corrected artifacts and re-ran every
gate fresh. Confirmed by direct read and byte inspection: each of the six maps
contains exactly one `## Phase 6 Save-Boundary Overlay` section (`git diff`
shows exactly 4 inserted lines per map, 24 total), and `user-flow.md` matches
the HEAD blob byte-for-byte outside the appended overlay — the restored
pre-task line carries the original single byte `0x97` at offset 12232, so no
modified-line hunk remains. Fresh gates, rerun independently with explicit
schema/model/output arguments (`--schema
docs/architecture-migration/maps/architecture-model.widget.schema.json`,
`--model docs/architecture-migration/maps/architecture-model.json`, outputs
`task-7-correction-model-v2.json` / `task-7-correction-runtime-v2.json`):

| Gate | Exit | Result |
| --- | ---: | --- |
| `verify-widget.mjs --suite model-v2` | 0 | PASS; 33 assertions, 21 mutations |
| `verify-widget.mjs --suite runtime-v2` | 0 | PASS; 47 assertions, 20 mutations |
| `generate-widget.mjs --check` | 0 | PASS; 14/14 checks; canonical SHA unchanged across check |
| two sequential `generate-widget.mjs` runs | 0, 0 | PASS; both outputs 15,945,248 bytes, byte-identical, SHA-256 `2b9d48ed6dc3e15ff6622f3d56737ab31c2b3e67f20f2f95af061c0ebd472c3b` |

`git diff --check` exited 0. No model, widget, schema, plan, production, test,
fixture, frozen-plan, or unrelated path was changed by this re-verification;
its only writes are the two correction validator output JSONs, this receipt
section, and the Phase 6 notepad appends.

## Final Verdict

`TASK 7: PASS`
