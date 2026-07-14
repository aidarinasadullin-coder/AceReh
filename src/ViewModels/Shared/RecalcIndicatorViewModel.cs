// ================================================================================
// REHAU Снеготаяние - ViewModel: Индикатор пересчёта
// ================================================================================
//
// Назначение: ViewModel для управления состоянием индикатора пересчёта
//
// Соответствует: design_guidelines.md
// - RecalcState: Info/Warning/Processing/Success
// - Warning: жёлтый фон (#FFF8E8), оранжевая рамка (#FFB300)
// - Processing: синий фон (#E3F2FD), синяя рамка (#2196F3)
//
// ================================================================================

using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Enums;

namespace SnowMeltingCalculator.ViewModels.Shared
{
    /// <summary>
    /// ViewModel для индикатора пересчёта
    /// </summary>
    public partial class RecalcIndicatorViewModel : ObservableObject
    {
        #region Observable Properties

        /// <summary>
        /// Текущее состояние индикатора
        /// </summary>
        [ObservableProperty]
        private RecalcState _state = RecalcState.Info;

        /// <summary>
        /// Показывать ли индикатор
        /// </summary>
        [ObservableProperty]
        private bool _isVisible = true;

        /// <summary>
        /// Показывать ли кнопку пересчёта
        /// </summary>
        [ObservableProperty]
        private bool _showRecalculateButton = false;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Текст сообщения в зависимости от состояния
        /// </summary>
        public string Message => State switch
        {
            RecalcState.Info => "Данные актуальны",
            RecalcState.Warning => "Изменены параметры. Требуется пересчёт.",
            RecalcState.Processing => "Выполняется пересчёт...",
            RecalcState.Success => "Пересчёт завершён",
            _ => ""
        };

        /// <summary>
        /// Путь к иконке в зависимости от состояния
        /// </summary>
        public string IconPath => State switch
        {
            RecalcState.Info => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z",
            RecalcState.Warning => "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z",
            RecalcState.Processing => "M12 6v3l4-4-4-4v3c-4.42 0-8 3.58-8 8 0 1.57.46 3.03 1.24 4.26L6.7 14.8c-.45-.83-.7-1.79-.7-2.8 0-3.31 2.69-6 6-6zm6.76 1.74L17.3 9.2c.44.84.7 1.79.7 2.8 0 3.31-2.69 6-6 6v-3l-4 4 4 4v-3c4.42 0 8-3.58 8-8 0-1.57-.46-3.03-1.24-4.26z",
            RecalcState.Success => "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z",
            _ => ""
        };

        #endregion

        #region Commands

        /// <summary>
        /// Команда пересчёта
        /// </summary>
        public IRelayCommand RecalculateCommand { get; }

        #endregion

        #region Constructor

        public RecalcIndicatorViewModel()
        {
            RecalculateCommand = new RelayCommand(OnRecalculate, CanRecalculate);
        }

        #endregion

        #region Private Methods

        private bool CanRecalculate() => State == RecalcState.Warning;

        private void OnRecalculate()
        {
            // Переход в состояние обработки
            State = RecalcState.Processing;
            ShowRecalculateButton = false;
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(IconPath));

            // Здесь должен быть вызов события или делегата для начала пересчёта
            // После завершения пересчёта вызвать MarkAsCalculated()
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Отметить данные как требующие пересчёта (Warning - жёлтый)
        /// </summary>
        public void MarkAsNeedsRecalculation()
        {
            State = RecalcState.Warning;
            ShowRecalculateButton = true;
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(IconPath));
        }

        /// <summary>
        /// Отметить данные как актуальные (Info - серый)
        /// </summary>
        public void MarkAsValid()
        {
            State = RecalcState.Info;
            ShowRecalculateButton = false;
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(IconPath));
        }

        /// <summary>
        /// Отметить пересчёт как завершённый (Success - зелёный)
        /// </summary>
        public void MarkAsCalculated()
        {
            State = RecalcState.Success;
            ShowRecalculateButton = false;
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(IconPath));

            // Автоматически скрыть через 3 секунды
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                // Вернуться в состояние Info
                State = RecalcState.Info;
                OnPropertyChanged(nameof(Message));
                OnPropertyChanged(nameof(IconPath));
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Начать процесс пересчёта (Processing - синий)
        /// </summary>
        public void StartProcessing()
        {
            State = RecalcState.Processing;
            ShowRecalculateButton = false;
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(IconPath));
        }

        #endregion
    }
}
