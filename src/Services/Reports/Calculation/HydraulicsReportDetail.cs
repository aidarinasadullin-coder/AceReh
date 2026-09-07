using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>Источник величин раздела «Свойства теплоносителя».</summary>
    public enum HydraulicsReportDetailSource
    {
        /// <summary>Свойства из канонического снимка гидравлики (ADR-013).</summary>
        Snapshot,

        /// <summary>Снимок пуст (файл старой версии, wire .smc не хранит
        /// свойства) — контрольная интерполяция по входам каноники (В13).</summary>
        ControlInterpolation,

        /// <summary>Контрольная интерполяция невозможна (вход вне диапазона
        /// базы) — значения выводятся «нет данных» (В2) + примечание.</summary>
        Unavailable
    }

    /// <summary>
    /// Детальные величины гидравлики для ПЗ (по образцу
    /// <see cref="ThermalReportDetail"/>): свойства теплоносителя Operating
    /// и Design + источник и примечание.
    /// </summary>
    public sealed class HydraulicsReportDetail
    {
        public HydraulicsReportDetailSource Source { get; init; }
        public GlycolPropertiesSnapshot? Operating { get; init; }
        public GlycolPropertiesSnapshot? Design { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>Поставщик детальных величин гидравлики (ADR-013, В13):
    /// канонический снимок <c>ProjectSession.HydraulicsState</c> как источник;
    /// fallback — контрольная интерполяция <see cref="IGlycolDataService.GetProperties"/>
    /// по входам каноники (ровно один вызов на режим), результат в канонику
    /// не пишется, dirty не создаётся.</summary>
    public interface IHydraulicsReportDataProvider
    {
        HydraulicsReportDetail Provide();
    }

    public sealed class HydraulicsReportDataProvider : IHydraulicsReportDataProvider
    {
        private readonly IProjectSession _projectSession;
        private readonly IGlycolDataService _glycolService;

        public HydraulicsReportDataProvider(IProjectSession projectSession, IGlycolDataService glycolService)
        {
            _projectSession = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
        }

        public HydraulicsReportDetail Provide()
        {
            var snapshot = _projectSession.HydraulicsState.Snapshot;
            if (snapshot.OperatingGlycolProperties is not null && snapshot.DesignGlycolProperties is not null)
            {
                return new HydraulicsReportDetail
                {
                    Source = HydraulicsReportDetailSource.Snapshot,
                    Operating = snapshot.OperatingGlycolProperties,
                    Design = snapshot.DesignGlycolProperties
                };
            }

            // Fallback (В13): те же входы, что у расчёта гидравлики —
            // тип/концентрация из канонического снимка, температуры из
            // канонического тепла и климата.
            var inputs = snapshot.GlobalInputs;
            var operatingTemperature = _projectSession.ThermalState.Snapshot.Result?.MeanTemperature ?? 0.0;
            var designTemperature = _projectSession.ClimateState.Snapshot.AirTemperature;

            var operating = Interpolate(inputs.GlycolType, inputs.GlycolConcentration, operatingTemperature);
            var design = Interpolate(inputs.GlycolType, inputs.GlycolConcentration, designTemperature);

            if (operating is null || design is null)
            {
                return new HydraulicsReportDetail
                {
                    Source = HydraulicsReportDetailSource.Unavailable,
                    Note = "Свойства теплоносителя недоступны: входы вне диапазона справочной базы гликолей."
                };
            }

            return new HydraulicsReportDetail
            {
                Source = HydraulicsReportDetailSource.ControlInterpolation,
                Operating = operating,
                Design = design,
                Note = "Свойства теплоносителя получены контрольной интерполяцией: файл сохранён без гидравлических свойств в снимке."
            };
        }

        private GlycolPropertiesSnapshot? Interpolate(GlycolType glycolType, double concentration, double temperature)
        {
            try
            {
                return GlycolPropertiesSnapshot.FromModel(_glycolService.GetProperties(glycolType, concentration, temperature));
            }
            catch (ArgumentOutOfRangeException)
            {
                return null; // В2: выход за диапазон базы — «нет данных» + примечание
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
