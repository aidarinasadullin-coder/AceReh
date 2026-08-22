# Task 12.1 protected correction baseline

## Repository boundary

- Root: `D:/IA/ace v.2`
- HEAD: `e655735dfa66c00cf9c53be93d511eda8989e8bf`
- Branch/upstream: `master` / `origin/master`
- Ahead/behind: `33` / `0`
- `baseline-status.bin`: 14197 bytes, SHA-256 `CD5A13F405E9D92576B7FBEACE7A19253369C9347474EC6E115721335BB51239`, 246 non-empty NUL records including the branch header.
- `baseline-staged.bin`: 0 bytes, SHA-256 `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855`.
- `baseline-unstaged.bin`: 1489 bytes, SHA-256 `0034B017A3D88A81C3A1935D87A58681505BCF527B158BD2ACEEFFC1D05BFA6F`.

The staged path stream was empty before capture and remained empty after all baseline artifacts were created. No Git index or tracked source/test file was changed by this task.

## Authority reconciliation

The active correction plan is SHA-256 `5BCEE8D2C450DFBDC7F05A044CD8DC7D1BB065F1678A825283AA18131BF12640`. The parent Phase 3 plan has Task 11 checked, Task 12 checked, and Task 13 unchecked. The exact Task 11 TRX reproduces `49/49`; its receipt records Debug build `0 warnings / 0 errors`. Task 12's five referenced logs and two TRX files exist and match every receipt hash. Parsed affected results are `293 Passed`, one known fixture `NotExecuted`, and zero failed. Parsed full Release results are `1613 Passed`, three named `NotExecuted`, and zero failed; the receipt accurately preserves the distinct console and TRX logger representations.

`TASK_CONTEXT.md` is stale because it still identifies Task 11 as unstarted/next. That known mismatch does not override the checked parent plan and reproducible Task 11/12 evidence; it establishes why this correction is legitimately Task 12.1. The context was not modified because reconciliation succeeded and Task 1 permits a context edit only for a blocker.

## Protected allow-list

`allowlist-preimages.json` records all 18 exact plan paths, including existence state for three future outputs and byte length/SHA-256 for every existing file. Eleven existing allow-listed paths were already dirty at capture and are explicitly status-bound in the ledger. Subsequent work must preserve these preimages outside task-owned hunks.

## Adversarial self-check

The verifier first accepted the unmodified in-memory path/hash ledger against live files. It then changed one expected SHA-256 nibble in memory and rejected the ledger with `hash mismatch`. In a separate probe it changed one expected path in memory and rejected the ledger with `path set mismatch`. Neither mutation was written to disk.

## Decision

Task 11 and Task 12 authority and raw evidence reconcile, the staged set is unchanged and empty, and Task 13 is unstarted. Task 12.1 baseline is released; no RED test or production work was performed.

PASS
