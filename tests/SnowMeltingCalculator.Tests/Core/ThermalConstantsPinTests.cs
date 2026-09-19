using NUnit.Framework;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Thermal;
using System.Linq;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Пин физических констант теплового расчёта (T2-01, ADR-010; ADR-016).
    /// R1 переключил <see cref="SnowMeltingCalculator.Services.Thermal.ThermalCalculator"/>
    /// с приватных литералов на <see cref="ThermalConstants"/>; значения пинятся
    /// ровно в прежнем виде — любое изменение должно быть явным решением
    /// владельца с пересмотром эталонной сверки отчёта.
    /// </summary>
    /// <remarks>
    /// Ложные константы SurfaceTempMelting/SurfaceTempPrevention/SurfaceTempAntiIce
    /// (2/0/−2) удалены в ADR-016 как никогда не соответствовавшие расчёту:
    /// температура поверхности — единственная карта
    /// <see cref="SnowMeltingCalculator.Models.Thermal.OperatingModeSurfaceTemperature"/>,
    /// значения (int)OperatingMode = 3/5/7 (+ ручные 1/2/4/6).
    /// </remarks>
    [TestFixture]
    public class ThermalConstantsPinTests
    {
        [Test]
        public void ThermalBlock_Values_MatchFormerThermalCalculatorLiterals()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ThermalConstants.SnowDensity, Is.EqualTo(900.0));
                Assert.That(ThermalConstants.IceHeatCapacity, Is.EqualTo(2100.0));
                Assert.That(ThermalConstants.IceMeltingHeat, Is.EqualTo(330000.0));
                Assert.That(ThermalConstants.WaterHeatCapacity, Is.EqualTo(4200.0));
                Assert.That(ThermalConstants.StefanBoltzmann, Is.EqualTo(5.77e-8));
                Assert.That(ThermalConstants.EmissionCoefficient, Is.EqualTo(0.055));
                Assert.That(ThermalConstants.AlphaBottom, Is.EqualTo(999999999.0));
                Assert.That(ThermalConstants.RodCoefficient, Is.EqualTo(0.6));
            });
        }

        [Test]
        public void OperatingModeValues_AreTheCalculatorSurfaceTemperatures()
        {
            // Фактические температуры поверхности калькулятора: карта
            // ToSurfaceTemperature (ADR-016), т.е. (int)OperatingMode.
            Assert.Multiple(() =>
            {
                Assert.That((int)OperatingMode.Melting, Is.EqualTo(5));
                Assert.That((int)OperatingMode.AntiIcing, Is.EqualTo(3));
                Assert.That((int)OperatingMode.Intensive, Is.EqualTo(7));
            });
        }

        /// <summary>
        /// План 2026-09-13 (вариант B): множество допустимых t_пов = целые
        /// 1..7 (пресеты 3/5/7 + ручные Manual1/2/4/6). Пин — любое
        /// расширение/сужение диапазона должно быть явным решением владельца.
        /// </summary>
        [Test]
        public void OperatingMode_DefinedValues_AreExactlyIntegersOneToSeven()
        {
            var defined = Enum.GetValues<OperatingMode>()
                .Select(m => (int)m)
                .OrderBy(v => v)
                .ToArray();

            Assert.That(defined, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(ValidationConstants.MinSurfaceTemperature, Is.EqualTo(1));
            Assert.That(ValidationConstants.MaxSurfaceTemperature, Is.EqualTo(7));
        }
    }
}
