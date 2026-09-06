using NUnit.Framework;
using SnowMeltingCalculator.Core.Constants;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Пин физических констант теплового расчёта (T2-01, ADR-010).
    /// R1 переключил <see cref="SnowMeltingCalculator.Services.Thermal.ThermalCalculator"/>
    /// с приватных литералов на <see cref="ThermalConstants"/>; значения пинятся
    /// ровно в прежнем виде — любое изменение должно быть явным решением
    /// владельца с пересмотром эталонной сверки отчёта.
    /// </summary>
    /// <remarks>
    /// <see cref="ThermalConstants.SurfaceTempMelting"/>,
    /// <see cref="ThermalConstants.SurfaceTempPrevention"/>,
    /// <see cref="ThermalConstants.SurfaceTempAntiIce"/> (2/0/−2) запрещены как
    /// источник температуры поверхности для отчёта: фактическая логика
    /// калькулятора — <c>(int)inputs.Mode</c> = 3/5/7. Пин ниже фиксирует
    /// расхождение, чтобы переключение на эти константы без исправления
    /// значений было невозможно незамеченным.
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
        public void SurfaceTemps_DifferFromCalculatorModeValues_MustNotBeUsedAsSurfaceTemp()
        {
            // Фактические температуры поверхности калькулятора: (int)OperatingMode.
            Assert.Multiple(() =>
            {
                Assert.That((int)Models.Thermal.OperatingMode.Melting, Is.EqualTo(5));
                Assert.That((int)Models.Thermal.OperatingMode.AntiIcing, Is.EqualTo(3));
                Assert.That((int)Models.Thermal.OperatingMode.Intensive, Is.EqualTo(7));

                Assert.That(ThermalConstants.SurfaceTempMelting, Is.Not.EqualTo((int)Models.Thermal.OperatingMode.Melting));
                Assert.That(ThermalConstants.SurfaceTempPrevention, Is.Not.EqualTo((int)Models.Thermal.OperatingMode.Melting));
                Assert.That(ThermalConstants.SurfaceTempAntiIce, Is.Not.EqualTo((int)Models.Thermal.OperatingMode.AntiIcing));
            });
        }
    }
}
