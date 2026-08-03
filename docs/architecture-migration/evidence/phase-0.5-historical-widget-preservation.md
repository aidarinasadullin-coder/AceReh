---
phase: phase-0.5-model-driven-architecture-widget
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: execution-time historical widget bytes verified against Task 1 ledger row 20; Task 1 protected-worktree ledger
generated_at_utc: 2026-07-31T12:00:00.0000000Z
working_directory: D:/IA/ace v.2
status: pass
---

# Phase 0.5 Historical Widget Preservation

## Mandatory Evidence Metadata

| Field | Value |
| --- | --- |
| Phase | `phase-0.5-model-driven-architecture-widget` |
| Execution snapshot SHA | `f0d19c34ac03075d64548f1059e9c6626d3596b5` |
| Source basis | Execution-time bytes of `architecture_widget.html`, matched to Task 1 ledger row 20 before copy; Task 1 protected-worktree ledger |
| Capture timestamp (UTC) | `2026-07-31T12:00:00.0000000Z` |
| Canonical working directory | `D:/IA/ace v.2` |
| Tool version | PowerShell `5.1.19041.6456`; Git `git version 2.53.0.windows.1` |
| Status | `pass` |
| Assertion totals | `20/20 pass` |
| Source input | `docs/architecture-migration/architecture_widget.html` |
| Archive output | `docs/architecture-migration/archive/architecture_widget.phase-0-historical.html` |
| Source bytes / SHA-256 before copy | `37294` / `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA` |
| Source bytes / SHA-256 after copy | `37294` / `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA` |
| Archive bytes / SHA-256 | `37294` / `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA` |
| Byte equality | `pass`: independent byte-array comparison found equal lengths and no differing byte |
| Changed-path allow-list | `pass`: Task 2 adds exactly the allow-listed archive and this receipt; the Task 1 receipt remains the only earlier Phase 0.5-created path |

## Provenance And Runtime Isolation

Both `architecture_widget.html` and this archival copy are provenance-only historical presentation artifacts. Neither is a runtime input nor a generator input. The future generated path is `docs/architecture-migration/architecture_widget.generated.html`; it is absent at Task 2 and differs textually from both the historical and archive paths.

The current applicable allow-listed runtime/generator source candidates were enumerated:

```text
docs/architecture-migration/widget/model-contract.mjs
docs/architecture-migration/widget/architecture-widget.template.html
docs/architecture-migration/widget/architecture-widget.css
docs/architecture-migration/widget/architecture-widget.mjs
docs/architecture-migration/widget/generate-widget.mjs
docs/architecture-migration/widget/verify-widget.mjs
docs/architecture-migration/widget/browser-qa.mjs
```

All seven are `not-yet-created`; therefore no current runtime/generator source exists to read either historical path. A read-only Git tracked-source search over `src` found zero references to `architecture_widget.html` and zero references to `architecture_widget.phase-0-historical.html`. This is a current-source result, not a repo-wide guarantee about later Task 3+ files. Plans, evidence, documentation, the historical source, and archive content were intentionally excluded from the runtime/generator reference claim.

## Exact Commands And Exit Codes

All Git commands were read-only and prefixed with `$env:GIT_MASTER='1';`.

```powershell
$env:GIT_MASTER='1'; git rev-parse --show-toplevel
$env:GIT_MASTER='1'; git -c core.quotepath=false status --porcelain=v1 --untracked-files=all
$ErrorActionPreference='Stop'; Test-Path/Get-Item/Get-FileHash for source, archive parent, and destination
[System.IO.File]::Copy($source, $archive, $false)
[System.IO.File]::ReadAllBytes($source); [System.IO.File]::ReadAllBytes($archive)
Get-FileHash -LiteralPath $source -Algorithm SHA256
Get-FileHash -LiteralPath $archive -Algorithm SHA256
$env:GIT_MASTER='1'; git grep --line-number --fixed-strings -e 'architecture_widget.html' -e 'architecture_widget.phase-0-historical.html' -- src
Test-Path and literal-content inspection of each current allow-listed runtime/generator candidate
```

| Command group | Exit code | Result |
| --- | ---: | --- |
| Workspace root | `0` | Root is exactly `D:/IA/ace v.2`. |
| Initial live worktree inspection | `0` | Matched the 62 Task 1 protected path/status identities before Task 2 output creation; subsequent content reconciliation found one explicitly attributed `TASK_CONTEXT.md` delta. |
| Pre-copy source and destination gate | `0` | Source existed; archive parent existed; destination was absent; source matched Task 1 row 20 size and SHA-256. |
| Native byte copy and immediate verification | `0` | Archive created without text decoding, newline conversion, normalization, or formatting. |
| Isolation search and candidate enumeration | `0` | Zero current runtime/generator references; seven candidate files are not yet created. |
| Temporary altered-byte and simulated-reference probes | `0` | Both invalid cases were rejected; canonical files retained the expected SHA-256. |

## Data-Surface QA

| Assertion | Result |
| --- | --- |
| Pre-copy size equals Task 1 row 20 (`37294`) | pass |
| Pre-copy SHA-256 equals Task 1 row 20 | pass |
| Destination was absent before native copy | pass |
| Source and archive byte lengths match after copy | pass |
| Source and archive SHA-256 values match after copy | pass |
| Source SHA-256 is unchanged from the pre-copy value | pass |
| Independent byte-array equality passes | pass |
| Archive is only at the approved archive path | pass |
| Historical, archive, and future generated paths are distinct | pass |
| Future generated path does not exist at Task 2 | pass |
| Current production tracked sources contain no historical/archive reference | pass |
| Current allow-listed runtime/generator candidate count is zero; all seven recorded `not-yet-created` | pass |
| Task 1 protected path/status identities remain present: 62 rows | pass |
| Task 1 protected content reconciliation: 59 present hashes plus 2 deleted states retain captured state | pass: `61/62` rows |
| Sole protected-content delta is `docs/architecture-migration/TASK_CONTEXT.md` | pass: separately authorized Task 1 completion workflow update, before Task 2 implementation; not owner drift and not a Task 2 output |
| Task 1 `TASK_CONTEXT.md` captured bytes / SHA-256 | `36465` / `CAC4BC2E2E183DE06D260FB67DB71584C9264F8FB163E766D0767F44CBC07D42` |
| Current pre-Task-2/verification `TASK_CONTEXT.md` bytes / SHA-256 | `39414` / `39D65AD5B974CEBF21D55819108C067D600DFA64F62CAAF37D80A46E2C333859` |
| Task 1 receipt remains the sole prior Phase 0.5-created path | pass |
| Task 2 additions are exactly the archive and this receipt | pass |

## Adversarial Probes

All probe files were created only under `C:/Users/Admin/AppData/Local/Temp/opencode/task2-widget-probe-<GUID>` and removed before final verification.

| Probe | Result |
| --- | --- |
| `stale_state` | pass: source SHA-256 was checked against the immutable Task 1 row before copy and rechecked after copy. |
| `dirty_worktree` | pass: live porcelain retains the 62 Task 1 protected path/status identities plus Task 1 receipt and the two Task 2 outputs. Content reconciliation found exactly one expected protected-content delta: `TASK_CONTEXT.md` was updated by the separately authorized and required orchestrator workflow action that recorded independently confirmed Task 1 before Task 2 began. The other 61 protected rows retain their captured content/deleted state; Task 2 did not write `TASK_CONTEXT.md`. |
| `misleading_success_output` | pass: native copy success was accepted only with independent size, SHA-256, and per-byte equality checks. |
| Generated/cached artifact stale state | pass: future generated path is absent; no generated artifact or cache was read or used. |
| Malformed/altered input | pass: one byte in a temporary archive copy was changed; equal-length byte comparison and SHA-256 comparison both rejected it. |
| Simulated generator historical read | pass: a temporary `generator.mjs` containing `readFileSync('architecture_widget.html')` was detected and rejected by the isolation check. |

## Limitations And Cleanup

- This receipt proves the current execution-time paths and sources only. All runtime/generator files listed above are not yet created, so it does not claim a future repository-wide guarantee; later tasks must repeat the isolation check.
- No browser, Node package, generator, runtime, or Task 3 artifact was created, installed, downloaded, or executed.
- Git is not a byte-comparison tool; byte identity was verified from `ReadAllBytes` in addition to SHA-256.
- Temporary altered-copy and simulated-reference probes were removed from `C:/Users/Admin/AppData/Local/Temp/opencode`; no process remained running.
- The Task 1 ledger is a pre-write boundary, not an assertion that every later orchestrator workflow update is forbidden. The only detected protected-content delta is the required completion-record update in `docs/architecture-migration/TASK_CONTEXT.md`: captured as `36465` bytes / `CAC4BC2E2E183DE06D260FB67DB71584C9264F8FB163E766D0767F44CBC07D42`, then independently observed before this verification as `39414` bytes / `39D65AD5B974CEBF21D55819108C067D600DFA64F62CAAF37D80A46E2C333859`. It records independently confirmed Task 1 workflow state, is neither unrelated owner drift nor a Task 2 output, and was not written by Task 2.

## DoneClaim

**DoneClaim TASK-2-HISTORICAL-WIDGET-PRESERVATION:** `docs/architecture-migration/archive/architecture_widget.phase-0-historical.html` is a byte-for-byte preservation copy of the immutable historical widget. Both artifacts remain provenance-only and are not runtime/generator inputs. The historical source is unchanged, both files are `37294` bytes with SHA-256 `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA`, and current runtime/generator isolation is confirmed within the explicit current-source boundary. All 62 Task 1 protected path/status identities remain present; 61 retain captured content/deleted state (59 present hashes and 2 deleted states). The sole content delta is the separately authorized/required post-Task-1 orchestrator workflow update to `TASK_CONTEXT.md`, recorded before Task 2 and not written by it. Task 2 adds exactly the archive and this receipt; failure probes were rejected in cleaned temporary copies.
