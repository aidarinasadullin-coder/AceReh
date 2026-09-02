---
title: "Product bug & residual risk backlog index"
labels: [control/docs-only, navigation/provenance-only]
status: "active index"
created: "2026-08-26"
---

# Backlog index — product bugs and residual risks

Единая точка входа для известных продуктовых багов и остаточных рисков,
зафиксированных в receipts фаз. Файл неавторитетный: не изменяет frozen plans,
шесть карт, модель, виджет и owner gates. Каждая запись ссылается на
первичный evidence-документ; статусы меняются только новыми dated-записями
в соответствующем досье, а не редактированием истории.

## 1. Product bugs (PB-*)

| ID | Название | Досье | Root cause | Fix |
|---|---|---|---|---|
| `PB-001` | Учалы Bug A — thermal runtime-поля теряются при reopen (8-полевый wire DTO восстанавливает нули) | [product-bugs/uchaly-v3-smc-save-reload.md](../product-bugs/uchaly-v3-smc-save-reload.md) | Установлен (факт сериализации, legacy DTO) | NONE |
| `PB-002` | Учалы Bug B — ~40.1 -> ~39.9 kW; producer несоответствия heat inputs не доказан | [product-bugs/uchaly-v3-smc-save-reload.investigation.md](../product-bugs/uchaly-v3-smc-save-reload.investigation.md) | Не установлен (гипотезы H1–H3) | NONE |

Директива владельца по обоим: сохранённые в проекте значения — источник
истины при открытии; логика расчётов не изменяется. Детали и план
characterization — в investigation-досье.

## 2. Residual risks (RR-*)

Источник — `evidence/phase-6-project-snapshot-save-boundary/phase-6-consolidated-receipt.md`
и `TASK_CONTEXT.md` (записи 2026-08-26). Риски сохраняются открытыми и не
блокируют приёмку Phase 6.

| ID | Риск | Статус |
|---|---|---|
| `RR-001` | `ProjectSnapshotPersistenceInputs.Templates` — sync-over-async (`GetAllAsync().GetAwaiter().GetResult()`), deadlock-prone на UI thread; безопасен только на cache-hit fast path | documented, non-gating |
| `RR-002` | Headless-среда: ручная WPF button/dialog/print QA не выполнялась; user-flow покрытие частичное | manual-QA gap |
| `RR-003` | Standalone negative probes (invalid-ID, missing-evidence-edge) — `NOT_PRESENT` (честное отсутствие, не сфабрикованный nonzero) | documented |
| `RR-004` | Внешний legacy fixture skip `D:\IA\ace\Тест\тест 40.smc` — отсутствие файла в worktree; зафиксирован как skip, не pass | environment limitation |

## 3. Предлагаемая разбивка ID по оставшимся работам — SYNTHESIS

**Важно:** canonical model (`maps/architecture-model.json`) **не содержит**
атрибута «фаза» ни у одной записи (проверено 2026-08-26: 0 вхождений).
Единственный канонический источник привязок фаз->ID —
[phase-to-id-provenance.md](phase-to-id-provenance.md), который фиксирует:
явные receipt-bindings найдены только для Phase 6 (`PE-P6-*` verified,
`PN-P6-*` pending); для фаз 0–5.1 bindings «none found».

Разбивка ниже — **синтез из карт, receipts и deferred-списков**, сделанный
для навигации. Она не авторитетна; канонизация потребует отдельного
owner-approved обновления модели (сейчас модель — frozen Phase 6 state).

### Restore coordinator (`ProjectData -> ProjectSession`)
`ST-023`, `INV-013`, `INV-012` (restore-часть), `DEC-002`, `DEC-003`,
`PN-03..05`, `PE-02..04`; закрывает `PB-001`/`PB-002` на уровне политики;
влияет на `CF-002`, `CF-003`, `CF-004`, `CF-013`, `CF-021`;
уменьшает нарушения `INV-007`/`INV-008` (сервисы зависят от ViewModel).

### Results derived projection
`ST-003`, `ST-024..ST-027`, `INV-009`; влияет на `CF-014`, `CF-015`.

### Legacy owners removal + DI cleanup
`ST-020` (+`ST-021/22` диспозиция), `INV-001`, `INV-006`, `INV-007`,
`INV-008`, `INV-010`, `INV-011`; `DEC-001`; удаление forwarding-алиасов
(`IProjectStateService` / `IProjectInfoService` / `IMarkDirtyService`).

### Полный user-flow и release gate
Missing flows `CF-004`, `CF-012`, `CF-017..CF-019`; deferred claims
(byte identity, crash atomicity, compatibility duration); Markdown removal —
отдельное owner-approved изменение.

### Owner decisions — resolved overlay for future planning

Решения владельца от 2026-08-26 снимают policy-blocker для подготовки
следующей фазы, но сами по себе не запускают её:

- `DEC-001 = A`: `CalculationContext` остаётся downstream compatibility/read
  projection seam; production writers — только Thermal/Hydraulics coordinators.
- `DEC-002 = no legacy support`: unreleased application не обязано читать старые
  `.smc`; restore target — только текущий формат, без legacy version branches.
- `DEC-003 = C`: validate-first; после успешной проверки — ordered commit; при
  неожиданном commit failure canonical module state переводится в
  clean/default state, смешанное partial state не сохраняется.

`DEC-002`/`DEC-003` относятся прежде всего к restore coordinator; `DEC-001` —
к Results projection и legacy cleanup. Full implementation, model/widget refresh
и Phase 7 execution остаются отдельными owner-gated действиями.

## 4. Session, regression, and tooling analysis

Read-only analysis of the local OpenCode session store is recorded in
[session-regression-tool-analysis-2026-08-26.md](session-regression-tool-analysis-2026-08-26.md).
It is non-authoritative and classifies intentional RED runs, resolved
regression cycles, infrastructure/tool failures, workflow blockers, and
continuation overhead. It does not authorize tool/config changes or Phase 7.

---

STATUS: PASS
