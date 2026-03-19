using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
            _viewModel = new MainViewModel(climateViewModel, thermalViewModel, constructionViewModel, circuitsViewModel);
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

        public MainViewModel(
            ClimateViewModel climateViewModel,
            ThermalViewModel thermalViewModel,
            ConstructionViewModel constructionViewModel,
            CircuitsViewModel circuitsViewModel)
        {
            _climateViewModel = climateViewModel;
            _thermalViewModel = thermalViewModel;
            _constructionViewModel = constructionViewModel;
            _circuitsViewModel = circuitsViewModel;
            
            // Установка начального представления
            _currentView = new ClimateView { DataContext = _climateViewModel };
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
            new MenuItem { Title = "Контура", Icon = "Pipe" },
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

        /// <summary>
        /// Переключение между представлениями
        /// </summary>
        private void NavigateToView(MenuItem menuItem)
        {
            try
            {
                CurrentView = menuItem.Title switch
                {
                    "Климат" => new ClimateView { DataContext = _climateViewModel },
                    "Тепловой расчёт" => new ThermalView { DataContext = _thermalViewModel },
                    "Конструкция" => new ConstructionView { DataContext = _constructionViewModel },
                    "Контура" => new CircuitsView { DataContext = _circuitsViewModel },
                    "Результаты" => new CircuitsResultsView { DataContext = _circuitsViewModel },
                    _ => new ClimateView { DataContext = _climateViewModel }
                };
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
                
                // Возвращаемся к климату
                CurrentView = new ClimateView { DataContext = _climateViewModel };
            }
        }
    }

    public class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}