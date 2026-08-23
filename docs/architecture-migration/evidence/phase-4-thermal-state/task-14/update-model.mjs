// Todo 14 one-shot architecture-model.json refresh (maps/model are the only source writes).
import { readFileSync, writeFileSync } from "node:fs";

const path = "docs/architecture-migration/maps/architecture-model.json";
const model = JSON.parse(readFileSync(path, "utf8"));

// --- metadata ---
model.metadata.phase = "phase-4-thermal-state";
model.metadata.snapshot_sha = "327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02";
model.metadata.source_basis = "phase-4-thermal-state-task-14-live-code-and-accepted-evidence";
model.metadata.provenance.phase_4_plan = "docs/architecture-migration/plans/phase-4-thermal-state.md";

// --- evidence additions (Phase 4 receipts) ---
const ev = (id, path, locator) => ({ id, path, locator, confidence: "verified", freshness: "current-at-snapshot" });
const p4 = "docs/architecture-migration/evidence/phase-4-thermal-state/";
const newEvidence = [
  ev("EV-P4-CHAR", p4 + "task-2/task-2-thermal-characterization.md", "41-case Thermal multiplicity characterization matrix and AMZ-2 two-row update"),
  ev("EV-P4-STATE", p4 + "task-3/task-3-thermal-state-contract.md", "canonical ProjectSessionThermalState contract, closed mutations and exhaustive origins receipt"),
  ev("EV-P4-DI", p4 + "task-4/task-4-project-session-di.md", "ProjectSession-owned Thermal slice DI guards; no independent state registration"),
  ev("EV-P4-BLOCKER", p4 + "task-5/blocker-analysis.md", "AMZ-1 owner-approved deviation: transitional ApplyNeedsRecalculation bridge"),
  ev("EV-P4-COORD", p4 + "task-6/task-567-merged-boundary.md", "AMZ-1 merged boundary: sealed singleton ThermalStateCoordinator, WPF adapter VM, compat service delegation, sole upstream subscriptions"),
  ev("EV-P4-CONTEXT", p4 + "task-8/task-8-context-hydraulics.md", "coordinator sole CalculationContext Thermal writer; Circuits/Hydraulics consumer semantics"),
  ev("EV-P4-LIFECYCLE", p4 + "task-9/task-9-lifecycle-restore.md", "canonical Restore lifecycle; second-load zero-stale fixed per DEC-T08/AMZ-2"),
  ev("EV-P4-PERSIST", p4 + "task-10/task-10-persistence-results.md", "ThermalPersistenceMapper pure save/restore halves and exact 8-field result wire contract"),
  ev("EV-P4-GUARDS", p4 + "task-11/task-11-ownership-guards.md", "ThermalStateLegacyStoreGuardTests: 8 NegativeFixture ownership categories incl. single upstream attach and coordinator-only context writer"),
  ev("EV-P4-GATES", p4 + "task-12/task-12-executable-gates.md", "frozen Release gates: full suite 1946 total / 1943 passed / 0 failed / 3 accepted NotExecuted identities"),
  ev("EV-P4-UIQA", p4 + "task-13/task-13-user-flow-qa.md", "agent-operated UI QA ten happy steps plus unknown-pipe failure branch PASS")
];
const existingEv = new Set(model.evidence.map((x) => x.id));
for (const e of newEvidence) if (!existingEv.has(e.id)) model.evidence.push(e);

// --- limitations: LIM-003 reflects implemented Thermal slice ---
const lim3 = model.limitations.find((l) => l.id === "LIM-003");
lim3.statement =
  "ProjectSession lifecycle shell plus ClimateState, ConstructionState and ThermalState slices are implemented; the Hydraulics state slice remains target-only and is not yet owned by ProjectSession.";

// --- invariants: INV-004 verified for the Thermal slice ---
const inv4 = model.invariants.find((i) => i.id === "INV-004");
inv4.status = "verified";
inv4.target_status = "implemented";
inv4.evidence = ["EV-ST", "EV-RE", "EV-P4-STATE", "EV-P4-COORD", "EV-P4-CONTEXT", "EV-P4-LIFECYCLE", "EV-P4-GUARDS", "EV-P4-GATES"];

// --- state_records: Thermal rows reflect sole canonical owner ---
const setState = (id, canonical, evidenceRefs, invariantRefs) => {
  const record = model.records.find((r) => r.id === id);
  const current = record.snapshot_states.current;
  Object.assign(current.canonical, canonical);
  current.evidence_refs = evidenceRefs;
  current.invariant_refs = invariantRefs;
};
setState("ST-012",
  { name: "Thermal inputs", current_owner: "ProjectSession.ThermalState (ProjectSessionThermalState)", target_owner: "ProjectSession.ThermalState", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P4-STATE", "EV-P4-COORD", "EV-P4-GUARDS"], ["INV-004"]);
setState("ST-013",
  { name: "Thermal pipe spacing", current_owner: "ProjectSession.ThermalState (Inputs.PipeSpacing)", target_owner: "ProjectSession.ThermalState.PipeSpacing", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P4-COORD", "EV-P4-LIFECYCLE"], ["INV-004"]);
setState("ST-014",
  { name: "Thermal last-derived result", current_owner: "ProjectSession.ThermalState (derived value; sole writable owner)", target_owner: "derived thermal result owned by ProjectSession.ThermalState", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P4-CONTEXT", "EV-P4-PERSIST"], ["INV-004"]);
setState("ST-015",
  { name: "Thermal status", current_owner: "ProjectSession.ThermalState (ThermalStatusSnapshot)", target_owner: "ProjectSession.ThermalState.Status", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P4-COORD", "EV-P4-BLOCKER", "EV-P4-GUARDS"], ["INV-004"]);
setState("ST-021",
  { name: "CalculationContext thermal inputs projection", current_owner: "CalculationContext projection bus; sole Thermal-side production writer is ThermalStateCoordinator", target_owner: "downstream compatibility projection of ProjectSession.ThermalState", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P4-CONTEXT", "EV-P4-GUARDS"], []);
setState("ST-022",
  { name: "CalculationContext results projections", current_owner: "CalculationContext projection bus; Thermal results written only by the coordinator, Hydraulics results by Circuits", target_owner: "derived context seam fed from canonical owners", migration_status: "migrated/verified", coverage_status: "covered" },
  ["EV-ST", "EV-P4-CONTEXT"], []);

// --- edges: compat-surface semantics + receipt-backed evidence ---
const patchEdge = (id, patch) => {
  const record = model.records.find((r) => r.id === id);
  const current = record.snapshot_states.current;
  if (patch.effect !== undefined) current.canonical.effect = patch.effect;
  if (patch.evidence) current.evidence_refs = [...new Set([...current.evidence_refs, ...patch.evidence])];
};
patchEdge("RE-001", { evidence: ["EV-P4-CONTEXT"] });
patchEdge("RE-002", { evidence: ["EV-P4-CONTEXT"] });
patchEdge("RE-005", { effect: "compat refresh surface only (RecalcMessage/NeedsRecalculation re-notify); canonical completion arrives via coordinator Completion", evidence: ["EV-P4-COORD"] });
patchEdge("RE-006", { effect: "compat echo fired only from canonical completions with changed spacing; no independent writer", evidence: ["EV-P4-COORD"] });
patchEdge("RE-007", { effect: "compat refresh surfaces fed from canonical completions; no independent writer", evidence: ["EV-P4-COORD"] });

// --- write back preserving 2-space indent + CRLF + trailing newline ---
const json = JSON.stringify(model, null, 2).replace(/\n/g, "\r\n") + "\r\n";
writeFileSync(path, json, "utf8");
console.log("model updated:", {
  phase: model.metadata.phase,
  evidence: model.evidence.length,
  records: model.records.length,
  inv4: inv4.status,
  lim3: lim3.statement.slice(0, 60) + "..."
});
