import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { lstat, readFile, realpath } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const STAGES = new Set([
  "awaiting-plan-start", "metis-analysis", "prometheus-draft", "awaiting-primary-plan",
  "sisyphus-review", "momus-review", "prometheus-corrections", "momus-final-review",
  "awaiting-owner-approval", "approved", "executing", "verification",
  "awaiting-owner-acceptance", "completed", "blocked",
]);
const TOP_KEYS = ["schemaVersion", "phase", "stage", "plan", "reviews", "ownerGates", "lastCompletedTask", "pendingGates", "nextAction", "stop", "blocker"];
const PLAN_KEYS = ["path", "mirrorPath", "bytes", "sha256", "frozen"];
const REVIEW_KEYS = ["planning", "finalDomains", "planningReceiptPath", "finalReceiptPath", "terminalRetryCount"];
const DOMAIN_KEYS = ["conformance", "architecture", "executable"];
const GATE_KEYS = ["planApproval", "executionAuthorization", "resultAcceptance"];
const EXECUTION_STAGES = new Set(["executing", "verification", "awaiting-owner-acceptance", "completed"]);
const FROZEN_PLAN_STAGES = new Set(["awaiting-owner-approval", "approved", ...EXECUTION_STAGES]);
const TERMINAL_REVIEW_STAGES = new Set(["sisyphus-review", "momus-review", "momus-final-review"]);
const RECEIPT_STAGES = new Set(["awaiting-owner-acceptance", "completed"]);
const PRE_FREEZE_STAGES = new Set([...STAGES].filter((stage) => !FROZEN_PLAN_STAGES.has(stage) && stage !== "blocked"));
const RECEIPT_KEYS = ["REVIEW_ID", "SUBJECT", "RECEIPT", "VERDICT", "REASON"];

const fail = (message) => { throw new Error(message); };
const isObject = (value) => value !== null && typeof value === "object" && !Array.isArray(value);
const requireObject = (value, label) => isObject(value) ? value : fail(`${label} must be an object`);
const requireText = (value, label) => typeof value === "string" && value.length > 0 ? value : fail(`${label} must be a non-empty string`);
const exactKeys = (value, keys, label) => {
  const actual = Object.keys(requireObject(value, label));
  const unknown = actual.find((key) => !keys.includes(key));
  if (unknown) fail(`unknown field: ${label}.${unknown}`);
  const missing = keys.find((key) => !actual.includes(key));
  if (missing) fail(`missing field: ${label}.${missing}`);
};
const approvedDomains = (domains) => DOMAIN_KEYS.every((key) => domains[key] === "approved");

function validateShape(state) {
  exactKeys(state, TOP_KEYS, "state");
  exactKeys(state.reviews, REVIEW_KEYS, "reviews");
  exactKeys(state.reviews.finalDomains, DOMAIN_KEYS, "reviews.finalDomains");
  exactKeys(state.ownerGates, GATE_KEYS, "ownerGates");
  if (state.schemaVersion !== 1) fail("schemaVersion must be 1");
  requireText(state.phase, "phase");
  if (!STAGES.has(state.stage)) fail(`unsupported stage: ${String(state.stage)}`);
  if (state.plan !== null) {
    exactKeys(state.plan, PLAN_KEYS, "plan");
    requireText(state.plan.path, "plan.path");
    requireText(state.plan.mirrorPath, "plan.mirrorPath");
    if (!Number.isInteger(state.plan.bytes) || state.plan.bytes < 0) fail("plan.bytes must be a non-negative integer");
    if (typeof state.plan.sha256 !== "string" || !/^[A-F0-9]{64}$/.test(state.plan.sha256)) fail("plan.sha256 must be 64 uppercase hexadecimal characters");
    if (typeof state.plan.frozen !== "boolean") fail("plan.frozen must be boolean");
  }
  if (!["pending", "approved"].includes(state.reviews.planning)) fail("reviews.planning must be pending or approved");
  for (const key of DOMAIN_KEYS) if (!["pending", "approved"].includes(state.reviews.finalDomains[key])) fail(`reviews.finalDomains.${key} must be pending or approved`);
  if (state.reviews.planningReceiptPath !== null) requireText(state.reviews.planningReceiptPath, "reviews.planningReceiptPath");
  if (state.reviews.finalReceiptPath !== null) requireText(state.reviews.finalReceiptPath, "reviews.finalReceiptPath");
  if (!Number.isInteger(state.reviews.terminalRetryCount) || state.reviews.terminalRetryCount < 0 || state.reviews.terminalRetryCount > 1) fail("terminalRetryCount must be an integer from 0 to 1");
  if (!["pending", "approved"].includes(state.ownerGates.planApproval)) fail("ownerGates.planApproval must be pending or approved");
  if (!["pending", "approved"].includes(state.ownerGates.executionAuthorization)) fail("ownerGates.executionAuthorization must be pending or approved");
  if (!["pending", "accepted", "rejected"].includes(state.ownerGates.resultAcceptance)) fail("ownerGates.resultAcceptance must be pending, accepted, or rejected");
  if (state.lastCompletedTask !== null && typeof state.lastCompletedTask !== "string") fail("lastCompletedTask must be string or null");
  if (!Array.isArray(state.pendingGates) || state.pendingGates.some((gate) => typeof gate !== "string") || new Set(state.pendingGates).size !== state.pendingGates.length) fail("pendingGates must be an array of unique strings");
  requireText(state.nextAction, "nextAction");
  if (typeof state.stop !== "boolean") fail("stop must be boolean");
  if (state.blocker !== null && (typeof state.blocker !== "string" || state.blocker.length === 0)) fail("blocker must be a non-empty string or null");
}

function validateInvariants(state) {
  if (TERMINAL_REVIEW_STAGES.has(state.stage) && (state.plan === null || !state.plan.frozen)) fail(`${state.stage} requires a frozen plan candidate`);
  if (PRE_FREEZE_STAGES.has(state.stage)) {
    if (state.reviews.planning !== "pending") fail(`${state.stage} requires reviews.planning pending`);
    if (approvedDomains(state.reviews.finalDomains)) fail(`${state.stage} cannot have all final domains approved`);
    if (Object.values(state.ownerGates).some((gate) => gate !== "pending")) fail(`${state.stage} requires all owner gates pending`);
    if (state.reviews.planningReceiptPath !== null || state.reviews.finalReceiptPath !== null) fail(`${state.stage} requires receipt paths null`);
  }
  if (FROZEN_PLAN_STAGES.has(state.stage) && (state.plan === null || !state.plan.frozen)) fail(`${state.stage} requires a frozen plan`);
  if (FROZEN_PLAN_STAGES.has(state.stage) && state.reviews.planning !== "approved") fail(`${state.stage} requires planning approved`);
  if (FROZEN_PLAN_STAGES.has(state.stage) && state.reviews.planningReceiptPath === null) fail("planning review requires reviews.planningReceiptPath");
  if (state.stage === "awaiting-owner-approval" && state.ownerGates.planApproval !== "pending") fail("awaiting-owner-approval requires planApproval pending");
  if (state.stage === "awaiting-owner-approval" && state.ownerGates.executionAuthorization !== "pending") fail("awaiting-owner-approval requires executionAuthorization pending");
  if (state.stage === "awaiting-owner-approval" && state.ownerGates.resultAcceptance !== "pending") fail("awaiting-owner-approval requires resultAcceptance pending");
  if (state.stage === "awaiting-owner-approval" && (approvedDomains(state.reviews.finalDomains) || state.reviews.finalReceiptPath !== null)) fail("awaiting-owner-approval cannot contain final approval evidence");
  if ((state.stage === "approved" || EXECUTION_STAGES.has(state.stage)) && state.ownerGates.planApproval !== "approved") fail(`${state.stage} requires planApproval approved`);
  if (state.stage === "approved" && state.ownerGates.executionAuthorization !== "pending") fail("approved requires executionAuthorization pending");
  if (state.stage === "approved" && state.ownerGates.resultAcceptance !== "pending") fail("approved requires resultAcceptance pending");
  if (EXECUTION_STAGES.has(state.stage) && state.ownerGates.executionAuthorization !== "approved") fail(`${state.stage} requires executionAuthorization approved`);
  if (["executing", "verification"].includes(state.stage) && state.ownerGates.resultAcceptance !== "pending") fail(`${state.stage} requires resultAcceptance pending`);
  if (["approved", "executing", "verification"].includes(state.stage) && approvedDomains(state.reviews.finalDomains)) fail(`${state.stage} cannot have all final domains approved`);
  if (["approved", "executing", "verification"].includes(state.stage) && state.reviews.finalReceiptPath !== null) fail(`${state.stage} requires reviews.finalReceiptPath null`);
  if (state.stage === "awaiting-owner-acceptance") {
    if (!approvedDomains(state.reviews.finalDomains)) fail("awaiting-owner-acceptance requires all final domains approved");
    if (state.ownerGates.resultAcceptance !== "pending") fail("awaiting-owner-acceptance requires resultAcceptance pending");
  }
  if (state.stage === "completed") {
    if (!approvedDomains(state.reviews.finalDomains)) fail("completed requires all final domains approved");
    if (state.ownerGates.resultAcceptance !== "accepted") fail("completed requires resultAcceptance accepted");
    if (state.pendingGates.length !== 0) fail("completed requires pendingGates empty");
    if (!state.stop) fail("completed requires stop true");
    if (state.blocker !== null) fail("completed requires blocker null");
    if (!/^Await an explicit owner direction to begin a separate .+ planning workflow; .+ is not started, approved, authorized, or executed\.$/.test(state.nextAction)) fail("completed nextAction must await explicit owner direction for a separate planning workflow");
  }
  if (state.stage === "blocked" && (state.blocker === null || !state.stop)) fail("blocked requires blocker and stop true");
  if (state.reviews.planningReceiptPath !== null && state.reviews.planningReceiptPath === state.reviews.finalReceiptPath) fail("planning and final receipt paths must differ");
}

const insideRoot = (root, candidate) => {
  const relative = path.relative(root, candidate);
  return relative !== "" && relative !== ".." && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative);
};

async function planFile(root, relativePath, label, state) {
  if (path.isAbsolute(relativePath)) fail(`${label} must be repository-relative`);
  const candidate = path.resolve(root, relativePath);
  if (!insideRoot(root, candidate)) fail(`${label} must stay within the repository`);
  const resolved = await realpath(candidate).catch(() => fail(`${label} does not exist`));
  if (!insideRoot(root, resolved)) fail(`${label} must stay within the repository`);
  const metadata = await lstat(resolved);
  if (!metadata.isFile()) fail(`${label} must be a regular file`);
  const bytes = await readFile(resolved);
  if (bytes.length !== state.plan.bytes) fail(`${label} byte count mismatch`);
  const digest = createHash("sha256").update(bytes).digest("hex").toUpperCase();
  if (digest !== state.plan.sha256) fail(`${label} SHA-256 mismatch`);
  return bytes;
}

async function validateReceipt(root, relativePath, label, state) {
  if (relativePath === null) fail(`${label} is required`);
  if (path.isAbsolute(relativePath)) fail(`${label} must be repository-relative`);
  const candidate = path.resolve(root, relativePath);
  if (!insideRoot(root, candidate)) fail(`${label} must stay within the repository`);
  const resolved = await realpath(candidate).catch(() => fail(`${label} does not exist`));
  if (!insideRoot(root, resolved)) fail(`${label} must stay within the repository`);
  if (!(await lstat(resolved)).isFile()) fail(`${label} must be a regular file`);
  const receipt = await readFile(resolved, "utf8");
  const fields = receipt.split(/\r?\n/).flatMap((line) => {
    const match = /^([A-Z_]+): (.+)$/.exec(line);
    return match ? [[match[1], match[2]]] : [];
  });
  if (fields.length !== RECEIPT_KEYS.length || fields.some(([key], index) => key !== RECEIPT_KEYS[index])) fail("terminal receipt must contain exactly REVIEW_ID, SUBJECT, RECEIPT, VERDICT, and REASON");
  const values = Object.fromEntries(fields);
  if (values.VERDICT !== "APPROVE") fail("terminal receipt must have VERDICT: APPROVE");
  if (values.SUBJECT !== `${state.phase}@${state.plan.sha256}`) fail("terminal receipt SUBJECT must match phase and plan SHA-256");
}

async function boulderDiagnostic(root, phase) {
  const file = path.join(root, ".omo", "boulder.json");
  try {
    const boulder = JSON.parse(await readFile(file, "utf8"));
    const active = typeof boulder.active_work_id === "string" ? boulder.active_work_id : null;
    return active && active !== phase ? [`external Boulder active work differs: ${active}`] : [];
  } catch {
    return [];
  }
}

export async function validateState(state, options) {
  const root = path.resolve(options.root);
  validateShape(state);
  validateInvariants(state);
  if (options.checkPlan) {
    if (state.plan === null) fail("--check-plan requires a frozen plan");
    if (!state.plan.frozen) fail("--check-plan requires plan.frozen true");
    if (state.plan.path === state.plan.mirrorPath) fail("canonical and mirror plan paths must differ");
    if (state.plan.path !== `docs/architecture-migration/plans/${state.phase}.md`) fail("plan.path must match the canonical phase plan path");
    if (state.plan.mirrorPath !== `.omo/plans/${state.phase}.md`) fail("plan.mirrorPath must match the phase mirror plan path");
    const canonical = await planFile(root, state.plan.path, "plan.path", state);
    const mirror = await planFile(root, state.plan.mirrorPath, "plan.mirrorPath", state);
    if (!canonical.equals(mirror)) fail("canonical and mirror plans differ");
  }
  if (FROZEN_PLAN_STAGES.has(state.stage)) await validateReceipt(root, state.reviews.planningReceiptPath, "reviews.planningReceiptPath", state);
  if (RECEIPT_STAGES.has(state.stage)) await validateReceipt(root, state.reviews.finalReceiptPath, "reviews.finalReceiptPath", state);
  return { valid: true, phase: state.phase, stage: state.stage, diagnostics: await boulderDiagnostic(root, state.phase) };
}

function discoverRoot() {
  const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
  return execFileSync("git", ["-C", scriptDirectory, "rev-parse", "--show-toplevel"], { encoding: "utf8" }).trim();
}

function argumentsForCli(argv) {
  if (argv[0] !== "validate") fail("usage: validate [--check-plan]");
  let checkPlan = false;
  for (let index = 1; index < argv.length; index += 1) {
    if (argv[index] === "--check-plan") checkPlan = true;
    else fail("usage: validate [--check-plan]");
  }
  return { checkPlan };
}

async function main() {
  try {
    const root = discoverRoot();
    const options = argumentsForCli(process.argv.slice(2));
    const stateFile = path.resolve(root, "docs", "architecture-migration", "STATE.json");
    const resolvedState = await realpath(stateFile).catch(() => fail("canonical STATE.json does not exist"));
    if (!insideRoot(root, resolvedState)) fail("canonical STATE.json must stay within the repository");
    if (!(await lstat(resolvedState)).isFile()) fail("canonical STATE.json must be a regular file");
    const state = JSON.parse(await readFile(resolvedState, "utf8"));
    process.stdout.write(`${JSON.stringify(await validateState(state, { root, checkPlan: options.checkPlan }))}\n`);
  } catch (error) {
    process.stderr.write(`state validation failed: ${error instanceof Error ? error.message : String(error)}\n`);
    process.exitCode = 1;
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) await main();
