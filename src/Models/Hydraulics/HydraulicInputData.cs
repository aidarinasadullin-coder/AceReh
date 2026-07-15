using CommunityToolkit.Mvvm.ComponentModel;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Входные данные для гидравлического расчёта контуров
    /// </summary>
    /// <remarks>
    /// Содержит только гидравлически-локальные данные, вводимые пользователем.
    /// Значения из ThermalModule, ClimateModule и ICalculationStateService
    /// передаются в калькулятор отдельно через контрактные объекты.
    /// </remarks>
    public partial class HydraulicInputData : ObservableObject
    {
        /// <summary>
        /// Тип гликоля
        /// </summary>
        /// <remarks>
        /// Этиленгликоль или пропиленгликоль
        /// По умолчанию: этиленгликоль
        /// </remarks>
        [ObservableProperty]
        private GlycolType _glycolType = GlycolType.Ethylene;

        /// <summary>
        /// Концентрация гликоля, %
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-90%
        /// По умолчанию: 50%
        /// </remarks>
        [ObservableProperty]
        private double _glycolConcentration = 50.0;

        /// <summary>
        /// Шаг подводки (VA_zul), см
        /// </summary>
        /// <remarks>
        /// По умолчанию: 5 см
        /// </remarks>
        [ObservableProperty]
        private double _supplySpacing_cm = 5.0;

        /// <summary>
        /// Доля тепла от подводок (q_zul), %
        /// </summary>
        /// <remarks>
        /// По умолчанию: 10%
        /// Диапазон: 0-100%
        /// </remarks>
        [ObservableProperty]
        private double _supplyHeatPercent = 10.0;

        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        /// <remarks>
        /// По умолчанию: HKV_D
        /// Определяет kv-значение для расчёта потерь на клапане
        /// </remarks>
        [ObservableProperty]
        private ValveType _valveType = ValveType.HKV_D;

        // === Валидация ===

        /// <summary>
        /// Признак валидности данных
        /// </summary>
        public bool IsValid => Validate().IsValid;

        /// <summary>
        /// Валидировать входные данные
        /// </summary>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate()
        {
            var result = ValidationResult.Success();

            if (GlycolConcentration < 10 || GlycolConcentration > 90)
                result.AddError($"Концентрация гликоля должна быть от 10 до 90% (текущая: {GlycolConcentration:F0}%)");

            if (SupplySpacing_cm <= 0)
                result.AddError("Шаг подводки должен быть положительным");

            if (SupplyHeatPercent < 0 || SupplyHeatPercent > 100)
                result.AddError($"Доля тепла от подводок должна быть от 0 до 100% (текущая: {SupplyHeatPercent:F0}%)");

            return result;
        }
    }
}
