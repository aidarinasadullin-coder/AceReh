# Phase 0.5 Model Validation v2

**PASS**

## Contract

- Draft: `https://json-schema.org/draft/2020-12/schema`; schema ID: `https://ace.local/contracts/architecture-model.widget.schema.json`; contract version: `2.0.0`.
- Exact document keys, single `records` authority, fixed document snapshot vocabulary, globally unique IDs, exact six views, and no legacy group arrays or standalone `edge_semantics` passed.
- Schema matrix: scalar values are JSON scalar/null; set values are arrays with recursively canonicalized UTF-8 JSON duplicate rejection; ordered values are arrays with order preserved; references are globally defined ID strings; absent optional canonicals differ from explicit null.
- Policy paths are direct and exact for all five kinds: node 3, edge 10, state_record 11, flow 4, coverage 4.

## Migration Reconciliation

- Immutable Task 2 mapping SHA-256: `54A05420D1554129D8B20AF82769ACAB437FE4001B625D5FCFF4610B41BA7283`.
- Accepted v1 source SHA-256: `EE9E0069D71F3ABFFA5FA1E9B698A97507694597FBE85B84C272C30D3F55EFBD`; accepted v1 schema SHA-256: `7AFBCE3CFADC8B77443462E52D0BB15C7BDD31569DE41FADCAAE941791B07194`.
- Complete mechanical reconciliation: 16 top-level fields, 245 records, 112 edge semantics, 280 IDs, and 355 reference occurrences; zero fabricated Target states in canonical model.

## Results

- Schema SHA-256: `8a0bc79c00fbd8f1d2c2e52e70085df0472e3675b99b9c5ec9209fa8eeb4c97b`.
- Model SHA-256: `f573e175c28aa9beb9dd1809eb49e34b41f182ba2742132e93c40639804efa97`.
- Counts: `{"records":245,"nodes":79,"edges":112,"state_records":27,"flows":22,"coverage":5,"evidence":11,"limitations":3,"invariants":15,"deferred_decisions":6}`; global IDs: `280`; views: `compile-time, di-runtime, persistence, reactive, state-ownership, user-flow`.
- Generic Draft 2020-12 validation: **degraded** because no generic validator is installed.
- Positive fixtures: all five kinds in baseline/current, valid-empty Target, evidence-backed Target, same-ID equal pair, and same-ID different pair passed.
- Mutations rejected: `21/21`: v1, unknown-version, id-mismatch, duplicate-id, unknown-kind, unknown-snapshot, legacy-top-level, legacy-record, invalid-evidence, invalid-comparison, invalid-target, wrong-endpoint, orphan-endpoint, missing-semantic, flow-gap, flow-duplicate, empty-policy, incomplete-policy, contradictory-policy, duplicate-set-member, unmapped-v1-field.

## Red Evidence

- Before Task 3 edits, `--suite model-v2` was rejected as unsupported; v1 rejected `2.0.0` and `records`, but accepted unrecognised `snapshot_states`.
