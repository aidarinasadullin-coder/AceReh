# Отчёт о тестировании задачи ThermalViewModel

## Новые тесты

### Unit тесты ThermalViewModel
- ✅ `Constructor_InitializesDefaultValues` — PASSED
- ✅ `Constructor_InitializesCollections` — PASSED
- ✅ `Constructor_SetsDefaultPipe` — PASSED
- ✅ `Constructor_NullCalculator_ThrowsException` — PASSED
- ✅ `Constructor_NullClimateData_ThrowsException` — PASSED
- ✅ `Constructor_NullConstructionData_ThrowsException` — PASSED

### Команда Calculate
- ✅ `Calculate_ValidInput_SetsResult` — PASSED
- ✅ `Calculate_InvalidInput_SetsValidationMessage` — PASSED
- ✅ `Calculate_SetsIsCalculatingDuringExecution` — PASSED
- ✅ `Calculate_UsesClimateData` — PASSED
- ✅ `Calculate_UsesConstructionData` — PASSED
- ✅ `Calculate_InvalidClimateData_ShowsError` — PASSED

### Команда Reset
- ✅ `Reset_ResetsAllPropertiesToDefaults` — PASSED

### Валидация
- ✅ `Validate_SupplyTemperatureTooLow_ReturnsFalse` — PASSED
- ✅ `Validate_SupplyTemperatureTooHigh_ReturnsFalse` — PASSED
- ✅ `Validate_DeltaTTooLow_ReturnsFalse` — PASSED
- ✅ `Validate_DeltaTTooHigh_ReturnsFalse` — PASSED
- ✅ `Validate_GroundTemperatureTooLow_ReturnsFalse` — PASSED
- ✅ `Validate_GroundTemperatureTooHigh_ReturnsFalse` — PASSED
- ✅ `Validate_PipeSpacingTooLow_ReturnsFalse` — PASSED
- ✅ `Validate_PipeSpacingTooHigh_ReturnsFalse` — PASSED
- ✅ `Validate_ValidInput_ReturnsTrue` — PASSED

### BuildThermalParameters
- ✅ `BuildThermalParameters_ReturnsCorrectParameters` — PASSED
- ✅ `BuildThermalParameters_IncludesClimateData` — PASSED
- ✅ `BuildThermalParameters_IncludesConstructionData` — PASSED

### Выбор режима
- ✅ `SelectedMode_AntiIcing_SetsCorrectValue` — PASSED
- ✅ `SelectedMode_Melting_SetsCorrectValue` — PASSED
- ✅ `SelectedMode_Intensive_SetsCorrectValue` — PASSED

### Выбор трубы
- ✅ `SelectedPipe_CanSelectDifferentPipes` — PASSED

### Обработка изменений данных
- ✅ `ClimateDataChanged_ClearsResult` — PASSED
- ✅ `ConstructionDataChanged_ClearsResult` — PASSED

## Регрессионные тесты
- Всего: 98
- Пройдено: 98
- Упало: 0

## Итог
✅ Все тесты прошли успешно