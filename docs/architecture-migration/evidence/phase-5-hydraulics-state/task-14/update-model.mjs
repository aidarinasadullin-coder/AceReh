// Todo 14 one-shot architecture-model.json refresh (maps/model are the only source writes).
// Adapted from evidence/phase-4-thermal-state/task-14/update-model.mjs (phase-4 pattern).
// Canonical changes are mirrored into baseline AND current snapshots (accepted-model
// convention baseline == current keeps the runtime diff pair all-unchanged); non-canonical
// refs (evidence/invariants) are refreshed on current only, as in phase 4.
import { readFileSync, writeFileSync } from "node:fs";

const path = "docs/architecture-migration/maps/architecture-model.json";
const model = JSON.parse(readFileSync(path, "utf8"));

// --- metadata ---
model.metadata.phase = "phase-5-hydraulics-state";
model.metadata.snapshot_sha = "0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38";
model.metadata.source_basis = "phase-5-hydraulics-state-task-14-live-code-and-accepted-evidence";
model.metadata.provenance.phase_5_plan = "docs/architecture-migration/plans/phase-5-hydraulics-state.md";

// --- evidence additions (Phase 5 receipts) ---
const ev = (id, path, locator) => ({ id, path, locator, confidence: "verified", freshness: "current-at-snapshot" });
const p5 = "docs/architecture-migration/evidence/phase-5-hydraulics-state/";
const newEvidence = [
  ev("EV-P5-DI", p5 + "task-4/di-negative-probe.md", "ProjectSession-owned Hydraulics slice DI guards; no independent state registration"),
  ev("EV-P5-BLOCKER", p5 + "task-5/blocker-analysis.md", "hydraulics status compatibility seam analysis with rejection probe"),
  ev("EV-P5-CORRECTION", p5 + "task-6/correction-notes.md", "correction lane: shared-session fixtures, canonical save/restore wiring via HydraulicsPersistenceMapper.BuildHydraulicsProjectData, serialized eight-field round-trip characterization and final gate battery"),
  ev("EV-P5-COORD", p5 + "task-7/trx-coordinator-release.json", "sealed singleton HydraulicsStateCoordinator focused Release gates"),
  ev("EV-P5-WRITERS", p5 + "task-8/writer-authority-updates.md", "sole CalculationContext hydraulics results writer authority updates; consumer semantics untouched"),
  ev("EV-P5-LIFECYCLE", p5 + "task-9/divergence-notes.md", "four owner-adjudicated semantic adaptations: User-origin dirty authority in the slice, unconditional per-attempt status termination in RunCalculation finally, auto-recalc dirty churn eliminated, DI construction-cycle deadlock fix reference"),
  ev("EV-P5-GUARDS", p5 + "task-11/trx-guards-release.json", "HydraulicsStateLegacyStoreGuardTests Release TRX: 8/8 NegativeFixture ownership categories passed"),
  ev("EV-P5-GATES", p5 + "task-12/arithmetic.json", "executable reconciliation: full suite 1979 parser outcome rows = 1976 passed / 0 failed / 3 accepted NotExecuted identities"),
  ev("EV-P5-UIQA", p5 + "ui-qa/observations.json", "agent-operated hydraulics UI QA: nine steps PASS including corrupt unknown-pipe failure branch")
];
const existingEv = new Set(model.evidence.map((x) => x.id));
for (const e of newEvidence) if (!existingEv.has(e.id)) model.evidence.push(e);

// --- limitations: LIM-003 reflects implemented Hydraulics slice ---
const lim3 = model.limitations.find((l) => l.id === "LIM-003");
lim3.statement =
  "ProjectSession lifecycle shell plus ClimateState, ConstructionState, ThermalState and HydraulicsState slices are implemented; every module input, derived-result and status group has one writable canonical owner.";

// --- invariants: INV-005 verified for the Hydraulics slice ---
const inv5 = model.invariants.find((i) => i.id === "INV-005");
inv5.status = "verified";
inv5.target_status = "implemented";
inv5.evidence = ["EV-ST", "EV-RE", "EV-P5-LIFECYCLE", "EV-P5-WRITERS", "EV-P5-GUARDS", "EV-P5-GATES"];

// --- state_records: Hydraulics rows reflect sole canonical owner ---
const setState = (id, canonical, evidenceRefs, invariantRefs) => {
  const record = model.records.find((r) => r.id === id);
  const current = record.snapshot_states.current;
  Object.assign(current.canonical, canonical);
  // accepted-model convention: mirror canonical into baseline so the runtime diff stays empty
  record.snapshot_states.baseline.canonical = { ...current.canonical };
  if (evidenceRefs) current.evidence_refs = evidenceRefs;
  if (invariantRefs) current.invariant_refs = invariantRefs;
};
setState("ST-016",
  { current_owner: "ProjectSession.HydraulicsState (ProjectSessionHydraulicsState)", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P5-LIFECYCLE", "EV-P5-GUARDS"], ["INV-005"]);
setState("ST-017",
  { current_owner: "ProjectSession.HydraulicsState (Collectors)", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P5-COORD", "EV-P5-LIFECYCLE"], ["INV-005"]);
setState("ST-018",
  { name: "Hydraulics last-derived results", current_owner: "ProjectSession.HydraulicsState (derived value; sole writable owner)", target_owner: "derived hydraulics results owned by ProjectSession.HydraulicsState", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P5-WRITERS", "EV-P5-CORRECTION"], ["INV-005"]);
setState("ST-019",
  { current_owner: "ProjectSession.HydraulicsState (HydraulicsStatusSnapshot)", target_owner: "ProjectSession.HydraulicsState.Status", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P5-BLOCKER", "EV-P5-LIFECYCLE"], ["INV-005"]);

// ST-022: hydraulics half of the context projection bus now has a sole coordinator writer
const st22 = model.records.find((r) => r.id === "ST-022").snapshot_states;
st22.current.canonical.current_owner =
  "CalculationContext projection bus; sole per-side production writers are ThermalStateCoordinator and HydraulicsStateCoordinator";
st22.baseline.canonical.current_owner = st22.current.canonical.current_owner;
st22.current.evidence_refs = [...new Set([...st22.current.evidence_refs, "EV-P5-WRITERS"])];

// --- edges: compat-surface semantics + receipt-backed evidence ---
const patchEdge = (id, patch) => {
  const record = model.records.find((r) => r.id === id);
  const current = record.snapshot_states.current;
  if (patch.effect !== undefined) {
    current.canonical.effect = patch.effect;
    record.snapshot_states.baseline.canonical.effect = patch.effect;
  }
  if (patch.participants !== undefined) {
    current.canonical.participants = patch.participants;
    record.snapshot_states.baseline.canonical.participants = [...patch.participants];
  }
  if (patch.evidence) current.evidence_refs = [...new Set([...current.evidence_refs, ...patch.evidence])];
};
patchEdge("RE-001", {
  effect: "compat refresh surface consumed by the coordinator handler (notifyThermal); Circuits remains a pure consumer without its own context subscription",
  participants: ["CalculationContext.ContextChanged", "HydraulicsStateCoordinator handler"],
  evidence: ["EV-P5-COORD"]
});
patchEdge("RE-002", {
  effect: "valid ThermalResult triggers exactly one coordinator CalculateAll pass via the connected callback; calculation-origin work never raises dirty (auto-recalc dirty churn eliminated)",
  evidence: ["EV-P5-LIFECYCLE"]
});
patchEdge("RE-004", {
  effect: "status machine notification translated from the canonical slice; coordinator terminates exactly one ResetHydraulicsState per calculation attempt, success or failure",
  evidence: ["EV-P5-LIFECYCLE"]
});
patchEdge("RE-008", {
  effect: "adapter forwards user input/collection edits to ProjectSession.HydraulicsState (ApplyGlobalInputs/ReplaceCollectors, User origin); the slice raises IMarkDirtyService once per changed user-origin commit and residual adapter MarkDirty calls collapse into the same idempotent root transition; zero UpdateHydraulics calls remain in the VM",
  participants: ["Circuits handlers", "ProjectSession.HydraulicsState", "HydraulicsStateCoordinator"],
  evidence: ["EV-P5-LIFECYCLE", "EV-P5-GUARDS"]
});

// --- write back preserving 2-space indent + CRLF + trailing newline ---
const json = JSON.stringify(model, null, 2).replace(/\n/g, "\r\n") + "\r\n";
writeFileSync(path, json, "utf8");
console.log("model updated:", {
  phase: model.metadata.phase,
  evidence: model.evidence.length,
  records: model.records.length,
  inv5: inv5.status,
  lim3: lim3.statement.slice(0, 60) + "..."
});
