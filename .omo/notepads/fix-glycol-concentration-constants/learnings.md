# fix-glycol-concentration-constants — learnings

## Task 1: ValidationConstants 0/60 → 10/90 + use in HydraulicValidator

### Result
- `MinGlycolConcentration`: 0.0 → 10.0
- `MaxGlycolConcentration`: 60.0 → 90.0
- `HydraulicValidator.Validate` больше не использует литералы `10`/`90` для
  границ и для текста ошибки — тянет их из `ValidationConstants`.

### Surprises / non-obvious findings
- `ValidationConstants` живёт в namespace `SnowMeltingCalculator.Core.Constants`,
  а не в `SnowMeltingCalculator.Core`. У `HydraulicValidator` уже был
  `using SnowMeltingCalculator.Core;`, но этого НЕДОСТАТОЧНО — пришлось добавить
  `using SnowMeltingCalculator.Core.Constants;`. План задачи предполагал, что
  `SnowMeltingCalculator.Core` покроет, на практике — нет. Без `using`
  компилятор выдаёт `CS0103`.
- В `ValidationConstants` есть шаблон `RangeErrorMessage = "{0} должен быть
  в диапазоне от {1} до {2}"`, но в `HydraulicValidator` он не используется —
  текст ошибки собран через интерполяцию. Оставил текущий стиль, чтобы не
  менять формулировку (замена только границ).

### Verification
- `dotnet build SnowMeltingCalculator.sln -clp:ErrorsOnly` → exit 0.
- Select-String по 4 критериям из задачи:
  - `MinGlycolConcentration\s*=\s*10\.0` → 1 совпадение.
  - `MaxGlycolConcentration\s*=\s*90\.0` → 1 совпадение.
  - `GlycolConcentration\s*<\s*10|GlycolConcentration\s*>\s*90` → 0 совпадений.
  - `MinGlycolConcentration|MaxGlycolConcentration` в `HydraulicValidator.cs` → 2.
- `dotnet test ... --filter "FullyQualifiedName~Glycol"` →
  `Пройдено: 184, не пройдено: 0, пропущено: 0`.

### Commit
`fix(hydraulics): correct glycol concentration range constants and use them in validator`

## Task 2: GlycolDataService uses ValidationConstants + cross-check / regression tests

### Result
- `GlycolDataService` больше не имеет приватных
  `MIN_CONCENTRATION` / `MAX_CONCENTRATION`. Удалены оба поля вместе с
  XML-doc комментариями (L44-52 в исходнике).
- 4 usages переведены на `ValidationConstants.MinGlycolConcentration` /
  `MaxGlycolConcentration`:
  - `IsConcentrationSupported` — диапазон (short-circuit `concentration == 0`
    НЕ тронут, остался выше как отдельная ветка для воды).
  - `GetMinConcentration` / `GetMaxConcentration` — возвращают константы.
  - `ValidateParameters` — guard + текст ошибки «должна быть 0% или в
    диапазоне X-Y%». Guard остался единственным, второй throw не добавлял.
- Добавлен `using SnowMeltingCalculator.Core.Constants;` (в `Core` нет
  нужного неймспейса — перепроверено в Task 1).
- В `GlycolDataServiceTests.cs` добавлены 2 теста (NUnit-стиль):
  - `ValidationConstants_And_HydraulicValidator_Agree_On_GlycolRange` —
    проверяет, что `ValidationConstants.{Min,Max}GlycolConcentration`
    равны 10.0 / 90.0 И что `service.GetMin/MaxConcentration()` отдают
    те же значения. Защита от повторного рассинхрона.
  - `GetProperties_ConcentrationOutOfRange_ThrowsArgumentOutOfRangeException`
    с `[TestCase(5.0)]` + `[TestCase(95.0)]` — регрессия: значения
    за пределами 10–90 должны выбрасывать `ArgumentOutOfRangeException`
    через существующий guard в `ValidateParameters`.

### Surprises / non-obvious findings
- Тесты на Moq (`IGlycolDataServiceTests.cs`) не привязаны к конкретным
  значениям 10.0 / 90.0 — они только сетапят `Setup(s => s.GetMin…)` и
  читают результат. Поскольку возвращаемое число остаётся тем же
  (10.0 / 90.0), эти тесты не ломаются и не требуют правок.
- Реальные `GetMinConcentration_ReturnsCorrectValue` /
  `GetMaxConcentration_ReturnsCorrectValue` в `GlycolDataServiceTests.cs`
  проверяют 10.0 / 90.0 — после рефакторинга числа те же, тесты
  по-прежнему зелёные. Трогать их не пришлось.
- `IsConcentrationSupported` (L186-188) сохранил отдельную ветку
  `if (concentration == 0) return true;` для воды — `0%` не попадает
  в `[10, 90]`, поэтому без short-circuit воду бы считал невалидной
  концентрацией.
- `Select-String` подтверждает:
  - `MIN_CONCENTRATION|MAX_CONCENTRATION` в `GlycolDataService.cs` → 0.
  - `ValidationConstants\.(Min|Max)GlycolConcentration` → 7 совпадений
    (≥ 2 требуемых).

### Verification
- `dotnet build SnowMeltingCalculator.sln -clp:ErrorsOnly` → exit 0,
  0 ошибок, 62 предупреждения (все pre-existing, не от нашего diff).
- `dotnet test ... --filter "FullyQualifiedName~Glycol"` →
  `Пройдено: 187, не пройдено: 0, пропущено: 0` (было 184 в Task 1,
  +3 новых: 1 cross-check + 2 out-of-range кейса).
- `dotnet test ... --filter "FullyQualifiedName~ValidationConstants_And_HydraulicValidator"` → 1 пройден.
- `dotnet test ... --filter "FullyQualifiedName~ConcentrationOutOfRange"` → 2 пройдено.
- Лог сборки: `.omo/evidence/fix-glycol-concentration-constants/task-2.build.log`.

### Commit
`fix(hydraulics): use ValidationConstants for glycol concentration range in GlycolDataService + tests`
