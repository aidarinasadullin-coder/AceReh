using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class HydraulicInputDataTests
    {
        [Test]
        public void DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var data = new HydraulicInputData();

            // Assert
            Assert.That(data.GlycolType, Is.EqualTo(GlycolType.Ethylene));
            Assert.That(data.GlycolConcentration, Is.EqualTo(50.0));
            Assert.That(data.SupplySpacing_cm, Is.EqualTo(5.0));
            Assert.That(data.SupplyHeatPercent, Is.EqualTo(10.0));
            Assert.That(data.ValveType, Is.EqualTo(ValveType.HKV_D));
        }
    }
}
