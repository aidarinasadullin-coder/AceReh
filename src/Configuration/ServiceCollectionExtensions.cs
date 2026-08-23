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
using SnowMeltingCalculator.Services.Reports.Calculation;
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

            // Каноническая граница применения тепловых команд (DEC-T04A):
            // ровно один singleton; ThermalViewModel получает его через ctor.
            // Срез ThermalState не регистрируется в DI отдельно — берётся
            // reference-identically с ProjectSession (как ConstructionState выше).
            services.AddSingleton<IThermalStateCoordinator>(sp => new ThermalStateCoordinator(
                sp.GetRequiredService<ProjectSession>().ThermalState,
                sp.GetRequiredService<CalculationContext>(),
                sp.GetRequiredService<IMarkDirtyService>(),
                sp.GetRequiredService<IThermalCalculator>(),
                sp.GetRequiredService<IClimateData>(),
                sp.GetRequiredService<IConstructionData>(),
                sp.GetRequiredService<IValidator<ThermalInputs>>(),
                sp.GetRequiredService<IValidator<ThermalCalculationResult>>()));

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
            services.AddSingleton<IConstructionTemplateRepository, ConstructionTemplateRepository>();

            // Services
            services.AddSingleton<IConstructionService, ConstructionService>();

            // Mutable compatibility model used only by the Construction adapter.
            services.AddSingleton<Construction>();
            services.AddSingleton<IConstructionData>(sp =>
                sp.GetRequiredService<IProjectSessionConstructionState>().CurrentProjection);

            // ViewModels
            services.AddSingleton<ConstructionViewModel>();
            services.AddTransient<MaterialEditorViewModel>();
            services.AddTransient<TemplateEditorViewModel>();

            // Views (редакторские окна — Transient, создаются по запросу)
            services.AddTransient<Views.Construction.MaterialEditorView>();
            services.AddTransient<Views.Construction.TemplateEditorView>();

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

            // Services - Валидатор контуров и коллекторов (без состояния)
            // Singleton: в WPF нет request-scope, scoped-регистрация жила бы в root scope.
            services.AddSingleton<ICircuitsValidator, CircuitsValidator>();

            // Services - Селектор типа коллектора (без состояния)
            services.AddSingleton<ICollectorTypeSelector, CollectorTypeSelector>();

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

            // Диалоговый сервис (шов для тестирования MessageBox)
            services.AddSingleton<IDialogService, MessageBoxService>();

            // Сервис редакторских диалогов (шов для редакторов материалов и шаблонов)
            services.AddSingleton<IEditorDialogService, EditorDialogService>();

            // ViewModel главного окна (shell)
            services.AddSingleton<ViewModels.Shell.MainViewModel>();

            return services;
        }

        /// <summary>
        /// Добавить сервисы модуля результатов
        /// </summary>
        public static IServiceCollection AddResultsModule(this IServiceCollection services)
        {
            // Services - Project lifecycle aggregate root and legacy compatibility views
            services.AddSingleton<ProjectSession>();
            services.AddSingleton<IProjectSession>(sp => sp.GetRequiredService<ProjectSession>());
            services.AddSingleton<IProjectInfoService>(sp => sp.GetRequiredService<ProjectSession>());
            services.AddSingleton<IProjectStateService>(sp => sp.GetRequiredService<ProjectSession>());
            services.AddSingleton<IMarkDirtyService>(sp => sp.GetRequiredService<ProjectSession>());
            services.AddSingleton<IProjectSessionConstructionState>(sp => (IProjectSessionConstructionState)sp.GetRequiredService<ProjectSession>().ConstructionState);
            services.AddSingleton<ConstructionDefaultStateInitializer>();
            services.AddSingleton<IPdfExportService, PdfExportService>();
            services.AddSingleton<ICalculationReportDataBuilder, CalculationReportDataBuilder>();
            services.AddSingleton<ICalculationReportMarkdownRenderer, CalculationReportMarkdownRenderer>();
            services.AddSingleton<ICalculationReportExportService, CalculationReportExportService>();
            services.AddSingleton<IProjectFileService, ProjectFileService>();
            services.AddSingleton<IConstructionVisualizationImageService, ConstructionVisualizationImageService>();
            services.AddSingleton<ProjectLoadOrchestrator>();
            services.AddSingleton<ResultsPdfDataBuilder>();
            services.AddSingleton<HydraulicSummaryBuilder>();

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
                .AddSingleton<MainWindow>()
                .AddValidators();
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
            services.AddTransient<IValidator<Material>, MaterialCrudValidator>();
            services.AddTransient<MaterialCrudValidator>();
            services.AddTransient<IValidator<ConstructionTemplate>, ConstructionTemplateValidator>();
            services.AddTransient<ConstructionTemplateValidator>();

            return services;
        }
    }
}
