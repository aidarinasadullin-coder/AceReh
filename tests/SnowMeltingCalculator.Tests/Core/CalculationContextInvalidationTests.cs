using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Регрессионные тесты контракта инвалидации результатов в CalculationContext.
    /// Правило: изменение любых входных данных сбрасывает все downstream-результаты,
    /// чтобы потребители не показывали stale-данные (см. отчёт аудита: асимметрия
    /// UpdateThermalInputs, которая не сбрасывала HydraulicsResults).
    /// </summary>
    [TestFixture]
    public class CalculationContextInvalidationTests
    {
        private CalculationContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _context = new CalculationContext();
        }

        private static ThermalCalculationResult ValidThermalResult() =>
            new ThermalCalculationResult { IsValid = true };

        private static List<CollectorSummary> ValidHydraulicsResults() =>
            new List<CollectorSummary> { new CollectorSummary { IsValid = true } };

        /// <summary>
        /// Заполняет контекст «полным циклом»: климат -> конструкция -> теплота -> гидравлика.
        /// </summary>
        private void SeedFullCalculation()
        {
            var climate = new Mock<IClimateData>();
            climate.SetupGet(c => c.SelectedCity).Returns("Москва");

            var construction = new Mock<IConstructionData>();

            _context.UpdateClimate(climate.Object);
            _context.UpdateConstruction(construction.Object);
            _context.UpdateThermalInputs(new ThermalInputs());
            _context.UpdateThermal(ValidThermalResult());
            _context.UpdateHydraulics(ValidHydraulicsResults());

            Assert.That(_context.ThermalResult, Is.Not.Null, "Precondition: тепловой результат должен быть");
            Assert.That(_context.HydraulicsResults, Is.Not.Null, "Precondition: гидравлический результат должен быть");
        }

        [Test]
        public void UpdateClimate_ResetsThermalAndHydraulicsResults()
        {
            SeedFullCalculation();

            var climate = new Mock<IClimateData>();
            climate.SetupGet(c => c.SelectedCity).Returns("Сочи");
            _context.UpdateClimate(climate.Object);

            Assert.That(_context.ThermalResult, Is.Null, "Смена климата должна сбрасывать тепловой результат");
            Assert.That(_context.HydraulicsResults, Is.Null, "Смена климата должна сбрасывать гидравлические результаты");
            Assert.That(_context.IsHydraulicsValid, Is.False);
        }

        [Test]
        public void UpdateConstruction_ResetsThermalAndHydraulicsResults()
        {
            SeedFullCalculation();

            _context.UpdateConstruction(new Mock<IConstructionData>().Object);

            Assert.That(_context.ThermalResult, Is.Null, "Смена конструкции должна сбрасывать тепловой результат");
            Assert.That(_context.HydraulicsResults, Is.Null, "Смена конструкции должна сбрасывать гидравлические результаты");
            Assert.That(_context.IsHydraulicsValid, Is.False);
        }

        [Test]
        public void UpdateThermal_ResetsHydraulicsResults()
        {
            SeedFullCalculation();

            _context.UpdateThermal(ValidThermalResult());

            Assert.That(_context.ThermalResult, Is.Not.Null);
            Assert.That(_context.HydraulicsResults, Is.Null, "Новый тепловой расчёт должен инвалидировать гидравлику");
            Assert.That(_context.IsHydraulicsValid, Is.False);
        }

        [Test]
        public void UpdateThermalInputs_DoesNotLeaveStaleHydraulicsResults()
        {
            SeedFullCalculation();

            // Изменение тепловых входов (например, другой шаг укладки или труба)
            // делает прежние гидравлические результаты stale: они рассчитаны
            // от старых тепловых данных. Контракт: контекст обязан их сбросить.
            _context.UpdateThermalInputs(new ThermalInputs());

            Assert.That(_context.HydraulicsResults, Is.Null,
                "Изменение ThermalInputs не должно оставлять stale HydraulicsResults");
            Assert.That(_context.IsHydraulicsValid, Is.False);
        }

        [Test]
        public void Reset_ClearsAllDataAndResults()
        {
            SeedFullCalculation();

            _context.Reset();

            Assert.That(_context.Climate, Is.Null);
            Assert.That(_context.Construction, Is.Null);
            Assert.That(_context.ThermalResult, Is.Null);
            Assert.That(_context.HydraulicsResults, Is.Null);
            Assert.That(_context.Hydraulics, Is.Null);
            Assert.That(_context.State, Is.EqualTo(CalculationState.NotInitialized));
            Assert.That(_context.ErrorMessage, Is.Empty);
        }

        [Test]
        public void Reset_RaisesSingleContextChangedEvent()
        {
            SeedFullCalculation();

            var events = new List<ContextChangedEventArgs>();
            _context.ContextChanged += (_, args) => events.Add(args);

            _context.Reset();

            Assert.That(events.Count, Is.EqualTo(1), "Reset должен присылать одно суммарное событие");
            Assert.That(events[0].PropertyName, Is.EqualTo(nameof(CalculationContext.Reset)));
        }

        [Test]
        public void UpdateThermalInputs_RaisesContextChangedEvent()
        {
            var events = new List<ContextChangedEventArgs>();
            _context.ContextChanged += (_, args) => events.Add(args);

            _context.UpdateThermalInputs(new ThermalInputs(), "Test");

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].PropertyName, Is.EqualTo(nameof(CalculationContext.ThermalInputs)));
            Assert.That(events[0].Source, Is.EqualTo("Test"));
        }
    }
}
