# Writer Authority

Plan SHA: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`

Commit `8ba5a36` moved the hydraulics context subscription and approved
publication boundary to `HydraulicsStateCoordinator`. The payload source literal
remains exactly `"CircuitsViewModel"` for compatibility.

The authority test now retains the thermal assertion unchanged and adds a
hydraulics source scan. It accepts `HydraulicsStateCoordinator` as the sole
approved production writer and explicitly records the existing
`CircuitsViewModel` null-coordinator compatibility seam used by isolated test
construction. Any other production file containing `UpdateHydraulics` is
rejected by the assertion.

Current source inspection found one coordinator publication call and no second
publication in the coordinator. The VM call is only in its null-coordinator
fallback branch; production DI supplies the coordinator, so removing that seam
would change existing isolated-fixture behavior and exceed Todo 8.
