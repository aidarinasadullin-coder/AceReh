using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.Views.Climate;
using SnowMeltingCalculator.Views.Construction;
using SnowMeltingCalculator.Views.Thermal;
using SnowMeltingCalculator.Views.Hydraulics;
using SnowMeltingCalculator.Views.Results;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private IProjectStateService? _projectStateService;
        private IDialogService? _dialogService;
        private bool _isClosingAfterSave;

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();

            // Регистрируем обработчик клавиатурных сокращений
            KeyDown += MainWindow_KeyDown;
        }

        private void InitializeViewModel()
        {
            var services = App.Services;
            if (services == null) return;

            var climateViewModel = services.GetRequiredService<ClimateViewModel>();
            var thermalViewModel = services.GetRequiredService<ThermalViewModel>();
            var constructionViewModel = services.GetRequiredService<ConstructionViewModel>();
            var circuitsViewModel = services.GetRequiredService<CircuitsViewModel>();
            var resultsViewModel = services.GetRequiredService<ResultsViewModel>();
            var calculationStateService = services.GetRequiredService<ICalculationStateService>();
            _projectStateService = services.GetRequiredService<IProjectStateService>();
            _dialogService = services.GetRequiredService<IDialogService>();
            var calculationContext = services.GetRequiredService<CalculationContext>();
            _viewModel = new MainViewModel(climateViewModel, thermalViewModel, constructionViewModel, circuitsViewModel, resultsViewModel, calculationStateService, _projectStateService, _dialogService, calculationContext);
            DataContext = _viewModel;

            // Подписываемся на изменение состояния боковой панели для анимации
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Обработчик клавиатурных сокращений
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+B для переключения боковой панели
            if (e.Key == Key.B && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_viewModel != null)
                {
                    _viewModel.ToggleSidebarCommand.Execute(null);
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+S для сохранения
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_viewModel?.ResultsViewModel != null)
                {
                    _viewModel.ResultsViewModel.SaveProjectCommand.Execute(null);
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+Shift+S для сохранения как
            if (e.Key == Key.S && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (_viewModel?.ResultsViewModel != null)
                {
                    _viewModel.ResultsViewModel.SaveProjectAsCommand.Execute(null);
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+O для открытия
            if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_viewModel?.ResultsViewModel != null)
                {
                    _viewModel.ResultsViewModel.OpenProjectCommand.Execute(null);
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+N для создания нового расчёта
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_viewModel?.NewCalculationCommand != null)
                {
                    _viewModel.NewCalculationCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
                return;
            }
        }

        /// <summary>
        /// Обработчик закрытия окна с проверкой несохранённых изменений
        /// </summary>
        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await MainWindow_ClosingAsync(e);
        }

        private async Task MainWindow_ClosingAsync(System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosingAfterSave)
            {
                _isClosingAfterSave = false;
                return;
            }

            if (_projectStateService == null || !_projectStateService.IsDirty)
            {
                return;
            }

            var result = _dialogService?.Show(
                "Текущий проект имеет несохранённые изменения. Сохранить перед закрытием?",
                "Закрытие приложения",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;

                case MessageBoxResult.No:
                    break;

                case MessageBoxResult.Yes:
                    e.Cancel = true;
                    if (_viewModel?.ResultsViewModel.SaveProjectCommand != null)
                    {
                        await _viewModel.ResultsViewModel.SaveProjectCommand.ExecuteAsync(null);
                        if (!_projectStateService.IsDirty)
                        {
                            _isClosingAfterSave = true;
                            Close();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Обработчик изменения свойств ViewModel для анимации
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsSidebarCollapsed) && _viewModel != null)
            {
                AnimateSidebar(_viewModel.IsSidebarCollapsed);
            }
        }

        /// <summary>
        /// Анимация сворачивания/разворачивания боковой панели
        /// </summary>
        private void AnimateSidebar(bool isCollapsed)
        {
            var sidebarGrid = FindName("SidebarGrid") as System.Windows.Controls.Grid;
            if (sidebarGrid == null) return;

            var animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            if (isCollapsed)
            {
                animation.From = 220;
                animation.To = 65;
            }
            else
            {
                animation.From = 65;
                animation.To = 220;
            }

            sidebarGrid.BeginAnimation(System.Windows.Controls.Grid.WidthProperty, animation);
        }

        #region Обработчики кнопок управления окном

        /// <summary>
        /// Обработчик перетаскивания окна за хедер
        /// </summary>
        private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Свернуть"
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Обработчик кнопки "Развернуть/Восстановить"
        /// </summary>
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                UpdateMaximizeRestoreButton(sender as System.Windows.Controls.Button, false);
            }
            else
            {
                WindowState = WindowState.Maximized;
                UpdateMaximizeRestoreButton(sender as System.Windows.Controls.Button, true);
            }
        }

        /// <summary>
        /// Обработчик кнопки "Закрыть"
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Обновление иконки кнопки развернуть/восстановить
        /// </summary>
        private void UpdateMaximizeRestoreButton(System.Windows.Controls.Button? button, bool isMaximized)
        {
            if (button == null) return;

            var path = FindVisualChild<Path>(button);
            if (path != null)
            {
                // Иконка "Развернуть": квадратная рамка
                // Иконка "Восстановить": передний квадрат + задний квадрат (только верх и право)
                path.Data = isMaximized
                    ? Geometry.Parse("M3,7 L3,17 L13,17 L13,7 Z M7,3 L17,3 L17,13") // Восстановить: передний полный + задний (верх+право)
                    : Geometry.Parse("M3,3 L15,3 L15,15 L3,15 Z"); // Развернуть: квадратная рамка
            }

            button.ToolTip = isMaximized ? "Восстановить" : "Развернуть";
        }

        /// <summary>
        /// Поиск визуального дочернего элемента
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        #endregion
    }

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

        public ResultsViewModel ResultsViewModel => _resultsViewModel;

        // Кэшированные View (создаются только один раз)
        private ClimateView? _climateView;
        private ThermalView? _thermalView;
        private ConstructionView? _constructionView;
        private CircuitsView? _circuitsView;
        private ResultsView? _resultsView;

        public MainViewModel(
            ClimateViewModel climateViewModel,
            ThermalViewModel thermalViewModel,
            ConstructionViewModel constructionViewModel,
            CircuitsViewModel circuitsViewModel,
            ResultsViewModel resultsViewModel,
            ICalculationStateService calculationStateService,
            IProjectStateService projectStateService,
            IDialogService dialogService,
            CalculationContext calculationContext)
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

            // Подписка на изменения состояния
            _calculationStateService.StateChanged += OnCalculationStateChanged;

            // Подписка на изменения состояния проекта для обновления заголовка окна
            _projectStateService.PropertyChanged += OnProjectStateChanged;

            // Начальное представление создаётся лениво при первом обращении (см. CurrentView)
            _selectedMenuItem = MenuItems[0];

            // Загрузка состояния боковой панели из настроек
            _isSidebarCollapsed = AppSettings.Instance.IsSidebarCollapsed;

            // Инициализация заголовка окна
            UpdateWindowTitle();
        }

        private object? _currentView;
        public object CurrentView
        {
            get => _currentView ??= (_climateView ??= new ClimateView { DataContext = _climateViewModel });
            private set => SetProperty(ref _currentView, value);
        }

        public MenuItem[] MenuItems { get; } = new[]
        {
            new MenuItem { Title = "Климат", Icon = "WeatherCloudy" },
            new MenuItem { Title = "Конструкция", Icon = "Layers" },
            new MenuItem { Title = "Тепловой расчёт", Icon = "Fire" },
            new MenuItem { Title = "Гидравлический расчёт", Icon = "Pipe" },
            new MenuItem { Title = "Результаты", Icon = "ChartBar" }
        };

        private MenuItem? _selectedMenuItem;
        public MenuItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (SetProperty(ref _selectedMenuItem, value) && value != null)
                {
                    NavigateToView(value);
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
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    await _resultsViewModel.SaveProjectCommand.ExecuteAsync(null);
                    if (!_projectStateService.IsDirty)
                    {
                        PerformNewCalculationReset();
                    }
                    break;

                case MessageBoxResult.No:
                    PerformNewCalculationReset();
                    break;

                case MessageBoxResult.Cancel:
                default:
                    return;
            }
        }

        /// <summary>
        /// Сброс всех ViewModel в начальное состояние при создании нового расчёта
        /// </summary>
        private void PerformNewCalculationReset()
        {
            _calculationContext.Reset();
            _resultsViewModel.Reset();
            _projectStateService.MarkClean();
            _climateViewModel.Reset();
            _constructionViewModel.Reset();
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
            CurrentTitle = SelectedMenuItem?.Title switch
            {
                "Климат" => "Климатические данные",
                "Конструкция" => "Конструкция",
                "Тепловой расчёт" => "Тепловой расчёт",
                "Гидравлический расчёт" => "Гидравлический расчёт",
                "Результаты" => "Результаты расчёта",
                _ => "Калькулятор снеготаяния РЕХАУ"
            };
        }

        /// <summary>
        /// Переключение между представлениями
        /// </summary>
        private void NavigateToView(MenuItem menuItem)
        {
            try
            {
                CurrentView = menuItem.Title switch
                {
                    "Климат" => _climateView ??= new ClimateView { DataContext = _climateViewModel },
                    "Тепловой расчёт" => _thermalView ??= new ThermalView { DataContext = _thermalViewModel },
                    "Конструкция" => _constructionView ??= new ConstructionView { DataContext = _constructionViewModel },
                    "Гидравлический расчёт" => _circuitsView ??= new CircuitsView { DataContext = _circuitsViewModel },
                    "Результаты" => GetResultsView(),
                    _ => _climateView ??= new ClimateView { DataContext = _climateViewModel }
                };

                UpdateCurrentTitle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании представления '{menuItem.Title}': {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Показываем сообщение об ошибке
                System.Windows.MessageBox.Show(
                    $"Ошибка при открытии вкладки '{menuItem.Title}':\n{ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Возвращаемся к климату (используем кэшированный View)
                CurrentView = _climateView ??= new ClimateView { DataContext = _climateViewModel };
            }
        }

        /// <summary>
        /// Получить представление результатов (с загрузкой данных гидравлики)
        /// </summary>
        private object GetResultsView()
        {
            _resultsViewModel.LoadHydraulicsDataOnNavigate();
            return _resultsView ??= new ResultsView { DataContext = _resultsViewModel };
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

    /// <summary>
    /// Элемент меню навигации
    /// </summary>
    public partial class MenuItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _icon = string.Empty;

        [ObservableProperty]
        private bool _hasWarning;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _isCalculating;

        [ObservableProperty]
        private string _badgeColor = string.Empty;

        /// <summary>
        /// SVG path data для иконки (вычисляется на основе Icon)
        /// </summary>
        public string IconPath => Icon switch
        {
            "WeatherCloudy" => "M12.74,5.47C15.1,6.5 16.35,9.03 15.88,11.57C17.8,12.03 19.16,13.83 18.87,15.89C18.59,17.95 16.75,19.31 14.66,19.06C14.41,19.05 14.19,19.26 14.19,19.5C14.19,19.78 13.96,20 13.68,20H6.32C6.04,20 5.81,19.78 5.81,19.5V19.44C4.27,19.03 3.21,17.61 3.44,16.04C3.67,14.46 5.12,13.38 6.71,13.58C6.76,13.59 6.81,13.54 6.8,13.49C6.5,11.55 7.7,9.72 9.57,9.13C9.62,9.11 9.65,9.06 9.63,9.01C9.27,7.78 9.88,6.5 11.07,6C11.26,5.92 11.46,5.86 11.67,5.83C12.03,5.77 12.39,5.79 12.74,5.87V5.47M12.75,7.38C12.33,7.37 11.92,7.56 11.65,7.89C11.38,8.22 11.27,8.65 11.35,9.06C11.38,9.22 11.29,9.38 11.14,9.44C9.89,9.88 9.03,11.09 9.14,12.41C9.15,12.56 9.06,12.7 8.91,12.74C7.98,13 7.28,13.79 7.12,14.75C7.11,14.83 7.05,14.89 6.97,14.88C6.14,14.81 5.35,15.26 5.03,16.03C4.71,16.8 4.92,17.69 5.56,18.23C5.64,18.3 5.75,18.31 5.84,18.26C5.93,18.21 6.03,18.21 6.12,18.26C6.5,18.5 6.96,18.62 7.43,18.62H13.57C15.3,18.62 16.71,17.21 16.71,15.48C16.71,14.33 16.07,13.27 15.04,12.73C14.92,12.67 14.86,12.53 14.9,12.41C15.25,11.22 14.76,9.96 13.71,9.34C13.59,9.27 13.54,9.12 13.58,8.99C13.69,8.59 13.62,8.17 13.39,7.83C13.16,7.48 12.78,7.25 12.36,7.22L12.75,7.38Z",
            "Layers" => "M12,16.54L19.37,11.33C19.69,11.11 20.13,11.2 20.35,11.53C20.57,11.85 20.48,12.29 20.15,12.51L12.77,17.71C12.29,18.04 11.66,18.04 11.18,17.71L3.8,12.51C3.47,12.29 3.38,11.85 3.6,11.53C3.82,11.2 4.26,11.11 4.58,11.33L12,16.54M12,13.17L19.37,7.96C19.69,7.74 20.13,7.83 20.35,8.16C20.57,8.48 20.48,8.92 20.15,9.14L12.77,14.34C12.29,14.67 11.66,14.67 11.18,14.34L3.8,9.14C3.47,8.92 3.38,8.48 3.6,8.16C3.82,7.83 4.26,7.74 4.58,7.96L12,13.17M12,9.81L19.37,4.6C19.69,4.38 20.13,4.47 20.35,4.8C20.57,5.12 20.48,5.56 20.15,5.78L12.77,10.98C12.29,11.31 11.66,11.31 11.18,10.98L3.8,5.78C3.47,5.56 3.38,5.12 3.6,4.8C3.82,4.47 4.26,4.38 4.58,4.6L12,9.81Z",
            "Fire" => "M17.66,11.2C17.43,10.9 17.15,10.64 16.89,10.38C16.22,9.78 15.46,9.35 14.82,8.72C13.33,7.26 13,4.85 13.95,3C13,3.23 12.17,3.75 11.46,4.32C8.96,6.4 7.92,10.07 9.12,13.22C9.13,13.23 9.13,13.24 9.12,13.25C9.1,13.27 9.07,13.28 9.04,13.27C6.95,12.44 5.85,10.2 6.33,8C4.31,9.36 3.27,11.94 3.96,14.32C4.07,14.7 4.21,15.07 4.38,15.42C4.6,15.9 4.86,16.36 5.16,16.79C6.64,18.85 9.04,20.14 11.65,20.14C15.23,20.14 18.27,17.6 18.96,14.1C19.35,12.29 18.8,10.45 17.66,11.2Z",
            "Pipe" => "M19,3H5C3.89,3 3,3.89 3,5V19C3,20.11 3.89,21 5,21H19C20.11,21 21,20.11 21,19V5C21,3.89 20.11,3 19,3M19,19H5V5H19V19M7,10H9V17H7V10M11,7H13V17H11V7M15,13H17V17H15V13Z",
            "ChartBar" => "M22,22H2V2H22V22M4,20H20V4H4V20M6,18H8V12H6V18M10,18H12V6H10V18M14,18H16V14H14V18M18,18H20V10H18V18Z",
            _ => ""
        };
    }
}