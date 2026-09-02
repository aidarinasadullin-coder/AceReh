---
title: "Standalone product bug — Учалы v.3.smc save/reload thermal + hydraulics loss"
labels: [control/docs-only, standalone product bug]
status: "evidence capture only — NO FIX AUTHORIZED"
evidence_session: "ses_fc4bf7548ffeSsRAq9nWOEERnj"
created: "2026-08-26"
---

# Standalone product bug receipt — `Учалы v.3.smc` save/reload

**Classification:** `control/docs-only`, `standalone product bug`.

**This is NOT an architecture-phase acceptance claim.** It does not alter the
frozen architecture plans, `docs/architecture-migration/TASK_CONTEXT.md`,
`STATE.json`, or any owner gate. It is outside Phase 6 and carries no execution
authorization. Phase 6 remains owner-accepted and immutable; its save boundary
intentionally leaves restore redesign, wire-schema changes, broad Results
projection, and formula/invalidation redesign deferred. This receipt does not
reopen or alter Phase 6.

**Primary evidence source:** OpenCode session
`ses_fc4bf7548ffeSsRAq9nWOEERnj` (read in full, not a truncated summary). The
user opened the investigation with a bug report and attached screenshots; the
session then read the raw `.smc` JSON and the relevant source files.

**Distinction of evidence tiers used below:**
- *User/session observed* — stated by the user in the transcript or visible in
  the attached screenshots, or inferred by the investigating session.
- *Verified in raw file* — read directly from `C:\Users\Admin\Desktop\Учалы v.3.smc`.
- *Verified in source* — read directly from the cited `.cs` files (via codegraph
  and the session's direct reads).
- *Hypothesis / unresolved* — not established by the evidence; recorded only as
  an open question for a future owner.

---

## 1. Reproduction context (as established by the source session)

The source session did **not** establish an exact click-by-click UI sequence.
The reproduction is recorded as the user described and the session confirmed:

1. Open the project (`C:\Users\Admin\Desktop\Учалы v.3.smc`) **before** saving,
   with a non-zero thermal `Alpha` and an approximately **40.1 kW** nominal power
   visible in the UI (user-observed via screenshots; the user wrote "ненулевым
   альфа и м мощностью 40,1 кВт - до сохранения").
2. Save the file `C:\Users\Admin\Desktop\Учалы v.3.smc`.
3. Close and reopen the **same** file.
4. Observe that thermal runtime values such as `Alpha` / flow are lost or zeroed,
   and the nominal / collector power presentation changes to approximately
   **39.9 kW** (user-observed via screenshots of the reopened file).

No exact pre-save `Alpha` or flow numeric values are asserted here beyond what
the transcript states ("non-zero"). No UI step sequence beyond open / save /
close / reopen is claimed.

---

## 2. File identity facts (verified in raw file)

Read directly from `C:\Users\Admin\Desktop\Учалы v.3.smc`:

- `.smc` schema `version`: `1.1` (verified in raw file).
- `climateData.selectedCity`: `Учалы` (verified in raw file).
- `thermalData.result.powerTotal`: `254.70032879058604` (verified in raw file).
- `thermalData.result.powerUp`: `251.47260172076727` (verified in raw file).
- `thermalData.result.powerDown`: `3.227727069818781` (verified in raw file).
- `thermalData.result` contains exactly eight fields:
  `powerUp`, `powerDown`, `powerTotal`, `supplyTemperature`,
  `returnTemperature`, `meanTemperature`, `deltaT`, `isValid` (verified in raw
  file).
- Two collectors are persisted with circuit results:
  - Collector 1: 5 circuits; circuit 1 `operatingResult.power`
    `5106.74159225125`, `supplyLength` `10`, `pipeSpacingCm` `20`.
  - Collector 2: 2 circuits; circuit 1 `operatingResult.power`
    `4126.1453264074935`, `supplyLength` `10`, `pipeSpacingCm` `20`.
  (verified in raw file).

---

## 3. Bug A — thermal runtime fields omitted by the 8-field wire DTO

**Observed symptom (user/session):** after reopen, thermal runtime values such
as `Alpha` and flow/heat breakdown are zeroed while the persisted eight power and
temperature fields remain correct.

**Persisted `ThermalResultProjectData` fields (eight, verified in source):**
`PowerUp`, `PowerDown`, `PowerTotal`, `SupplyTemperature`, `ReturnTemperature`,
`MeanTemperature`, `DeltaT`, `IsValid`.

- Class definition: `src/Models/Project/ProjectData.cs` (session cites
  approximately line `265`; treat line numbers as source references, not
  immutable evidence).
- Save mapping: `ThermalPersistenceMapper.BuildResultProjectData`
  (`src/Services/Project/ThermalPersistenceMapper.cs`, session cites lines
  `79-98`; codegraph confirms the method sets only the eight fields above and
  explicitly does not carry runtime-only fields).

**Runtime fields reported as omitted (verified in source via
`BuildSavedResult` / `ToDomainResult`):**
`Alpha`, `MeltingHeat`, `RadiationHeat`, `ConvectionHeat`, `ExcessTemperature`,
`RFb`, `RD`, `ParameterM`, `EfficiencyEtaR`, `MassFlowRate`, `VolumeFlowRate`,
and validation details (`ValidationErrors` is restored as `null`).

- Restore mapping: `ThermalPersistenceMapper.BuildSavedResult`
  (`src/Services/Project/ThermalPersistenceMapper.cs`, session cites lines
  `182-210`; codegraph confirms `alpha: 0.0`, `meltingHeat: 0.0`,
  `radiationHeat: 0.0`, `convectionHeat: 0.0`, `excessTemperature: 0.0`,
  `rFb: 0.0`, `rD: 0.0`, `parameterM: 0.0`, `efficiencyEtaR: 0.0`,
  `massFlowRate: 0.0`, `volumeFlowRate: 0.0`, `validationErrors: null`).
- Domain projection: `ThermalPersistenceMapper.ToDomainResult`
  (`src/Services/Project/ThermalPersistenceMapper.cs`, session cites lines
  `216-234`; codegraph confirms it publishes only the eight persisted fields, so
  runtime-only fields remain CLR defaults `0.0`).

**Conclusion for Bug A:** the loss is a serialization fact, not a formula defect.
The wire DTO carries eight fields; the omitted runtime fields restore as CLR
defaults. The source session additionally verified (via git history of the base
commit `7d0ca2b`) that this eight-field DTO shape predates the architecture
migration, supporting the user's "не связан с рефакторингом" observation
(verified in source / session-observed).

---

## 4. Bug B — collector powers reflect inconsistent heat inputs (40.1 -> 39.9 kW)

**Observed symptom (user/session):** the nominal / collector power presentation
shifts from approximately **40.1 kW** before save to approximately **39.9 kW**
after reopen.

**Inconsistency observation (verified in raw file + session arithmetic):**
- Saved thermal `powerTotal` = `254.70032879058604` (verified in raw file).
- Collector 1, circuit 1: `operatingResult.power` `5106.74159225125` over an
  implied area of `20.0` implies a heat input of approximately **255.34**
  (`5106.74159225125 / 20.0`), which does **not** match the saved `powerTotal`
  of `254.70`.
- Collector 2, circuit 1: `operatingResult.power` `4126.1453264074935` over an
  implied area of `16.2` aligns approximately with **254.70**
  (`4126.1453264074935 / 16.2`), matching the saved `powerTotal`.

This is recorded as an **inconsistency observation**, not a proven single
root-cause path. The exact producer of the inconsistent heat inputs (why
collector 1's persisted circuit power corresponds to `q ≈ 255.34` while the
canonical saved thermal result is `254.70`) is **unresolved**.

**Not claimed:** the approximately 40.1 -> 39.9 kW change is **not** presented
here as a confirmed formula defect. It is preserved as a user-visible symptom
with an unresolved producer path. The session hypothesized (without proving) that
the hydraulics snapshot at save time may have been computed from a different
thermal context than the canonical `powerTotal`; this remains a hypothesis.

**Source citation (reference only, not a fix):** hydraulics restore path
`HydraulicsPersistenceMapper.BuildRestoreCandidate`
(`src/Services/Project/HydraulicsPersistenceMapper.cs`, session cites line `29`;
codegraph confirms it rebuilds collectors from persisted inputs/results without
re-deriving heat inputs). `ResultsViewModel` save orchestration was cited by the
session as the assembly point for `ProjectData` at save time (reference only).

---

## 5. Confidence table

| Fact | Observed by user/session | Verified in raw file | Verified in source | Hypothesis / unresolved |
|---|---|---|---|---|
| Non-zero Alpha and ~40.1 kW visible before save | Yes (screenshots) | No | No | — |
| Alpha / flow zeroed after reopen | Yes (screenshots) | No | Yes (restore sets 0.0) | — |
| Nominal/collector power ~40.1 -> ~39.9 kW after reopen | Yes (screenshots) | No | No | producer path unresolved |
| `.smc` version `1.1`, city `Учалы` | — | Yes | — | — |
| `thermalData.result.powerTotal` = `254.70032879058604` | — | Yes | — | — |
| Thermal wire DTO has exactly 8 fields | — | Yes | Yes (`BuildResultProjectData`) | — |
| Runtime fields (Alpha, MeltingHeat, …, VolumeFlowRate, ValidationErrors) omitted | — | No | Yes (`BuildSavedResult`/`ToDomainResult`) | — |
| Collector 1 circuit power implies ~255.34 vs saved 254.70 | — | Yes (arithmetic) | — | exact producer unresolved |
| Collector 2 circuit power aligns ~254.70 | — | Yes (arithmetic) | — | — |
| DTO shape predates migration (legacy) | Yes (session git check) | — | Yes (base commit `7d0ca2b`) | — |

**Explicit statement:** the exact producer of the inconsistent heat inputs in
Bug B is unresolved. No single root-cause path is proven.

---

## 6. Non-authorized candidate directions (questions for a future owner)

These are **not** recommendations and are **not** authorized or implemented by
this receipt. They are framed as open questions for a future owner-gated
characterization/fix plan:

- **Lossless DTO extension (question):** should `ThermalResultProjectData` be
  extended to carry `Alpha`, `MeltingHeat`, `RadiationHeat`, `ConvectionHeat`,
  `ExcessTemperature`, `RFb`, `RD`, `ParameterM`, `EfficiencyEtaR`,
  `MassFlowRate`, `VolumeFlowRate`, and `ValidationErrors`, populated in
  `BuildResultProjectData` and restored in `BuildSavedResult`/`ToDomainResult`,
  while keeping old files readable (missing fields -> 0)?
- **Compatibility fallback / recalculation policy (question):** for old files
  whose runtime fields are absent, should load trigger a full recalculation, or
  should the file remain the source of truth (DEC-T08)? What is the agreed
  behavior?
- **Synchronization correction (question):** at save time, should the hydraulics
  snapshot be synchronized with the canonical thermal `powerTotal` so collector
  circuit powers derive from the same heat input? What code path produces the
  current mismatch?

No candidate above is selected, approved, or implemented here.

---

## 7. Scope and non-claim statement

- This receipt is documentation and evidence capture only. No `src/`, tests,
  `.smc` files, maps, widget/model artifacts, canonical plans,
  `TASK_CONTEXT.md`, `docs/architecture-migration/AGENTS.md`, `STATE.json`, or
  existing `.omo` plan was modified.
- It does not alter frozen plans, owner gates, or Phase 6.
- The correct next step after this receipt is a future owner-gated
  characterization/fix plan; this receipt does not choose between candidate fixes.
- It is independent of the phase-to-ID provenance receipt and does not modify its
  files. `learnings.md` and `decisions.md` of this notepad are owned by other
  workers and are not touched here.

---

STATUS: PASS
FIX AUTHORIZATION: NONE
ARCHITECTURE PHASE CLAIM: NONE
