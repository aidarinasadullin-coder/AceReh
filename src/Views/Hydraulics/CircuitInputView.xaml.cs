using System.Windows.Controls;

namespace SnowMeltingCalculator.Views.Hydraulics
{
    /// <summary>
    /// Представление для ввода параметров контура
    /// </summary>
    /// <remarks>
    /// Используется в HydraulicsView для отображения и редактирования
    /// параметров отдельного контура системы снеготаяния.
    /// 
    /// DataBinding:
    /// - CircuitNumber - номер контура
    /// - CircuitName - название контура
    /// - Length - длина контура (м)
    /// - SupplyLength - длина подводки (м)
    /// - Area - площадь контура (м²)
    /// - FlowRate - расход (л/ч)
    /// - PressureLossKPa - потери давления (кПа)
    /// - Velocity - скорость потока (м/с)
    /// - FlowRegime - режим течения
    /// - ThrottlingMbar - дросселирование (мбар)
    /// - ValveSetting - настройка вентиля
    /// - IsReferenceCircuit - признак опорного контура
    /// - Status - статус контура
    /// </remarks>
    public partial class CircuitInputView : UserControl
    {
        public CircuitInputView()
        {
            InitializeComponent();
        }
    }
}