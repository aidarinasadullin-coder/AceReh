using System;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Предикат HydraulicsStateSnapshot.IsCalculated (ADR-012): «расчёт
    /// выполнен для текущих данных» = есть контуры с длиной &gt; 0 и у всех
    /// коллекторов посчитан Summary. Общий гейт вкладки 4 и вкладки 5.
    /// </summary>
    [TestFixture]
    public sealed class HydraulicsStateSnapshotPredicateTests
    {
        private static HydraulicCollectorSnapshot Collector(double circuitLength, HydraulicCollectorSummarySnapshot? summary) =>
            new(1, "HKV-D", ValveType.HKV_D,
                new[] { new HydraulicCircuitSnapshot(1, circuitLength, 10, 5, 10, 20) },
                summary);

        private static HydraulicCollectorSummarySnapshot Summary() => new(1, 110, 10, 20, 30, 40, 1.2, "HKV-D");

        private static HydraulicsStateSnapshot Snapshot(params HydraulicCollectorSnapshot[] collectors) =>
            new(HydraulicGlobalInputsSnapshot.Default, collectors, HydraulicsStatusSnapshot.Default);

        [Test]
        public void EmptySnapshot_IsNotCalculated()
        {
            Assert.That(HydraulicsStateSnapshot.Default.IsCalculated(), Is.False,
                "пустая гидравлика не «рассчитана»");
        }

        [Test]
        public void ZeroLengthCircuits_AreNotCalculated()
        {
            var snapshot = Snapshot(Collector(0, Summary()));

            Assert.That(snapshot.IsCalculated(), Is.False,
                "контуры без длины — расчёт бессмыслен, вкладка серая");
        }

        [Test]
        public void MissingSummary_IsNotCalculated()
        {
            var snapshot = Snapshot(Collector(110, null));

            Assert.That(snapshot.IsCalculated(), Is.False,
                "длины введены, но расчёт не выполнен — серый");
        }

        [Test]
        public void PartiallyCalculatedCollectors_AreNotCalculated()
        {
            var snapshot = Snapshot(Collector(110, Summary()), Collector(110, null));

            Assert.That(snapshot.IsCalculated(), Is.False,
                "частичный расчёт (не у всех коллекторов Summary) — не «рассчитано»");
        }

        [Test]
        public void LengthsWithSummaries_AreCalculated()
        {
            var snapshot = Snapshot(Collector(110, Summary()));

            Assert.That(snapshot.IsCalculated(), Is.True,
                "длины > 0 и у всех коллекторов Summary — «рассчитано»");
        }
    }
}
