using System.Windows;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Валидатор контуров и коллекторов
    /// </summary>
    /// <remarks>
    /// Содержит логику проверки возможности удаления
    /// и подтверждения удаления через диалоговые окна.
    /// </remarks>
    public class CircuitsValidator : ICircuitsValidator
    {
        /// <summary>
        /// Проверить возможность удаления контура
        /// </summary>
        /// <remarks>
        /// Нельзя удалить, если:
        /// 1. Контур не выбран (circuit == null)
        /// 2. Коллектор не выбран (collector == null)
        /// 3. В коллекторе только 1 контур (минимум 1 контур должен остаться)
        /// </remarks>
        public bool CanRemoveCircuit(CircuitRow? circuit, CollectorData? collector)
        {
            if (circuit == null)
                return false;

            if (collector == null)
                return false;

            return collector.Circuits.Count > 1;
        }

        /// <summary>
        /// Проверить возможность удаления коллектора
        /// </summary>
        /// <remarks>
        /// Нельзя удалить, если:
        /// 1. Коллектор не выбран (collector == null)
        /// 2. В системе только 1 коллектор (минимум 1 коллектор должен остаться)
        /// </remarks>
        public bool CanRemoveCollector(CollectorData? collector, int collectorsCount)
        {
            if (collector == null)
                return false;

            return collectorsCount > 1;
        }

        /// <summary>
        /// Подтвердить удаление контура через диалоговое окно
        /// </summary>
        public bool ConfirmDeleteCircuit(int circuitNumber)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить контур №{circuitNumber}?",
                "Удаление контура",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Подтвердить удаление коллектора через диалоговое окно
        /// </summary>
        public bool ConfirmDeleteCollector(int collectorNumber)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить коллектор №{collectorNumber}?\nВсе контуры этого коллектора будут удалены.",
                "Удаление коллектора",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            return result == MessageBoxResult.Yes;
        }
    }
}