---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - Read the canonical architecture schema/model, target invariants, approved Todo 11, and architecture_widget.html as historical input only
  - PowerShell deterministic widget-spec structural validator (inline below)
exit_code: 0
status: pass
raw_output: Inline observed validation output.
limitations:
  - This is an implementation-neutral specification; no HTML, CSS, JavaScript, component, visual design, or widget implementation is authorized.
  - Target rendering describes unimplemented architecture and must never be labeled observed current state.
---

# Model-Driven Architecture Widget Specification

## Scope Boundary

The future widget consumes exactly one runtime input: one canonical architecture
model document validated against its declared schema version. That future
canonical document contains its baseline/current/target records, invariant
records, deferred-decision records, evidence, and view membership in one atomic
payload. `maps/architecture-model.baseline.json` is the Phase 0 fixture for the
current contract, not a second runtime source; because it has no target records,
its Target mode is an explicit valid-empty/unimplemented state. A complete
single-document test fixture may add target/invariant records under the future
schema contract to exercise Target and Diff. `target-invariants.md` is design
provenance, never a runtime input. The current `architecture_widget.html` is
historical presentation input only and remains unchanged. This document defines
behavior and acceptance, not aesthetics or an implementation technology.

## Input And State Contract

1. Parse one JSON model atomically; never merge hidden secondary data sources.
2. Validate required fields, IDs, references, enums, snapshots, evidence paths,
   edge semantics, ordered flows, and six-view membership before rendering.
3. Retain the last valid model only within the current runtime session. A failed
   replacement must show an error and must not partially mutate the visible graph.
4. Reject any attempt to load or merge a second runtime model document.
5. Compute a deterministic fingerprint from canonical input bytes and expose the
   model `snapshot_sha`, source basis, validation status, and limitations.
6. Mark input `stale` when its snapshot SHA differs from the dossier snapshot,
   when a referenced evidence path/fingerprint is unavailable or changed, or when
   the schema/model version is unsupported. Stale content remains visibly labeled
   and cannot be presented as verified current architecture.

## Modes

| Mode | Contract |
| --- | --- |
| Baseline | Show records with `baseline` snapshot membership as observed at the captured Phase 0 boundary. |
| Current | Show records with `current` membership and label the model source basis and freshness. |
| Target | Show only target records/invariants present in the one canonical input and label them `unimplemented`; for the Phase 0 fixture show a valid-empty target state and never synthesize `ProjectSession` as current. |
| Diff | Compare an ordered left snapshot with a distinct right snapshot and classify records as added, removed, changed, unresolved, or invariant violation. Unchanged records may be hidden but remain countable. |

Diff exposes two independent selectors. The deterministic default is
`left=Current`, `right=Target`; changing mode away from Diff preserves the pair
for the runtime session. Selecting the same snapshot is invalid: the second
selection is rejected, focus remains on it, and the prior distinct pair remains
active. Swapping selectors reverses direction without changing stable IDs.

`added` means absent on the left and present on the right. `removed` means
present on the left and absent on the right. `changed` requires the same stable
ID in both snapshots with at least one changed canonical field. Classification
precedence is: unresolved identity/evidence first; then added/removed; then
changed/unchanged. An invariant violation is an orthogonal flag that may
accompany added, removed, changed, or unchanged and links to an `INV-*` record;
it never hides the directional classification. Status is never color-only.

## Views And Combined Filters

The six views remain distinct filters over the shared model:
`compile-time`, `di-runtime`, `state-ownership`, `reactive`, `persistence`, and
`user-flow`. Selecting multiple views forms a union of matching records while
preserving each record's view badges, edge kind/source kind, per-view counts,
and evidence. Combined filtering must not relabel compile references as runtime
resolution or collapse persistence/reactive/user-flow semantics.

Search matches stable ID, display name, kind, source kind, participant, state
reference, evidence locator, invariant, and status. Risk/status filters include
verified, derived, degraded, unimplemented, stale, unresolved, and violation.
The legend explains every visible node/edge/status/diff token in text.

## Evidence Drill-Down

Activating a record opens a non-destructive detail region containing stable ID,
kind and source kind, snapshots/views, confidence, endpoints or participants,
state refs, trigger/effect, evidence ID/path/locator, limitations, and linked
invariants. Evidence navigation preserves mode, filters, search, focus return,
and selected record. Missing evidence produces an explicit unavailable state,
not a broken or silently omitted link.

## Interaction And Accessibility

- Keyboard order follows input, mode, view filters, search/status filters,
  results, details, then evidence. All controls support keyboard activation.
- Focus is always visible; opening/closing details moves and restores focus
  deterministically. No focus trap exists outside a true modal error dialog.
- Controls and records have programmatic names. Counts, loading, validation,
  filter changes, stale state, errors, and no-results changes are announced by
  an appropriate screen-reader live region without repeating unchanged content.
- Status, risk, mode, and diff use text/icon/shape in addition to color and meet
  applicable contrast requirements.
- Reduced-motion preference removes nonessential transitions; information and
  focus order remain identical. No interaction depends on animation or hover.

## Responsive And Offline Behavior

At a narrow viewport, controls wrap into a stable ordered disclosure region,
the record list remains primary, and details follow the selected record without
horizontal page scrolling. Dense tables become labeled record groups rather
than clipped columns. At wider widths, list and detail may coexist without
changing semantics or keyboard order.

The widget operates offline after its model and implementation assets are
available. External evidence URLs are optional enhancements; repository-relative
evidence remains readable as text when navigation is unavailable. Offline status
is explicit and does not change validation or architecture classifications.

## Empty, Error, And Stale States

| State | Required behavior |
| --- | --- |
| Empty input | Explain that no model was supplied; expose the input action; render no fabricated graph. |
| Invalid JSON | Identify parse failure without exposing a partial model. |
| Schema/reference failure | Identify the failing field/ID/reference and block normal architecture rendering. |
| Unsupported version | Show supported contract identifier and reject interpretation. |
| Valid empty filter/no match | Preserve controls and distinguish zero matches from empty input. |
| Stale model | Keep the stale banner adjacent to mode/status, disclose the reason, and prevent verified-current wording. |
| Missing evidence | Preserve the architecture record and mark only its evidence navigation unavailable/degraded. |
| Offline | Preserve local model behavior, announce offline state, and disable only unavailable external navigation. |

## Deterministic Acceptance Matrix

Every mode/view pair appears exactly once. `Model fixture` means the validated
canonical input; automation may use a deterministic in-memory derivative.

| Acceptance ID | Mode | View / state | Setup / input | Action | Expected visible state | Expected accessibility announcement | Evidence / automation method |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `WA-001` | Baseline | compile-time | Model fixture | Select mode/view | Baseline compile nodes/edges and evidence | Baseline, compile-time, result count | Assert snapshot/view membership and labels |
| `WA-002` | Baseline | di-runtime | Model fixture | Select mode/view | DI records retain registration/resolution/source kinds | Baseline, DI/runtime, result count | Assert DRE kinds and source kinds |
| `WA-003` | Baseline | state-ownership | Model fixture | Select mode/view | Current owners and target-owner distinction | Baseline, state ownership, result count | Assert ST rows and target labels |
| `WA-004` | Baseline | reactive | Model fixture | Select mode/view | Reactive trigger/effect/participants visible | Baseline, reactive, result count | Assert RE semantic records |
| `WA-005` | Baseline | persistence | Model fixture | Select mode/view | Persistence boundaries and sequence kinds visible | Baseline, persistence, result count | Assert PN/PE membership |
| `WA-006` | Baseline | user-flow | Model fixture | Select mode/view | Ordered CF flows and coverage statuses visible | Baseline, user flow, result count | Assert contiguous positions |
| `WA-007` | Current | compile-time | Model fixture | Select mode/view | Current compile records with source/freshness | Current, compile-time, result count | Assert current membership and SHA |
| `WA-008` | Current | di-runtime | Model fixture | Select mode/view | Current DI filter without invocation overclaim | Current, DI/runtime, result count | Assert limitations/source kinds |
| `WA-009` | Current | state-ownership | Model fixture | Select mode/view | Legacy/seam owners remain current | Current, state ownership, result count | Assert migration statuses |
| `WA-010` | Current | reactive | Model fixture | Select mode/view | Unknown multiplicities remain unknown | Current, reactive, result count | Assert no fabricated counters |
| `WA-011` | Current | persistence | Model fixture | Select mode/view | Current/legacy compatibility remains evidence-bound | Current, persistence, result count | Assert coverage links |
| `WA-012` | Current | user-flow | Model fixture | Select mode/view | Covered/partial/missing statuses retained | Current, user flow, result count | Assert CF status totals |
| `WA-013` | Target | compile-time | One complete canonical test document | Select mode/view | Target compile constraints labeled unimplemented | Target, compile-time, unimplemented | Reject second input and observed-current label |
| `WA-014` | Target | di-runtime | One complete canonical test document | Select mode/view | Target dependency/DI constraints only | Target, DI/runtime, unimplemented | Assert invariant records in same document |
| `WA-015` | Target | state-ownership | One complete canonical test document | Select mode/view | Composite lifecycle/four slices and owner constraints | Target, state ownership, unimplemented | Assert target records/invariants in same document |
| `WA-016` | Target | reactive | One complete canonical test document | Select mode/view | Lifetime/multiplicity constraints, no invented edges | Target, reactive, unimplemented | Assert target records in same document |
| `WA-017` | Target | persistence | One complete canonical test document | Select mode/view | Wire/restore constraints and deferred policy | Target, persistence, unimplemented | Assert invariant/decision records in same document |
| `WA-018` | Target | user-flow | Phase 0 fixture | Select mode/view | Valid-empty target; target remains unimplemented | Target, user flow, zero modeled target records | Assert no synthesized target/current record |
| `WA-019` | Diff | compile-time | One canonical copied model; default Current to Target | Select mode/view | Directional added/removed/changed/unresolved classes | Diff Current to Target, compile-time, change counts | Mutate copied record field in memory |
| `WA-020` | Diff | di-runtime | One canonical copied model; Current to Target | Select mode/view | Dependency classification plus violation flag | Diff Current to Target, DI/runtime, violation count | Inject copied prohibited dependency |
| `WA-021` | Diff | state-ownership | One canonical copied model; Current to Target | Select mode/view | Directional owner changes and dual-owner violations | Diff Current to Target, state ownership, violation count | Inject copied duplicate writer |
| `WA-022` | Diff | reactive | One canonical copied model; Current to Target | Select mode/view | Changed lifetime and unresolved multiplicity | Diff Current to Target, reactive, unresolved count | Remove copied lifetime evidence |
| `WA-023` | Diff | persistence | One canonical copied model; Current to Target | Select mode/view | Compatibility/restore changes and deferred flags | Diff Current to Target, persistence, unresolved count | Mutate copied compatibility status |
| `WA-024` | Diff | user-flow | One canonical copied model; Current to Target | Swap to Target to Current | Added/removed direction reverses; pair persists across filters | Diff Target to Current, user flow, change count | Assert swap, persistence, and same-snapshot rejection |
| `WA-025` | Combined | compile-time + di-runtime | Model fixture | Select both views | Union with distinct view/kind badges and per-view counts | Two views selected, combined result count | Assert no kind collapse or duplicates |
| `WA-026` | Error | invalid JSON | Truncated JSON | Load input | Parse error, no partial graph | Model load failed: invalid JSON | In-memory malformed input |
| `WA-027` | Error | invalid reference | Copied orphan endpoint/evidence | Load input | Exact failing ID, normal render blocked | Model validation failed, failing ID | In-memory orphan probe |
| `WA-028` | Current | stale model | Copied mismatched snapshot SHA | Load input | Persistent stale banner/reason; no verified-current wording | Stale architecture model, reason | In-memory SHA mismatch |
| `WA-029` | Current | evidence navigation | Model fixture | Open record then evidence | Detail contains locator; state/focus preserved | Evidence details opened for stable ID | Keyboard automation and focus assertions |
| `WA-030` | Current | keyboard/screen reader | Model fixture | Traverse/filter/open/close | Visible focus and deterministic order | Mode/filter/count/detail announcements | Accessibility tree and keyboard assertions |
| `WA-031` | Current | reduced motion | Model fixture with reduced motion | Change filters/details | Same information without nonessential motion | Same state announcements | Emulate reduced-motion preference |
| `WA-032` | Current | narrow viewport | Model fixture at narrow width | Filter and open details | No page overflow; labeled groups; details follow record | Same control/record names and order | Viewport/layout and keyboard assertions |
| `WA-033` | Current | offline | Model fixture, network unavailable | Load/open local evidence | Local behavior preserved; external navigation disabled | Offline; local model available | Block network and assert state |
| `WA-034` | Current | no match | Valid model and unmatched query | Search | Zero-match state with controls/model retained | No matching architecture records | Deterministic search query |
| `WA-035` | Error | missing evidence | Copied missing evidence target | Open record | Record retained; evidence marked unavailable/degraded | Evidence unavailable for stable ID | In-memory missing-path probe |
| `WA-036` | Error | unsupported version | Copied unsupported contract ID | Load input | Version rejected; supported contract shown | Unsupported architecture model version | In-memory version probe |
| `WA-037` | Error | empty input | No model document supplied | Open widget | Empty-input explanation and input action; no fabricated graph | No architecture model supplied | Launch with absent input and assert zero records |

## Deterministic QA

The following PowerShell checks this specification only and mutates copied ID
sets in memory for negative probes.

```powershell
$ErrorActionPreference='Stop'
$path='docs/architecture-migration/widget-spec.md'
$text=Get-Content -Raw -LiteralPath $path
$specText=@($text-split'(?m)^## Deterministic QA\s*$')[0]
$rows=@([regex]::Matches($text,'(?m)^\| `(WA-\d{3})` \| (?<mode>[^|]+) \| (?<view>[^|]+) \|(?<rest>.*)$'))
$ids=@($rows|%{$_.Groups[1].Value})
if($rows.Count-ne37-or($ids|Sort-Object -Unique).Count-ne37){throw 'acceptance IDs'}
$modes=@('Baseline','Current','Target','Diff');$views=@('compile-time','di-runtime','state-ownership','reactive','persistence','user-flow')
foreach($mode in $modes){foreach($view in $views){$count=@($rows|?{$_.Groups['mode'].Value.Trim()-eq$mode-and$_.Groups['view'].Value.Trim()-eq$view}).Count;if($count-ne1){throw "mode/view $mode $view $count"}}}
foreach($token in 'exactly one input','Baseline','Current','Target','Diff','compile-time','di-runtime','state-ownership','reactive','persistence','user-flow','added','removed','changed','unresolved','invariant violation','evidence','keyboard','visible','screen-reader','reduced-motion','narrow viewport','offline','Empty input','Invalid JSON','unsupported version','no match','stale','Missing evidence'){if($text-notmatch[regex]::Escape($token)){throw "missing $token"}}
if(@([regex]::Matches($specText,'(?m)^The future widget consumes exactly one runtime input:')).Count-ne1-or$specText-notmatch'not a second runtime source'-or$specText-notmatch'never a runtime input'){throw 'single input contract'}
if($specText-notmatch'left=Current.*right=Target'-or$specText-notmatch'Selecting the same snapshot is invalid'-or$specText-notmatch'Swapping selectors reverses direction'){throw 'diff pair contract'}
foreach($token in '`added` means absent on the left','`removed` means','precedence is','orthogonal flag'){if($specText-notmatch('(?i)'+[regex]::Escape($token))){throw "diff semantics $token"}}
$special=@('WA-025','WA-026','WA-028','WA-029','WA-030','WA-031','WA-032','WA-033','WA-034','WA-035','WA-036','WA-037');foreach($id in $special){if($ids-notcontains$id){throw "missing special state $id"}}
foreach($id in $special){$row=$rows|?{$_.Groups[1].Value-eq$id};if([string]::IsNullOrWhiteSpace($row.Groups['rest'].Value)-or(@($row.Value-split'\|').Count-ne10)){throw "incomplete special row $id"}}
if(($rows|?{$_.Groups[1].Value-eq'WA-037'}).Value-notmatch'no fabricated graph'){throw 'empty input behavior'}
if($specText-match'(?i)<html|<script|<style|```(html|css|javascript|typescript)'){throw 'implementation artifact'}
$missingPair=@($rows|?{$_.Groups[1].Value-ne'WA-001'});if(@($missingPair|?{$_.Groups['mode'].Value.Trim()-eq'Baseline'-and$_.Groups['view'].Value.Trim()-eq'compile-time'}).Count-ne0){throw 'negative missing pair accepted'}
$duplicate=@($ids)+$ids[0];if(($duplicate|Sort-Object -Unique).Count-eq$duplicate.Count){throw 'negative duplicate accepted'}
[pscustomobject]@{acceptance_rows=$rows.Count;mode_view_pairs=($modes.Count*$views.Count);special_states=$special.Count;single_input='pass';diff_pair='pass';diff_semantics='pass';six_views=$views.Count;implementation_artifacts=0;negative_missing_pair='rejected';negative_duplicate_id='rejected';result='pass'}|Format-List
```

Observed output:

```text
acceptance_rows          : 37
mode_view_pairs          : 24
special_states           : 12
single_input             : pass
diff_pair                : pass
diff_semantics           : pass
six_views                : 6
implementation_artifacts : 0
negative_missing_pair    : rejected
negative_duplicate_id    : rejected
result                   : pass
```

## DoneClaim

**DoneClaim WIDGET-SPEC-11:** The future widget has one canonical model input,
four modes, six independent combinable views, evidence and invariant drill-down,
deterministic diff/freshness/error behavior, keyboard and screen-reader
requirements, reduced-motion, responsive and offline contracts, and a 37-row
acceptance matrix containing every mode/view pair exactly once. No widget
implementation artifact was created or modified.
