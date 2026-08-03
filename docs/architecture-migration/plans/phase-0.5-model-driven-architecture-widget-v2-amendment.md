# phase-0-5-model-driven-architecture-widget-v2-amendment - Work Plan

## TL;DR (For humans)
<!-- Fill this LAST, after the detailed plan below is written, so it summarizes the REAL plan. -->
<!-- Plain English for a non-engineer: NO file paths, NO todo numbers, NO wave/agent/tool names. -->

**What you'll get:** A narrowly scoped v2 amendment that fixes the rejected architecture-widget data contract and Diff runtime, proves the accepted v1 facts survive migration, and safely returns execution to the already-approved widget work.

**Why this approach:** One stable record with snapshot-local values eliminates duplicate identity and makes real Current/Target changes representable. An amendment overlay preserves the approved plan and evidence instead of rewriting history.

**What it will NOT do:** It will not change the application, persistence, tests, current or historical widget, generated UI, packages, release files, or Phase 1. It will not install tools or silently accept the failed v1 runtime.

**Effort:** Large
**Risk:** High - schema migration, runtime identity, evidence preservation, and workflow gates form one coupled boundary.
**Decisions to sanity-check:** v2-only contract; valid-empty canonical Target; amendment F5 releases Task 5 but is not phase acceptance.

Your next move: complete the mandatory dual high-accuracy review, then import the approved Markdown under the repository-facing amendment path. Full execution detail follows below.

---

> TL;DR (machine): Large/high-risk amendment; five sequential implementation tasks plus five verifier gates; v2 schema/model/runtime and versioned evidence only.

## Scope
### Must have
- Repository-facing import target: `docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md`; preserve UTF-8/LF bytes, record source primary session and SHA-256 in `TASK_CONTEXT.md`, and make it immutable after owner approval.
- Preserve `docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget.md`, Phase 0 plans/maps/evidence, accepted v1 Task 3 receipt, and rejected Task 4 receipt as immutable history.
- Reopen only original Tasks 3 and 4 under contract `2.0.0`; release only original Task 5 after amendment F5.
- One runtime document; one globally unique ID; one singular `record_kind`; one `snapshot_states` source of presence, values, and snapshot-local provenance.
- The v2 document envelope retains accepted top-level `metadata`, `contract_version`, `snapshots`, `views`, `canonical_diff_fields`, `evidence`, `limitations`, `invariants`, and `deferred_decisions`; replaces the five v1 record-kind arrays with one required `records` array; and removes standalone `edge_semantics`. Top-level `snapshots` is vocabulary metadata (`baseline:observed`, `current:observed`, `target:unimplemented`), never record membership or presence authority. Unknown document-level keys reject. Every global dictionary retains its accepted v1 item schema unless the field-level mapping explicitly records a normalized change.
- Exact six views: `compile-time`, `di-runtime`, `state-ownership`, `reactive`, `persistence`, `user-flow` over one shared model.
- Lossless, field-level v1 migration reconciliation; honest valid-empty Target; model-driven Diff with `added|removed|changed|unchanged|unresolved`.
- Snapshot-local edge/reference and flow-order validation; immutable indexes and atomic replacement.
- Versioned, reproducible receipts with execution-time Git/tool/hash basis and independent acceptance.

### Future execution path allow-list
After reviewed-plan owner approval and separate execution authorization, execution may write only: `docs/architecture-migration/maps/architecture-model.widget.schema.json`, `docs/architecture-migration/maps/architecture-model.json`, `docs/architecture-migration/widget/model-contract.mjs`, `docs/architecture-migration/widget/architecture-widget.mjs`, `docs/architecture-migration/widget/verify-widget.mjs`, `docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot.md`, `docs/architecture-migration/evidence/phase-0.5-v1-to-v2-mapping.json`, `docs/architecture-migration/evidence/phase-0.5-model-validation-v2.md`, `docs/architecture-migration/evidence/phase-0.5-acceptance-v2.json`, `docs/architecture-migration/evidence/phase-0.5-v2-amendment-scope-gate.md`, the five F1-F5 receipt paths named below, and `docs/architecture-migration/TASK_CONTEXT.md`. The repository-facing amendment plan is imported by the planning workflow before execution and becomes protected after approval; it is not an implementation write.

### Supersession map
| Approved Phase 0.5 clause | v2 overlay | Disposition |
| --- | --- | --- |
| Task 3 top-level values plus `snapshots` | one record with `snapshot_states` | superseded |
| Task 4 membership indexes | indexes derived only from snapshot-state keys | superseded |
| Task 4 canonical comparison | executable model policy | superseded |
| Task 4 Diff precedence that treated unresolved identity/evidence before presence | presence first; unresolved only when both snapshot states exist | superseded |
| Task 5 direct v1 fields/membership | immutable active-snapshot projection API | amended handoff |
| Task 5 37 acceptance IDs and all other Tasks 5-10 constraints | unchanged | retained |

The amendment takes precedence only for these rows. The approved plan SHA and every other clause remain authoritative.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- No edits to production, application tests/fixtures, `.smc`, persistence, formulas, DI/ViewModels, current/historical/generated widget, browser/CSS/templates/screenshots, packages/lockfiles/configuration/commands, installer/publish/release, or unrelated dirty paths.
- No original Tasks 5-10 or original F1-F5 execution; no Phase 1 planning/execution; no owner gate crossed by inference.
- No tool/LSP/browser/SDK/workload/schema-validator installation; generic Draft 2020-12 remains honestly `degraded` unless already available.
- No v1 parser/adapter/fallback, dual document, second membership field, duplicate identity, hard-coded fallback policy, fabricated Target, or use of rejected Task 4/historical HTML/audit as migration source.
- No rewriting approved plans, accepted v1 evidence, rejected evidence, or failed v2 receipts; no shared receipt append/overwrite.
- No commit, stage, reset, clean, stash, broad restore/checkout, or broad formatting.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after with Node `.mjs` deterministic positive/mutation/aggregation suites. Product tests are out of scope.
- Every command records cwd, UTC, input/output SHA-256, versions, exit code, assertion totals, limitations, and terminal status. Temporary fixtures live outside the repository or under the current task attempt directory.
- A receipt passes only when `assertions_total >= 1`, `passed == total`, `failed == 0`, `unresolved_blockers == 0`; generic validator `degraded` is an allowed disclosed limitation.

## Execution strategy
### Parallel execution waves
Tasks 1-5 are sequential because each freezes the next contract boundary. F1-F4 run in parallel read-only lanes with distinct output ownership; F5 aggregates sequentially.

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 | reviewed/approved amendment and separate execution authorization | 2-5 | none |
| 2 | 1 | 3-5 | none |
| 3 | 2 | 4-5 | none |
| 4 | independently accepted 3 | 5 | none |
| 5 | independently accepted 4 | F1-F4 | none |
| F1-F4 | 5 | F5 | each other; read-only except distinct receipt |
| F5 | F1-F4 APPROVE | original Task 5 | none |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [ ] 1. Capture the authorized amendment repository, tool, and dirty-worktree boundary
  What to do / Must NOT do: Re-read all three instruction/context files; capture canonical root, branch/upstream, HEAD, porcelain-v1 with `--untracked-files=all`, Node/SDK, validator availability, and per-path `absent|present` state, tracked status, size and SHA-256. Parse quoted/Cyrillic paths, deletes and renames deterministically; compare future scope to this snapshot, never merely to HEAD. Write only `docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot.md`; do not install, stage, or mutate owner paths.
  Parallelization: Wave 1 | Blocked by: reviewed plan, owner approval, separate execution authorization | Blocks: 2
  References: `AGENTS.md:3-14`; `docs/architecture-migration/AGENTS.md:6-27,87-95`; `TASK_CONTEXT.md:311-374`; approved Phase 0.5 plan; accepted v1 schema/model/receipts.
  Acceptance criteria: exact root `D:/IA/ace v.2`; every porcelain row maps exactly once; deleted paths use `absent`, renames preserve both path identities, untracked directories are expanded; protected hashes either match live bytes or terminal status is `blocked`; no other path changes.
  QA scenarios: Happy—independent parser reproduces all rows/hashes. Failure—temporary status corpus with rename, delete, Unicode, quoted space, absent upstream and hash drift is rejected. Evidence: repository snapshot receipt.
  Commit: N | evidence precedes implementation and Git action requires separate owner instruction.

- [ ] 2. Freeze the exact v2 contract and lossless v1-to-v2 mapping
  What to do / Must NOT do: Write only `docs/architecture-migration/evidence/phase-0.5-v1-to-v2-mapping.json` before repository implementation. Source precedence is the Task 1 hash-bound accepted v1 model → accepted v1 schema → accepted validation receipt; Phase 0 maps are cross-checks only; rejected runtime, HTML and audit are never inputs. The JSON contains the accepted v1 source SHA, complete source record/field inventory, and for every field its source/destination, source/destination semantics, snapshot derivation, conversion, and loss class `preserved|normalized|split|merged|excluded|unresolved`; zero unexplained fields. It is immutable after Task 2 acceptance and remains the independent F2 migration source after the canonical model path is overwritten.
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 3
  References: `maps/architecture-model.json`; `maps/architecture-model.widget.schema.json`; `evidence/phase-0.5-model-validation.md`; Phase 0 maps/inventory.
  Acceptance criteria: all IDs/references map exactly once; group maps to singular immutable `record_kind`; `edge_semantics` has an exact per-field destination and snapshot provenance; no legacy field or fabricated Target survives. This owner-approved amendment explicitly permits only these structural transformations: split v1 record values/provenance into the snapshots named by its membership, merge same-ID disjoint v1 representations into one record when all snapshot states remain lossless, merge five group arrays into `records` while adding immutable `record_kind`, and fold `edge_semantics` into edge canonical states. Any other split/merge or conflict is blocked.
  QA scenarios: Happy—one representative of each kind maps and reconciles and the mapping JSON reproduces the Task 1 v1 hash. Failure—unmapped field, duplicate destination, semantic drift, lost reference, retained `snapshots`, copied Target, unknown edge semantic, or source-hash drift rejects the inventory. Evidence: immutable versioned mapping JSON plus Task 3 receipt section.
  Commit: N | contract freeze evidence is versioned and independently verified.

- [ ] 3. Reopen Task 3 and implement the canonical v2 schema, model, and semantic validator
  What to do / Must NOT do: Write only `maps/architecture-model.widget.schema.json`, `maps/architecture-model.json`, `widget/model-contract.mjs`, `widget/verify-widget.mjs`, and `evidence/phase-0.5-model-validation-v2.md`. Keep Draft 2020-12 `$id` and require version `2.0.0`. Document top-level keys are exactly `metadata`, `contract_version`, `snapshots`, `views`, `canonical_diff_fields`, `records`, `evidence`, `limitations`, `invariants`, `deferred_decisions`; `records` replaces `nodes|edges|state_records|flows|coverage`, and standalone `edge_semantics` is forbidden. Document `snapshots` is fixed status vocabulary only and is never consulted for record presence. Record top-level keys are exactly `id`, singular `record_kind`, `snapshot_states`; kinds are `node|edge|state_record|flow|coverage`; snapshot-state keys only `baseline|current|target`; absence means not present. Define an exact schema matrix in schema/receipt: every canonical path's JSON type, requiredness, nullability, collection comparison, and reference semantics. `views` is a non-empty duplicate-free set of six enums; IDs/references are strings. Preserve optional v1 structures only with their actual validated type—never invent a type.
  What to do / Must NOT do: Model policy enumerates every canonical field exactly once per kind using direct field-name paths only (no nesting/wildcards): node `kind,name,views`; edge `kind,name,from,to,views,source_kind,state_refs,trigger,effect,participants`; state `name,current_owner,target_owner,writers,readers,copies,reactive_effects,persistence,migration_status,coverage_status,views`; flow `sequence_id,name,position,views`; coverage `kind,name,coverage_status,views`. Comparison modes are `scalar|set|ordered`; each path must match declared type. Absent optional value is a sentinel distinct from explicit `null`; required absence rejects. Scalar is deterministic JSON scalar equality; set rejects canonical duplicates, recursively canonicalizes members and compares sorted UTF-8 canonical JSON; ordered preserves order. No empty policy lists or runtime fallback.
  What to do / Must NOT do: Snapshot state requires `canonical,status,confidence,evidence_refs,limitation_refs,invariant_refs,decision_refs,comparison:{status,reasons}`. Comparable requires empty reasons; unresolved requires reasons. Presence classification later outranks unresolved. Baseline/Current require evidence or degraded confidence plus limitation and unresolved reason. Target is `unimplemented`, derived/degraded, design-sourced, never copied. Edges resolve node endpoints and state refs in the same snapshot. Non-empty flow sequences use unique contiguous positions `1..N` independently per snapshot; empty sequence is valid. Migrate v1 mechanically and keep canonical Target empty unless accepted target intent exists.
  Parallelization: Wave 3 | Blocked by: 2 | Blocks: 4
  References: Task 2 mapping; current schema/model; `widget/model-contract.mjs:20-85`; `TASK_CONTEXT.md:127-155,258-299`.
  Acceptance criteria: canonical command `node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-0.5-model-validation-v2.md` exits 0; six views exact; IDs globally unique; mapping hash and complete reconciliation embedded; v1 history byte-identical; generic Draft status honest.
  QA scenarios: Happy—canonical model plus each kind/snapshot, same-ID equal/different pairs, evidence-backed Target and valid-empty Target validate. Failure—mutations for v1/unknown version, `$id` mismatch, duplicate ID across/within kind, unknown kind/snapshot/property, legacy/top-level value, invalid refs/evidence/comparison/Target, orphan same-snapshot endpoint, wrong endpoint kind, missing semantic migration, independent flow gaps/duplicates, invalid/empty/incomplete policy, duplicate set member, and unmapped v1 field all reject. Evidence: versioned validation receipt and immutable temporary mutations.
  Commit: N | independent Task 3 acceptance is required before Task 4.

- [ ] 4. Reopen Task 4 and implement immutable v2 runtime with honest directional Diff
  What to do / Must NOT do: Write only `widget/architecture-widget.mjs`, `widget/verify-widget.mjs`, `evidence/phase-0.5-acceptance-v2.json`; changing `model-contract.mjs` or Task 3 semantics invalidates Task 3 acceptance and requires re-verification. Load/validate exactly one v2 object, structured-clone/deep-freeze it, build one unique `by_id` and snapshot indexes solely from `snapshot_states`, and atomically replace only a fully built candidate. Failure returns `{state: previousState, error:{category,detail}}` with identical previous state identity and unchanged model, controls, counts, provenance and stale flag.
  What to do / Must NOT do: Diff output is frozen `{id,record_kind,direction,before,after,changed_fields,reasons,invariant_violation}`; absent side is `null`; changed fields sorted. Presence first gives added/removed even if surviving provenance is unresolved; unresolved applies only to records present both sides; otherwise policy comparison gives changed/unchanged. `invariant_violation` is orthogonal: collect the union of `invariant_refs` from every present compared side, resolve each against top-level `invariants`, and set true iff any referenced invariant has accepted status `unverified`; missing references reject during validation. Added/removed evaluate the surviving side, both-present directions evaluate the union, and swapping never changes the flag. Invariant status never changes direction or `changed_fields`. Swap reverses added/removed, preserves other directions, swaps before/after. Same-snapshot request rejects and preserves pair. Policy mutation in memory must change behavior, proving no hard-coded fallback.
  What to do / Must NOT do: Counts use pre-filter snapshot/diff population and post-filter visible count. Target population 0 → `valid-empty-target`; population >0 and visible 0 → `no-match`; empty Baseline/Current → `empty-snapshot`; empty Diff union → `empty-diff`; filtered non-empty Diff → `no-match`; malformed/invalid input always rejects. Filters never alter population classification.
  Parallelization: Wave 4 | Blocked by: independent Task 3 acceptance | Blocks: 5
  References: `widget/architecture-widget.mjs:12-34`; Task 3 contract/policy; `widget-spec.md`; rejected `phase-0.5-acceptance.json`.
  Acceptance criteria: one canonical verifier invocation runs positive and negative suites into separate task-temporary JSON files, then atomically aggregates once into `phase-0.5-acceptance-v2.json`; receipt retains both commands, exits, suite IDs, totals and hashes. All five directions are real; canonical field changes for each kind produce changed; policy changes drive behavior; canonical hash cannot be mutated; no hidden source/import exists.
  QA scenarios: Happy—node name, valid edge endpoint, owner, flow position and coverage changes; equal, added, removed, unresolved; changed+unverified invariant; added/removed with surviving unverified invariant; verified-only refs false; swap preserves violation; name collision, set reorder, ordered reorder, all empty/no-match states. Failure—two documents, array document, v1, duplicate identity, invalid policy, orphan/wrong-snapshot endpoint, bad flow, same pair, alias mutation, partial replacement, hard-coded-policy disagreement, name/position identity, missing invariant ref, direction altered by invariant status, and invalid replacement after valid-empty all reject without state change. Evidence: versioned aggregate JSON receipt.
  Commit: N | independent Task 4 acceptance is required.

- [ ] 5. Reconcile Tasks 3-4, enforce exact scope, and release only original Task 5
  What to do / Must NOT do: Re-run model/runtime suites; require separate Task 3 and Task 4 verifier approvals; compare every path to Task 1 pre-state; recompute protected hashes; require zero forbidden paths and zero legacy runtime reads/hidden sources. Write only `evidence/phase-0.5-v2-amendment-scope-gate.md` and factual `TASK_CONTEXT.md` updates. Do not modify approved plan. Record amendment overlay/SHA, superseded clauses, approvals, last action and next action. After success: `Stage=executing`, active plan remains original approved Phase 0.5 plan, amendment path is separate metadata, Phase acceptance pending, next action original Task 5, Phase 1 blocked. Intermediate Tasks 1-4 use receipts instead of context writes as a narrow approved exception to avoid unverified workflow transitions.
  Parallelization: Wave 5 | Blocked by: independent Task 4 acceptance | Blocks: F1-F4
  References: Task 1 snapshot; versioned receipts; approved plan Task 5; `TASK_CONTEXT.md:311-374`.
  Acceptance criteria: exact allow-list; protected mismatch 0; forbidden path 0; required Diff/empty-state probes pass; v1 history unchanged; no Task 5 implementation; workflow fields exact. Any validator change after Task 3 forces Task 3 re-acceptance.
  QA scenarios: Happy—deterministic scope verifier exits 0 and proves ordered receipts. Failure—altered v1 receipt/plan, forbidden path, missing verifier, legacy read, second document, absent changed assertion, premature Task 5/Phase 1, or crossed owner gate rejects. Evidence: scope-gate receipt.
  Commit: N | commit remains separately owner-authorized.

## Final verification wave
> F1-F4 run in parallel after Todo 5, own distinct immutable receipts, and never edit implementation. ALL must APPROVE. F5 aggregates sequentially.
- [ ] F1. Verify amendment plan, dependency, supersession, and gate compliance
  Reconstruct Tasks 1-5, owner approval/execution receipts, approved-plan overlay, and exact execution order. Reject missing/inverted dependency, ambiguous supersession, stale plan hash, or premature Task 5. Write only `evidence/phase-0.5-v2-amendment-final-verification-f1-plan-compliance.md`; terminal `APPROVE|REJECT` with exact totals.
  QA: Run `node docs/architecture-migration/widget/verify-widget.mjs --suite v2-amendment-f1 --plan docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md --context docs/architecture-migration/TASK_CONTEXT.md --output docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f1-plan-compliance.md`; happy path exits 0 with one terminal `APPROVE`, matching plan SHA and all five dependencies. Against task-temporary copies, remove one task, invert one dependency, alter the plan hash, remove one owner gate and mark Task 5 started; each probe must exit nonzero and emit terminal `REJECT` without modifying canonical artifacts.
- [ ] F2. Independently verify v2 schema, migration, and semantic contract
  Re-run model suite and an independently built mutation matrix; reconcile every accepted v1 field/ID/reference; verify schema matrix, policy completeness, six views, Target honesty, snapshot-local endpoints/flows, and zero legacy authority. Reject loss, fabrication, global-only validation or policy contradiction. Write only `evidence/phase-0.5-v2-amendment-final-verification-f2-contract-quality.md`.
  QA: Run `node docs/architecture-migration/widget/verify-widget.mjs --suite v2-amendment-f2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --mapping docs/architecture-migration/evidence/phase-0.5-v1-to-v2-mapping.json --v1-receipt docs/architecture-migration/evidence/phase-0.5-model-validation.md --output docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f2-contract-quality.md`; happy path binds mapping source SHA to Task 1 and the accepted v1 receipt, exits 0 with terminal `APPROVE`, and reports zero unmapped fields. Task-temporary mutations for mapping source-hash drift, lost field/ID, fabricated Target, duplicate ID, orphan same-snapshot endpoint, global-only flow validation, retained `snapshots`, and incomplete/contradictory policy each exit nonzero with terminal `REJECT`.
- [ ] F3. Independently verify runtime immutability, atomicity, Diff, and empty states
  Re-run runtime suites and independently construct all five kinds/directions, swaps, policy mutation, alias mutation, failed replacement and complete population/filter matrix. Reject unreachable changed, overwrite, fallback policy, second source, partial state, or misclassified empty state. Write only `evidence/phase-0.5-v2-amendment-final-verification-f3-runtime-qa.md`.
  QA: Run `node docs/architecture-migration/widget/verify-widget.mjs --suite v2-amendment-f3 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f3-runtime-qa.md`; happy path exits 0, terminal `APPROVE`, and asserts all five directions, five changed-kind fixtures, swap symmetry, immutable aliases, identical-state failed replacement, policy mutation, and the full empty/no-match matrix. Task-temporary mutations restoring unchanged/unchanged, duplicate overwrite, hard-coded policy, partial commit, second source and filtered-Target defect each exit nonzero with terminal `REJECT`.
- [ ] F4. Verify path scope, history preservation, and workflow integrity
  Compare live tree to Task 1 pre-state, including present hashes and absent files; verify approved plans, v1 receipts, widgets, production/tests/config/release and unrelated dirty paths; require Phase 1 blocked and result acceptance pending. Write only `evidence/phase-0.5-v2-amendment-final-verification-f4-scope-fidelity.md`.
  QA: Run `node docs/architecture-migration/widget/verify-widget.mjs --suite v2-amendment-f4 --snapshot docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot.md --context docs/architecture-migration/TASK_CONTEXT.md --output docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f4-scope-fidelity.md`; happy path exits 0 with terminal `APPROVE`, protected mismatches 0, forbidden paths 0, result acceptance pending and Phase 1 blocked. Task-temporary ledger/context probes for altered protected bytes, absent-before path collision, extra changed path, rewritten v1 receipt, generated-widget/browser artifact and crossed workflow gate each exit nonzero with terminal `REJECT`.
- [ ] F5. Aggregate four distinct approvals and release the original Task 5 dependency
  Read exactly F1-F4 as distinct regular files; require matching amendment, snapshot SHA/source basis, one terminal APPROVE each, positive totals, zero failures/blockers, and disclosed degraded generic validation. Re-run allow-list/protected checks, atomically write `evidence/phase-0.5-v2-amendment-final-verification.md`, then update only factual `TASK_CONTEXT.md` fields. Missing/duplicate/symlink receipt, metadata drift, altered hash, extra path, or owner-acceptance/completed state rejects without shared-file mutation. F5 is technical approval only, not owner acceptance; Phase 1 stays blocked.
  QA: Run `node docs/architecture-migration/widget/verify-widget.mjs --suite v2-amendment-f5 --receipts docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f1-plan-compliance.md,docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f2-contract-quality.md,docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f3-runtime-qa.md,docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification-f4-scope-fidelity.md --snapshot docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot.md --context docs/architecture-migration/TASK_CONTEXT.md --output docs/architecture-migration/evidence/phase-0.5-v2-amendment-final-verification.md`; happy path exits 0 with `4/4 APPROVE`, aggregate `APPROVE`, original Task 5 next and Phase 1 blocked. Using task-temporary copies, test missing, duplicate, directory/symlink, mismatched metadata/hash, duplicate verdict, zero assertions, unresolved blocker, changed protected bytes, extra path and completed context; each exits nonzero and leaves canonical aggregate/context unchanged.

## Commit strategy
Planning guidance only; no staging or commits without a separate owner Git instruction after all gates.

1. `docs(architecture): define widget model contract v2` — schema, model, model validator, v2 validation receipt.
2. `fix(architecture): support snapshot-aware widget diff` — runtime, runtime verifier additions, aggregate v2 acceptance receipt.
3. `docs(architecture): record widget v2 amendment verification` — imported amendment plan, snapshot/scope/F1-F5 receipts, factual context update.

Use path-selective staging only. Never include `.omo`, `.opencode`, production/tests, installer/publish/presentations, generated widget, or unrelated dirty paths.

## Success criteria
- Contract is `2.0.0`; schema `$id` is stable; one document, one unique ID, one `snapshot_states` authority, no legacy `snapshots` or duplicate current/target records.
- Field-level v1 reconciliation has zero unexplained/lost facts and preserves accepted/rejected historical evidence.
- Six views remain filters over one model; edges/references and flows validate independently by snapshot.
- Executable policy covers every declared canonical field exactly once; absent differs from null; set/ordered/scalar semantics are deterministic.
- Added, removed, changed, unchanged and unresolved are independently proven; swaps, changed fields and invariant flags are correct.
- Target is honest and initially valid-empty unless accepted design evidence supports it; valid-empty, empty-snapshot, empty-diff and no-match are distinct.
- Model/indexes/results are deeply immutable; invalid replacement returns the identical old state; no hidden source or fallback policy exists.
- Protected mismatches and forbidden changed paths are zero; F1-F4 and F5 APPROVE.
- Original Task 5 is the sole next action; Phase 0.5 result acceptance remains pending and Phase 1 remains blocked.

Rollback boundary: rollback is path-specific and owner-authorized. Task 1 records `absent|present-with-hash`; restore only captured bytes for pre-existing files and remove only amendment-created absent-before files. Never use reset/clean/stash/broad restore. Failure receipts and decision-log history remain. `TASK_CONTEXT.md` is not silently byte-restored: record a factual transition to `blocked` with receipt links. On failure Task 4 remains pending/rejected, Task 5 and Phase 1 remain blocked, and no owner gate is crossed.
