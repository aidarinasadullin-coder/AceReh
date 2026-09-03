using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Интерфейс валидатора контуров и коллекторов
    /// </summary>
    public interface ICircuitsValidator
    {
        /// <summary>
        /// Проверить возможность удаления контура
        /// </summary>
        /// <param name="circuit">Контур для удаления</param>
        /// <param name="collector">Коллектор, содержащий контур</param>
        /// <returns>true — можно удалить, false — нельзя</returns>
        bool CanRemoveCircuit(CircuitRow? circuit, CollectorData? collector);

        /// <summary>
        /// Проверить возможность удаления коллектора
        /// </summary>
        /// <param name="collector">Коллектор для удаления</param>
        /// <param name="collectorsCount">Общее количество коллекторов</param>
        /// <returns>true — можно удалить, false — нельзя</returns>
        bool CanRemoveCollector(CollectorData? collector, int collectorsCount);

        /// <summary>
        /// Подтвердить удаление контура
        /// </summary>
        /// <param name="circuitNumber">Номер контура</param>
        /// <returns>true — удалить, false — отменить</returns>
        bool ConfirmDeleteCircuit(int circuitNumber);

        /// <summary>
        /// Подтвердить удаление коллектора
        /// </summary>
        /// <param name="collectorNumber">Номер коллектора</param>
        /// <returns>true — удалить, false — отменить</returns>
        bool ConfirmDeleteCollector(int collectorNumber);
    }
}