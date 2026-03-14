using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Configuration
{
    /// <summary>
    /// Конфигурация сервисов приложения
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавить сервисы климатического модуля
        /// </summary>
        public static IServiceCollection AddClimateModule(this IServiceCollection services)
        {
            // Repositories
            services.AddSingleton<IClimateDataRepository, ClimateDataRepository>();

            // Services
            services.AddSingleton<IClimateDataService, ClimateDataService>();

            // ViewModels
            services.AddSingleton<ClimateViewModel>();

            // Data
            services.AddSingleton<IClimateData, ClimateData>();

            return services;
        }

        /// <summary>
        /// Добавить сервисы модуля теплового расчёта
        /// </summary>
        public static IServiceCollection AddThermalModule(this IServiceCollection services)
        {
            // Services
            services.AddSingleton<IThermalCalculator, ThermalCalculator>();

            // Data
            services.AddSingleton<IConstructionData, ConstructionData>();

            // ViewModels
            services.AddSingleton<ThermalViewModel>();

            return services;
        }

        /// <summary>
        /// Добавить все сервисы приложения
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            return services
                .AddClimateModule()
                .AddThermalModule();
        }
    }
}