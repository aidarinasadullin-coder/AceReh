using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class ValveTypeTests
    {
        [Test]
        public void ValveType_HasCorrectValues()
        {
            // Assert
            Assert.That((int)ValveType.HKV_D, Is.EqualTo(0));
            Assert.That((int)ValveType.IV_1_25, Is.EqualTo(1));
            Assert.That((int)ValveType.IV_1_5, Is.EqualTo(2));
        }
        
        [Test]
        public void ValveType_HasThreeValues()
        {
            // Assert
            var values = Enum.GetValues<ValveType>();
            Assert.That(values.Length, Is.EqualTo(3));
        }
        
        [Test]
        public void ValveType_NamesAreCorrect()
        {
            // Assert
            Assert.That(ValveType.HKV_D.ToString(), Is.EqualTo("HKV_D"));
            Assert.That(ValveType.IV_1_25.ToString(), Is.EqualTo("IV_1_25"));
            Assert.That(ValveType.IV_1_5.ToString(), Is.EqualTo("IV_1_5"));
        }
    }
}