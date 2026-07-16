using System.ComponentModel;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Интерфейс сервиса состояния проекта
    /// </summary>
    public interface IProjectStateService : IProjectInfoService, INotifyPropertyChanged
    {
        /// <summary>
        /// Текущий путь к файлу проекта
        /// </summary>
        string? CurrentFilePath { get; set; }

        /// <summary>
        /// Признак наличия несохранённых изменений
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Пометить проект как содержащий несохранённые изменения
        /// </summary>
        void MarkDirty();

        /// <summary>
        /// Пометить проект как сохранённый
        /// </summary>
        void MarkClean();
    }
}
