using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Поставщик детальных тепловых величин отчёта (ADR-010): канонический
    /// снимок <c>ProjectSession.ThermalState</c> как источник, fallback —
    /// ровно один контрольный пересчёт существующим <see cref="ThermalCalculator"/>
    /// по текущим входам проекта. Результат пересчёта в канонику не пишется,
    /// dirty не создаётся, события не публикуются.
    /// </summary>
    /// <remarks>
    /// Сборка входов пересчёта воспроизводит <c>ThermalViewModel.BuildThermalInputs</c>
    /// один-в-один: Mode/Supply/Ground/Pipe/PipeSpacing из
    /// <see cref="ThermalStateSnapshot.Inputs"/>, LambdaE — из канонической
    /// проекции конструкции; плотность/теплоёмкость теплоносителя — дефолты
    /// <see cref="ThermalInputs"/> (в пайплайне эти поля не настраиваются и
    /// не сохраняются), иначе пересчёт разошёлся бы с фактическим расчётом.
    /// </remarks>
    public sealed class ThermalReportDataProvider : IThermalReportDataProvider
    {
        /// <summary>Допуск сверки мощностей с сохранёнными полями (Вт/м²) — точность округления отображения.</summary>
        public const double PowerMismatchTolerance = 0.1;

        private readonly IProjectSession _projectSession;
        private readonly IThermalCalculator _calculator;

        public ThermalReportDataProvider(
            IProjectSession projectSession,
            IThermalCalculator calculator)
        {
            _projectSession = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        /// <inheritdoc />
        public ThermalReportDetail Provide()
        {
            var snapshot = _projectSession.ThermalState.Snapshot;
            var result = snapshot.Result;

            if (HasStoredDetail(result))
            {
                var stale = snapshot.Status.Phase == ThermalCalculationPhase.NeedsRecalculation;
                return FromSnapshot(result!, stale);
            }

            return Recalculate(snapshot);
        }

        /// <summary>
        /// Runtime-поля считаются отсутствующими, только когда равны нулю все
        /// ключевые производные величины: α, RFb, m, JHmü, объёмный расход.
        /// Для любого валидного расчёта они строго положительны, поэтому
        /// нулевой набор однозначно identifies файл, сохранённый до DEC-T08-расширения.
        /// </summary>
        private static bool HasStoredDetail(ThermalResultSnapshot? result)
        {
            return result is not null
                && (result.Alpha != 0.0
                    || result.RFb != 0.0
                    || result.ParameterM != 0.0
                    || result.ExcessTemperature != 0.0
                    || result.VolumeFlowRate != 0.0);
        }

        private static ThermalReportDetail FromSnapshot(ThermalResultSnapshot result, bool isStale)
        {
            return new ThermalReportDetail
            {
                Source = ThermalReportDetailSource.Snapshot,
                IsStale = isStale,
                Alpha = result.Alpha,
                MeltingHeat = result.MeltingHeat,
                RadiationHeat = result.RadiationHeat,
                ConvectionHeat = result.ConvectionHeat,
                ExcessTemperature = result.ExcessTemperature,
                RFb = result.RFb,
                RD = result.RD,
                ParameterM = result.ParameterM,
                EfficiencyEtaR = result.EfficiencyEtaR,
                MassFlowRate = result.MassFlowRate,
                VolumeFlowRate = result.VolumeFlowRate,
                ValidationErrors = result.ValidationErrors
            };
        }

        private ThermalReportDetail Recalculate(ThermalStateSnapshot snapshot)
        {
            var savedResult = snapshot.Result;
            var inputsSnapshot = snapshot.Inputs;

            var inputs = new ThermalInputs
            {
                Mode = inputsSnapshot.Mode,
                SupplyTemperature = inputsSnapshot.SupplyTemperature,
                GroundTemperature = inputsSnapshot.GroundTemperature,
                Pipe = inputsSnapshot.Pipe?.ToPipeType() ?? PipeType.StandardPipes[1],
                PipeSpacing = inputsSnapshot.PipeSpacing,
                LambdaE = _projectSession.ConstructionState.CurrentProjection.LambdaE
            };

            ThermalCalculationResult recalc;
            try
            {
                recalc = _calculator.Calculate(
                    inputs,
                    _projectSession.ClimateState.Snapshot,
                    _projectSession.ConstructionState.CurrentProjection);
            }
            catch (Exception ex)
            {
                return new ThermalReportDetail
                {
                    Source = ThermalReportDetailSource.RecalculationInvalid,
                    ValidationErrors = new[] { $"Ошибка контрольного пересчёта: {ex.Message}" }
                };
            }

            if (!recalc.IsValid)
            {
                return new ThermalReportDetail
                {
                    Source = ThermalReportDetailSource.RecalculationInvalid,
                    ValidationErrors = recalc.ValidationErrors
                };
            }

            return new ThermalReportDetail
            {
                Source = ThermalReportDetailSource.Recalculated,
                Note = BuildRecalculationNote(recalc, savedResult),
                Alpha = recalc.Alpha,
                MeltingHeat = recalc.MeltingHeat,
                RadiationHeat = recalc.RadiationHeat,
                ConvectionHeat = recalc.ConvectionHeat,
                ExcessTemperature = recalc.ExcessTemperature,
                RFb = recalc.RFb,
                RD = recalc.RD,
                ParameterM = recalc.ParameterM,
                EfficiencyEtaR = recalc.EfficiencyEtaR,
                MassFlowRate = recalc.MassFlowRate,
                VolumeFlowRate = recalc.VolumeFlowRate,
                ValidationErrors = recalc.ValidationErrors
            };
        }

        private static string BuildRecalculationNote(
            ThermalCalculationResult recalc,
            ThermalResultSnapshot? savedResult)
        {
            var culture = AppCulture.Culture;
            var notes = new List<string>
            {
                "Значения теплового раздела получены контрольным пересчётом по текущим входам проекта: файл сохранён без детальных тепловых величин.",
                "Расходы пересчёта определены при дефолтном теплоносителе (1053 кг/м³; 3,39 кДж/(кг·К)) — как в расчётном пайплайне программы."
            };

            if (savedResult is not null
                && (Math.Abs(recalc.PowerUp - savedResult.PowerUp) > PowerMismatchTolerance
                    || Math.Abs(recalc.PowerTotal - savedResult.PowerTotal) > PowerMismatchTolerance))
            {
                notes.Add(string.Format(
                    culture,
                    "Сохранённые мощности ({0:F1}/{1:F1} Вт/м²) отличаются от пересчитанных ({2:F1}/{3:F1} Вт/м²) — проект сохранён предыдущей версией методики.",
                    savedResult.PowerUp,
                    savedResult.PowerTotal,
                    recalc.PowerUp,
                    recalc.PowerTotal));
            }

            return string.Join(" ", notes);
        }
    }
}
