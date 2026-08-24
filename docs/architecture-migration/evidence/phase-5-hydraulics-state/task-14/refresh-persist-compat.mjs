// Todo 14 one-shot persistence-compatibility.md hydraulics-row refresh (S save-source / E evidence cells).
// R restore-use cells, V1 fixture cells, WN policy and classifications are untouched (restore semantics
// still apply the same DTO through BuildRestoreCandidate). Run once from repo root.
import { readFileSync, writeFileSync } from "node:fs";

const path = "docs/architecture-migration/maps/persistence-compatibility.md";
let text = readFileSync(path, "utf8");

const S = {
  "PP-009": "canonical snapshot via `HydraulicsPersistenceMapper.BuildHydraulicsProjectData`",
  "PP-053": "canonical GlobalInputs snapshot",
  "PP-054": "canonical GlobalInputs snapshot",
  "PP-055": "canonical GlobalInputs.SupplySpacingCm",
  "PP-056": "canonical GlobalInputs.SupplyHeatPercent",
  "PP-057": "maps canonical Collectors snapshots",
  "PP-058": "OperatingResult?.Power ?? 0 of canonical CircuitSnapshot",
  "PP-059": "OperatingResult?.FlowRate ?? 0 of canonical CircuitSnapshot",
  "PP-060": "OperatingResult?.Velocity ?? 0 of canonical CircuitSnapshot",
  "PP-065": "OperatingResult?.Throttling ?? 0 of canonical CircuitSnapshot",
  "PP-066": "OperatingResult?.ValveTurns ?? 0 of canonical CircuitSnapshot",
  "PP-074": "canonical CollectorSummarySnapshot",
  "PP-075": "canonical CollectorSummarySnapshot",
  "PP-076": "canonical CollectorSummarySnapshot",
  "PP-077": "canonical CollectorSummarySnapshot",
  "PP-078": "canonical CollectorSummarySnapshot",
  "PP-079": "canonical CollectorSummarySnapshot",
  "PP-080": "canonical CollectorSummarySnapshot",
  "PP-081": "canonical CollectorSummarySnapshot",
  "PP-082": "canonical CollectorSnapshot",
  "PP-083": "canonical CollectorSnapshot",
  "PP-084": "canonical CollectorSnapshot",
  "PP-085": "canonical CollectorSnapshot.Circuits mapped by the pure mapper",
  "PP-086": "mapped when canonical Summary exists",
  "PP-087": "canonical CircuitSnapshot",
  "PP-088": "canonical CircuitSnapshot",
  "PP-089": "canonical CircuitSnapshot",
  "PP-090": "canonical CircuitSnapshot.SupplySpacingCm",
  "PP-091": "canonical CircuitSnapshot",
  "PP-092": "canonical CircuitSnapshot.PipeSpacingCm",
  "PP-093": "mapped when canonical OperatingResult exists",
  "PP-094": "mapped when canonical DesignResult exists",
  "PP-095": "canonical OperatingResult?.Power ?? 0",
  "PP-096": "canonical OperatingResult?.FlowRate ?? 0",
  "PP-097": "canonical OperatingResult?.Velocity ?? 0",
  "PP-098": "`GetFlowRegimeDescription(OperatingResult?.FlowRegime)`",
  "PP-099": "canonical OperatingResult?.Throttling ?? 0",
  "PP-100": "canonical OperatingResult?.ValveTurns ?? 0"
};
const E_OVERRIDE = {
  "PP-009": "ProjectData.cs:56; HydraulicsPersistenceMapper.cs:15-47; ResultsViewModel.cs:1711; ProjectLoadOrchestrator.cs:171-173,200"
};

const lines = text.split("\n");
let changed = 0;
for (let i = 0; i < lines.length; i++) {
  const m = lines[i].match(/^\| (PP-\d{3}) \|/);
  if (!m || !(m[1] in S)) continue;
  const id = m[1];
  const cells = lines[i].split("|");
  cells[10] = " " + S[id] + " ";
  const projectDataSeg = cells[13].trim().split(";")[0].trim();
  let e = E_OVERRIDE[id]
    ?? `${projectDataSeg}; HydraulicsPersistenceMapper.cs:15-208; ResultsViewModel.cs:1711; ProjectLoadOrchestrator.cs:171-201`;
  if (id === "PP-092") e += "; ProjectRoundTripTests.cs:212-257";
  cells[13] = " " + e + " ";
  lines[i] = cells.join("|");
  changed++;
}
text = lines.join("\n");
if (changed !== 38) throw new Error(`expected 38 refreshed hydraulics rows, got ${changed}`);
writeFileSync(path, text, "utf8");
console.log("refreshed hydraulics rows:", changed);
