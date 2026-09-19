using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Каталог советов v1 (план 2026-09-13-thermal-advice, §4):
    /// RETURN_NEGATIVE и DELTAT_MAX. Условия — те же неравенства на тех же
    /// константах, что у <see cref="ThermalResultValidator"/>, поэтому набор
    /// советов по веткам обратки/ΔT&gt;30 совпадает с его ошибками.
    /// Направления лечения — по физике T_обратка = 2×T_средняя − T_подача,
    /// ΔT = 2×(T_подача − T_средняя): подача ↓ или шаг ↑.
    /// </summary>
    public class ThermalAdviceService : IThermalAdviceService
    {
        /// <summary>Заголовок целевого шага советов v1 (все советы — Thermal)</summary>
        private const string ThermalStepTitle = "Тепловой расчёт";

        /// <summary>
        /// Целевой перепад рекомендации — канон «для ΔT ≈ 15 К» (тот же, что в
        /// постоянном хинте ThermalViewModel.RecommendedSupplyTemperature =
        /// T_средняя + 15/2). Одна цель закрывает оба лимита: обратка =
        /// T_средняя − 15/2 ≥ 0 и ΔT ≈ 15 ≤ 30 — советы сходятся за одну
        /// итерацию (отзыв владельца 2026-09-14: граничные ориентиры вели
        /// к блужданию 50 → 45,9 → 38,0).
        /// </summary>
        private const double TargetDeltaT = 15.0;

        /// <inheritdoc/>
        public IReadOnlyList<ThermalAdvice> Build(ThermalCalculationResult result)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(result));

            var advice = new List<ThermalAdvice>();

            double returnTemperature = 2.0 * result.MeanTemperature - result.SupplyTemperature;
            double recommendedSupply = result.MeanTemperature + TargetDeltaT / 2.0;
            if (returnTemperature < 0)
            {
                advice.Add(new ThermalAdvice
                {
                    Id = "RETURN_NEGATIVE",
                    // Warning — слой рекомендаций ничего не гейтит; красным
                    // говорит только shell (решение владельца 2026-09-15)
                    Severity = ThermalAdviceSeverity.Warning,
                    Message = string.Create(AppCulture.Culture,
                        $"Обратка {(returnTemperature).ToString("F1", AppCulture.Culture)} °C: уменьшите температуру подачи " +
                        $"до ≈{(recommendedSupply).ToString("F1", AppCulture.Culture)} °C (для ΔT ≈ {(TargetDeltaT).ToString("F0", AppCulture.Culture)} К) или увеличьте шаг укладки"),
                    TargetStep = NavigationTarget.Thermal,
                    TargetTitle = ThermalStepTitle
                });
            }

            if (result.DeltaT > ValidationConstants.MaxDeltaT)
            {
                advice.Add(new ThermalAdvice
                {
                    Id = "DELTAT_MAX",
                    Severity = ThermalAdviceSeverity.Warning,
                    Message = string.Create(AppCulture.Culture,
                        $"Перепад {(result.DeltaT).ToString("F1", AppCulture.Culture)} K превышает {(ValidationConstants.MaxDeltaT).ToString("F0", AppCulture.Culture)} K: " +
                        $"уменьшите подачу до ≈{(recommendedSupply).ToString("F1", AppCulture.Culture)} °C (для ΔT ≈ {(TargetDeltaT).ToString("F0", AppCulture.Culture)} К) " +
                        $"или увеличьте шаг укладки"),
                    TargetStep = NavigationTarget.Thermal,
                    TargetTitle = ThermalStepTitle
                });
            }

            return advice;
        }
    }
}
