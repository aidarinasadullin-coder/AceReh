using System.Windows.Controls;
using System.Windows.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;

namespace SnowMeltingCalculator.Views.Hydraulics
{
    /// <summary>
    /// Представление для таблицы контуров гидравлического расчёта
    /// </summary>
    public partial class CircuitsView : UserControl
    {
        public CircuitsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик клика по табло "Рабочая температура"
        /// </summary>
        private void OnOperatingModeClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CircuitsViewModel vm)
            {
                vm.CurrentMode = HydraulicMode.OperatingTemperature;
            }
        }

        /// <summary>
        /// Обработчик клика по табло "Расчётная температура"
        /// </summary>
        private void OnDesignModeClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CircuitsViewModel vm)
            {
                vm.CurrentMode = HydraulicMode.DesignTemperature;
            }
        }
    }
}