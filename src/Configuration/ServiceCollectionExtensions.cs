using System.IO;
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
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Hydraulics;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

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

            // Репозиторий истории поиска
            services.AddSingleton<ISearchHistoryRepository>(sp =>
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "search_history.db");
                // Убеждаемся, что директория существует
                var dbDirectory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }
                return SearchHistoryRepository.Create(dbPath);
            });

            // Services
            services.AddSingleton<IClimateDataService, ClimateDataService>();
            services.AddSingleton<ISearchHistoryService, SearchHistoryService>();

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

            // Services - Калькулятор контуров (без состояния)
            services.AddSingleton<ICircuitsCalculator, CircuitsCalculator>();

            // Services - Валидатор контуров и коллекторов
            services.AddScoped<ICircuitsValidator, CircuitsValidator>();

            // Services - Селектор типа коллектора (без состояния)
            services.AddScoped<ICollectorTypeSelector, CollectorTypeSelector>();

            // ViewModels - Singleton для модуля "Контура" (сохранение состояния между навигациями)
            services.AddSingleton<CircuitsViewModel>();

            // ViewModels - Transient для дочерних ViewModel
            services.AddTransient<CollectorViewModel>();

            return services;
        }

        /// <summary>
        /// Добавить сервисы навигации и состояния
        /// </summary>
        public static IServiceCollection AddNavigationServices(this IServiceCollection services)
        {
            // Services - Singleton для глобального состояния расчёта
            services.AddSingleton<ICalculationStateService, CalculationStateService>();

            // Singleton inter-module calculation bus
            services.AddSingleton<CalculationContext>();

            return services;
        }

        /// <summary>
        /// Добавить сервисы модуля результатов
        /// </summary>
        public static IServiceCollection AddResultsModule(this IServiceCollection services)
        {
            // Services
            services.AddSingleton<IProjectInfoService, ProjectInfoService>();
            services.AddSingleton<IPdfExportService, PdfExportService>();
            services.AddSingleton<IProjectFileService, ProjectFileService>();
            services.AddSingleton<IConstructionVisualizationImageService, ConstructionVisualizationImageService>();

            // ViewModels
            services.AddSingleton<ResultsViewModel>();

            return services;
        }

        /// <summary>
        /// Добавить все сервисы приложения
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            return services
                .AddNavigationServices()
                .AddClimateModule()
                .AddThermalModule()
                .AddConstructionModule()
                .AddHydraulicsModule()
                .AddResultsModule()
                .AddValidators()
                .AddValidationPipeline();
        }

        /// <summary>
        /// Добавить валидаторы расчётных данных
        /// </summary>
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddTransient<IValidator<IClimateData>, ClimateValidator>();
            services.AddTransient<IValidator<ConstructionModel>, ConstructionValidator>();
            services.AddTransient<IValidator<ThermalInputs>, ThermalValidator>();
            services.AddTransient<IValidator<ThermalCalculationResult>, ThermalResultValidator>();
            services.AddTransient<IValidator<HydraulicInputData>, HydraulicValidator>();
            services.AddTransient<IValidator<CircuitRow>, CircuitValidator>();

            return services;
        }

        /// <summary>
        /// Добавить конвейер валидации
        /// </summary>
        public static IServiceCollection AddValidationPipeline(this IServiceCollection services)
        {
            services.AddTransient<IValidationPipeline, ValidationPipeline>();
            return services;
        }
    }
}