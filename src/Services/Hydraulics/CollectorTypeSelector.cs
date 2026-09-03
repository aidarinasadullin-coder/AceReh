using System.Globalization;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Автоматический подбор типа коллектора по расходу
    /// </summary>
    /// <remarks>
    /// Правила выбора:
    /// - ≤ 1.5 м³/ч → HKV-D (2-12 контуров)
    /// - 1.5 < G < 2.5 м³/ч → IV 1¼" (2-12 контуров)
    /// - 2.5 ≤ G < 7 м³/ч → IV 1½" (2-12 контуров)
    /// - ≥ 7 м³/ч → предупреждение о превышении расхода
    /// 
    /// Дополнительно проверяется:
    /// - Δp ≤ 320 мбар (32000 Па) — ограничение РЕХАУ
    ///   Проверка выполняется для ОБОИХ режимов: рабочего и холодного пуска
    /// </remarks>
    public class CollectorTypeSelector : ICollectorTypeSelector
    {
        /// <summary>
        /// Автоматически подобрать тип коллектора по расходу
        /// </summary>
        public CollectorSelectionResult SelectCollectorType(CollectorData collector)
        {
            var result = new CollectorSelectionResult();

            var summary = collector.Summary;
            if (summary == null)
            {
                return result;
            }

            // Суммарный расход в м³/ч
            var totalFlowRate_m3h = summary.TotalFlowRate / 1000.0;

            // Проверка превышения давления (320 мбар = 32000 Па = 32 кПа)
            // Проверка выполняется для ОБОИХ режимов: рабочего и холодного пуска
            var warnings = new List<string>();

            // Рабочий режим
            if (summary.PressureLoss_Operating_Pa > CollectorSummary.MaxAllowedPressure_Pa)
            {
                double pressureKPa = summary.PressureLoss_Operating_Pa / 1000.0;
                warnings.Add($"Превышение давления (рабочий режим): {pressureKPa:F1} кПа > 32 кПа");
            }

            // Холодный пуск
            if (summary.PressureLoss_Cold_Pa > CollectorSummary.MaxAllowedPressure_Pa)
            {
                double pressureKPa = summary.PressureLoss_Cold_Pa / 1000.0;
                warnings.Add($"Превышение давления (холодный пуск): {pressureKPa:F1} кПа > 32 кПа");
            }

            bool flowRateExceeded = totalFlowRate_m3h >= 7.0;

            // Установка предупреждений
            if (warnings.Count > 0)
            {
                // Объединяем предупреждения о давлении
                result.Warning = string.Join("\n", warnings);
            }
            // Предупреждение о расходе (только если давление в норме)
            else if (flowRateExceeded)
            {
                // Предупреждение о превышении расхода
                // Используем инвариантную культуру для форматирования (точка как разделитель)
                result.Warning = $"Превышение расхода: {totalFlowRate_m3h.ToString("F2", CultureInfo.InvariantCulture)} м³/ч ≥ 7.0 м³/ч. Рекомендуется разделить на несколько коллекторов.";
            }

            // Автоматический выбор типа коллектора по расходу
            // (не зависит от предупреждений о давлении)
            if (totalFlowRate_m3h >= 2.5)
            {
                result.CollectorType = "IV 1½\" (2-12 контуров)";
                result.ValveType = ValveType.IV_1_5;
            }
            else if (totalFlowRate_m3h > 1.5)
            {
                result.CollectorType = "IV 1¼\" (2-12 контуров)";
                result.ValveType = ValveType.IV_1_25;
            }
            else
            {
                result.CollectorType = "HKV-D (2-12 контуров)";
                result.ValveType = ValveType.HKV_D;
            }

            return result;
        }
    }
}