import { createHash } from "node:crypto";
import { ContractError, parseJson, validateModel } from "./model-contract.mjs";

export const MODES = Object.freeze(["baseline", "current", "target", "diff"]);
export const VIEWS = Object.freeze(["compile-time", "di-runtime", "state-ownership", "reactive", "persistence", "user-flow"]);
const SNAPSHOTS = Object.freeze(["baseline", "current", "target"]);
const hash = (text) => createHash("sha256").update(text).digest("hex");
const fail = (category, detail) => { throw new ContractError(category, detail); };
const freeze = (value) => {
  if (value && typeof value === "object" && !Object.isFrozen(value)) {
    Object.values(value).forEach(freeze);
    Object.freeze(value);
  }
  return value;
};
const canonical = (value) => Array.isArray(value) ? `[${value.map(canonical).sort().join(",")}]` : value && typeof value === "object" ? `{${Object.keys(value).sort().map((key) => `${JSON.stringify(key)}:${canonical(value[key])}`).join(",")}}` : JSON.stringify(value);
const equal = (left, right, comparison) => comparison === "set" ? canonical(left) === canonical(right) : JSON.stringify(left) === JSON.stringify(right);
const snapshot = (mode) => mode === "baseline" ? "baseline" : mode === "target" ? "target" : "current";
const recordState = (record, name) => record.snapshot_states[name] ?? null;
const stateRecord = (record, name) => {
  const value = recordState(record, name);
  return value === null ? null : freeze({ id: record.id, record_kind: record.record_kind, ...value });
};
const indexes = (model) => {
  const byId = Object.create(null);
  const snapshots = Object.fromEntries(SNAPSHOTS.map((name) => [name, Object.create(null)]));
  model.records.forEach((record) => {
    if (byId[record.id]) fail("duplicate-id", record.id);
    byId[record.id] = record;
    SNAPSHOTS.forEach((name) => { if (recordState(record, name)) snapshots[name][record.id] = record; });
  });
  return freeze({ by_id: byId, snapshots, records: freeze([...model.records]) });
};
const provenance = (model, raw) => freeze({ raw_sha256: raw === null ? null : hash(raw), canonical_sha256: hash(canonical(model)), snapshot_sha: model.metadata.snapshot_sha, source_basis: model.metadata.source_basis, contract_version: model.contract_version, generic_draft_2020_12: "degraded" });
const candidate = (input, schema) => {
  const raw = typeof input === "string" ? input : null;
  const document = raw === null ? input : parseJson(raw, "model");
  if (!document || Array.isArray(document) || typeof document !== "object") fail("single-document", "expected one JSON object");
  validateModel(document, schema);
  const model = freeze(structuredClone(document));
  return freeze({ model, indexes: indexes(model), provenance: provenance(model, raw) });
};
const makeState = (accepted, controls = {}) => freeze({ model: accepted.model, indexes: accepted.indexes, provenance: accepted.provenance, mode: controls.mode ?? "current", views: freeze([...(controls.views ?? VIEWS)].sort()), search: controls.search ?? "", statuses: freeze([...(controls.statuses ?? [])].sort()), selected: controls.selected ?? null, diff_pair: freeze({ ...(controls.diff_pair ?? { left: "current", right: "target" }) }), stale: controls.stale ?? false, error: controls.error ?? null });
const update = (current, patch) => makeState(current, { ...current, ...patch, views: patch.views ? [...patch.views] : current.views, statuses: patch.statuses ? [...patch.statuses] : current.statuses, diff_pair: patch.diff_pair ? { ...patch.diff_pair } : current.diff_pair });
const allowed = (values, value, category) => values.includes(value) ? value : fail(category, String(value));
const policy = (current, kind) => current.model.canonical_diff_fields[kind];
const invariantViolation = (current, before, after) => [...new Set([...(before?.invariant_refs ?? []), ...(after?.invariant_refs ?? [])])].some((id) => current.model.invariants.find((item) => item.id === id)?.status === "unverified");
const compare = (current, record, before, after) => {
  if (!before) return freeze({ direction: "added", changed_fields: freeze([]), reasons: freeze([]) });
  if (!after) return freeze({ direction: "removed", changed_fields: freeze([]), reasons: freeze([]) });
  if (before.comparison.status === "unresolved" || after.comparison.status === "unresolved") return freeze({ direction: "unresolved", changed_fields: freeze([]), reasons: freeze([...new Set([...before.comparison.reasons, ...after.comparison.reasons])].sort()) });
  const changedFields = policy(current, record.record_kind).filter((entry) => !equal(before.canonical[entry.path], after.canonical[entry.path], entry.comparison)).map((entry) => entry.path).sort();
  return freeze({ direction: changedFields.length ? "changed" : "unchanged", changed_fields: freeze(changedFields), reasons: freeze([]) });
};

export function createState(input, schema) { return makeState(candidate(input, schema)); }
export function replaceDocument(current, input, schema) { try { return freeze({ state: makeState(candidate(input, schema), { ...current, error: null }), error: null }); } catch (cause) { return freeze({ state: current, error: freeze({ category: cause instanceof ContractError ? cause.category : "replacement", detail: cause instanceof Error ? cause.message : String(cause) }) }); } }
export function setMode(current, mode) { return update(current, { mode: allowed(MODES, mode, "mode"), error: null }); }
export function setViews(current, value) { if (!Array.isArray(value) || value.length === 0) fail("views", "non-empty array required"); return update(current, { views: [...new Set(value.map((view) => allowed(VIEWS, view, "view")))], error: null }); }
export function setSearch(current, value) { if (typeof value !== "string") fail("search", "string required"); return update(current, { search: value, error: null }); }
export function setStatusFilters(current, value) { if (!Array.isArray(value)) fail("status", "array required"); const allowedStatuses = new Set(["stale", "unresolved", "violation", ...current.indexes.records.flatMap((record) => Object.values(record.snapshot_states).flatMap((item) => [item.status, item.confidence]))]); value.forEach((item) => { if (typeof item !== "string" || !allowedStatuses.has(item)) fail("status", String(item)); }); return update(current, { statuses: [...new Set(value)], error: null }); }
export function selectRecord(current, id) { if (id !== null && !current.indexes.by_id[id]) fail("selection", String(id)); return update(current, { selected: id, error: null }); }
export function setDiffPair(current, value) { if (!value || !SNAPSHOTS.includes(value.left) || !SNAPSHOTS.includes(value.right)) fail("diff-pair", "unsupported snapshot"); if (value.left === value.right) fail("same-snapshot", value.left); return update(current, { diff_pair: value, error: null }); }
export function swapDiffPair(current) { return setDiffPair(current, { left: current.diff_pair.right, right: current.diff_pair.left }); }
export function lookupRecord(current, id) { return current.indexes.by_id[id] ?? null; }
export function diff(current) {
  const { left, right } = current.diff_pair;
  return freeze([...new Set([...Object.keys(current.indexes.snapshots[left]), ...Object.keys(current.indexes.snapshots[right])])].sort().map((id) => {
    const record = current.indexes.by_id[id]; const before = stateRecord(record, left); const after = stateRecord(record, right); const result = compare(current, record, before, after);
    return freeze({ id, record_kind: record.record_kind, direction: result.direction, before, after, changed_fields: result.changed_fields, reasons: result.reasons, invariant_violation: invariantViolation(current, before, after) });
  }));
}
const recordForQuery = (record, name) => stateRecord(record, name);
const matches = (current, row) => {
  const text = JSON.stringify(row).toLowerCase();
  const diffRow = row.diff ?? null;
  return row.canonical.views.some((view) => current.views.includes(view)) && (!current.search || text.includes(current.search.toLowerCase())) && (!current.statuses.length || current.statuses.some((status) => status === "stale" ? current.stale : status === "unresolved" ? diffRow?.direction === "unresolved" : status === "violation" ? diffRow?.invariant_violation : row.status === status || row.confidence === status));
};
export function query(current) {
  const rows = current.mode === "diff" ? diff(current).map((item) => item.before ?? item.after).map((row, index) => freeze({ ...row, diff: diff(current)[index] })) : Object.values(current.indexes.snapshots[snapshot(current.mode)]).map((record) => recordForQuery(record, snapshot(current.mode)));
  return freeze(rows.filter((row) => matches(current, row)).sort((left, right) => left.id.localeCompare(right.id)));
}
export function counts(current) {
  const modeSnapshot = current.mode === "diff" ? null : snapshot(current.mode);
  const population = modeSnapshot === null ? diff(current).length : Object.keys(current.indexes.snapshots[modeSnapshot]).length;
  const visible = query(current).length;
  const classification = modeSnapshot === "target" && population === 0 ? "valid-empty-target" : modeSnapshot !== null && population === 0 ? "empty-snapshot" : modeSnapshot === null && population === 0 ? "empty-diff" : visible === 0 ? "no-match" : null;
  return freeze({ visible, population, target_membership: Object.keys(current.indexes.snapshots.target).length, classification, valid_empty_target: classification === "valid-empty-target" ? classification : null, no_match: classification === "no-match" ? classification : null });
}
