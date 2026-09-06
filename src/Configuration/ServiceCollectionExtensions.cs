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
                sp.GetRequiredService<ProjectSession>(),
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
            services.AddSingleton<IHydraulicsStateCoordinator>(sp => new HydraulicsStateCoordinator(
                sp.GetRequiredService<ProjectSession>().HydraulicsState,
                sp.GetRequiredService<ICalculationStateService>(),
                sp.GetRequiredService<CalculationContext>()));

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

            // Read-only адаптер панели «Сводка» (Фаза 1 редизайна)
            services.AddSingleton<ViewModels.Shell.SummaryViewModel>();

            return services;
        }

        /// <summary>
        /// Добавить сервисы модуля результатов
        /// </summary>
        public static IServiceCollection AddResultsModule(this IServiceCollection services)
        {
            // Services - Project lifecycle aggregate root and legacy compatibility views
            // Явная фабрика: авто-резолв параметров конструктора ProjectSession тянет
            // IMarkDirtyService (фабричная регистрация ниже -> GetRequiredService<ProjectSession>),
            // что даёт реентерабельный дедлок DI .NET 8 на construction-цикле.
            // hydraulicsDirtyService = null каноничен: срез использует саму сессию
            // как dirty-owner (ProjectSessionHydraulicsState: markDirtyService ?? this).
            services.AddSingleton(sp => new ProjectSession(
                sp.GetRequiredService<IClimateData>(),
                sp.GetRequiredService<CalculationContext>(),
                hydraulicsDirtyService: null));
            services.AddSingleton<IProjectSession>(sp => sp.GetRequiredService<ProjectSession>());

            // Phase 9: forwarding aliases IProjectInfoService/IProjectStateService removed —
            // consumers depend on IProjectSession. IMarkDirtyService is retained as the
            // internal session dirty seam (ProjectSession : IMarkDirtyService): module
            // adapters and the thermal coordinator receive the session itself, which is
            // the canonical dirty owner.
            services.AddSingleton<IMarkDirtyService>(sp => sp.GetRequiredService<ProjectSession>());
            services.AddSingleton<IProjectSessionConstructionState>(sp => (IProjectSessionConstructionState)sp.GetRequiredService<ProjectSession>().ConstructionState);
            services.AddSingleton<ConstructionDefaultStateInitializer>();
            services.AddSingleton<IPdfExportService, PdfExportService>();
            services.AddSingleton<ICalculationReportDataBuilder, CalculationReportDataBuilder>();
            services.AddSingleton<ICalculationReportMarkdownRenderer, CalculationReportMarkdownRenderer>();
            services.AddSingleton<IThermalReportDataProvider, ThermalReportDataProvider>();
            services.AddSingleton<ICalculationReportExportService, CalculationReportExportService>();
            // Мини-фаза PDF-PZ: PDF-рендер и экспорт пояснительной записки.
            // Бутстрапп шрифтов вызывается при построении контейнера — до
            // первого рендера любого PDF: если краткий PDF отрендерится
            // раньше, глобальный резолвер Inter установить уже не удастся
            // (PDFsharp меняет его только до первой шрифтовой операции).
            CalculationReportPdfFontBootstrapper.EnsureInitialized();
            services.AddSingleton<ICalculationReportPdfRenderer, CalculationReportPdfRenderer>();
            services.AddSingleton<ICalculationReportPdfExportService, CalculationReportPdfExportService>();
            services.AddSingleton<IProjectFileService, ProjectFileService>();
            services.AddSingleton<IProjectDisplayModeState, ProjectDisplayModeState>();
            services.AddSingleton<IProjectSnapshotPersistenceInputs, ProjectSnapshotPersistenceInputs>();
            services.AddSingleton<IProjectSnapshotFactory, ProjectSnapshotFactory>();
            services.AddSingleton<IProjectSaveService, ProjectSaveService>();
            services.AddSingleton<IConstructionVisualizationImageService, ConstructionVisualizationImageService>();
            // Phase 9 (INV-008): application-сервисы зависят от adapter-интерфейсов;
            // DI связывает интерфейсы с теми же singleton-экземплярами адаптеров модулей.
            services.AddSingleton<IProjectLoadClimateAdapter>(sp => sp.GetRequiredService<ClimateViewModel>());
            services.AddSingleton<IProjectLoadConstructionAdapter>(sp => sp.GetRequiredService<ConstructionViewModel>());
            services.AddSingleton<IProjectLoadThermalAdapter>(sp => sp.GetRequiredService<ThermalViewModel>());
            services.AddSingleton<IProjectLoadHydraulicsAdapter>(sp => sp.GetRequiredService<CircuitsViewModel>());
            services.AddSingleton<IReportConstructionLayerSource>(sp => sp.GetRequiredService<ConstructionViewModel>());
            services.AddSingleton<IReportCollectorDataSource>(sp => sp.GetRequiredService<CircuitsViewModel>());
            services.AddSingleton<ProjectLoadOrchestrator>();
            services.AddSingleton<ResultsPdfDataBuilder>();
            services.AddSingleton<HydraulicSummaryBuilder>();
            services.AddSingleton<ResultsKpiPresenter>();

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
