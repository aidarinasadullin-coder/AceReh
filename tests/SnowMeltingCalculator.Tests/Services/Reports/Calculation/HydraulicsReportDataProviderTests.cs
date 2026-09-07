using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Reports.Calculation;

namespace SnowMeltingCalculator.Tests.Services.Reports.Calculation
{
    /// <summary>
    /// Тесты провайдера детальных величин гидравлики (P4, ADR-013/В13):
    /// свойства из канонического снимка; старый файл (снимок пуст) —
    /// ровно один контрольный вызов GetProperties на режим по входам
    /// каноники; выход за диапазон базы — Unavailable (В2).
    /// </summary>
    [TestFixture]
    public class HydraulicsReportDataProviderTests
    {
        private Mock<IGlycolDataService> _glycolService = null!;
        private ProjectSession _session = null!;
        private HydraulicsReportDataProvider _provider = null!;

        private static readonly GlycolPropertiesSnapshot Operating =
            new(1053.0, 3.39, 4.5, 0.47, 38.0);
        private static readonly GlycolPropertiesSnapshot Design =
            new(1049.0, 3.41, 12.0, 0.45, 96.0);

        [SetUp]
        public void SetUp()
        {
            _glycolService = new Mock<IGlycolDataService>();
            _session = new ProjectSession();
            _provider = new HydraulicsReportDataProvider(_session, _glycolService.Object);
        }

        [Test]
        public void Provide_SnapshotHasProperties_ReturnsThemWithoutInterpolation()
        {
            _session.HydraulicsState.Restore(
                new HydraulicsStateSnapshot(
                    HydraulicGlobalInputsSnapshot.Default,
                    Array.Empty<HydraulicCollectorSnapshot>(),
                    HydraulicsStatusSnapshot.Default,
                    Operating,
                    Design),
                HydraulicsMutationOrigin.ProjectLoad);

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(HydraulicsReportDetailSource.Snapshot));
                Assert.That(detail.Operating, Is.EqualTo(Operating));
                Assert.That(detail.Design, Is.EqualTo(Design));
                Assert.That(detail.Note, Is.Null);
                _glycolService.Verify(s => s.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()), Times.Never,
                    "снимок пуст не был — контрольная интерполяция не выполняется");
            });
        }

        [Test]
        public void Provide_LegacyFile_ControlInterpolationByCanonicalInputs()
        {
            // Сценарий старого файла: wire .smc не хранит свойства (ADR-013) —
            // снимок с null; контрольная интерполяция по входам каноники.
            var inputs = new HydraulicGlobalInputsSnapshot(GlycolType.Ethylene, 50.0, 5.0, 10.0);
            _session.HydraulicsState.Restore(
                new HydraulicsStateSnapshot(inputs, Array.Empty<HydraulicCollectorSnapshot>(), HydraulicsStatusSnapshot.Default),
                HydraulicsMutationOrigin.ProjectLoad);
            var realService = new GlycolDataService();
            _glycolService
                .Setup(s => s.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns<GlycolType, double, double>((type, concentration, temperature) =>
                    realService.GetProperties(type, concentration, temperature));

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(HydraulicsReportDetailSource.ControlInterpolation));
                Assert.That(detail.Note, Does.Contain("контрольной интерполяцией"));
                // Пин: интерполяция == GetProperties(входы каноники) — те же
                // тип/концентрация и температуры, что у расчёта гидравлики.
                var expectedOperating = GlycolPropertiesSnapshot.FromModel(
                    realService.GetProperties(inputs.GlycolType, inputs.GlycolConcentration,
                        _session.ThermalState.Snapshot.Result?.MeanTemperature ?? 0.0));
                Assert.That(detail.Operating, Is.EqualTo(expectedOperating));
                _glycolService.Verify(
                    s => s.GetProperties(inputs.GlycolType, inputs.GlycolConcentration, It.IsAny<double>()),
                    Times.Exactly(2), "ровно один контрольный вызов на режим");
            });
        }

        [Test]
        public void Provide_InterpolationEqualsRealService_BitwiseOnCanonicalInputs()
        {
            var inputs = new HydraulicGlobalInputsSnapshot(GlycolType.Propylene, 40.0, 5.0, 10.0);
            _session.HydraulicsState.Restore(
                new HydraulicsStateSnapshot(inputs, Array.Empty<HydraulicCollectorSnapshot>(), HydraulicsStatusSnapshot.Default),
                HydraulicsMutationOrigin.ProjectLoad);
            var realService = new GlycolDataService();
            _glycolService
                .Setup(s => s.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns<GlycolType, double, double>((type, concentration, temperature) =>
                    realService.GetProperties(type, concentration, temperature));

            var detail = _provider.Provide();

            var operatingTemperature = _session.ThermalState.Snapshot.Result?.MeanTemperature ?? 0.0;
            var designTemperature = _session.ClimateState.Snapshot.AirTemperature;
            Assert.Multiple(() =>
            {
                Assert.That(detail.Operating, Is.EqualTo(
                    GlycolPropertiesSnapshot.FromModel(realService.GetProperties(inputs.GlycolType, inputs.GlycolConcentration, operatingTemperature))));
                Assert.That(detail.Design, Is.EqualTo(
                    GlycolPropertiesSnapshot.FromModel(realService.GetProperties(inputs.GlycolType, inputs.GlycolConcentration, designTemperature))));
            });
        }

        [Test]
        public void Provide_TemperatureOutOfRange_ReturnsUnavailableWithNote()
        {
            // В2: выход за диапазон базы — «нет данных» + предупреждение;
            // провайдер отдаёт Unavailable, значения не заполняются.
            var inputs = new HydraulicGlobalInputsSnapshot(GlycolType.Ethylene, 50.0, 5.0, 10.0);
            _session.HydraulicsState.Restore(
                new HydraulicsStateSnapshot(inputs, Array.Empty<HydraulicCollectorSnapshot>(), HydraulicsStatusSnapshot.Default),
                HydraulicsMutationOrigin.ProjectLoad);
            // T_холодного пуска = -35 °C — ниже MIN_TEMPERATURE базы гликолей
            // (-34,4), но в пределах валидации климата (-50..+10): контрольная
            // интерполяция невозможна (В2).
            Assert.That(
                _session.ClimateState.ApplyIndividualEdit(
                    new ClimateEdit(ClimateEditField.AirTemperature, -35.0), ClimateMutationOrigin.User).IsChanged,
                Is.True);

            var detail = _provider.Provide();

            Assert.Multiple(() =>
            {
                Assert.That(detail.Source, Is.EqualTo(HydraulicsReportDetailSource.Unavailable));
                Assert.That(detail.Note, Does.Contain("вне диапазона"));
                Assert.That(detail.Operating, Is.Null);
                Assert.That(detail.Design, Is.Null);
            });
        }
    }
}
