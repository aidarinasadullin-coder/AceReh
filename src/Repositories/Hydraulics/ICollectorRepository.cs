using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Repositories.Hydraulics
{
    /// <summary>
    /// Интерфейс репозитория коллекторов РЕХАУ
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для работы с данными о коллекторах:
    /// - Получение списка коллекторов
    /// - Поиск по идентификатору
    /// - Фильтрация по типу
    /// - Подбор по количеству контуров
    /// 
    /// Данные загружаются из data/rehau_products.json
    /// 
    /// Поддерживаемые коллекторы:
    /// - HKV-D (бытовой): 2, 4, 6, 8, 10, 12 контуров
    /// - IV (промышленный): DN25 (1¼"), DN40 (1½")
    /// </remarks>
    public interface ICollectorRepository
    {
        /// <summary>
        /// Получить все коллекторы
        /// </summary>
        /// <returns>Список всех коллекторов</returns>
        /// <remarks>
        /// Загружает данные из data/rehau_products.json
        /// </remarks>
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Collector>> GetAllAsync();

        /// <summary>
        /// Получить коллектор по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор коллектора</param>
        /// <returns>Коллектор или null, если не найден</returns>
        /// <remarks>
        /// Идентификаторы:
        /// - "HKV-D-2", "HKV-D-4", ..., "HKV-D-12"
        /// - "IV-1.25", "IV-1.5"
        /// </remarks>
        System.Threading.Tasks.Task<Collector?> GetByIdAsync(string id);

        /// <summary>
        /// Получить коллекторы по типу
        /// </summary>
        /// <param name="type">Тип коллектора (HKV или IV)</param>
        /// <returns>Список коллекторов указанного типа</returns>
        /// <remarks>
        /// Фильтрация по CollectorType:
        /// - CollectorType.HKV — бытовые коллекторы
        /// - CollectorType.IV — промышленные коллекторы
        /// </remarks>
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Collector>> GetByTypeAsync(CollectorType type);

        /// <summary>
        /// Получить коллектор по количеству контуров
        /// </summary>
        /// <param name="circuits">Количество контуров</param>
        /// <returns>Коллектор или null, если не найден</returns>
        /// <remarks>
        /// Для HKV-D:
        /// - 2 контура → HKV-D-2
        /// - 4 контура → HKV-D-4
        /// - и т.д.
        /// 
        /// Для IV: возвращает первый доступный промышленный коллектор
        /// </remarks>
        System.Threading.Tasks.Task<Collector?> GetByCircuitsAsync(int circuits);

        /// <summary>
        /// Подобрать коллектор для заданного количества контуров и расхода
        /// </summary>
        /// <param name="circuits">Количество контуров</param>
        /// <param name="totalFlowRate_m3_h">Суммарный расход, м³/ч</param>
        /// <returns>Рекомендуемый коллектор или null, если не найден</returns>
        /// <remarks>
        /// Алгоритм подбора:
        /// 1. Если circuits ≤ 12: подобрать HKV-D
        /// 2. Проверить ограничение по расходу (≤ MaxFlowRate)
        /// 3. Если не подходит: предложить IV
        /// 
        /// Ограничения:
        /// - HKV-D: макс. 12 контуров, макс. 1.5 м³/ч, макс. 320 мбар
        /// </remarks>
        Collector? SelectCollector(int circuits, double totalFlowRate_m3_h);

        /// <summary>
        /// Получить список доступных количеств контуров для HKV-D
        /// </summary>
        /// <returns>Список количеств контуров: 2, 4, 6, 8, 10, 12</returns>
        System.Collections.Generic.IEnumerable<int> GetAvailableCircuitCounts();

        /// <summary>
        /// Проверить, подходит ли коллектор для заданных параметров
        /// </summary>
        /// <param name="collector">Коллектор</param>
        /// <param name="circuits">Количество контуров</param>
        /// <param name="totalFlowRate_m3_h">Суммарный расход, м³/ч</param>
        /// <param name="pressure_mbar">Давление, мбар</param>
        /// <returns>true, если коллектор подходит</returns>
        /// <remarks>
        /// Проверка ограничений:
        /// - Количество контуров ≤ Circuits
        /// - Расход ≤ MaxFlowRate
        /// - Давление ≤ MaxPressure
        /// </remarks>
        bool IsCollectorSuitable(
            Collector collector,
            int circuits,
            double totalFlowRate_m3_h,
            double pressure_mbar);

        /// <summary>
        /// Получить максимальное количество контуров для HKV-D
        /// </summary>
        /// <returns>Максимальное количество контуров (12)</returns>
        int GetMaxCircuitsForHKV();

        /// <summary>
        /// Получить максимальный расход для HKV-D
        /// </summary>
        /// <returns>Максимальный расход, м³/ч (1.5)</returns>
        double GetMaxFlowRateForHKV();

        /// <summary>
        /// Получить максимальное давление для HKV-D
        /// </summary>
        /// <returns>Максимальное давление, мбар (320)</returns>
        double GetMaxPressureForHKV();
    }
}