---
title: "Investigation dossier — Учалы v.3.smc save/reload (PB-001, PB-002)"
labels: [control/docs-only, standalone product bug, investigation planning]
status: "investigation planning — NO FIX AUTHORIZED"
parent_receipt: "uchaly-v3-smc-save-reload.md"
created: "2026-08-26"
---

# Investigation dossier — `Учалы v.3.smc` (PB-001, PB-002)

**Классификация:** `control/docs-only`. Компаньон к неизменяемому evidence
receipt `uchaly-v3-smc-save-reload.md` (STATUS: PASS, FIX AUTHORIZATION: NONE).
Этот файл фиксирует контекст расследования; он не изменяет родительский
receipt, планы, модель, виджет и не открывает никакую фазу.

## 0. Директива владельца (зафиксирована дословно)

> «нужно брать из того что было сохранено в проекте. потому что логика расчетов
> этих параметров не изменяется.»

Целевой контракт: **save -> close/reopen**. Валидные сохранённые thermal и
hydraulic results являются источником истины при открытии проекта; скрытый
пересчёт при открытии недопустим.

## 1. Реестр ID

| ID | Суть | Статус root cause | Fix |
|---|---|---|---|
| `PB-001` | Bug A: thermal runtime-поля (`Alpha`, `MeltingHeat`, `RadiationHeat`, `ConvectionHeat`, `ExcessTemperature`, `RFb`, `RD`, `ParameterM`, `EfficiencyEtaR`, `MassFlowRate`, `VolumeFlowRate`, `ValidationErrors`) не входят в 8-полевый wire DTO и восстанавливаются нулями (`BuildSavedResult` / `ToDomainResult`) | Установлен: факт сериализации, не формулы; форма DTO предшествует миграции (base commit `7d0ca2b`) | Не авторизован |
| `PB-002` | Bug B: смещение мощности ~40.1 -> ~39.9 kW после reopen; сохранённый `powerTotal` = 254.70, но circuit 1 коллектора 1 подразумевает q ≈ 255.34 | **Не установлен**: producer несоответствия heat inputs не доказан | Не авторизован |

## 2. Рабочие гипотезы PB-002 (подлежат проверке, не утверждения)

- **H1 — порядок публикаций при save:** hydraulic snapshot собирается из
  теплового контекста, отличного от канонического `powerTotal`. Аналогично
  restore-порядку в `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync`
  (thermal finalization может триггерить hydraulic recalculation, затем
  hydraulics восстанавливаются последними), в момент save возможен захват
  circuit powers из промежуточного состояния.
- **H2 — тайминг захвата адаптером:** `CircuitsViewModel` фиксирует результаты
  до завершения публикации теплового результата.
- **H3 — дрейф входных данных между модулями:** расхождение
  `pipeSpacingCm` / `supplyLength` / площади даёт разные подразумеваемые
  heat inputs.

Проверка каждой гипотезы — characterization-тест, утверждающий равенство
hydraulic heat inputs каноническому `powerTotal` в момент сборки snapshot.

## 3. Запланированный characterization-контракт (не исполняется)

Сценарий `save -> reopen` на известном сеансе:

1. Задать известные climate/construction/thermal inputs, выполнить расчёт.
2. Сохранить; зафиксировать `ProjectData`.
3. Assertions на save:
   - `ThermalData.Result` (8 полей) == каноническому результату поле-в-поле;
   - `HydraulicsData` circuit `OperatingResult.Power` согласован с каноническим
     `powerTotal` (H1/H2/H3 различаются предикатом);
4. Переоткрыть (`LoadProjectDataAsync`); assertions на reopen:
   - `ThermalState.Snapshot.Result` == сохранённым значениям, ровно ноль
     пересчётов (подсчёт публикаций координатора);
   - hydraulic circuit rows == файловым значениям;
   - отсутствие невидимых fallback-расчётов.

Для отсутствующего/невалидного/неполного thermal result политика fallback
(пересчёт vs файл-истина, DEC-T08) — отдельное owner-решение (см. §5, Q2).

## 4. Границы

- Изменения `src/`, тестов, `.smc` фикстур — запрещены без отдельной авторизации.
- Расширение wire DTO затрагивает `INV-012`; допустимо только отдельным
  owner-approved изменением.
- Полный restore coordinator — deferred Phase 7+; это досье его не открывает.
- Фикстура `C:\Users\Admin\Desktop\Учалы v.3.smc` вне worktree — только как
  evidence, не как тестовый ресурс репозитория.
- Родительский receipt остаётся байт-неизменным.

## 5. Открытые вопросы для владельца (из receipt §6, без выбора)

1. Lossless DTO extension (переносить runtime-поля в wire) — да/нет?
2. Для старых файлов без runtime-полей: полный пересчёт при загрузке или файл
   остаётся источником истины?
3. Синхронизация hydraulic snapshot с каноническим thermal `powerTotal` при
   save — требуется ли, и какой кодовый путь создаёт текущее расхождение?

---

STATUS: PASS
FIX AUTHORIZATION: NONE
ARCHITECTURE PHASE CLAIM: NONE
