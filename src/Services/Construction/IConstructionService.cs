using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Интерфейс сервиса для работы с конструкциями
    /// </summary>
    public interface IConstructionService
    {
        /// <summary>
        /// Рассчитать термические сопротивления для всех слоёв конструкции
        /// </summary>
        /// <param name="construction">Конструкция</param>
        void CalculateThermalResistances(ConstructionModel construction);

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление слоёв над трубой (R1)
        /// </summary>
        /// <param name="layersAbovePipe">Слои над трубой</param>
        /// <returns>R1, м²·К/Вт</returns>
        double CalculateR1(IEnumerable<Layer> layersAbovePipe);

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление слоёв под трубой (R2)
        /// </summary>
        /// <param name="layersBelowPipe">Слои под трубой</param>
        /// <param name="groundwaterLevel">Уровень грунтовых вод, м</param>
        /// <returns>R2, м²·К/Вт</returns>
        double CalculateR2(IEnumerable<Layer> layersBelowPipe, double groundwaterLevel);

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        /// <param name="construction">Конструкция для валидации</param>
        /// <returns>Результат валидации</returns>
        ValidationResult ValidateConstruction(ConstructionModel construction);

        /// <summary>
        /// Создать конструкцию из шаблона
        /// </summary>
        /// <param name="template">Шаблон конструкции</param>
        /// <param name="materials">Список материалов</param>
        /// <returns>Созданная конструкция</returns>
        ConstructionModel CreateFromTemplate(ConstructionTemplate template, IEnumerable<Material> materials);

        /// <summary>
        /// Получить общую толщину слоёв над трубой
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <returns>Толщина, мм</returns>
        double GetTotalThicknessAbovePipe(ConstructionModel construction);

        /// <summary>
        /// Получить общую толщину слоёв под трубой
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <returns>Толщина, мм</returns>
        double GetTotalThicknessBelowPipe(ConstructionModel construction);
    }
}