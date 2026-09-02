# Phase 7 Dossier Debt Audit

Дата: 2026-09-01

Источник контекста:
- `docs/architecture-migration/AGENTS.md`
- `docs/architecture-migration/TASK_CONTEXT.md`
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/owner-result-acceptance.md`
- `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md`

## Итоговая матрица

| Артефакт | Классификация | Вывод |
|---|---|---|
| `docs/architecture-migration/AGENTS.md:16-19` | MUST UPDATE | Прямое противоречие: написано `Phase 7 has not started`, хотя `TASK_CONTEXT.md` и owner receipt фиксируют Phase 7 как accepted. Governance rules сохраняются, меняется только текущий статус и следующий шаг. |
| `maps/architecture-model.json` | MUST UPDATE | Metadata и provenance остались на Phase 6; отсутствуют Phase 7 evidence refs и current restore/report/UI records. |
| `architecture-widget.html` | MUST UPDATE | Generated artifact не содержит Phase 7, потому что строится из устаревшей canonical model. Нужна регенерация после model update. |
| `maps/compile-time.md` | TARGETED OVERLAY | Основные Phase 1-6 записи корректны. Добавить только Phase 7 restore/report/UI compile-time facts. Concrete ViewModel dependencies `ProjectLoadOrchestrator` остаются открытым `INV-008`. |
| `maps/di-runtime.md` | TARGETED OVERLAY | Добавить accepted single restore boundary, four slice references и UI/DI handoff. Не объявлять `INV-008` закрытым. |
| `maps/state-ownership.md` | TARGETED OVERLAY | Phase 1-5 ownership overlays в целом актуальны. Phase 7 должна уточнить restore ownership и Results/report projection, не выдавая Results cleanup за выполненный. |
| `maps/reactive.md` | TARGETED OVERLAY | Старые `RE-011`/`RE-012` содержат `unknown` по части multiplicity/order. Phase 7 evidence закрывает только заявленные restore/publication/UI аспекты; неизвестные counters нельзя стирать без receipt. |
| `maps/persistence.md` | TARGETED OVERLAY | Уточнить validation-before-mutation, restore order, finalization и fresh source of truth. Transactional rollback, crash atomicity и compatibility duration остаются deferred. |
| `maps/user-flow.md` | TARGETED OVERLAY | Обновить restore, second-load, report/export и rejected-load flow по Phase 7 receipts. Исторические CF rows не переписывать механически. |
| `maps/state-inventory.md` | TARGETED OVERLAY / PARTIAL CORRECTION | ST-001..ST-005 в baseline table исторически корректны, но должны быть явно superseded Phase 1 overlay. ST-023..ST-027 остаются Results/DTO legacy debt, если Phase 7 не закрыла их полностью. |
| `maps/target-invariants.md` | TARGETED OVERLAY | Верхний текст `ProjectSession ... target-only and unimplemented` теперь устарел как current interpretation. `INV-001..INV-005` должны отражать принятые slices; `INV-008`, `INV-009`, часть `INV-011..INV-013` остаются открытыми либо частично verified. |
| `maps/characterization-tests.md` | TARGETED OVERLAY | Добавить Phase 7 test/receipt mapping. Не превращать оставшиеся unknown counters и отсутствующие Excel/preview/print tests в закрытые claims. |
| `maps/persistence-compatibility.md` | KEEP + POSSIBLE REFERENCES UPDATE | Wire contract и compatibility matrix не требуют полной переписи. Добавлять только Phase 7 restore evidence refs там, где фактически изменился restore boundary. |
| `architecture-model.baseline.json` | KEEP AS HISTORICAL | Не менять. Это Phase 0 baseline. |
| `evidence/phase-0.5-repository-snapshot.md` и исторические Phase 1-6 receipts | KEEP AS HISTORICAL | Не переписывать под current state. |
| Frozen `plans/phase-7-project-restore-coordinator-relaunch.md` | KEEP IMMUTABLE | Не изменять после review/acceptance. |
| Production code/tests | OUT OF SCOPE | В рамках этого audit не менять. |

## Future debt

- удаление concrete `ViewModel` dependencies из `ProjectLoadOrchestrator` (`INV-008`)
- полная очистка Results ownership и переход к чистой derived projection (`INV-009`)
- transactional in-memory rollback, если это будет отдельно принято владельцем
- crash/atomic persistence guarantees
- политика длительности и диапазона legacy `.smc` compatibility
- remaining unknown reactive/runtime counters
- недостающие characterization flows: Excel, preview, print и часть lifecycle counters

## Минимальная correction boundary

Если владелец разрешит отдельную docs-only correction, менять только:

1. `docs/architecture-migration/AGENTS.md`
2. `docs/architecture-migration/maps/architecture-model.json`
3. затронутые current overlays в `docs/architecture-migration/maps/*.md`
4. `docs/architecture-migration/architecture-widget.html` через регенерацию
5. отдельный deterministic generation/hash receipt

Не менять:

- `docs/architecture-migration/maps/architecture-model.baseline.json`
- historical evidence snapshots и frozen Phase 7 plan
- production code и tests
