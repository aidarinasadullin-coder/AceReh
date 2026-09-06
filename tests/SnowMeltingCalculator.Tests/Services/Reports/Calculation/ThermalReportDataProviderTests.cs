using System;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Tests.Construction;

namespace SnowMeltingCalculator.Tests.Services.Reports.Calculation
{
    /// <summary>
    /// T2-02…T2-07: поставщик детальных тепловых величин отчёта (ADR-010).
    /// Проверяют источник (снимок/контрольный пересчёт), предикат stale,
    /// сверку с сохранёнными полями и невалидный пересчёт; контрольный
    /// пересчёт не пишет в канонику и не создаёт dirty.
    /// </summary>
    [TestFixture]
    public class ThermalReportDataProviderTests
    {
        private ProjectSession _session = null!;
        private ThermalReportDataProvider _provider = null!;

        [SetUp]
        public async Task SetUpAsync()
        {
            _session = new ProjectSession();
            _provider = new ThermalReportDataProvider(
                _session,
                new ThermalCalculator());

            // Реалистичная заводская конструкция (бетон 100 / ЭППС 80 / основание):
            // без слоёв контрольный пересчёт честно даёт невалидный результат,
            // что отдельными тестами не покрывается как missing-data.
            var materialRepository = new MockMaterialRepository();
            await materialRepository.LoadMaterialsAsync();
            var constructionState = (ProjectSessionConstructionState)_session.ConstructionState;
            new ConstructionDefaultStateInitializer(materialRepository, constructionState)
                .Apply(ConstructionMutationOrigin.Reset);
        }

        private static ThermalResultSnapshot MakeResult(
            double alpha = 14.0,
            double powerUp = 330.0,
            double powerTotal = 335.0,
            bool fullDetail = true)
        {
            return new ThermalResultSnapshot(
                alpha: fullDetail ? alpha : 0.0,
                powerUp: powerUp,
                powerDown: 5.0,
                powerTotal: powerTotal,
                meltingHeat: fullDetail ? 48.0 : 0.0,
                radiationHeat: fullDetail ? 320.0 : 0.0,
                convectionHeat: fullDetail ? 282.0 : 0.0,
                excessTemperature: fullDetail ? 60.0 : 0.0,
                meanTemperature: 45.0,
                supplyTemperature: 53.0,
                returnTemperature: 37.4,
                deltaT: 15.6,
                rFb: fullDetail ? 0.128 : 0.0,
                rD: fullDetail ? 5.6 : 0.0,
                parameterM: fullDetail ? 9.0 : 0.0,
                efficiencyEtaR: fullDetail ? 0.79 : 0.0,
                massFlowRate: fullDetail ? 22.0 : 0.0,
                volumeFlowRate: fullDetail ? 21.6 : 0.0,
                isValid: true,
                validationErrors: null);
        }

        private static ThermalInputsSnapshot MakeInputs(
            double supplyTemperature = 53.0)
        {
            return new ThermalInputsSnapshot(
                OperatingMode.Melting,
                supplyTemperature,
                10.0,
                null,
                200);
        }

        [Test]
        public void Provide_FullSnapshot_ReturnsSnapshotWithoutRecalculation()
        {
            // T2-02: полная каноника — пересчёт не выполняется.
            var restore = _session.ThermalState.Restore(MakeInputs(), MakeResult());
            Assert.That(restore.Status, Is.EqualTo(ThermalMutationStatus.Changed));

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(ThermalReportDetailSource.Snapshot));
                Assert.That(detail.HasValues, Is.True);
                Assert.That(detail.Alpha, Is.EqualTo(14.0));
                Assert.That(detail.VolumeFlowRate, Is.EqualTo(21.6));
                Assert.That(detail.IsStale, Is.False);
            });
        }

        [Test]
        public void Provide_ZeroDetail_RunsExactlyOneRecalculation_WithoutCanonicalWrite()
        {
            // T2-03: нулевая каноника (старый файл) — ровно один контрольный
            // пересчёт; каноника не изменена, dirty нет.
            var restore = _session.ThermalState.Restore(MakeInputs(), MakeResult(fullDetail: false));
            Assert.That(restore.Status, Is.EqualTo(ThermalMutationStatus.Changed));

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(ThermalReportDetailSource.Recalculated));
                Assert.That(detail.HasValues, Is.True);
                Assert.That(detail.Alpha, Is.GreaterThan(0.0));
                Assert.That(detail.RFb, Is.GreaterThan(0.0));
                Assert.That(detail.VolumeFlowRate, Is.GreaterThan(0.0));
                Assert.That(detail.Note, Does.Contain("контрольным пересчётом").IgnoreCase);
            });

            // Канонический результат (8 wire-полей) не подменён пересчётом.
            Assert.That(_session.ThermalState.Snapshot.Result, Is.Not.Null);
            Assert.That(_session.ThermalState.Snapshot.Result!.PowerUp, Is.EqualTo(330.0));
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void Provide_Recalculation_CompareWithSavedPowers_ProducesMismatchNote()
        {
            // T2-04: сохранённые мощности отличаются от пересчитанных — примечание.
            _session.ThermalState.Restore(MakeInputs(), MakeResult(fullDetail: false, powerUp: 999.0, powerTotal: 1004.0));

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(ThermalReportDetailSource.Recalculated));
                Assert.That(detail.Note, Does.Contain("отличаются от пересчитанных"));
            });
        }

        [Test]
        public void Provide_StaleSavedResult_ReturnsSnapshotWithStale()
        {
            // T2-05: результат сохранён, входы изменились (NeedsRecalculation) —
            // источник снимок + REPORT_INPUTS_STALE.
            _session.ThermalState.Restore(MakeInputs(), MakeResult());
            _session.ThermalState.ApplyNeedsRecalculation("Изменены данные", ThermalMutationOrigin.User);

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(ThermalReportDetailSource.Snapshot));
                Assert.That(detail.IsStale, Is.True);
            });
        }

        [Test]
        public void Provide_UpstreamInvalidation_ZeroedResult_RecalculationExcludesStale()
        {
            // T2-05 (приоритет правил): upstream-инвалидация обнулила результат —
            // успешный пересчёт по текущим входам не даёт REPORT_INPUTS_STALE.
            _session.ThermalState.Restore(MakeInputs(), MakeResult());
            _session.ThermalState.InvalidateFromClimate("Изменён климат");

            Assert.That(_session.ThermalState.Snapshot.Result, Is.Null);

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(ThermalReportDetailSource.Recalculated));
                Assert.That(detail.IsStale, Is.False);
            });
        }

        [Test]
        public void Provide_EmptyConstruction_InvalidRecalculation_ReturnsInvalidSource()
        {
            // T2-07: пересчёт не даёт валидного результата — пустая конструкция
            // (без слоёв, R2=0) даёт отрицательную мощность вниз. Restore
            // невалидные входы отклоняет, поэтому сценарий создаётся пустой
            // сессией. Источник RecalculationInvalid + ошибки валидации.
            var emptySession = new ProjectSession();
            var provider = new ThermalReportDataProvider(
                emptySession,
                new ThermalCalculator());

            var detail = provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(ThermalReportDetailSource.RecalculationInvalid));
                Assert.That(detail.HasValues, Is.False);
                Assert.That(detail.ValidationErrors, Is.Not.Empty);
            });
        }

        [Test]
        public void Builder_WithStaleDetail_ProducesReportInputsStaleWarning()
        {
            // Проводка предупреждений в билдер отчёта.
            var detail = new ThermalReportDetail { Source = ThermalReportDetailSource.Snapshot, IsStale = true };
            var builder = new CalculationReportDataBuilder();

            var data = builder.Build(new ProjectData(), CalculationReportMode.Operating, thermalDetail: detail);

            Assert.That(data.Warnings.Any(w => w.Code == "REPORT_INPUTS_STALE"), Is.True);
        }

        [Test]
        public void Builder_WithInvalidDetail_ProducesMissingThermalDetailWarning()
        {
            var detail = new ThermalReportDetail
            {
                Source = ThermalReportDetailSource.RecalculationInvalid,
                ValidationErrors = new[] { "Подача ниже средней температуры" }
            };
            var builder = new CalculationReportDataBuilder();

            var data = builder.Build(new ProjectData(), CalculationReportMode.Operating, thermalDetail: detail);

            var warning = data.Warnings.FirstOrDefault(w => w.Code == "MISSING_THERMAL_DETAIL");
            Assert.Multiple(() =>
            {
                Assert.That(warning, Is.Not.Null);
                Assert.That(warning!.Severity, Is.EqualTo("Error"));
                Assert.That(warning.RelatedValues, Does.Contain("Подача ниже средней температуры"));
            });
        }
    }
}
