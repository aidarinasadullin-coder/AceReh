using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Application-owned поверхности restore-адаптеров (Phase 9, INV-008):
    /// <see cref="ProjectLoadOrchestrator"/> зависит только от этих интерфейсов,
    /// а не от конкретных module-ViewModel. Реализации — singleton-адаптеры
    /// модулей; DI связывает интерфейс с тем же экземпляром.
    /// </summary>
    /// <remarks>
    /// Интерфейсы принадлежат application-слою (Services.Project) и описывают
    /// ровно те операции, которые требует координация restore/reset. Ни один
    /// член не раскрывает WPF-типы кроме read-only коллекций-каталогов.
    /// </remarks>
    public interface IProjectLoadClimateAdapter
    {
        /// <summary>Поисковый фильтр списка городов (view-side состояние адаптера).</summary>
        string SearchQuery { get; set; }

        /// <summary>Найти город каталога по имени (read-only поиск).</summary>
        CityInfo? FindCityByName(string cityName);
    }

    public interface IProjectLoadConstructionAdapter
    {
        /// <summary>Каталог материалов (read-only).</summary>
        ObservableCollection<Material> AvailableMaterials { get; }

        /// <summary>Зеркалировать канонический snapshot в адаптер модуля (без канонических мутаций).</summary>
        void ApplyLifecycleSnapshotToAdapter(ConstructionStateSnapshot snapshot);
    }

    public interface IProjectLoadThermalAdapter
    {
        /// <summary>Каталог стандартных труб (read-only).</summary>
        ObservableCollection<PipeType> AvailablePipes { get; }

        OperatingMode SelectedMode { get; set; }
        double SupplyTemperature { get; set; }
        double GroundTemperature { get; set; }
        PipeType? SelectedPipe { get; set; }
        int PipeSpacing { get; set; }

        /// <summary>Сбросить адаптер к дефолтам (без канонических мутаций).</summary>
        void Reset();

        /// <summary>Опубликовать сохранённый результат файла через адаптер (restore-time writer).</summary>
        void LoadResult(ThermalCalculationResult result, ThermalInputs? inputs = null);

        /// <summary>Ровно один fallback-расчёт из восстановленных входов (Phase 7 finalize).</summary>
        Task CalculateFromRestoreAsync();
    }

    public interface IProjectLoadHydraulicsAdapter
    {
        /// <summary>Сбросить адаптер к дефолтам (без канонических мутаций).</summary>
        void Reset();

        /// <summary>Зеркалировать канонический snapshot в адаптер модуля (без канонических мутаций).</summary>
        void ApplyLifecycleSnapshotToAdapter(HydraulicsStateSnapshot snapshot);
    }
}
