using System.Collections.ObjectModel;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Application-owned поверхности источников данных отчёта (Phase 9, INV-008):
    /// <see cref="ResultsPdfDataBuilder"/> зависит только от этих интерфейсов,
    /// а не от конкретных module-ViewModel. DI связывает интерфейс с тем же
    /// singleton-экземпляром адаптера модуля, поэтому содержимое отчёта
    /// байт-идентично прежнему (читаются те же объекты).
    /// </summary>
    public interface IReportConstructionLayerSource
    {
        /// <summary>Слои над трубой для схемы конструкции.</summary>
        ObservableCollection<Layer> LayersAbovePipe { get; }

        /// <summary>Слои под трубой для схемы конструкции.</summary>
        ObservableCollection<Layer> LayersBelowPipe { get; }
    }

    public interface IReportCollectorDataSource
    {
        /// <summary>Коллекторы модуля с результатами расчёта.</summary>
        ObservableCollection<CollectorData>? Collectors { get; }
    }
}
