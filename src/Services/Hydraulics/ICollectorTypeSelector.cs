using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Интерфейс для автоматического подбора типа коллектора
    /// </summary>
    public interface ICollectorTypeSelector
    {
        /// <summary>
        /// Автоматически подобрать тип коллектора по расходу
        /// </summary>
        /// <param name="collector">Данные коллектора</param>
        /// <returns>Результат подбора с предупреждениями</returns>
        CollectorSelectionResult SelectCollectorType(CollectorData collector);
    }

    /// <summary>
    /// Результат подбора типа коллектора
    /// </summary>
    public class CollectorSelectionResult
    {
        /// <summary>
        /// Тип коллектора (строка для отображения)
        /// </summary>
        public string CollectorType { get; set; } = "HKV-D (2-12 контуров)";

        /// <summary>
        /// Тип клапана
        /// </summary>
        public ValveType ValveType { get; set; } = ValveType.HKV_D;

        /// <summary>
        /// Предупреждение (если есть)
        /// </summary>
        public string? Warning { get; set; }
    }
}