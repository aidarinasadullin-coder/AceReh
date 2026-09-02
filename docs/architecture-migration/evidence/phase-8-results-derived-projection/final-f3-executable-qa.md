# Final F3 — Executable QA / User Risk Check

Дата: 2026-09-03.

REVIEW_ID: F3-P8-EXECUTABLE-QA
SUBJECT: Phase 8 executed result — executable evidence audit
RECEIPT: this file; consolidated in `final-f4-consolidated-stop.md`
VERDICT: APPROVE
REASON:

1. **Every slice carries commands + TRX + receipt** (logs under `evidence/phase-8-results-derived-projection/logs/`): slice 1 — 27/27; slice 2 — 69/69; slice 3 — 111 passed/1 known skip; slice 4 — 63/63 (first run 60/3, failures diagnosed and fixed within the slice, recorded); slice 5 — 73 passed/1 known skip (first run 71/2, fixed, recorded); slice 6 — full regression 2023 passed/5 pre-existing/1 known skip (first run 8 NRE failures, fixed via out-overload/fixture fields, recorded); slice 7 — 59 passed/1 known skip.
2. **Build-before-test discipline**: every focused run preceded by a successful `dotnet build -c Debug`; zero occurrences of a 0-test "pass" (all filters verified against real classes; every run shows non-zero executed totals).
3. **Full-suite regression**: 2029 total → 2023 passed / 5 failed / 1 skipped; the 5 failures are the pre-existing import-removal cluster (`LoadProjectDataAsync_{Early,Late}RestoreFailure_*` ×4, `ProjectData_Load_ImportsCustomMaterialsBeforeLayers`), proven pre-existing by git diff HEAD (import calls removed from `ProjectLoadOrchestrator.cs` before this session; 0 occurrences in worktree vs 2 in HEAD). Outside the Phase 8 write-set; flagged for owner decision, not silently "fixed".
4. **Known skip (honest, not pass)**: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` — external fixture `D:\IA\ace\Тест\тест 40.smc` absent (RR-004 environment limitation).
5. **User risk**: user-visible flows exercise canonical values with frozen stabilization contracts; wire compatibility unchanged; the two behavior-contract re-pins (adapter-seam → canonical-source assertions) are recorded with preserved user-visible contracts. Manual WPF button/dialog QA remains impossible in this headless environment (same recorded gap as Phase 6/7).
6. **Residual risks**: shared `CircuitRow` mutation path (Phase 9); material-rename window on reconstructed layers (documented in slice-3 receipt); staged-scope fallback pending owner confirmation.
