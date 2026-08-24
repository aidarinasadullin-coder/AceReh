# Todo 5 Blocker Analysis

Plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`

## Blocker

The focused merged-boundary suite has one failure:

```text
ProjectSessionHydraulicsStateTests.BeginCompleteAndFailCalculation_UseExpectedPhasesAndResultSubtree
```

The test calls `FailCalculation("boom")` after `CompleteCalculation(...)` and
expects `Changed`. The frozen phase-5 contract requires `FailCalculation` to
reject unless the canonical state is in the `Calculating` phase. The current
implementation returns `Rejected` with the exact error:

```text
FailCalculation requires an active calculation.
```

This is an acceptance mismatch, not an AMZ-H1 closed-API transition problem.
Changing the implementation to accept an inactive failure would violate the
approved plan and its negative-probe acceptance criterion. The test must be
reconciled by the owner or the plan contract must be explicitly superseded.

## Verification

- Plan/state validation: passed.
- Production Debug build: passed, 0 warnings, 0 errors.
- Production Release build: passed, 0 warnings, 0 errors.
- Non-contradictory focused suites: passed, 44/44.
- Project-session state tests excluding the contradictory test: passed, 8/8.
- Full merged focused suite: 52/53 passed; 1 failed at the test above.
- AMZ-H1 bridge: not required.

No characterization or production test files were modified.

## RESOLUTION (planner decision, post-blocker)

The only production caller of `SetHydraulicsError` is
`CircuitsViewModel.cs:583`, inside the calculation exception handler between
`SetHydraulicsCalculating` and `ResetHydraulicsState`. Production therefore
reaches `FailCalculation` only while the canonical phase is `Calculating`.

The strict closed-API semantics are retained: `FailCalculation` changes state
only from `Calculating`; from `Actual` or any other phase it returns
`Rejected`, leaves the snapshot unchanged, and emits no event. The Todo 3 test
was corrected to assert that rejection after completion and to cover the happy
path `BeginCalculation -> FailCalculation`, followed by `SystemApply` status
normalization to `Actual`.

This resolution is a test-contract correction and a canonical reset-normalization
fix. It does not weaken the production API or add an AMZ-H1 bridge.
