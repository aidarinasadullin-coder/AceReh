using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.Views.Climate;
using SnowMeltingCalculator.Views.Construction;
using SnowMeltingCalculator.Views.Thermal;
using SnowMeltingCalculator.Views.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
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
            var calculationStateService = services.GetRequiredService<ICalculationStateService>();
            _viewModel = new MainViewModel(climateViewModel, thermalViewModel, constructionViewModel, circuitsViewModel, calculationStateService);
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
                // Иконка "Развернуть": квадрат
                // Иконка "Восстановить": два квадрата (один поверх другого)
                path.Data = isMaximized 
                    ? Geometry.Parse("M4,4 L4,20 L20,20 L20,4 Z M8,4 L8,0 L24,0 L24,16 L20,16") // Восстановить
                    : Geometry.Parse("M0,0 L16,0 L16,16 L0,16 Z"); // Развернуть
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
        private readonly ICalculationStateService _calculationStateService;

        // Кэшированные View (создаются только один раз)
        private ClimateView? _climateView;
        private ThermalView? _thermalView;
        private ConstructionView? _constructionView;
        private CircuitsView? _circuitsView;
        private CircuitsResultsView? _circuitsResultsView;

        public MainViewModel(
            ClimateViewModel climateViewModel,
            ThermalViewModel thermalViewModel,
            ConstructionViewModel constructionViewModel,
            CircuitsViewModel circuitsViewModel,
            ICalculationStateService calculationStateService)
        {
            _climateViewModel = climateViewModel;
            _thermalViewModel = thermalViewModel;
            _constructionViewModel = constructionViewModel;
            _circuitsViewModel = circuitsViewModel;
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            
            // Подписка на изменения состояния
            _calculationStateService.StateChanged += OnCalculationStateChanged;
            
            // Установка начального представления (используем кэшированный View)
            _currentView = _climateView ??= new ClimateView { DataContext = _climateViewModel };
            _selectedMenuItem = MenuItems[0];
            
            // Загрузка состояния боковой панели из настроек
            _isSidebarCollapsed = AppSettings.Instance.IsSidebarCollapsed;
        }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
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
                "Конструкция" => "Конструкция системы",
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
                    "Результаты" => _circuitsResultsView ??= new CircuitsResultsView { DataContext = _circuitsViewModel },
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
        private bool _isCalculating;

        [ObservableProperty]
        private string _badgeColor = string.Empty;
    }
}