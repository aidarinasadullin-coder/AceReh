using System.Windows.Controls;

namespace SnowMeltingCalculator.Views.Hydraulics
{
    /// <summary>
    /// Представление для отображения итогов коллектора в CircuitsViewModel
    /// </summary>
    /// <remarks>
    /// Отображает:
    /// - Параметры коллектора (номер, тип вентиля, количество контуров)
    /// - Общие параметры (общая длина труб, мощность, расход)
    /// - Потери давления (при рабочей и расчётной температуре)
    /// - Предупреждения и статус расчёта
    /// 
    /// DataBinding:
    /// - SelectedCollector.Summary - итоги коллектора (CollectorSummary)
    /// - SelectedCollector.CollectorNumber - номер коллектора
    /// - SelectedCollector.ValveType - тип вентиля
    /// </remarks>
    public partial class CircuitsResultsView : UserControl
    {
        public CircuitsResultsView()
        {
            InitializeComponent();
        }
    }
}