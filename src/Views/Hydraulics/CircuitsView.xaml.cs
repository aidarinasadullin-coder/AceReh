using System.Windows.Controls;

namespace SnowMeltingCalculator.Views.Hydraulics
{
    /// <summary>
    /// Представление гидравлического расчёта: контуры, коллекторы, потери давления.
    /// </summary>
    /// <remarks>
    /// Фаза 3 редизайна: переключение режима «Рабочая/Расчётная» и режима
    /// таблицы «Компактно/Полностью» — сегмент-контролы, биндящие
    /// CircuitsViewModel (IsOperatingMode/IsDesignMode/IsCompactView/
    /// IsFullView); code-behind-обработчики табло удалены (ADR-007 п.2).
    /// </remarks>
    public partial class CircuitsView : UserControl
    {
        public CircuitsView()
        {
            InitializeComponent();
        }
    }
}
