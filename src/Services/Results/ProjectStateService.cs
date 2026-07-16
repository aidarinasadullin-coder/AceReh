using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Сервис состояния проекта
    /// </summary>
    public class ProjectStateService : IProjectStateService, IMarkDirtyService, INotifyPropertyChanged
    {
        private string? _currentFilePath;
        private bool _isDirty;

        /// <summary>
        /// Номер проекта
        /// </summary>
        public string ProjectNumber { get; set; } = string.Empty;

        /// <summary>
        /// Наименование объекта
        /// </summary>
        public string ProjectObject { get; set; } = string.Empty;

        /// <summary>
        /// Текущий путь к файлу проекта
        /// </summary>
        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                if (_currentFilePath == value)
                {
                    return;
                }

                _currentFilePath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Признак наличия несохранённых изменений
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Пометить проект как содержащий несохранённые изменения
        /// </summary>
        public void MarkDirty()
        {
            if (_isDirty)
            {
                return;
            }

            _isDirty = true;
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <summary>
        /// Пометить проект как сохранённый
        /// </summary>
        public void MarkClean()
        {
            if (!_isDirty)
            {
                return;
            }

            _isDirty = false;
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <summary>
        /// Событие изменения свойства
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Вызвать событие изменения свойства
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
