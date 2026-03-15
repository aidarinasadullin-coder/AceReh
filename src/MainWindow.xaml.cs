using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Views.Climate;
using SnowMeltingCalculator.Views.Construction;
using SnowMeltingCalculator.Views.Thermal;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            var services = App.Services;
            if (services == null) return;

            var climateViewModel = services.GetRequiredService<ClimateViewModel>();
            var thermalViewModel = services.GetRequiredService<ThermalViewModel>();
            var constructionViewModel = services.GetRequiredService<ConstructionViewModel>();
            var mainViewModel = new MainViewModel(climateViewModel, thermalViewModel, constructionViewModel);
            DataContext = mainViewModel;
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

        public MainViewModel(
            ClimateViewModel climateViewModel,
            ThermalViewModel thermalViewModel,
            ConstructionViewModel constructionViewModel)
        {
            _climateViewModel = climateViewModel;
            _thermalViewModel = thermalViewModel;
            _constructionViewModel = constructionViewModel;
            
            // Установка начального представления
            _currentView = new ClimateView { DataContext = _climateViewModel };
            _selectedMenuItem = MenuItems[0];
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
            new MenuItem { Title = "Тепловой расчёт", Icon = "Fire" },
            new MenuItem { Title = "Конструкция", Icon = "Layers" },
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
                    // TODO: Добавить другие представления по мере реализации
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