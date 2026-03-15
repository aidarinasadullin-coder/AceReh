using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.ViewModels.Construction;
using System.Windows;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// Провайдер сервисов (для доступа из других мест)
        /// </summary>
        public static IServiceProvider? Services { get; private set; }

        /// <summary>
        /// При запуске приложения
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Настройка DI
            var services = new ServiceCollection();
            
            // Регистрация всех сервисов приложения
            services.AddApplicationServices();
            
            _serviceProvider = services.BuildServiceProvider();
            Services = _serviceProvider;

            try
            {
                // Загрузка климатических данных
                var climateService = _serviceProvider.GetRequiredService<IClimateDataService>();
                await climateService.LoadClimateDataAsync();

                // Загрузка материалов для конструктора конструкции
                var materialRepository = _serviceProvider.GetRequiredService<IMaterialRepository>();
                await materialRepository.LoadMaterialsAsync();

                // Установка провайдера в ViewModelLocator
                var viewModelLocator = Current.Resources["ViewModelLocator"] as ViewModelLocator;
                viewModelLocator?.SetServiceProvider(_serviceProvider);

                // Инициализация ConstructionViewModel
                var constructionViewModel = _serviceProvider.GetRequiredService<ConstructionViewModel>();
                await constructionViewModel.InitializeCommand.ExecuteAsync(null);

                // Создание главного окна
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске приложения: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"InnerException StackTrace: {ex.InnerException.StackTrace}");
                }
                
                MessageBox.Show(
                    $"Ошибка при запуске приложения:\n{ex.Message}\n\n{(ex.InnerException != null ? ex.InnerException.Message : "")}\n\n{ex.StackTrace}",
                    "Ошибка запуска",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown();
            }
        }
    }
}