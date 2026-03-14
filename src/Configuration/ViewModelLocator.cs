using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Configuration
{
    /// <summary>
    /// Локатор ViewModels для привязки в XAML
    /// </summary>
    public class ViewModelLocator
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Создать локатор
        /// </summary>
        public ViewModelLocator()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// ViewModel климатического модуля
        /// </summary>
        public ClimateViewModel ClimateViewModel => _serviceProvider.GetRequiredService<ClimateViewModel>();

        /// <summary>
        /// ViewModel теплового расчёта
        /// </summary>
        public ThermalViewModel ThermalViewModel => _serviceProvider.GetRequiredService<ThermalViewModel>();

        /// <summary>
        /// Инициализировать сервисы при старте приложения
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var climateService = serviceProvider.GetRequiredService<IClimateDataService>();
            await climateService.LoadClimateDataAsync();
        }
    }
}