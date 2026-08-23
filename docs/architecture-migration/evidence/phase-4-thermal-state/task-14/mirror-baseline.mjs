// Todo 14 follow-up: mirror canonical updates into baseline snapshots
// (established convention: accepted model keeps baseline == current).
import { readFileSync, writeFileSync } from "node:fs";

const path = "docs/architecture-migration/maps/architecture-model.json";
const model = JSON.parse(readFileSync(path, "utf8"));

const ids = ["ST-012", "ST-013", "ST-014", "ST-015", "ST-021", "ST-022", "RE-005", "RE-006", "RE-007"];
for (const id of ids) {
  const record = model.records.find((r) => r.id === id);
  if (!record) throw new Error(`missing record ${id}`);
  record.snapshot_states.baseline = structuredClone(record.snapshot_states.current);
}

const json = JSON.stringify(model, null, 2).replace(/\n/g, "\r\n") + "\r\n";
writeFileSync(path, json, "utf8");

// verify: no diff-policy-relevant baseline/current divergence remains
const POLICY = {
  node: ["kind", "name", "views"],
  edge: ["kind", "name", "from", "to", "views", "source_kind", "state_refs", "trigger", "effect", "participants"],
  state_record: ["name", "current_owner", "target_owner", "writers", "readers", "copies", "reactive_effects", "persistence", "migration_status", "coverage_status", "views"],
  flow: ["sequence_id", "name", "position", "views"],
  coverage: ["kind", "name", "coverage_status", "views"]
};
const canon = (v) => (Array.isArray(v) ? `[${v.map(canon).sort().join(",")}]` : v && typeof v === "object" ? `{${Object.keys(v).sort().map((k) => `${JSON.stringify(k)}:${canon(v[k])}`).join(",")}}` : JSON.stringify(v));
let remaining = [];
for (const r of model.records) {
  const b = r.snapshot_states.baseline, c = r.snapshot_states.current;
  if (!b || !c) continue;
  for (const f of POLICY[r.record_kind]) {
    if (canon(b.canonical[f]) !== canon(c.canonical[f])) { remaining.push(`${r.id}.${f}`); break; }
  }
}
console.log("mirrored:", ids.join(","), "| residual canonical diffs:", JSON.stringify(remaining));
