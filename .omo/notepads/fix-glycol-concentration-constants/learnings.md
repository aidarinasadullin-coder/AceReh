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
