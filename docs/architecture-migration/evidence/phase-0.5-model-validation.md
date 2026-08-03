---
phase: phase-0.5-model-driven-architecture-widget
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: accepted-phase-0-dossier; Task 3 schema/model payload
generated_at_utc: 2026-07-31T14:05:16.8750930Z
working_directory: D:/IA/ace v.2
status: degraded
---

# Phase 0.5 Task 3 Model Validation

## Correction Status

Independent verifier session `ses_0479222f7ffeeS4phaES1DoAuT` correctly
rejected the earlier hand-written validator despite a canonical exit `0` and
the original 13 probes. Its bypass classes were omitted schema-required
metadata; state owners; invariant, deferred-decision, and limitation content;
provenance and canonical-Diff fields; closed-object boundaries; and unresolved
present `limitation_refs` when evidence was non-empty.

The correction retains the rewritten schema walker. `model-contract.mjs` now
rejects unsupported schema keywords, types, non-local or unresolved `$ref`, and
any `additionalProperties` form other than the explicitly accepted `false`.
It continues to enforce every construct used by the accepted schema: `required`,
`const`, `enum`, `pattern`, `minItems`, `maxItems`, `uniqueItems`, object/array/
string/integer types, `$ref`, `allOf`, and all declared
`additionalProperties: false` boundaries. `limitation_refs` is optional in the
schema, so the semantic evidence-coverage check treats its absence as an empty
array while still resolving it when present. The schema and model remain
byte-identical to the hashes recorded before this correction; no contract-data
defect was proven.

## Final Commands And Results

Executed from `D:/IA/ace v.2` with Node `v24.14.0`, PowerShell
`5.1.19041.6456`, and Git `2.53.0.windows.1`:

```powershell
node --check "docs/architecture-migration/widget/model-contract.mjs"
node --check "docs/architecture-migration/widget/verify-widget.mjs"
node "docs/architecture-migration/widget/verify-widget.mjs" --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" --model "docs/architecture-migration/maps/architecture-model.json"
node "C:\Users\Admin\AppData\Local\Temp\opencode\task3-final-probes.mjs"
node "C:\Users\Admin\AppData\Local\Temp\opencode\task3-crosscheck.mjs"
```

Both syntax checks exited `0`. The canonical CLI exited `0` and printed:

```json
{"result":"pass","schema":{"id":"https://ace.local/contracts/architecture-model.widget.schema.json","draft":"https://json-schema.org/draft/2020-12/schema","generic_draft_2020_12":"degraded"},"model":{"contract_version":"1.0.0","counts":{"nodes":79,"edges":112,"state_records":27,"flows":22,"coverage":5,"edge_semantics":112,"evidence":11,"invariants":15,"deferred_decisions":6,"limitations":3},"ids":280,"views":["compile-time","di-runtime","persistence","reactive","state-ownership","user-flow"],"generic_draft_2020_12":"degraded"}}
```

The real-CLI mutation script produced `53/53` intended rejections, exceeding
the required `32/32`: the original `13/13` named probes plus 40 correction and
coverage regressions. A separate positive mutation exited `0`, proving that an
omitted optional `limitation_refs` is schema-valid. An in-memory schema with an
unsupported `format` keyword exited `1` with category `schema`.

The 53 rejected probes cover malformed JSON; duplicate IDs; orphan endpoints;
invalid enums; missing edge semantics; orphan evidence; unsupported identity and
version; target/current separation; flow order; omitted/seventh views; all
required metadata fields and invalid const/pattern values; both state owners;
invariant statement/status; all four deferred-decision fields; limitation
statement; every required provenance and diff field; unknown top-level and
representative nested closed-object properties for every closed object schema;
unresolved limitation references with evidence; declared object/array/string/
integer types; duplicate arrays; and empty arrays subject to `minItems`.

## Independent Model Check

`task3-crosscheck.mjs` independently derived the exact accepted source sets
from `maps/target-invariants.md`: `INV-001` through `INV-015` (`15/15`) and
`DEC-001` through `DEC-006` (`6/6`). It separately derived model counts:
`nodes=79`, `edges=112`, `state_records=27`, `flows=22`, `coverage=5`,
`edge_semantics=112`, `evidence=11`, and `limitations=3`.

## Hashes And Scope

The receipt deliberately does not claim its own final hash. Final SHA-256:

| Path | SHA-256 |
| --- | --- |
| `maps/architecture-model.widget.schema.json` | `7afbce3cfadc8b77443462e52d0bb15c7bdd31569de41fadcaae941791b07194` |
| `maps/architecture-model.json` | `ee9e0069d71f3abffa5fa1e9b698a97507694597fbe85b84c272c30d3f55efbd` |
| `widget/model-contract.mjs` | `54eeeb9c5ac36c1e55d52c43571ae7a33c7786be3687daaaf993e89a7a5d9787` |
| `widget/verify-widget.mjs` | `c8b579df70ce9be4ae90ae412e9baa33d9d64104cc4a23c1a7338d1133c38605` |
| `maps/architecture-model.schema.json` (protected) | `a9a27dccd0e6fd0dff9582a36d3a139becb3851aae0de8ef4939af4b0973ea70` |
| `maps/architecture-model.baseline.json` (protected) | `d7cd9620f36d8e15a9d03efc56726a3ccafe6eb4a6ba92f4265543423ad58a8f` |
| `architecture_widget.html` (historical) | `d6f1925e188fd9e8d1485d44f040c41781580a072b5a2da8ea7fe2b4752930ca` |
| `archive/architecture_widget.phase-0-historical.html` | `d6f1925e188fd9e8d1485d44f040c41781580a072b5a2da8ea7fe2b4752930ca` |

Correction-lane repository writes are limited to
`widget/model-contract.mjs` and this receipt. The model and widget schema match
their prior receipt hashes. `model-contract.mjs` has 78 pure non-comment LOC;
`verify-widget.mjs` has 30, both within the 250 LOC limit.

## Limitation

No generic Draft 2020-12 validator is installed or was installed. This
self-contained walker fully covers the constructs present in the accepted local
schema and rejects unsupported extensions deterministically, but generic Draft
2020-12 validation remains `degraded`.

## DoneClaim

**DoneClaim TASK-3-VALIDATOR-CORRECTED:** The corrected self-contained validator
passes the canonical real CLI with model-derived deterministic counts; enforces
all constructs actually used by the accepted schema or rejects unsupported
extensions; rejects `53/53` intended real-CLI mutations, including the original
13 and all prior verifier bypass classes; independently matches all 15 invariant
and 6 decision IDs; preserves the schema, model, and protected Phase 0/historical
inputs at their recorded hashes; and leaves only the explicitly disclosed
generic Draft 2020-12 limitation.

## Phase 0.5 Task 4 Runtime Validation

Task 4 added a pure Node runtime with one-document validation, recursive frozen
snapshots, plain frozen indexes, atomic replacement, stable-ID Diff and
deterministic view/filter queries. It has no file, network, DOM or browser
operation. The canonical Target remains the explicit `valid-empty-target` state.

Failing-first commands before implementation both exited `1` with controlled
`cli-arguments: expected exactly --schema <path> --model <path>` because the
runtime suite capability was absent:

```powershell
node "docs/architecture-migration/widget/verify-widget.mjs" --suite runtime --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" --model "docs/architecture-migration/maps/architecture-model.json" --output "C:\Users\Admin\AppData\Local\Temp\opencode\task4-red-runtime.json"
node "docs/architecture-migration/widget/verify-widget.mjs" --suite runtime-negative --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" --model "docs/architecture-migration/maps/architecture-model.json" --output "C:\Users\Admin\AppData\Local\Temp\opencode\task4-red-runtime-negative.json"
```

Final Node syntax checks, canonical Task 3 CLI and both Task 4 suite commands
exit `0`. The positive suite has 19 assertions; the negative suite has 18.
Both use only schema-valid deep-cloned in-memory Target derivatives with unique
stable IDs. The current one-document contract has no per-snapshot canonical
values for one stable ID, so `changed` is not representable without fabricating a
duplicate record; same-ID cross-snapshot comparison is explicitly `unchanged`
unless evidence/confidence makes it `unresolved`. The atomic acceptance JSON
retains separate `runtime` and `runtime-negative` summaries, records
`visible_partial_commits: 0` and `source_model_mutations: 0`, and rerunning each
suite replaces only its own stable `TASK4-POS-*` or `TASK4-NEG-*` rows.

Canonical input SHA-256 values remain schema
`7afbce3cfadc8b77443462e52d0bb15c7bdd31569de41fadcaae941791b07194` and model
`ee9e0069d71f3abffa5fa1e9b698a97507694597fbe85b84c272c30d3f55efbd`.
Generic Draft 2020-12 coverage remains degraded because no generic validator is
installed; Task 4 relies on the accepted self-contained contract validator.

**DoneClaim TASK-4-IMMUTABLE-RUNTIME:** The pure runtime accepts exactly one
validated model, commits only fully frozen candidates, retains the prior state on
controlled failure, exposes deterministic Baseline/Current/Target/Diff, six-view
union filtering, stable-ID lookup and directional Diff, and writes deterministic
Task 4 acceptance evidence atomically.
