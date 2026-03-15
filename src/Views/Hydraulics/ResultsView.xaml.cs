using System.Windows.Controls;

namespace SnowMeltingCalculator.Views.Hydraulics
{
    /// <summary>
    /// Представление для отображения результатов расчёта
    /// </summary>
    /// <remarks>
    /// Отображает результаты гидравлического расчёта:
    /// - Параметры потока (скорость, число Рейнольдса, режим течения)
    /// - Потери давления (удельные, в контуре, в подводке, в вентиле)
    /// - Предупреждения и ошибки
    /// - Информацию о рекомендуемом коллекторе
    /// 
    /// DataBinding:
    /// - Result - результат расчёта (HydraulicResult)
    /// - Warnings - список предупреждений
    /// - HasWarnings - признак наличия предупреждений
    /// - HasErrors - признак наличия ошибок
    /// - ErrorMessage - сообщение об ошибке
    /// - SelectedCollector - выбранный коллектор
    /// - TotalPressureLossKPa - общие потери в кПа
    /// - TotalPressureLossMbar - общие потери в мбар
    /// </remarks>
    public partial class ResultsView : UserControl
    {
        public ResultsView()
        {
            InitializeComponent();
        }
    }
}