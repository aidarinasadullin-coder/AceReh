using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Интерфейс строителя данных детального расчётного отчёта.
    /// </summary>
    public interface ICalculationReportDataBuilder
    {
        /// <summary>
        /// Построить снимок детального отчёта по сохранённому проекту.
        /// </summary>
        /// <param name="project">Данные проекта.</param>
        /// <param name="mode">Режим отчёта.</param>
        /// <param name="reportDate">Дата формирования отчёта (опционально).</param>
        /// <returns>Модель данных отчёта.</returns>
        CalculationReportData Build(
            ProjectData project,
            CalculationReportMode mode,
            DateTime? reportDate = null);
    }
}
