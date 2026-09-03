# Final F3 — Executable QA / User Risk Check

Дата: 2026-09-03.

REVIEW_ID: F3-P9-EXECUTABLE-QA
SUBJECT: Phase 9 executed result — executable evidence audit
RECEIPT: this file; consolidated in `final-f4-consolidated-stop.md`
VERDICT: APPROVE
REASON:

1. **Every slice carries commands + TRX + receipt** (TRX under
   `evidence/phase-9-legacy-seams-cleanup/logs/`):
   slice 1 — baseline 38 passed/2 known-failed (LIM-P8-2 cluster re-run:
   exactly 5 failed + RR-004 skip); slice 2 — re-pin first run 2/3 (failures
   diagnosed and fixed within the slice, recorded), final 5/5, full suite
   2028/0/1; slice 3 — 26/26; slice 4 — first run 58/4 (GetField on the removed
   field, fixed, recorded), final 62/62 + 178 adjacent/0; slice 5 — static test
   RED (1 failed) then GREEN, focused 101/101, adjacent 118/1→0 after re-pin,
   combined 220/0/1; slice 6 — first run 205/17 (DI seam + reflection sites,
   fixed, recorded), final 300/0/1; slice 7 — full regression first run
   2031/1 (guard source-pin, re-pinned, recorded), final **2032 passed /
   0 failed / 1 skipped**.
2. **Build-before-test discipline**: every focused run preceded by a
   successful `dotnet build -c Debug`; no 0-test "pass" (every filter matched
   real classes; the one early 0-match miss was re-run after a forced rebuild
   and is recorded in the slice-5 trail).
3. **Zero-test rule**: enforced — the plan's own named static test is fixed by
   name (`ApplicationServiceViewModelDecouplingTests`), so the slice-5 filter
   cannot silently match nothing.
4. **Known skip (honest, not pass)**: `ResultsViewModel_LoadsRealProject_
   TwoCollectorsSummaryCardsMatchFile` — external fixture `D:\IA\ace\Тест\тест
   40.smc` absent (RR-004 environment limitation, unchanged since Phase 6).
5. **User risk**: user-visible hydraulic views show canonical values with
   Results-owned display objects (no projection change — stabilization and
   PDF fixtures green); dirty/save/load semantics unchanged (characterized);
   wire compatibility untouched; the one owner-approved behavior change
   (import-less restore, LIM-P8-2 decision B) is recorded in the dossier and
   user-flow map. Manual WPF button/dialog QA remains impossible headless
   (RR-002, same recorded gap as Phase 6/7/8).
6. **Residual risks**: shared-object duplication `ToDomainResult` (pinned both
   sides, slice-3 debt note); `IMarkDirtyService` internal seam (slice-6
   deviation); verifier exemplar pending owner authorization (non-blocking,
   suites PASS).
