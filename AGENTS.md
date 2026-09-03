# Repository Agent Instructions

## Architecture invariants

The architecture migration (phases 1–11, `docs/architecture-migration/`) is
complete and owner-accepted. These invariants are standing rules for all
future work:

- `ProjectSession` is the aggregate root, with explicit climate,
  construction, thermal, and hydraulics slices.
- Each value has exactly one writable canonical owner.
- ViewModels are WPF adapters, not canonical state stores.
- Services do not depend on concrete ViewModels.
- Results is derived and does not own module inputs.
- Supported `.smc` behavior and wire compatibility remain intact unless a
  separately approved change says otherwise.

The machine-checkable form of these rules lives in
`tests/SnowMeltingCalculator.Tests/Architecture/ArchitectureRulesTests.cs`;
the human-readable architecture view and the decision log live in
`docs/architecture/`.

## Architecture dossier

`docs/architecture-migration/` is the completed migration dossier: frozen
plans, evidence, receipts, and the architecture maps. It is provenance —
do not rewrite its history. When a task changes state ownership or
persistence, update `docs/architecture/` and its ADR log, or record why
they stay unchanged. Any new migration-style phase requires explicit owner
direction and follows the dossier workflow
(`docs/architecture-migration/AGENTS.md`).

## Review

- No change is committed without owner review: work is handed over as an
  uncommitted, buildable tree with a green `dotnet test` run.
- Material changes — anything touching state ownership, persistence,
  architecture rules, or a wide multi-file surface — additionally get one
  independent read-only review pass (subagent or equivalent) before
  implementation; findings that change the design are recorded in
  `docs/architecture/README.md`.
- Migration-scale work reopens the dossier workflow and its explicit
  owner gates.
