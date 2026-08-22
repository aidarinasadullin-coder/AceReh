import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { mkdtemp, mkdir, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";
import { test } from "node:test";

import { validateState } from "./validate-state.mjs";

const sha256 = (bytes) => createHash("sha256").update(bytes).digest("hex").toUpperCase();

const validState = (planBytes) => ({
  schemaVersion: 1,
  phase: "phase-3.1-climate-thermal-invalidation-on-project-load",
  stage: "completed",
  plan: {
    path: "docs/architecture-migration/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md",
    mirrorPath: ".omo/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md",
    bytes: planBytes.length,
    sha256: sha256(planBytes),
    frozen: true,
  },
  reviews: {
    planning: "approved",
    finalDomains: { conformance: "approved", architecture: "approved", executable: "approved" },
    planningReceiptPath: "docs/architecture-migration/evidence/phase-3.1/planning.md",
    finalReceiptPath: "docs/architecture-migration/evidence/phase-3.1/final.md",
    terminalRetryCount: 1,
  },
  ownerGates: { planApproval: "approved", executionAuthorization: "approved", resultAcceptance: "accepted" },
  lastCompletedTask: "task-12",
  pendingGates: [],
  nextAction: "Await an explicit owner direction to begin a separate Phase 4 planning workflow; Phase 4 is not started, approved, authorized, or executed.",
  stop: true,
  blocker: null,
});

async function repositoryFixture() {
  const root = await mkdtemp(path.join(tmpdir(), "state-validator-"));
  const canonical = path.join(root, "docs", "architecture-migration", "plans", "phase-3.1-climate-thermal-invalidation-on-project-load.md");
  const mirror = path.join(root, ".omo", "plans", "phase-3.1-climate-thermal-invalidation-on-project-load.md");
  const receipt = path.join(root, "docs", "architecture-migration", "evidence", "phase-3.1", "final.md");
  const planningReceipt = path.join(root, "docs", "architecture-migration", "evidence", "phase-3.1", "planning.md");
  const planBytes = Buffer.from("approved plan\n");
  await mkdir(path.dirname(canonical), { recursive: true });
  await mkdir(path.dirname(mirror), { recursive: true });
  await mkdir(path.dirname(receipt), { recursive: true });
  await mkdir(path.join(root, ".git"));
  await writeFile(canonical, planBytes);
  await writeFile(mirror, planBytes);
  const subject = `phase-3.1-climate-thermal-invalidation-on-project-load@${sha256(planBytes)}`;
  await writeFile(planningReceipt, `REVIEW_ID: planning\nSUBJECT: ${subject}\nRECEIPT: terminal plan review\nVERDICT: APPROVE\nREASON: plan approved\n`);
  await writeFile(receipt, `REVIEW_ID: final\nSUBJECT: ${subject}\nRECEIPT: F1-F4\nVERDICT: APPROVE\nREASON: all domains approved\n`);
  return { root, planBytes, receipt };
}

test("accepts a valid completed state and exact frozen plans", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();

  // When
  const result = await validateState(validState(planBytes), { root, checkPlan: true });

  // Then
  assert.deepEqual(result, { valid: true, phase: "phase-3.1-climate-thermal-invalidation-on-project-load", stage: "completed", diagnostics: [] });
});

test("rejects unknown fields", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = { ...validState(planBytes), extra: true };

  // When / Then
  await assert.rejects(validateState(state, { root }), /unknown field: state\.extra/);
});

test("rejects a plan path that escapes the repository", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.plan.path = "../outside.md";

  // When / Then
  await assert.rejects(validateState(state, { root, checkPlan: true }), /plan\.path must match the canonical phase plan path/);
});

test("rejects a frozen plan hash mismatch", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.plan.sha256 = "0".repeat(64);

  // When / Then
  await assert.rejects(validateState(state, { root, checkPlan: true }), /plan\.path SHA-256 mismatch/);
});

test("rejects completed gate contradictions", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.ownerGates.resultAcceptance = "pending";

  // When / Then
  await assert.rejects(validateState(state, { root }), /completed requires resultAcceptance accepted/);
});

test("rejects completed state without approved planning and owner gates", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.reviews.planning = "pending";
  state.ownerGates.planApproval = "pending";

  // When / Then
  await assert.rejects(validateState(state, { root }), /completed requires planning approved/);
});

test("rejects approved state with premature execution authorization", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.stage = "approved";
  state.ownerGates.executionAuthorization = "approved";
  state.ownerGates.resultAcceptance = "pending";
  state.reviews.finalDomains = { conformance: "pending", architecture: "pending", executable: "pending" };
  state.reviews.finalReceiptPath = null;
  state.stop = true;

  // When / Then
  await assert.rejects(validateState(state, { root }), /approved requires executionAuthorization pending/);
});

test("rejects awaiting owner acceptance with an unapproved final domain", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.stage = "awaiting-owner-acceptance";
  state.ownerGates.resultAcceptance = "pending";
  state.reviews.finalDomains.executable = "pending";

  // When / Then
  await assert.rejects(validateState(state, { root }), /awaiting-owner-acceptance requires all final domains approved/);
});

test("rejects terminal retry counts greater than one", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.reviews.terminalRetryCount = 2;

  // When / Then
  await assert.rejects(validateState(state, { root }), /terminalRetryCount must be an integer from 0 to 1/);
});

test("rejects a completed state whose receipt is not a file", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.reviews.finalReceiptPath = "docs/architecture-migration/evidence/phase-3.1";

  // When / Then
  await assert.rejects(validateState(state, { root }), /reviews\.finalReceiptPath must be a regular file/);
});

test("rejects a completed state without an approving terminal receipt", async () => {
  // Given
  const { root, planBytes, receipt } = await repositoryFixture();
  const state = validState(planBytes);
  await writeFile(receipt, `REVIEW_ID: final\nSUBJECT: ${state.phase}@${state.plan.sha256}\nRECEIPT: F1-F4\nVERDICT: BLOCKED\nREASON: unresolved\n`);

  // When / Then
  await assert.rejects(validateState(state, { root }), /terminal receipt must have VERDICT: APPROVE/);
});

test("rejects a malformed terminal receipt", async () => {
  // Given
  const { root, planBytes, receipt } = await repositoryFixture();
  const state = validState(planBytes);
  await writeFile(receipt, "UNEXPECTED: extra\nVERDICT: APPROVE\n");

  // When / Then
  await assert.rejects(validateState(state, { root }), /terminal receipt must contain exactly/);
});

test("rejects a terminal receipt for another plan identity", async () => {
  // Given
  const { root, planBytes, receipt } = await repositoryFixture();
  const state = validState(planBytes);
  await writeFile(receipt, "REVIEW_ID: final\nSUBJECT: another-phase@0000\nRECEIPT: F1-F4\nVERDICT: APPROVE\nREASON: stale\n");

  // When / Then
  await assert.rejects(validateState(state, { root }), /terminal receipt SUBJECT must match phase and plan SHA-256/);
});

test("rejects awaiting owner approval without a planning receipt", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.stage = "awaiting-owner-approval";
  state.ownerGates = { planApproval: "pending", executionAuthorization: "pending", resultAcceptance: "pending" };
  state.reviews.finalDomains = { conformance: "pending", architecture: "pending", executable: "pending" };
  state.reviews.planningReceiptPath = null;
  state.reviews.finalReceiptPath = null;
  state.pendingGates = ["planApproval"];
  state.stop = true;

  // When / Then
  await assert.rejects(validateState(state, { root }), /planning review requires reviews.planningReceiptPath/);
});

test("rejects one receipt path used for both owner gates", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.reviews.finalReceiptPath = state.reviews.planningReceiptPath;

  // When / Then
  await assert.rejects(validateState(state, { root }), /planning and final receipt paths must differ/);
});

test("rejects terminal plan review without a frozen candidate", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.stage = "momus-final-review";
  state.plan = null;
  state.reviews = { planning: "pending", finalDomains: { conformance: "pending", architecture: "pending", executable: "pending" }, planningReceiptPath: null, finalReceiptPath: null, terminalRetryCount: 0 };
  state.ownerGates = { planApproval: "pending", executionAuthorization: "pending", resultAcceptance: "pending" };
  state.stop = false;

  // When / Then
  await assert.rejects(validateState(state, { root }), /momus-final-review requires a frozen plan candidate/);
});

test("rejects awaiting owner approval with premature gates", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.stage = "awaiting-owner-approval";
  state.ownerGates = { planApproval: "pending", executionAuthorization: "approved", resultAcceptance: "pending" };
  state.reviews.finalDomains = { conformance: "pending", architecture: "pending", executable: "pending" };
  state.reviews.finalReceiptPath = null;

  // When / Then
  await assert.rejects(validateState(state, { root }), /awaiting-owner-approval requires executionAuthorization pending/);
});

test("rejects a completed state with a blocker", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.blocker = "stale blocker";

  // When / Then
  await assert.rejects(validateState(state, { root }), /completed requires blocker null/);
});

test("rejects a completed state that advertises execution", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.nextAction = "/architecture-start phase-4";

  // When / Then
  await assert.rejects(validateState(state, { root }), /completed nextAction must await explicit owner direction/);
});

test("rejects a completed state that bypasses separate planning", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.nextAction = "Await an explicit owner direction to execute Phase 4.";

  // When / Then
  await assert.rejects(validateState(state, { root }), /completed nextAction must await explicit owner direction/);
});

test("rejects premature approvals before planning", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.stage = "awaiting-plan-start";
  state.plan = null;
  state.reviews.planning = "approved";
  state.reviews.finalDomains = { conformance: "pending", architecture: "pending", executable: "pending" };
  state.reviews.planningReceiptPath = null;
  state.reviews.finalReceiptPath = null;
  state.ownerGates = { planApproval: "pending", executionAuthorization: "pending", resultAcceptance: "pending" };

  // When / Then
  await assert.rejects(validateState(state, { root }), /awaiting-plan-start requires reviews.planning pending/);
});

test("rejects identical canonical and mirror plan paths", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.plan.mirrorPath = state.plan.path;

  // When / Then
  await assert.rejects(validateState(state, { root, checkPlan: true }), /canonical and mirror plan paths must differ/);
});

test("accepts an awaiting-plan-start state without a plan", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.phase = "phase-4";
  state.stage = "awaiting-plan-start";
  state.plan = null;
  state.reviews.planning = "pending";
  state.reviews.finalDomains = { conformance: "pending", architecture: "pending", executable: "pending" };
  state.reviews.planningReceiptPath = null;
  state.reviews.finalReceiptPath = null;
  state.reviews.terminalRetryCount = 0;
  state.ownerGates = { planApproval: "pending", executionAuthorization: "pending", resultAcceptance: "pending" };
  state.lastCompletedTask = null;
  state.pendingGates = ["planning"];
  state.nextAction = "/architecture-plan phase-4";
  state.stop = false;

  // When
  const result = await validateState(state, { root, checkPlan: false });

  // Then
  assert.equal(result.stage, "awaiting-plan-start");
});

test("rejects a plan check before a frozen plan exists", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  const state = validState(planBytes);
  state.stage = "awaiting-plan-start";
  state.plan = null;
  state.reviews.planning = "pending";
  state.reviews.finalDomains = { conformance: "pending", architecture: "pending", executable: "pending" };
  state.reviews.planningReceiptPath = null;
  state.reviews.finalReceiptPath = null;
  state.ownerGates = { planApproval: "pending", executionAuthorization: "pending", resultAcceptance: "pending" };

  // When / Then
  await assert.rejects(validateState(state, { root, checkPlan: true }), /--check-plan requires a frozen plan/);
});

test("reports stale external Boulder state without invalidating completed state", async () => {
  // Given
  const { root, planBytes } = await repositoryFixture();
  await mkdir(path.join(root, ".omo"), { recursive: true });
  await writeFile(path.join(root, ".omo", "boulder.json"), JSON.stringify({ active_work_id: "phase-3" }));

  // When
  const result = await validateState(validState(planBytes), { root });

  // Then
  assert.deepEqual(result.diagnostics, ["external Boulder active work differs: phase-3"]);
});

test("rejects a production CLI state override", () => {
  // Given / When
  const result = spawnSync(process.execPath, ["docs/architecture-migration/workflow/validate-state.mjs", "validate", "--state", "alternate.json"], { cwd: path.resolve("."), encoding: "utf8" });

  // Then
  assert.equal(result.status, 1);
  assert.match(result.stderr, /usage: validate \[--check-plan\]/);
});
