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
        private readonly IProjectSession _projectStateService;
        private readonly IDialogService _dialogService;
        private readonly CalculationContext _calculationContext;
        private readonly IProjectSessionClimateState _climateState;
        private readonly IProjectSessionConstructionState _constructionState;
        private readonly IProjectSessionThermalState _thermalState;
        private readonly IProjectSessionHydraulicsState _hydraulicsState;
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
            IProjectSession projectStateService,
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
            _hydraulicsState = session.HydraulicsState;
            _constructionDefaultStateInitializer = constructionDefaultStateInitializer
                ?? throw new ArgumentNullException(nameof(constructionDefaultStateInitializer));

            // Подписка на изменения состояния
            _calculationStateService.StateChanged += OnCalculationStateChanged;

            // Подписка на изменения состояния проекта для обновления заголовка окна
            _projectStateService.PropertyChanged += OnProjectStateChanged;

            // Степпер и статус-бар обновляются по событиям модульных VM
            _climateViewModel.PropertyChanged += OnModuleViewModelChanged;
            _constructionViewModel.PropertyChanged += OnModuleViewModelChanged;
            _thermalViewModel.PropertyChanged += OnModuleViewModelChanged;
            _circuitsViewModel.PropertyChanged += OnModuleViewModelChanged;
            _resultsViewModel.PropertyChanged += OnModuleViewModelChanged;

            _selectedMenuItem = MenuItems[0];

            // Загрузка состояния боковой панели из настроек
            _isSidebarCollapsed = AppSettings.Instance.IsSidebarCollapsed;

            // Инициализация заголовка окна
            UpdateWindowTitle();
            RefreshShellStatus();
        }

        public MenuItem[] MenuItems { get; } = new[]
        {
            // Заголовки — по эталону 01 (Фаза 3Б): короткое «Гидравлика»
            // помещается в слот шага без обрезки.
            new MenuItem { Number = 1, Title = "Климат", Icon = "WeatherCloudy", Target = NavigationTarget.Climate },
            new MenuItem { Number = 2, Title = "Конструкция", Icon = "Layers", Target = NavigationTarget.Construction },
            new MenuItem { Number = 3, Title = "Тепловой расчёт", Icon = "Fire", Target = NavigationTarget.Thermal },
            new MenuItem { Number = 4, Title = "Гидравлика", Icon = "Pipe", Target = NavigationTarget.Hydraulics },
            new MenuItem { Number = 5, Title = "Результаты", Icon = "ChartBar", Target = NavigationTarget.Results }
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
                    RefreshShellStatus();
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
            if (e.PropertyName == nameof(IProjectSession.IsDirty) ||
                e.PropertyName == nameof(IProjectSession.CurrentFilePath))
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
            // Новый расчёт — чистый бланк: заводской УГВ, а не УГВ предыдущего
            // проекта (план 2026-09-04, D1).
            var constructionResult = _constructionDefaultStateInitializer.Apply(
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
            _hydraulicsState.ResetToDefaults(HydraulicsMutationOrigin.UserReset);
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

            CurrentModulePlateText = CurrentNavigationTarget switch
            {
                NavigationTarget.Climate => "КЛИМАТ",
                NavigationTarget.Construction => "КОНСТРУКЦИЯ",
                NavigationTarget.Thermal => "ТЕПЛОВОЙ",
                NavigationTarget.Hydraulics => "ГИДРАВЛИКА",
                NavigationTarget.Results => "РЕЗУЛЬТАТЫ",
                _ => string.Empty
            };
        }

        #region Степпер и статус-бар (Фаза 1 редизайна)

        /// <summary>
        /// Общий хук событий модульных VM: любые изменения валидации,
        /// пересчёта или готовности обновляют степпер и статус-бар.
        /// Чтение — только кэшированных свойств (не геттеров с побочными
        /// эффектами вроде ClimateViewModel.IsValid — ревью Ф1, F4).
        /// </summary>
        private void OnModuleViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshShellStatus();
        }

        /// <summary>
        /// Пересчитывает состояния шагов степпера и содержимое статус-бара.
        /// Только чтение существующих VM и снапшотов сессии (R2/R3/R5 не
        /// затрагиваются).
        /// </summary>
        private void RefreshShellStatus()
        {
            RefreshStepStatuses();
            RefreshStatusBar();
        }

        private void RefreshStepStatuses()
        {
            var climate = MenuItemByTitle("Климат");
            if (climate != null)
            {
                climate.StepStatus = !string.IsNullOrWhiteSpace(_climateViewModel.ValidationMessage)
                    ? StepStatus.Error
                    : !string.IsNullOrWhiteSpace(_climateState.Snapshot.SelectedCity)
                        ? StepStatus.Ready
                        : StepStatus.Draft;
            }

            var construction = MenuItemByTitle("Конструкция");
            if (construction != null)
            {
                construction.StepStatus = !string.IsNullOrWhiteSpace(_constructionViewModel.ValidationMessage)
                    ? StepStatus.Error
                    : StepStatus.Ready;
            }

            var thermal = MenuItemByTitle("Тепловой расчёт");
            if (thermal != null)
            {
                thermal.StepStatus = _thermalViewModel.IsCalculating
                    ? StepStatus.Recalculating
                    : !string.IsNullOrWhiteSpace(_thermalViewModel.ValidationMessage)
                        ? StepStatus.Error
                        : !_thermalViewModel.NeedsRecalculation
                            ? StepStatus.Ready
                            : StepStatus.Draft;
            }

            // Поиск по Target, не по заголовку: короткие названия шагов
            // меняются по эталонам (Фаза 3Б), NavigationTarget — контракт.
            var hydraulics = MenuItems.FirstOrDefault(m => m.Target == NavigationTarget.Hydraulics);
            if (hydraulics != null)
            {
                hydraulics.StepStatus = _circuitsViewModel.IsCalculating
                    ? StepStatus.Recalculating
                    : (hydraulics.HasError || !string.IsNullOrWhiteSpace(_circuitsViewModel.ValidationMessage))
                        ? StepStatus.Error
                        : StepStatus.Ready;
            }

            var results = MenuItemByTitle("Результаты");
            if (results != null)
            {
                results.StepStatus = _resultsViewModel.IsDataReady
                    ? StepStatus.Ready
                    : StepStatus.Draft;
            }
        }

        private MenuItem? MenuItemByTitle(string title) =>
            MenuItems.FirstOrDefault(m => m.Title == title);

        private void RefreshStatusBar()
        {
            var thermalNeedsRecalculation = _thermalViewModel.NeedsRecalculation;

            // Основной слот — валидация/статус активного модуля
            var validationText = CurrentNavigationTarget switch
            {
                NavigationTarget.Climate => _climateViewModel.ValidationMessage,
                NavigationTarget.Construction => _constructionViewModel.ValidationMessage,
                NavigationTarget.Thermal => thermalNeedsRecalculation
                    ? _thermalViewModel.RecalcMessage
                    : _thermalViewModel.ValidationMessage,
                NavigationTarget.Hydraulics => _circuitsViewModel.ValidationMessage,
                NavigationTarget.Results => _resultsViewModel.StatusMessage,
                _ => string.Empty
            };
            CurrentValidationText = validationText ?? string.Empty;

            // Слот пересчёта справа — только тепловой модуль умеет NeedsRecalculation
            CurrentRecalcText = thermalNeedsRecalculation
                && CurrentNavigationTarget != NavigationTarget.Thermal
                    ? _thermalViewModel.RecalcMessage ?? string.Empty
                    : string.Empty;

            CurrentStatusKind = CurrentNavigationTarget switch
            {
                NavigationTarget.Thermal when thermalNeedsRecalculation => ShellStatusKind.Warning,
                NavigationTarget.Results => _resultsViewModel.IsDataReady
                    ? ShellStatusKind.Success
                    : ShellStatusKind.Warning,
                _ when !string.IsNullOrWhiteSpace(CurrentValidationText) => ShellStatusKind.Error,
                _ => ShellStatusKind.Success
            };
        }

        private ShellStatusKind _currentStatusKind = ShellStatusKind.Success;
        /// <summary>Семантика статус-бара: цвет скошенной плашки.</summary>
        public ShellStatusKind CurrentStatusKind
        {
            get => _currentStatusKind;
            private set => SetProperty(ref _currentStatusKind, value);
        }

        private string _currentModulePlateText = "КЛИМАТ";
        /// <summary>Короткое имя модуля для скошенной плашки.</summary>
        public string CurrentModulePlateText
        {
            get => _currentModulePlateText;
            private set => SetProperty(ref _currentModulePlateText, value);
        }

        private string _currentValidationText = string.Empty;
        /// <summary>Валидация/статус активного модуля (слот статус-бара).</summary>
        public string CurrentValidationText
        {
            get => _currentValidationText;
            private set => SetProperty(ref _currentValidationText, value);
        }

        private string _currentRecalcText = string.Empty;
        /// <summary>Сообщение о необходимости пересчёта (правый слот).</summary>
        public string CurrentRecalcText
        {
            get => _currentRecalcText;
            private set => SetProperty(ref _currentRecalcText, value);
        }

        #endregion

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
            // Поиск по Target, не по заголовку (Фаза 3Б: шаг переименован в «Гидравлика»).
            var hydraulicsMenuItem = MenuItems.FirstOrDefault(m => m.Target == NavigationTarget.Hydraulics);
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
