using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// KPI-презентер дашборда Results: чистая функция «канонический snapshot +
    /// режим → KPI» (DE-3, по образцу <see cref="HydraulicSummaryBuilder"/>).
    /// Без собственного состояния и WPF-типов; VM делегирует вычисления и
    /// назначает read-model наблюдаемым свойствам. Формулы 1:1 с прежними
    /// методами ResultsViewModel (характеризация:
    /// ResultsKpiCharacterizationTests).
    /// </summary>
    /// <remarks>
    /// Источники данных — канонические снимки: HydraulicsState (коллекторы)
    /// и ThermalState (внутренний диаметр трубы); VM передаёт их развёрнуто,
    /// чтобы презентер не зависел от сессии.
    /// </remarks>
    public class ResultsKpiPresenter
    {
        /// <summary>Коэффициент расширения воды (примерно 0.034 при 80°C).</summary>
        private const double WaterExpansionBeta = 0.034;

        /// <summary>Коэффициент запаса объёма расширительного бака.</summary>
        private const double ExpansionTankReserveFactor = 1.2;

        /// <summary>
        /// Построить KPI дашборда: суммарная мощность, объём системы, параметры
        /// насоса и расширительный бак.
        /// </summary>
        public ResultsKpiReadModel BuildKpis(
            IReadOnlyList<HydraulicCollectorSnapshot>? collectors,
            double? pipeInnerDiameterMm,
            bool isOperatingMode)
        {
            // Суммируем мощности коллекторов; при пустом списке (или null)
            // итерация просто не выполняется, и итог корректно обнуляется
            // (stale-значение из предыдущего проекта недопустимо).
            double totalPower_W = 0;

            double totalLength = 0;
            double totalFlowRate_Lh = 0;
            double maxPressureLoss_Pa = 0;

            foreach (var collector in collectors ?? Array.Empty<HydraulicCollectorSnapshot>())
            {
                if (collector?.Summary != null)
                {
                    totalPower_W += collector.Summary.TotalPower;
                    totalFlowRate_Lh += collector.Summary.TotalFlowRate;

                    // Максимальные потери в зависимости от режима
                    double pressureLoss = isOperatingMode
                        ? collector.Summary.PressureLoss_Operating_Pa
                        : collector.Summary.PressureLoss_Cold_Pa;

                    if (pressureLoss > maxPressureLoss_Pa)
                    {
                        maxPressureLoss_Pa = pressureLoss;
                    }
                }

                if (collector?.Circuits == null) continue;

                // TotalLength контура = CircuitLength + SupplyLength (как CircuitRow.TotalLength).
                foreach (var circuit in collector.Circuits)
                {
                    totalLength += circuit.CircuitLength + circuit.SupplyLength;
                }
            }

            // Внутренний диаметр трубы — из канонического ThermalState snapshot
            // (Todo 10 / DEC-T07), мм → м.
            double innerDiameter_m = (pipeInnerDiameterMm ?? 0) / 1000.0;

            // V = π × d²/4 × L × 1000 (литры); без трубы объём нулевой.
            var systemVolume_L = innerDiameter_m > 0
                ? Math.PI * Math.Pow(innerDiameter_m, 2) / 4.0 * totalLength * 1000.0
                : 0;

            return new ResultsKpiReadModel
            {
                TotalThermalPower_kW = totalPower_W / 1000.0,
                TotalPipeLength = totalLength,
                SystemVolume_L = systemVolume_L,
                PumpFlowRate_m3h = totalFlowRate_Lh / 1000.0,
                PumpHead_kPa = maxPressureLoss_Pa / 1000.0,
                ExpansionTankVolume_L = systemVolume_L * WaterExpansionBeta * ExpansionTankReserveFactor
            };
        }

        /// <summary>
        /// Построить напор насоса (максимальные потери давления) для текущего режима;
        /// используется при переключении режима без полного пересчёта KPI.
        /// </summary>
        public double BuildPumpHead(
            IReadOnlyList<HydraulicCollectorSnapshot>? collectors,
            bool isOperatingMode)
        {
            double maxPressureLoss_Pa = 0;

            foreach (var collector in collectors ?? Array.Empty<HydraulicCollectorSnapshot>())
            {
                if (collector?.Summary != null)
                {
                    double pressureLoss = isOperatingMode
                        ? collector.Summary.PressureLoss_Operating_Pa
                        : collector.Summary.PressureLoss_Cold_Pa;

                    if (pressureLoss > maxPressureLoss_Pa)
                    {
                        maxPressureLoss_Pa = pressureLoss;
                    }
                }
            }

            return maxPressureLoss_Pa / 1000.0;
        }
    }

    /// <summary>
    /// Read-model KPI дашборда Results: значения назначаются наблюдаемым
    /// свойствам ResultsViewModel без промежуточных пересчётов в VM.
    /// </summary>
    public sealed class ResultsKpiReadModel
    {
        /// <summary>Суммарная тепловая мощность всех контуров, кВт.</summary>
        public double TotalThermalPower_kW { get; init; }

        /// <summary>Общая длина труб, м.</summary>
        public double TotalPipeLength { get; init; }

        /// <summary>Объём системы (труб), литры.</summary>
        public double SystemVolume_L { get; init; }

        /// <summary>Суммарный расход насоса, м³/ч.</summary>
        public double PumpFlowRate_m3h { get; init; }

        /// <summary>Напор насоса (максимальные потери), кПа.</summary>
        public double PumpHead_kPa { get; init; }

        /// <summary>Объём расширительного бака, литры (V_системы × β × 1.2).</summary>
        public double ExpansionTankVolume_L { get; init; }
    }
}
