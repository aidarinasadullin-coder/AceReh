using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// ViewModel для выбора и отображения коллектора РЕХАУ
    /// </summary>
    /// <remarks>
    /// Предоставляет функционал для:
    /// - Загрузки списка коллекторов
    /// - Подбора коллектора по параметрам
    /// - Фильтрации по типу
    /// - Отображения информации о коллекторе
    /// </remarks>
    public partial class CollectorViewModel : ObservableObject
    {
        #region Services

        private readonly ICollectorRepository _collectorRepository;

        #endregion

        #region Observable Properties

        /// <summary>
        /// Список доступных коллекторов
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Collector> _availableCollectors = new ObservableCollection<Collector>();

        /// <summary>
        /// Выбранный коллектор
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorInfo))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorName))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorDiameter))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorKv))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorMaxFlow))]
        [NotifyPropertyChangedFor(nameof(SelectedCollectorMaxPressure))]
        [NotifyPropertyChangedFor(nameof(CanShowDetails))]
        private Collector? _selectedCollector;

        /// <summary>
        /// Тип коллектора для фильтрации
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredCollectors))]
        private CollectorType _selectedCollectorType = CollectorType.HKV;

        /// <summary>
        /// Количество контуров для подбора
        /// </summary>
        [ObservableProperty]
        private int _circuitCount = 4;

        /// <summary>
        /// Общий расход (л/ч)
        /// </summary>
        [ObservableProperty]
        private double _totalFlowRate;

        /// <summary>
        /// Признак загрузки
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Отфильтрованный список коллекторов по типу
        /// </summary>
        public ObservableCollection<Collector> FilteredCollectors
        {
            get
            {
                var filtered = AvailableCollectors
                    .Where(c => c.Type == SelectedCollectorType)
                    .ToList();

                return new ObservableCollection<Collector>(filtered);
            }
        }

        /// <summary>
        /// Признак возможности отображения деталей
        /// </summary>
        public bool CanShowDetails => SelectedCollector != null;

        /// <summary>
        /// Информация о выбранном коллекторе
        /// </summary>
        public string SelectedCollectorInfo => SelectedCollector?.GetDescription() ?? "Коллектор не выбран";

        /// <summary>
        /// Название выбранного коллектора
        /// </summary>
        public string SelectedCollectorName => SelectedCollector?.Name ?? "—";

        /// <summary>
        /// Диаметр выбранного коллектора
        /// </summary>
        public string SelectedCollectorDiameter => SelectedCollector?.ConnectionSize ?? "—";

        /// <summary>
        /// Kv выбранного коллектора
        /// </summary>
        public string SelectedCollectorKv => SelectedCollector != null
            ? $"{SelectedCollector.Kv:F2} м³/ч"
            : "—";

        /// <summary>
        /// Максимальный расход выбранного коллектора
        /// </summary>
        public string SelectedCollectorMaxFlow => SelectedCollector != null
            ? $"{SelectedCollector.MaxFlowRate_L_h:F0} л/ч"
            : "—";

        /// <summary>
        /// Максимальное давление выбранного коллектора
        /// </summary>
        public string SelectedCollectorMaxPressure => SelectedCollector != null
            ? $"{SelectedCollector.MaxPressure:F0} мбар"
            : "—";

        /// <summary>
        /// Доступные количества контуров для HKV
        /// </summary>
        public int[] AvailableCircuitCountsHKV => new[] { 2, 4, 6, 8, 10, 12 };

        /// <summary>
        /// Доступные типы коллекторов
        /// </summary>
        public CollectorType[] AvailableCollectorTypes => new[] { CollectorType.HKV, CollectorType.IV };

        #endregion

        #region Commands

        /// <summary>
        /// Команда загрузки коллекторов
        /// </summary>
        [RelayCommand]
        private async Task LoadCollectorsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var collectors = await _collectorRepository.GetAllAsync();

                AvailableCollectors.Clear();
                foreach (var collector in collectors)
                {
                    AvailableCollectors.Add(collector);
                }

                OnPropertyChanged(nameof(FilteredCollectors));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки коллекторов: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Команда подбора коллектора
        /// </summary>
        [RelayCommand]
        private async Task SelectCollectorAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Преобразование л/ч в м³/ч
                double totalFlowRate_m3_h = TotalFlowRate / 1000.0;

                var collector = await _collectorRepository.SelectCollectorAsync(
                    CircuitCount,
                    totalFlowRate_m3_h);

                if (collector != null)
                {
                    SelectedCollector = collector;
                }
                else
                {
                    ErrorMessage = "Не найден подходящий коллектор для заданных параметров";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка подбора коллектора: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Команда выбора коллектора из списка
        /// </summary>
        [RelayCommand]
        private void SelectCollectorFromList(Collector collector)
        {
            if (collector != null)
            {
                SelectedCollector = collector;
            }
        }

        /// <summary>
        /// Команда сброса выбора
        /// </summary>
        [RelayCommand]
        private void ClearSelection()
        {
            SelectedCollector = null;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Команда фильтрации по типу
        /// </summary>
        [RelayCommand]
        private void FilterByType(CollectorType type)
        {
            SelectedCollectorType = type;
            OnPropertyChanged(nameof(FilteredCollectors));
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор для дизайнера
        /// </summary>
        public CollectorViewModel() : this(null)
        {
        }

        /// <summary>
        /// Основной конструктор
        /// </summary>
        /// <param name="collectorRepository">Репозиторий коллекторов</param>
        public CollectorViewModel(ICollectorRepository? collectorRepository)
        {
            _collectorRepository = collectorRepository ?? new CollectorRepository();

            // Загрузка коллекторов при создании
            _ = LoadCollectorsAsync();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Установить параметры для подбора
        /// </summary>
        /// <param name="circuitCount">Количество контуров</param>
        /// <param name="totalFlowRate">Общий расход, л/ч</param>
        public void SetSelectionParameters(int circuitCount, double totalFlowRate)
        {
            CircuitCount = circuitCount;
            TotalFlowRate = totalFlowRate;
        }

        /// <summary>
        /// Получить выбранный коллектор
        /// </summary>
        /// <returns>Выбранный коллектор или null</returns>
        public Collector? GetSelectedCollector()
        {
            return SelectedCollector;
        }

        /// <summary>
        /// Проверить совместимость коллектора с параметрами
        /// </summary>
        /// <param name="collector">Коллектор</param>
        /// <param name="circuitCount">Количество контуров</param>
        /// <param name="totalFlowRate">Общий расход, л/ч</param>
        /// <returns>true, если коллектор совместим</returns>
        public bool IsCollectorCompatible(Collector collector, int circuitCount, double totalFlowRate)
        {
            if (collector == null)
                return false;

            // Проверка количества контуров
            if (collector.Circuits < circuitCount)
                return false;

            // Проверка расхода
            if (collector.MaxFlowRate_L_h < totalFlowRate)
                return false;

            return true;
        }

        /// <summary>
        /// Получить рекомендацию по коллектору
        /// </summary>
        /// <returns>Текст рекомендации</returns>
        public string GetRecommendation()
        {
            if (SelectedCollector == null)
                return "Выполните подбор коллектора";

            if (TotalFlowRate > SelectedCollector.MaxFlowRate_L_h)
                return $"Внимание: расход ({TotalFlowRate:F0} л/ч) превышает максимальный для коллектора ({SelectedCollector.MaxFlowRate_L_h:F0} л/ч)";

            if (CircuitCount > SelectedCollector.Circuits)
                return $"Внимание: количество контуров ({CircuitCount}) превышает количество выходов коллектора ({SelectedCollector.Circuits})";

            double utilizationRate = TotalFlowRate / SelectedCollector.MaxFlowRate_L_h * 100;

            if (utilizationRate < 30)
                return $"Рекомендация: загрузка коллектора {utilizationRate:F0}% — рассмотрите коллектор меньшего размера";

            if (utilizationRate > 80)
                return $"Рекомендация: загрузка коллектора {utilizationRate:F0}% — рассмотрите коллектор большего размера";

            return $"Коллектор подобран корректно. Загрузка: {utilizationRate:F0}%";
        }

        #endregion

        #region PropertyChanged Handlers

        /// <summary>
        /// Обработчик изменения типа коллектора
        /// </summary>
        partial void OnSelectedCollectorTypeChanged(CollectorType value)
        {
            OnPropertyChanged(nameof(FilteredCollectors));
        }

        #endregion
    }
}