# Phase 5 Correction Notes

Plan SHA: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`

The correction lane updated affected characterization fixtures to construct one
shared `ProjectSession` for the hydraulics adapter and the lifecycle/save
orchestrator. This removes the test-only split-session artifact and preserves
the canonical restore and save paths.

## Characterization integrity resolution

Provenance note: the source-grep form of the save-projection assertions
(`Does.Contain("BuildCanonicalSnapshot")`,
`Does.Contain("HydraulicsPersistenceMapper.BuildHydraulicsProjectData")`)
originated in committed `34b739e` (task-10), not in this correction lane.

Owner-directed resolution (2026-08-24): the
`SaveProjection_ContainsCurrentHydraulicsWireFieldsAndVersionLiteral` test was
rewritten as a real behavioral round-trip instead of either assertion form:

- representative project → production restore boundary
  (`ProjectLoadOrchestrator.RestoreModulesFromProjectAsync`) → canonical
  `ProjectSession.HydraulicsState.Snapshot`;
- projection through `HydraulicsPersistenceMapper.BuildHydraulicsProjectData`
  (the sole wire-format writer invoked by `ResultsViewModel.SaveCurrentProject`);
- serialization with the exact `System.Text.Json` options used by
  `ProjectFileService` for `.smc` files;
- assertions on the serialized payload for all eight wire fields with value
  checks (`glycolType=propylene`, `flowRegimeString=Laminar/Turbulent` on both
  results, `version=1.1`).

No assertion was weakened; all eight original wire fields plus the version
literal are checked against real serialized output.

Arrange-only fixture changes retain one shared `ProjectSession` and canonical
coordinator wiring for the adapter, lifecycle orchestrator, and save fixture.
The characterization suite passes `13/13`.

## Final gate battery (2026-08-24, lane resume after limit interruptions)

All gates re-run on the final source state, which additionally contains the DI
construction-cycle fix and four owner-adjudicated semantic test adaptations
(details in `task-9/divergence-notes.md`):

| Gate | Result |
|---|---|
| Debug production build | 0 warnings, 0 errors |
| Release production build | 0 warnings, 0 errors |
| HydraulicsMultiplicityCharacterizationTests | 13 passed / 0 failed |
| HydraulicsStateLegacyStoreGuardTests (H11) | 8/8 PASS (`task-11/trx-guards-release.json`, refreshed post-fix) |
| H3 integration set | 63 passed / 0 failed |
| H4 authority/cold-start set | 24 passed / 0 failed |
| Lifecycle/VM battery | 166 passed / 0 failed / 1 accepted skip |
| FULL Release suite | **1976 passed / 0 failed / 1 accepted skip** (`task-11/trx-fullsuite-final.json`) |

Accepted skip identity: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
(missing external fixture `D:\IA\ace\Test\test 40.smc`; unrelated to this
correction).
