using System;
using System.ComponentModel;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Aggregate root of the current project. Owns only lifecycle state:
    /// identity, current file path, dirty flag, and the restore-in-progress guard.
    /// </summary>
    public interface IProjectSession : INotifyPropertyChanged
    {
        /// <summary>
        /// Номер проекта.
        /// </summary>
        string ProjectNumber { get; set; }

        /// <summary>
        /// Наименование объекта.
        /// </summary>
        string ProjectObject { get; set; }

        /// <summary>
        /// Текущий путь к файлу проекта. <c>null</c> означает новый/несохранённый проект.
        /// </summary>
        string? CurrentFilePath { get; set; }

        /// <summary>
        /// Признак наличия несохранённых изменений.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Признак выполнения загрузки/восстановления проекта.
        /// </summary>
        bool IsLoadProjectInProgress { get; }

        /// <summary>
        /// Пометить проект как содержащий несохранённые изменения.
        /// </summary>
        void MarkDirty();

        /// <summary>
        /// Пометить проект как сохранённый.
        /// </summary>
        void MarkClean();

        /// <summary>
        /// Начать операцию восстановления проекта. Возвращает lease, который при dispose
        /// уменьшает глубину вложенности; guard сбрасывается в false только при выходе
        /// из самого внешнего scope.
        /// </summary>
        IDisposable BeginProjectRestore();
    }
}
