# capture-improvement-items - Work Plan

## TL;DR (For humans)

**Что получите:** 9 новых пунктов (№ 10–18) в `docs/Планируемые_изменения.md`, выявленных в ходе рефакторинга `refactor-dedupe-params` и плана `fix-thermal-to-hydraulics-sync`. Документ остаётся в едином стиле «Проблема → Решение → Приоритет».

**Усилия:** XS — 1 файл правки, текст уже подготовлен.

## Todos

- [x] 1. Append items 10–18 to docs/Планируемые_изменения.md
  Commit: Y | docs: add improvement items 10-18 from refactor-dedupe-params learnings

---

## READY-TO-PASTE CONTENT

> Вставить ВСЁ содержимое ниже (начиная с `---` и заканчивая закрывающей заметкой) ПЕРЕД существующей закрывающей заметкой `*Все предложения основаны...` в `docs/Планируемые_изменения.md`. Старую закрывающую заметку УДАЛИТЬ (новая включена в конце блока).

---

## 10. Зоопарк `ValidationResult` и отсутствие единого конвейера валидации (системная архитектура)

### Проблема

В кодовой базе **минимум два** одноимённых класса `ValidationResult` в разных пространствах имён — копия-в-копию (~140 строк дубликата каждый), не связанных ни наследованием, ни общим интерфейсом:

| Класс | Namespace | Файл |
|-------|-----------|------|
| `ValidationResult` | `SnowMeltingCalculator.Models.Hydraulics` | `src/Models/Hydraulics/ValidationResult.cs` |
| `ValidationResult` | `SnowMeltingCalculator.Models.Construction` | `src/Models/Construction/ValidationResult.cs` |

Оба имеют `IsValid`, `Errors`, `Warnings`, `AddError`, `AddWarning`, `Merge`, `GetAllMessages` — идентичный API. Любой код, принимающий `ValidationResult`, молча привязан к одному из двух неймспейсов. Их нельзя смешивать без явного маппинга.

Одновременно **валидация одного и того же объекта конструкции идёт по двум веткам** с пересечением и расхождением правил:

| Ветка | Метод | Расположение | Кто вызывает |
|-------|-------|--------------|--------------|
| Уровень модели | `Construction.ValidateConstruction()` | `src/Models/Construction/Construction.cs:276–327` | `Construction.IsValid` property (стр. 89), UI напрямую |
| Уровень сервиса | `ConstructionValidator.Validate()` | `src/Services/Construction/ConstructionValidator.cs:63–94` (через `ConstructionService.ValidateConstruction()` — `ConstructionService.cs:122`) | `ConstructionViewModel.Validate()` (стр. 565), тесты |

**Пересечение** — оба пути проверяют: наличие слоёв, минимальную толщину над трубой, УГВ, материалы.

**Расхождение** (зафиксировано по коду):

| Проверка | `Construction.ValidateConstruction()` (модель) | `ConstructionValidator.Validate()` (сервис) |
|----------|------------------------------------------------|---------------------------------------------|
| Мин. толщина над трубой | хардкод `50.0`/`40.0` (стр. 288) | именованные константы `MinThicknessAbovePipe*` |
| Макс. толщина слоя | хардкод `1000` (стр. 298) | константа `MaxLayerThickness` |
| УГВ диапазон | хардкод `0–10` (стр. 305) | константы `MinGroundwaterLevel`/`MaxGroundwaterLevel` |
| Материалы: MaxSupplyTemp | для **всех** слоёв (стр. 313) | только для `Concrete`/`Screed` (стр. 185) |
| Материалы: MinOutdoorTemp | для **всех** слоёв (стр. 319) | только для `Coating` (стр. 197) |
| λБ при высоком УГВ | **нет** | `ValidateLambdaForGroundwater` (стр. 91) |

Если константы в `ConstructionValidator` изменят, модель продолжит проверять по хардкоду — поведение UI и сервиса разойдётся.

В целом проверки **распределены как минимум по 5 классам** с разными сигнатурами возвращаемых значений:

| Класс | Метод | Возвращаемый тип | Расположение |
|-------|-------|------------------|--------------|
| `ThermalCalculator` | `Validate(...)` | `bool` + `out string[]` | `src/Services/Thermal/ThermalCalculator.cs:590` |
| `ThermalViewModel` | `ValidateInput()` | `bool` | `src/ViewModels/Thermal/ThermalViewModel.cs:358` (вызван на стр. 268) |
| `HydraulicInputData` | `Validate()` | `ValidationResult` (Hydraulics) | `src/Models/Hydraulics/HydraulicInputData.cs:75` |
| `Construction` (модель) | `ValidateConstruction()` | `ValidationResult` (Construction) | `src/Models/Construction/Construction.cs:276` |
| `ConstructionValidator` | `Validate(...)` | `ValidationResult` (Construction) | `src/Services/Construction/ConstructionValidator.cs:63` |

Дополнительно: `CircuitsCalculator.CalculateAllCircuits()` вызывает `inputData.Validate()` **внутри калькулятора** (`src/Services/Hydraulics/CircuitsCalculator.cs:142`) — валидация смешана с расчётом, а не вынесена в конвейер.

Нет ни единого конвейера, ни общего контракта. Часть валидаторов возвращает `bool` + `out string[]`, часть — `ValidationResult` (двух разных типов), часть живёт в модели, часть в сервисе, часть в калькуляторе, часть в ViewModel. Каждый новый пункт валидации (включая п. 1, 2, 3, 5, 11 ниже) будет лепить проверку в очередное место и усиливать хаос.

### Чем чревато

1. **Рассогласованность логики** — проверку можно починить в одном валидаторе и оставить баг в другом (модельный путь vs сервисный путь).
2. **Невозможность атомарной валидации** — нет единого вызова «проверить всё перед расчётом». Пользователь видит ошибки по шагам из разных ViewModel, а не единый список.
3. **Проблема с тестированием** — тесты `ConstructionServiceTests` покрывают сервисный путь; модельный путь не покрыт отдельными тестами. Изменение модельного пути не сломает тесты сервиса, но изменит поведение UI.
4. **Невозможность расширения** — добавление нового модуля требует решения «какой `ValidationResult` использовать?» вместо просто «реализовать `IValidator<T>`».
5. **Дублирование кода** — два класса `ValidationResult` (~140 строк копипасты). Добавление метода требует изменения в обоих.

### Предлагаемое решение (три варианта)

**Вариант А (минимальный):**
- Удалить `Construction.ValidateConstruction()` (метод модели) и `Construction.IsValid` property (стр. 89). Оставить только сервисный путь `ConstructionValidator.Validate()`.
- Объединить два `ValidationResult` в один класс `Core/ValidationResult.cs`.
- Устраняет двойной путь для конструкции и дубликат `ValidationResult`, но не решает проблему зоопарка валидаторов в целом.

**Вариант Б (правильный — рекомендуется):**
1. **Единый `ValidationResult`** — один класс `Core/ValidationResult.cs` (или интерфейс `IValidationResult` + реализация). Удалить дубликаты.
2. **Единый контракт** — все валидаторы возвращают `ValidationResult`, а не `bool` + `out string[]`. Привести `ThermalCalculator.Validate` и `ThermalViewModel.ValidateInput` к тому же контракту.
3. **`IValidator<T>` интерфейс** — `public interface IValidator<T> { ValidationResult Validate(T input); }`
4. **Каноничные валидаторы** (по одному на модуль): `ClimateValidator`, `ConstructionValidator` (max supply temp с **сравнением фактической T_подачи**), `ThermalValidator` (входные параметры), `ThermalResultValidator` (пост-расчёт: T_обратки > 0, ΔT ≤ 30 — решает п. 1), `HydraulicValidator`, `CircuitValidator`.
5. **`IValidationPipeline.ValidateAll(context)`** — сборка результатов из всех валидаторов в единый `ValidationResult` (через `Merge`). UI показывает один диалог.
6. **Удалить валидацию из моделей и калькуляторов**: `Construction.IsValid` (стр. 89), `HydraulicInputData.Validate()` (стр. 75), `CircuitsCalculator` вызов `inputData.Validate()` (стр. 142) — валидация до вызова калькулятора.

**Вариант В (компромисс):**
- Оставить текущую структуру, но: удалить `Construction.ValidateConstruction()` (метод модели), объединить `ValidationResult` в один, добавить `ThermalResultValidator` для пост-расчётной проверки, исправить MaxSupplyTemp warning (сравнивать с фактической T_подачи). Меньший рефакторинг, устраняет острые проблемы.

### Приоритет

**Высокий (системный).** Это не отдельный баг, а рамочное решение, которое определяет, **как** реализовывать п. 1–3, 5, 11. Без него каждый фикс добавляет проверки в новом месте и усиливает хаос. Должно быть принято до серьёзной реализации валидационных пунктов.

---

## 11. Баг констант гликоля: `ValidationConstants` vs `HydraulicInputData` (гидравлика, противоречие констант)

### Проблема

В коде **две противоречащие системы границ** для концентрации гликоля:

| Источник | Границы | Файл:строка |
|----------|---------|-------------|
| `ValidationConstants.MinGlycolConcentration` | **0.0 %** | `src/Core/Constants/ValidationConstants.cs:239` |
| `ValidationConstants.MaxGlycolConcentration` | **60.0 %** | `src/Core/Constants/ValidationConstants.cs:244` |
| `HydraulicInputData.Validate()` — хардкод | **10–90 %** | `src/Models/Hydraulics/HydraulicInputData.cs:79` |

```csharp
// ValidationConstants.cs:239,244
public const double MinGlycolConcentration = 0.0;
public const double MaxGlycolConcentration = 60.0;

// HydraulicInputData.cs:79
if (GlycolConcentration < 10 || GlycolConcentration > 90)
    result.AddError($"Концентрация гликоля должна быть от 10 до 90% (текущая: {GlycolConcentration:F0}%)");
```

Противоречия:
- `ValidationConstants` разрешает 0–60 %, `HydraulicInputData.Validate()` требует 10–90 %.
- **90 > 60**: верхние границы несовместимы. Значение 65 % проходит `HydraulicInputData` (65 ≤ 90), но нарушает `ValidationConstants` (65 > 60). Значение 5 % проходит `ValidationConstants` (5 ≥ 0), но нарушает `HydraulicInputData` (5 < 10).
- `HydraulicInputData.Validate()` вообще не ссылается на `ValidationConstants` — границы зашиты литералами `10` и `90`.

**Критически:** константы `MinGlycolConcentration` / `MaxGlycolConcentration` — **мёртвый код** (grep подтвердил: единственные упоминания — объявления в `ValidationConstants.cs:239,244`, ни одного вызова). Инфраструктура `ValidationExtensions.ValidateRange()` (строки 22–69) принимает min/max параметрами и не берёт их из `ValidationConstants`. То есть даже при желании использовать эти константы инфраструктура к этому не заточена.

**Каноничный диапазон подтверждён:**
- README: «Этиленгликоль: 10–90 % концентрация».
- `data/glycol_data.json:27`: `"concentration_vol_pct": [10, 20, 30, 40, 50, 60, 70, 80, 90]` — данные покрывают 10–90 %.
- Константы 0–60 ошибочны (0 % — чистая вода, 60 % — заниженная верхняя граница при наличии данных до 90 %).

Это тот же тип дефекта, что описан в п. 4 (дублирование `MinVelocity` с разным значением), но здесь противоречие ещё жёстче — границы физически не пересекаются, плюс константы вообще никто не читает.

### Чем чревато

1. **Мёртвые константы** — `ValidationConstants` 0–60 не используются ни в одном вызове. Любой, кто добавит UI-валидацию через `ValidationExtensions.ValidateRange(..., ValidationConstants.MinGlycolConcentration, ...)`, получит диапазон 0–60 — пользователь сможет ввести 65 %, что пройдёт UI, но может вызвать проблемы в расчёте.
2. **Экстраполяция за пределы таблицы** — `glycol_data.json` покрывает 10–90 % (`concentration_vol_pct: [10,20,30,40,50,60,70,80,90]`). Значение 95 % пройдёт хардкод-валидацию `HydraulicInputData` (95 ≤ 90? нет, 95 > 90 — отловится). Но если кто-то случайно использует мёртвые константы 0–60, то 65 % пройдёт, а данных в JSON для диапазона 60–90 достаточно, но за пределами 90 — нет → молчаливая экстраполяция с мусором.
3. **Дублирование границ** — два источника истины (константы + хардкод), уже рассинхронизированы. Классический источник багов при расширении.

### Предлагаемое решение (Вариант Б — правильный)

1. **Зафиксировать каноничный диапазон** в `ValidationConstants`: `MinGlycolConcentration = 10.0`, `MaxGlycolConcentration = 90.0` (подтверждено README и `glycol_data.json:27` — `"concentration_vol_pct": [10,20,30,40,50,60,70,80,90]`).
2. **Использовать константы** в `HydraulicInputData.Validate()` (стр. 79): заменить литералы `10`/`90` на `ValidationConstants.MinGlycolConcentration`/`MaxGlycolConcentration`.
3. **Добавить защиту в `GlycolDataService.GetProperties()`**: если запрашиваемая концентрация вне `[MinGlycolConcentration, MaxGlycolConcentration]` → бросать `ArgumentOutOfRangeException`, а не экстраполировать молча за пределы таблицы. Это защищает от случаев, когда валидация обойдена (прямое присвоение, тесты, загрузка проекта).
4. `ValidationConstants` — единственное место хранения границ; все валидаторы и UI-биндинги ссылаются на него.

### Затрагиваемые файлы

- `src/Core/Constants/ValidationConstants.cs:239,244`
- `src/Models/Hydraulics/HydraulicInputData.cs:79`
- `src/Services/Hydraulics/GlycolDataService.cs` (метод `GetProperties` — добавить guard)

### Приоритет

**Высокий.** Реальный баг: противоречащие границы валидации позволяют или блокируют значения, которые не должны проходить.

---

## 12. Pre-existing warnings, блокирующие gate `TreatWarningsAsErrors` (сборка)

### Проблема

Чистый билд `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true --no-incremental` падает из-за предупреждений в трёх файлах, не связанных с текущим фиксом:

| Файл:строка | Код | Описание |
|-------------|-----|----------|
| `src/ViewModels/Hydraulics/CollectorViewModel.cs:191` | `CS1998` | async-метод без `await` — выполняется синхронно |
| `src/ViewModels/Results/ResultsViewModel.cs:518` | `CS8604` | возможный null-аргумент для непустого ссылочного типа |
| `src/UI/MainWindow.xaml.cs:32` | `CS0169` | неиспользуемое поле |

Эти предупреждения не вызваны планом `fix-thermal-to-hydraulics-sync`, но блокируют gate `TreatWarningsAsErrors=true`, который требуется во всех планах. Тестовый проект также содержит ~40 предупреждений (`CS8625`, `CS8618`, `CS0219`, `CS8602`, `CS1998`) в файлах `Converters/`, `Views/Hydraulics/`, `Integration/`, `Repositories/`, `Services/`.

### Предлагаемое решение

Отдельным маленьким планом:
- `CS1998` — добавить `await Task.CompletedTask` или убрать `async`.
- `CS8604` — проверить на null или пометить `!` (null-forgiving).
- `CS0169` — удалить неиспользуемое поле.
- Для тестового проекта — либо починить по тому же шаблону, либо подавить на уровне проекта (`<NoWarn>CS8618;CS8625</NoWarn>`).

### Затрагиваемые файлы

- `src/ViewModels/Hydraulics/CollectorViewModel.cs`
- `src/ViewModels/Results/ResultsViewModel.cs`
- `src/UI/MainWindow.xaml.cs`
- `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` (возможно, `<NoWarn>`)

### Приоритет

Средний. Не дефект текущего фикса, но мешает любому плану с build-gate'ом `TreatWarningsAsErrors`.

---

## 13. Архитектурно переписать `UpdateFromThermalModule` — принцип «один писатель» (архитектура)

### Проблема

`CircuitsViewModel.UpdateFromThermalModule()` (`src/ViewModels/Hydraulics/CircuitsViewModel.cs:679–717`) пишет тепловые данные в `CalculationContext` с `source = "CircuitsViewModel"`:

- `_calculationContext.UpdateThermalInputs(inputs, "CircuitsViewModel")`
- `_calculationContext.UpdateThermal(result, "CircuitsViewModel")`

Это нарушает принцип «один писатель» для тепловых данных:
- Канонический писатель тепла в runtime — `ThermalViewModel` (`src/ViewModels/Thermal/ThermalViewModel.cs:280–296`, вызывает `UpdateThermal` с `source = "Thermal"`).
- `CircuitsViewModel` — гидравлическая VM — должен быть **чистым потребителем** контекста (читать и реагировать), а не писать обратно тепловые данные.
- `UpdateFromThermalModule` сейчас используется для явного пуша при загрузке проекта и в прямых тестах. Но он пишет в контекст, что семантически неверно: загрузка проекта должна идти через `ThermalViewModel` или отдельный сервис загрузки.

После фикса `OnCalculationContextChanged` (план `fix-thermal-to-hydraulics-sync`) обработчик игнорирует события с `source == "CircuitsViewModel"`, чтобы избежать двойного пересчёта. Это работает, но означает, что `UpdateFromThermalModule` пишет в контекст, а `CircuitsViewModel` тут же игнорирует собственную запись — обходной путь вместо чистой архитектуры.

### Предлагаемое решение

- Убрать запись в контекст из `UpdateFromThermalModule`. Метод должен только обновлять `InputData` и вызывать `Calculate()` — без `_calculationContext.UpdateThermal*`.
- Перенести ответственность за публикацию тепловых данных в контекст при загрузке проекта в `ThermalViewModel` (или отдельный `ProjectLoadService`), который вызывает `ThermalViewModel.Result = loadedResult` → `UpdateThermal("Thermal")`.
- После этого `CircuitsViewModel` становится чистым потребителем: единственный путь обновления — через `OnCalculationContextChanged`.
- Обновить прямые тесты `UpdateFromThermalModule_*` — они вызывают метод напрямую и проверяют `InputData`, а не запись в контекст.

### Затрагиваемые файлы

- `src/ViewModels/Hydraulics/CircuitsViewModel.cs:679–717` (`UpdateFromThermalModule`)
- `src/ViewModels/Thermal/ThermalViewModel.cs` (загрузка проекта)
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs` (`UpdateFromThermalModule_*`)

### Приоритет

Средний. Архитектурный долг, выявленный после рефакторинга `refactor-dedupe-params`. Не блокирует работу, но каждый новый сценарий загрузки/пуша будет обходить чистый путь.

---

## 14. Обработка невалидного / Reset теплового расчёта (тепловой → гидравлика)

### Проблема

`ThermalViewModel.Calculate()` публикует `ThermalResult` в контекст только при `IsValid == true`:

```csharp
// ThermalViewModel.cs (~line 290)
if (result.IsValid)
    _calculationContext.UpdateThermal(result, "Thermal");
```

Если результат **невалидный** (например, труба не выбрана, температуры некорректны), `UpdateThermal` не вызывается. `CalculationContext.ThermalResult` остаётся со старым (возможно валидным) значением, и гидравлика (`CircuitsViewModel`) продолжает отображать устаревшие данные без какого-либо сигнала инвалидации.

Симптом, наблюдённый при ручном QA F3: если труба не выбрана, тепловой расчёт не публикует результат, а блок «Данные укладки и мощности» в гидравлике показывает значения по умолчанию (50 °C / 30 °C / 180 / 80) без явного уведомления об ошибке.

### Предлагаемое решение

- Публиковать невалидный результат в контекст: `_calculationContext.UpdateThermal(result, "Thermal")` — даже при `IsValid == false`.
- В `CircuitsViewModel.OnCalculationContextChanged` для `ThermalResult` с `IsValid == false` вызывать `NotifyThermalPropertiesChanged()` (чтобы UI показал fallback-значения), но **не** вызывать `Calculate()` (расчёт гидравлики по невалидным данным бессмысленен).
- Либо ввести отдельное событие инвалидации (`ThermalInvalidated`) в контексте, которое сбрасывает `InputData` и уведомляет UI.

### Затрагиваемые файлы

- `src/ViewModels/Thermal/ThermalViewModel.cs:280–296` (публикация результата)
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs:759–780` (`OnCalculationContextChanged`)

### Приоритет

Средний. Без этого гидравлика может отображать устаревшие данные после невалидного теплового расчёта.

---

## 15. Мёртвое поле `CalculationContext.HydraulicsResults` (архитектура, dead code)

### Проблема

`CalculationContext` содержит поле/свойство `HydraulicsResults` и метод `UpdateHydraulics(...)`, а также флаг `IsHydraulicsValid`. Однако `CircuitsViewModel.Calculate()` (`src/ViewModels/Hydraulics/CircuitsViewModel.cs:397–547`) **никогда не вызывает** `UpdateHydraulics(...)`.

Следствие:
- `IsHydraulicsValid` всегда `false`.
- `HydraulicsResults` всегда пусто/null.
- Результаты гидравлического расчёта не публикуются в общий контекст — другие модули (если им когда-либо понадобится доступ к результатам гидравлики) не смогут их получить через `CalculationContext`.

### Предлагаемое решение

Одно из двух:
1. **Подключить**: в `CircuitsViewModel.Calculate()` после расчёта вызвать `_calculationContext.UpdateHydraulics(summary, "CircuitsViewModel")` и установить `IsHydraulicsValid = true`. Тогда другие модули смогут читать результаты гидравлики из контекста.
2. **Удалить**: убрать `HydraulicsResults`, `UpdateHydraulics`, `IsHydraulicsValid` из `CalculationContext`, если публикация результатов гидравлики в общий контекст не нужна.

### Затрагиваемые файлы

- `src/Core/CalculationContext.cs` (поле `HydraulicsResults`, метод `UpdateHydraulics`, флаг `IsHydraulicsValid`)
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs:397–547` (`Calculate`)

### Приоритет

Средний. Dead code, который вводит в заблуждение — его наличие намекает на несуществующую интеграцию результатов гидравлики в контекст.

---

## 16. Покрыть тестами другие контекстные пути (тесты)

### Проблема

Баг `fix-thermal-to-hydraulics-sync` проявился, потому что путь `ThermalViewModel.Calculate → CalculationContext → CircuitsViewModel.OnCalculationContextChanged` **не был покрыт** интеграционными тестами. Аналогичный риск существует для других контекстных путей, которые после рефакторинга `refactor-dedupe-params` должны обновлять гидравлику:

| Путь | Риск |
|------|------|
| `CalculationContext.Construction` → `CircuitsViewModel` | Изменение конструкции (R1/R2/LambdaE) может не обновить гидравлику |
| `CalculationContext.Climate` → `CircuitsViewModel` | Сброс климата / выбор другого города может не обновить расчётную температуру |
| Ручное изменение `InputData` в гидравлике | Может не синхронизироваться с контекстом |

`OnCalculationContextChanged` обрабатывает `ThermalInputs`, `ThermalResult`, `Climate`. Но проверены только тепловые случаи. Случай `Climate` имеет `UpdateFromClimateModule()`, но интеграционный тест реального контекстного пути (как `ClimateToHydraulicsIntegrationTests`) проверяет только `PropertyChanged`, а не `CalculationContext.UpdateClimate → OnCalculationContextChanged`.

### Предлагаемое решение

Добавить интеграционные тесты по образцу `ThermalResultChangedViaContext_*`:
- `ConstructionChangedViaContext_NotifiesHydraulics` — `_calculationContext.UpdateConstruction(construction, "Construction")` → проверить, что гидравлика реагирует (если должна).
- `ClimateChangedViaContext_UpdatesDesignTemperature` — `_calculationContext.UpdateClimate(climate, "Climate")` → проверить `DesignTemperature` и `Calculate`.
- `GlycolChangedManually_Recalculates` — ручное изменение `InputData.GlycolConcentration` → проверить `Calculate`.

### Затрагиваемые файлы

- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs` (новые тесты)

### Приоритет

Низкий–Средний. Профилактика — предотвращает повторение того же класса бага в других путях.

---

## 17. Reusable WPF UI-автоматизация для smoke-тестов (инфраструктура тестирования)

### Проблема

При ручном QA (F3 плана `fix-thermal-to-hydraulics-sync`) UI-автоматизация WPF через `System.Windows.Automation` оказалась ненадёжной:
- `SelectionItemPattern.Select()` на `ComboBox` не триггерит WPF-биндинг → труба «не выбрана».
- Навигация по вкладкам через `SelectionItemPattern` на `ListBoxItem` иногда не переключает вид.
- Кириллица в PowerShell-скриптах требует UTF-8 BOM.

В evidence появился `f3-automation.ps1` (447 строк), который пришлось переписывать дважды. Каждый следующий план с ручным QA будет писать подобный скрипт с нуля.

### Предлагаемое решение

- Доработать `f3-automation.ps1` до общего reusable скрипта для smoke-тестов WPF:
  - Надёжный выбор `ComboBox` через `SendInput` / keyboard-навигацию (Alt+Down → arrows → Enter).
  - Хелперы `Find-TabControl`, `Select-ComboBoxItem`, `Click-CalculateButton`, `Read-HydraulicsBlock`.
  - Поддержка параметризации (имя вкладки, имя контрола, ожидаемое значение).
- Либо добавить в проект UI-тесты через **TestStack.White** или **FlaUI** (WPF-specific automation frameworks), которые надёжнее raw UIAutomation.
- Добавить `AutomationProperties.AutomationId` на ключевые контролы (`ComboBox` трубы, `TextBox` температуры подачи, кнопка «Рассчитать») для стабильного поиска.

### Затрагиваемые файлы

- `.omo/evidence/fix-thermal-to-hydraulics-sync/f3-automation.ps1` (базовый скрипт)
- `src/Views/ThermalView.xaml`, `src/Views/Hydraulics/CircuitsView.xaml` (AutomationProperties)
- Возможно: новый тест-проект `tests/SnowMeltingCalculator.UI.Tests` (FlaUI/White)

### Приоритет

Низкий. Инфраструктурное улучшение — не баг, но ускорит будущие QA-циклы.

---

## 18. `.omo/run-continuation/*.json` в `.gitignore` (репозиторий)

### Проблема

Файлы `.omo/run-continuation/ses_*.json` создаются и модифицируются при каждой сессии OMO. Они появились в `git diff` после плана `fix-thermal-to-hydraulics-sync` (16 файлов `ses_*.json`):

```
.omo/run-continuation/ses_09d96976cffext3rzsq4b8Ne77.json
.omo/run-continuation/ses_09d9b6135ffe3RwL5nxwdgBI5V.json
... (всего 16 файлов)
```

Это runtime-метаданные OMO (session state), не имеющие отношения к коду проекта. Они засоряют `git diff` и мешают ревью.

### Предлагаемое решение

Добавить в `.gitignore`:

```gitignore
# OMO runtime session state
.omo/run-continuation/
```

При необходимости оставить `.omo/` в индексе (планы, notepads, evidence), но исключить именно `run-continuation/`.

### Затрагиваемые файлы

- `.gitignore`

### Приоритет

Низкий. Косметическое — не влияет на функциональность, но убирает шум из `git diff`.

---

*Все предложения основаны на анализе исходного кода (`ThermalCalculator.cs`, `CircuitsCalculator.cs`, `ValidationConstants.cs`, `HydraulicInputData.cs`, `ConstructionValidator.cs`, `ConstructionService.cs`, `Construction.cs`, `CircuitsViewModel.cs`, `materials_db.json`) и сверке с документацией (`docs/Formulas_Snegotayanie.md`). Пункты 10–18 выявлены дополнительно в процессе рефакторинга `refactor-dedupe-params` и плана `fix-thermal-to-hydraulics-sync`.*

## END READY-TO-PASTE CONTENT
