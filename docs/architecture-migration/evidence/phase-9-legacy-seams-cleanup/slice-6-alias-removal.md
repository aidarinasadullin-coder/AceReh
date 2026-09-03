# Slice 6 — Legacy alias removal

Класс: production/test. Дата: 2026-09-03.

## Что удалено (production)

1. **`IProjectStateService`** (`src/Services/Results/IProjectStateService.cs` — удалён) —
   все потребители переведены на `IProjectSession` (тот же набор членов:
   ProjectNumber/ProjectObject/CurrentFilePath/IsDirty/MarkDirty/MarkClean +
   INotifyPropertyChanged).
2. **`IProjectInfoService`** (`.../IProjectInfoService.cs` — удалён) —
   самостоятельных потребителей не имел.
3. **`ProjectStateService`** (`.../ProjectStateService.cs` — удалён из
   production) — forwarding-адаптер перенесён в тест-поддержку
   (`tests/.../Fixtures/ProjectStateService.cs`, то же пространство имён
   `SnowMeltingCalculator.Services.Results`; реализует `IMarkDirtyService` —
   внутренний шов, как раньше). Все тестовые call-sites компилируются без
   изменений семантики.

## Re-target потребителей

| Потребитель | Изменение |
|---|---|
| `MainWindow.xaml.cs` | поле/ctor-параметр `IProjectStateService` → `IProjectSession` (+using Services.Project) |
| `MainViewModel.cs` | поле/ctor-параметр → `IProjectSession`; `nameof(IProjectStateService.IsDirty/CurrentFilePath)` → `nameof(IProjectSession.…)` (те же строки PropertyChanged) |
| `ResultsViewModel.cs` | ctor-параметры `IProjectStateService` (arg 1) и `IMarkDirtyService` (arg 2) удалены; поле `_projectStateService` удалено — pass-through и MarkDirty идут через `_projectSession` (`MarkDirty()` ×2 — `:71/:91`); 9 доп. использований поля заменены на `_projectSession` |
| `ProjectSession.cs` | из декларации класса убран `IProjectStateService` (`: IProjectSession, IMarkDirtyService`) |
| `ServiceCollectionExtensions.cs` | удалены forwarding-регистрации `IProjectInfoService`/`IProjectStateService`; `IMarkDirtyService → ProjectSession` СОХРАНЕНА как регистрация внутреннего шва (см. «Отклонение») |
| 11 тестовых ctor-сайтов ResultsViewModel | сняты 1-й и 2-й аргументы (10 файлов) |
| 3 тестовых сайта MainViewModel | передаётся `.Session` вместо wrapper'а (ctor + SetField ×3) |

## Отклонение от плана (записано, не скрыто)

План предписывал удалить и `IMarkDirtyService`, считая параметры в
`ClimateViewModel`/`ConstructionViewModel`/`ThermalViewModel` мёртвыми.
**Live-проверка опровергла посылку**: параметры живые —

- `ClimateViewModel:254-261` передаёт сервис в `new ProjectSessionClimateState(markDirtyService, …)`;
- `ConstructionViewModel:227-241` — аналогично в `ProjectSessionConstructionState`;
- `ThermalViewModel:235/291` — в `ThermalStateCoordinator` (path dirty-intent,
  зафиксированный счётным harness'ом `CountingMarkDirtyService` в замороженных
  `ThermalMultiplicityCharacterizationTests`, `DirtyIntentCount`);
- `ProjectSession` передаёт себя в `ProjectSessionHydraulicsState` (`this ?? param`).

Полное удаление типа = перекладка ctor-поверхностей срезов/координатора во всех
замороженных кратностных suite'ах — риск, которого план требует избегать.
Решение: `IMarkDirtyService` сохранён как **внутренний шов dirty-owner'а**
(`ProjectSession : IMarkDirtyService`; DI-регистрация `IMarkDirtyService →
ProjectSession` — единственная, без alias-потребителей). Результат для
INV-целей тот же: ни один потребитель не зависит от «сервиса состояния» как
alias-поверхности; dirty-семантика байт-в-байт прежняя (маршрут MarkDirty не
изменился). Остаток (тип-шов) зафиксирован в state-ownership-заметке слайса 8.

## Re-pin'ы (записаны)

- `DiRegistrationTests` (5 мест): `GetRequiredService<IMarkDirtyService>` →
  `GetRequiredService<ProjectSession>` (identity-ассерты «один инстанс на все
  поверхности» сохранены); alias-резолвы → `IProjectSession`.
- `ProjectSessionTests` (2 сайта): аналогично.
- `ProjectStateServiceTests.DependencyInjection_*` (2 теста): переопределены на
  внутренний шов (`ProjectStateService`+`IMarkDirtyService` / `IProjectSession`).
- `ResetOrchestrationTests` (reflection-хелперы): тип поля
  `_projectStateService` → `IProjectSession`.
- `ResultsStabilizationPhase1ContractsTests.LoadProjectDataAsync_…`:
  source-пин `_projectStateService.MarkClean();` → `_projectSession.MarkClean();`.
- `ResultsViewModelOpenProjectTests` (×2): `nameof(IProjectStateService.IsDirty)`
  → `nameof(IProjectSession.IsDirty)` (та же строка события).

## Прогоны (TRX под `logs/`)

1. `dotnet build` — exit 0 (промежуточные состояния: CS0103 ×9 →
   глобальная замена идентификатора; CS1503/CS0246 ×~20 → тест-поддержка +
   re-pin'ы; всё в этом slice).
2. Прогон 1 (18 suite-фильтров): 205 passed / 17 failed → диагностированы
   (DI без регистрации для ctor-швов VM, SetField с wrapper'ом, source-пин).
3. Финальный прогон: `slice-6-alias-removal.trx` —
   **300 passed / 0 failed / 1 skip** (RR-004).
4. Grep-доказательство: `grep -rn "IProjectStateService\|IProjectInfoService" src/`
   → 0 кодовых совпадений (1 совпадение в комментарии Phase 9).

## Dirty baseline delta (этот slice)

Production: удалены 3 файла алиасов; `ProjectSession.cs`, `MainWindow.xaml.cs`,
`MainViewModel.cs`, `ResultsViewModel.cs`, `ServiceCollectionExtensions.cs`.
Tests: `Fixtures/ProjectStateService.cs` (новый), 12 файлов re-pin'ов/ctor-правок.

## Статус

SLICE 6: PASS — forwarding-алиасы удалены, dirty/save/load семантика неизменна.
