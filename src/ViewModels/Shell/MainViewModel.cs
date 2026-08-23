using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.ViewModels.Shell
{
    /// <summary>
    /// ViewModel для главного окна с навигацией
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly ClimateViewModel _climateViewModel;
        private readonly ThermalViewModel _thermalViewModel;
        private readonly ConstructionViewModel _constructionViewModel;
        private readonly CircuitsViewModel _circuitsViewModel;
        private readonly ResultsViewModel _resultsViewModel;
        private readonly ICalculationStateService _calculationStateService;
        private readonly IProjectStateService _projectStateService;
        private readonly IDialogService _dialogService;
        private readonly CalculationContext _calculationContext;
        private readonly IProjectSessionClimateState _climateState;
        private readonly IProjectSessionConstructionState _constructionState;
        private readonly IProjectSessionThermalState _thermalState;
        private readonly ConstructionDefaultStateInitializer _constructionDefaultStateInitializer;

        public ResultsViewModel ResultsViewModel => _resultsViewModel;
        public ClimateViewModel ClimateViewModel => _climateViewModel;
        public ThermalViewModel ThermalViewModel => _thermalViewModel;
        public ConstructionViewModel ConstructionViewModel => _constructionViewModel;
        public CircuitsViewModel CircuitsViewModel => _circuitsViewModel;

        public MainViewModel(
            ClimateViewModel climateViewModel,
            ThermalViewModel thermalViewModel,
            ConstructionViewModel constructionViewModel,
            CircuitsViewModel circuitsViewModel,
            ResultsViewModel resultsViewModel,
            ICalculationStateService calculationStateService,
            IProjectStateService projectStateService,
            IDialogService dialogService,
            CalculationContext calculationContext,
            IProjectSession? projectSession = null,
            ConstructionDefaultStateInitializer? constructionDefaultStateInitializer = null)
        {
            _climateViewModel = climateViewModel;
            _thermalViewModel = thermalViewModel;
            _constructionViewModel = constructionViewModel;
            _circuitsViewModel = circuitsViewModel;
            _resultsViewModel = resultsViewModel;
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _projectStateService = projectStateService ?? throw new ArgumentNullException(nameof(projectStateService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            var session = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _climateState = session.ClimateState;
            _constructionState = session.ConstructionState;
            _thermalState = session.ThermalState;
            _constructionDefaultStateInitializer = constructionDefaultStateInitializer
                ?? throw new ArgumentNullException(nameof(constructionDefaultStateInitializer));

            // Подписка на изменения состояния
            _calculationStateService.StateChanged += OnCalculationStateChanged;

            // Подписка на изменения состояния проекта для обновления заголовка окна
            _projectStateService.PropertyChanged += OnProjectStateChanged;

            _selectedMenuItem = MenuItems[0];

            // Загрузка состояния боковой панели из настроек
            _isSidebarCollapsed = AppSettings.Instance.IsSidebarCollapsed;

            // Инициализация заголовка окна
            UpdateWindowTitle();
        }

        public MenuItem[] MenuItems { get; } = new[]
        {
            new MenuItem { Title = "Климат", Icon = "WeatherCloudy", Target = NavigationTarget.Climate },
            new MenuItem { Title = "Конструкция", Icon = "Layers", Target = NavigationTarget.Construction },
            new MenuItem { Title = "Тепловой расчёт", Icon = "Fire", Target = NavigationTarget.Thermal },
            new MenuItem { Title = "Гидравлический расчёт", Icon = "Pipe", Target = NavigationTarget.Hydraulics },
            new MenuItem { Title = "Результаты", Icon = "ChartBar", Target = NavigationTarget.Results }
        };

        private NavigationTarget _currentNavigationTarget = NavigationTarget.Climate;
        public NavigationTarget CurrentNavigationTarget
        {
            get => _currentNavigationTarget;
            private set => SetProperty(ref _currentNavigationTarget, value);
        }

        private MenuItem? _selectedMenuItem;
        public MenuItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (SetProperty(ref _selectedMenuItem, value) && value != null)
                {
                    CurrentNavigationTarget = value.Target;
                    UpdateCurrentTitle();
                }
            }
        }

        private bool _isSidebarCollapsed;
        /// <summary>
        /// Признак свёрнутой боковой панели
        /// </summary>
        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                if (SetProperty(ref _isSidebarCollapsed, value))
                {
                    // Сохраняем состояние в настройках
                    AppSettings.Instance.IsSidebarCollapsed = value;
                    AppSettings.Instance.Save();

                    // Уведомляем об изменении для триггеров в XAML
                    OnPropertyChanged(nameof(IsSidebarExpanded));
                }
            }
        }

        /// <summary>
        /// Признак развёрнутой боковой панели (для удобства в XAML)
        /// </summary>
        public bool IsSidebarExpanded => !IsSidebarCollapsed;

        /// <summary>
        /// Команда переключения состояния боковой панели
        /// </summary>
        [RelayCommand]
        private void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        private string _windowTitle = "Калькулятор снеготаяния REHAU";
        /// <summary>
        /// Заголовок главного окна с учётом несохранённых изменений
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            private set => SetProperty(ref _windowTitle, value);
        }

        private void UpdateWindowTitle()
        {
            var prefix = _projectStateService.IsDirty ? "*" : string.Empty;
            var fileName = !string.IsNullOrEmpty(_projectStateService.CurrentFilePath)
                ? System.IO.Path.GetFileName(_projectStateService.CurrentFilePath)
                : null;

            WindowTitle = (prefix, fileName) switch
            {
                ("*", null) => "*Новый расчёт — Калькулятор снеготаяния REHAU",
                ("*", _) => $"*{fileName} — Калькулятор снеготаяния REHAU",
                ("", _) when fileName != null => $"{fileName} — Калькулятор снеготаяния REHAU",
                _ => "Калькулятор снеготаяния REHAU"
            };
        }

        private void OnProjectStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IProjectStateService.IsDirty) ||
                e.PropertyName == nameof(IProjectStateService.CurrentFilePath))
            {
                UpdateWindowTitle();
            }
        }

        /// <summary>
        /// Команда создания нового расчёта
        /// </summary>
        [RelayCommand]
        private async Task NewCalculation()
        {
            if (!_projectStateService.IsDirty)
            {
                PerformNewCalculationReset();
                return;
            }

            var result = _dialogService.Show(
                "Текущий проект имеет несохранённые изменения. Сохранить перед созданием нового расчёта?",
                "Создать новый расчёт",
                DialogButtons.YesNoCancel,
                DialogIcon.Question);

            switch (result)
            {
                case SnowMeltingCalculator.Services.Navigation.DialogResult.Yes:
                    await _resultsViewModel.SaveProjectCommand.ExecuteAsync(null);
                    if (!_projectStateService.IsDirty)
                    {
                        PerformNewCalculationReset();
                    }
                    break;

                case SnowMeltingCalculator.Services.Navigation.DialogResult.No:
                    PerformNewCalculationReset();
                    break;

                case SnowMeltingCalculator.Services.Navigation.DialogResult.Cancel:
                default:
                    return;
            }
        }

        /// <summary>
        /// Сброс всех ViewModel в начальное состояние при создании нового расчёта
        /// </summary>
        private void PerformNewCalculationReset()
        {
            var constructionResult = _constructionDefaultStateInitializer.Apply(
                _constructionState.Snapshot.GroundwaterLevel,
                ConstructionMutationOrigin.Reset);

            _calculationContext.Reset();
            _resultsViewModel.Reset();
            _projectStateService.MarkClean();
            _climateState.ResetToDefaults(ClimateMutationOrigin.ProjectLoadReset);
            _climateViewModel.SearchQuery = string.Empty;
            _constructionViewModel.ApplyLifecycleSnapshotToAdapter(constructionResult.After);
            // Канонический Thermal-сброс жизненным циклом нового расчёта (не
            // пользователем): результат/статус очищаются без user-dirty
            // (DEC-T08, Todo 9); адаптер ниже зеркалит дефолты без мутаций.
            _thermalState.ResetToDefaults(ThermalMutationOrigin.ProjectLoadReset);
            _thermalViewModel.Reset();
            _circuitsViewModel.Reset();
            _projectStateService.MarkClean();
        }

        private string _currentTitle = "Климатические данные";
        /// <summary>
        /// Заголовок текущей вкладки
        /// </summary>
        public string CurrentTitle
        {
            get => _currentTitle;
            private set => SetProperty(ref _currentTitle, value);
        }

        private void UpdateCurrentTitle()
        {
            CurrentTitle = CurrentNavigationTarget switch
            {
                NavigationTarget.Climate => "Климатические данные",
                NavigationTarget.Construction => "Конструкция",
                NavigationTarget.Thermal => "Тепловой расчёт",
                NavigationTarget.Hydraulics => "Гидравлический расчёт",
                NavigationTarget.Results => "Результаты расчёта",
                _ => "Калькулятор снеготаяния РЕХАУ"
            };
        }

        #region Обработка событий состояния расчёта

        /// <summary>
        /// Обработчик события изменения состояния расчёта
        /// </summary>
        private void OnCalculationStateChanged(object? sender, ModuleStateChangedEventArgs e)
        {
            // Обновить бейджи в зависимости от модуля
            switch (e.Module)
            {
                case "Thermal":
                    UpdateThermalBadge(e.State, e.Message);
                    break;
                case "Hydraulics":
                    UpdateHydraulicsBadge(e.State);
                    break;
            }
        }

        /// <summary>
        /// Обновить бейдж теплового расчёта
        /// </summary>
        private void UpdateThermalBadge(ModuleState state, string? message)
        {
            var thermalMenuItem = MenuItems.FirstOrDefault(m => m.Title == "Тепловой расчёт");
            if (thermalMenuItem == null) return;

            switch (state)
            {
                case ModuleState.Actual:
                    thermalMenuItem.HasWarning = false;
                    thermalMenuItem.IsCalculating = false;
                    thermalMenuItem.BadgeColor = string.Empty;
                    break;
                case ModuleState.NeedsRecalculation:
                    thermalMenuItem.HasWarning = true;
                    thermalMenuItem.IsCalculating = false;
                    thermalMenuItem.BadgeColor = "#FFB300"; // Оранжевый
                    break;
                case ModuleState.Calculating:
                    thermalMenuItem.HasWarning = false;
                    thermalMenuItem.IsCalculating = true;
                    thermalMenuItem.BadgeColor = "#2196F3"; // Синий
                    break;
            }
        }

        /// <summary>
        /// Обновить бейдж гидравлического расчёта
        /// </summary>
        private void UpdateHydraulicsBadge(ModuleState state)
        {
            var hydraulicsMenuItem = MenuItems.FirstOrDefault(m => m.Title == "Гидравлический расчёт");
            if (hydraulicsMenuItem == null) return;

            switch (state)
            {
                case ModuleState.Actual:
                    hydraulicsMenuItem.HasWarning = false;
                    hydraulicsMenuItem.HasError = false;
                    hydraulicsMenuItem.IsCalculating = false;
                    hydraulicsMenuItem.BadgeColor = string.Empty;
                    break;
                case ModuleState.NeedsRecalculation:
                    // Гидравлика не использует NeedsRecalculation (автопересчёт)
                    break;
                case ModuleState.Calculating:
                    hydraulicsMenuItem.HasWarning = false;
                    hydraulicsMenuItem.IsCalculating = true;
                    hydraulicsMenuItem.BadgeColor = "#2196F3"; // Синий
                    break;
                case ModuleState.Error:
                    hydraulicsMenuItem.HasError = true;
                    hydraulicsMenuItem.HasWarning = false;
                    hydraulicsMenuItem.IsCalculating = false;
                    hydraulicsMenuItem.BadgeColor = "#F44336";
                    break;
            }
        }

        #endregion
    }
}
