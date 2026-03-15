using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Repositories.Hydraulics;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;

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

            // ViewModels
            services.AddSingleton<ThermalViewModel>();

            // Примечание: IConstructionData регистрируется в AddConstructionModule

            return services;
        }

        /// <summary>
        /// Добавить сервисы модуля конструктора конструкции
        /// </summary>
        public static IServiceCollection AddConstructionModule(this IServiceCollection services)
        {
            // Repositories
            services.AddSingleton<IMaterialRepository, MaterialRepository>();
            services.AddSingleton<IConstructionRepository, ConstructionRepository>();

            // Services
            services.AddSingleton<IConstructionService, ConstructionService>();

            // Data - Construction реализует IConstructionData
            services.AddSingleton<Construction>();
            services.AddSingleton<IConstructionData>(sp => sp.GetRequiredService<Construction>());

            // ViewModels
            services.AddSingleton<ConstructionViewModel>();

            return services;
        }

        /// <summary>
        /// Добавить сервисы модуля гидравлики
        /// </summary>
        public static IServiceCollection AddHydraulicsModule(this IServiceCollection services)
        {
            // Repositories - Singleton для кэширования данных
            services.AddSingleton<ICollectorRepository, CollectorRepository>();

            // Services - Singleton для кэширования данных
            services.AddSingleton<IGlycolDataService, GlycolDataService>();
            services.AddSingleton<IHydraulicCalculator, HydraulicCalculator>();
            services.AddSingleton<HydraulicValidator>();

            // ViewModels - Singleton для основного ViewModel (подписка на события)
            services.AddSingleton<HydraulicsViewModel>();

            // ViewModels - Transient для дочерних ViewModel
            services.AddTransient<CircuitViewModel>();
            services.AddTransient<CollectorViewModel>();

            return services;
        }

        /// <summary>
        /// Добавить все сервисы приложения
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            return services
                .AddClimateModule()
                .AddThermalModule()
                .AddConstructionModule()
                .AddHydraulicsModule();
        }
    }
}