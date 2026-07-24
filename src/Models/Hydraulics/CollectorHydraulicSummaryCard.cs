using CommunityToolkit.Mvvm.ComponentModel;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Карточка итогов гидравлики для одного коллектора.
    /// Канонический read-only снимок CollectorData.Summary для отображения в Results.
    /// </summary>
    public partial class CollectorHydraulicSummaryCard : ObservableObject
    {
        /// <summary>
        /// Номер коллектора
        /// </summary>
        [ObservableProperty]
        private int _collectorNumber;

        /// <summary>
        /// Отображаемый тип коллектора с количеством контуров
        /// </summary>
        [ObservableProperty]
        private string _collectorTypeDisplay = string.Empty;

        /// <summary>
        /// Количество контуров
        /// </summary>
        [ObservableProperty]
        private int _circuitCount;

        /// <summary>
        /// Общая длина труб, м
        /// </summary>
        [ObservableProperty]
        private double _totalPipeLength;

        /// <summary>
        /// Общая мощность, Вт
        /// </summary>
        [ObservableProperty]
        private double _totalPower;

        /// <summary>
        /// Общий расход, л/ч
        /// </summary>
        [ObservableProperty]
        private double _totalFlowRate;

        /// <summary>
        /// Потери давления при рабочей температуре, Па
        /// </summary>
        [ObservableProperty]
        private double _operatingPressureLossPa;

        /// <summary>
        /// Псевдоним для совместимости с reflection-тестами и CollectorSummary
        /// </summary>
        public double PressureLoss_Operating_Pa => OperatingPressureLossPa;

        /// <summary>
        /// Потери давления при расчётной (холодной) температуре, Па
        /// </summary>
        [ObservableProperty]
        private double _coldPressureLossPa;

        /// <summary>
        /// Псевдоним для совместимости с reflection-тестами и CollectorSummary
        /// </summary>
        public double PressureLoss_Cold_Pa => ColdPressureLossPa;

        /// <summary>
        /// Kv коллектора (коэффициент пропускной способности), м³/ч
        /// </summary>
        [ObservableProperty]
        private double _kv;

        public CollectorHydraulicSummaryCard(CollectorData collector)
        {
            if (collector == null) return;

            CollectorNumber = collector.CollectorNumber;
            CollectorTypeDisplay = collector.CollectorTypeDisplayWithCount;

            var summary = collector.Summary;
            if (summary != null)
            {
                CircuitCount = summary.CircuitCount;
                TotalPipeLength = summary.TotalPipeLength;
                TotalPower = summary.TotalPower;
                TotalFlowRate = summary.TotalFlowRate;
                OperatingPressureLossPa = summary.PressureLoss_Operating_Pa;
                ColdPressureLossPa = summary.PressureLoss_Cold_Pa;
                Kv = summary.Kv;
            }
        }
    }
}
