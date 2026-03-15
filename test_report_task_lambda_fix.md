# Отчёт о тестировании: Исправление выбора λ при изменении материала

## Описание исправления

### Проблема
В `Layer.cs` при изменении материала для слоя под трубой всегда использовался `LambdaA`, независимо от УГВ.

### Решение
1. **Layer.cs**: Упрощена логика setter'а для Material - теперь всегда устанавливается `LambdaA` по умолчанию, а для слоёв под трубой `UpdateLambda()` вызывается отдельно с учётом УГВ.

2. **ConstructionViewModel.cs**: В методе `OnLayerPropertyChanged` добавлен вызов `layer.UpdateLambda(GroundwaterLevel)` при изменении материала.

## Изменённые файлы

### src/Models/Construction/Layer.cs
- Строки 35-50: Упрощена логика setter'а для Material
- Добавлен комментарий о том, что для слоёв под трубой `UpdateLambda()` вызывается отдельно

### src/ViewModels/Construction/ConstructionViewModel.cs
- Строки 648-660: В методе `OnLayerPropertyChanged` добавлен вызов `UpdateLambda()` при изменении материала

## Результаты тестирования

### Регрессионные тесты
- Всего: 196
- Пройдено: 196
- Провалено: 0

### Ключевые тесты
- ✅ `SetGroundwaterLevelBelow1Meter_UpdatesLambdaForBelowPipeLayers` — PASSED
- ✅ `SetGroundwaterLevelAbove1Meter_UsesLambdaAForBelowPipeLayers` — PASSED
- ✅ `AddLayerBelowPipe_SetsCorrectPosition` — PASSED
- ✅ `UpdateCalculations_CalculatesR2Correctly` — PASSED

## Итог
✅ Все тесты прошли успешно. Исправление корректно обрабатывает выбор λ при изменении материала для слоёв под трубой с учётом УГВ.