# Todo 3 Divergence Notes

Plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`

The original `BeginCompleteAndFailCalculation_UseExpectedPhasesAndResultSubtree`
assertion expected `FailCalculation` to succeed after completion. That was
written before the compatibility semantics were pinned. The post-blocker
correction now asserts rejection from `Actual` with no snapshot or event
change, and separately covers the accepted `Calculating -> Error` path.

Resolution: `task-5/blocker-analysis.md`, section
`RESOLUTION (planner decision, post-blocker)`.
