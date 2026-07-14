using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    /// <summary>
    /// Тесты для класса Collector
    /// </summary>
    [TestFixture]
    public class CollectorTests
    {
        #region Вычисляемые свойства

        [Test]
        public void IsResidential_ReturnsTrueForHKV()
        {
            // Arrange
            var collector = new Collector { Type = CollectorType.HKV };

            // Act & Assert
            Assert.That(collector.IsResidential, Is.True);
            Assert.That(collector.IsIndustrial, Is.False);
        }

        [Test]
        public void IsIndustrial_ReturnsTrueForIV()
        {
            // Arrange
            var collector = new Collector { Type = CollectorType.IV };

            // Act & Assert
            Assert.That(collector.IsIndustrial, Is.True);
            Assert.That(collector.IsResidential, Is.False);
        }

        [Test]
        public void MaxPressure_Pa_CalculatesCorrectly()
        {
            // Arrange
            var collector = new Collector { MaxPressure = 320 };

            // Act & Assert
            Assert.That(collector.MaxPressure_Pa, Is.EqualTo(32000));
        }

        [Test]
        public void MaxFlowRate_L_h_CalculatesCorrectly()
        {
            // Arrange
            var collector = new Collector { MaxFlowRate = 1.5 };

            // Act & Assert
            Assert.That(collector.MaxFlowRate_L_h, Is.EqualTo(1500));
        }

        [Test]
        public void MaxPressure_Pa_WithZeroValue_ReturnsZero()
        {
            // Arrange
            var collector = new Collector { MaxPressure = 0 };

            // Act & Assert
            Assert.That(collector.MaxPressure_Pa, Is.EqualTo(0));
        }

        [Test]
        public void MaxFlowRate_L_h_WithZeroValue_ReturnsZero()
        {
            // Arrange
            var collector = new Collector { MaxFlowRate = 0 };

            // Act & Assert
            Assert.That(collector.MaxFlowRate_L_h, Is.EqualTo(0));
        }

        #endregion

        #region IsSuitableForCircuits

        [Test]
        public void IsSuitableForCircuits_ReturnsTrueForValidCount()
        {
            // Arrange
            var collector = new Collector
            {
                Type = CollectorType.HKV,
                Circuits = 4
            };

            // Act & Assert
            Assert.That(collector.IsSuitableForCircuits(2), Is.True);
            Assert.That(collector.IsSuitableForCircuits(4), Is.True);
        }

        [Test]
        public void IsSuitableForCircuits_ReturnsFalseForExceededCount()
        {
            // Arrange
            var collector = new Collector
            {
                Type = CollectorType.HKV,
                Circuits = 4
            };

            // Act & Assert
            Assert.That(collector.IsSuitableForCircuits(6), Is.False);
        }

        [Test]
        public void IsSuitableForCircuits_ReturnsFalseForLessThanTwo()
        {
            // Arrange
            var collector = new Collector
            {
                Type = CollectorType.HKV,
                Circuits = 4
            };

            // Act & Assert
            Assert.That(collector.IsSuitableForCircuits(1), Is.False);
            Assert.That(collector.IsSuitableForCircuits(0), Is.False);
        }

        [Test]
        public void IsSuitableForCircuits_ForIndustrial_ReturnsTrue()
        {
            // Arrange
            var collector = new Collector
            {
                Type = CollectorType.IV,
                Circuits = 1
            };

            // Act & Assert
            Assert.That(collector.IsSuitableForCircuits(1), Is.True);
            Assert.That(collector.IsSuitableForCircuits(10), Is.True);
        }

        #endregion

        #region IsSuitableForFlowRate

        [Test]
        public void IsSuitableForFlowRate_ReturnsTrueForValidFlow()
        {
            // Arrange
            var collector = new Collector { MaxFlowRate = 1.5 };

            // Act & Assert
            Assert.That(collector.IsSuitableForFlowRate(1.0), Is.True);
            Assert.That(collector.IsSuitableForFlowRate(1.5), Is.True);
        }

        [Test]
        public void IsSuitableForFlowRate_ReturnsFalseForExceededFlow()
        {
            // Arrange
            var collector = new Collector { MaxFlowRate = 1.5 };

            // Act & Assert
            Assert.That(collector.IsSuitableForFlowRate(2.0), Is.False);
        }

        [Test]
        public void IsSuitableForFlowRate_WithZeroFlow_ReturnsTrue()
        {
            // Arrange
            var collector = new Collector { MaxFlowRate = 1.5 };

            // Act & Assert
            Assert.That(collector.IsSuitableForFlowRate(0), Is.True);
        }

        #endregion

        #region IsSuitableForPressure

        [Test]
        public void IsSuitableForPressure_ReturnsTrueForValidPressure()
        {
            // Arrange
            var collector = new Collector { MaxPressure = 320 };

            // Act & Assert
            Assert.That(collector.IsSuitableForPressure(200), Is.True);
            Assert.That(collector.IsSuitableForPressure(320), Is.True);
        }

        [Test]
        public void IsSuitableForPressure_ReturnsFalseForExceededPressure()
        {
            // Arrange
            var collector = new Collector { MaxPressure = 320 };

            // Act & Assert
            Assert.That(collector.IsSuitableForPressure(400), Is.False);
        }

        [Test]
        public void IsSuitableForPressure_WithZeroPressure_ReturnsTrue()
        {
            // Arrange
            var collector = new Collector { MaxPressure = 320 };

            // Act & Assert
            Assert.That(collector.IsSuitableForPressure(0), Is.True);
        }

        #endregion

        #region GetDescription

        [Test]
        public void GetDescription_ReturnsCorrectDescription()
        {
            // Arrange
            var collector = new Collector
            {
                FullName = "Коллектор HKV-D 4 контура",
                Circuits = 4,
                Kv = 1.2,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };

            // Act
            var description = collector.GetDescription();

            // Assert
            Assert.That(description, Does.Contain("HKV-D 4"));
            Assert.That(description, Does.Contain("Kv=1.2"));
            Assert.That(description, Does.Contain("1.5 м³/ч"));
            Assert.That(description, Does.Contain("320 мбар"));
        }

        [Test]
        public void GetDescription_WithAllFields_ReturnsCompleteDescription()
        {
            // Arrange
            var collector = new Collector
            {
                FullName = "Коллектор IV 1¼\"",
                Circuits = 1,
                Kv = 1.45,
                MaxFlowRate = 2.0,
                MaxPressure = 400
            };

            // Act
            var description = collector.GetDescription();

            // Assert
            Assert.That(description, Does.Contain("IV 1¼"));
            Assert.That(description, Does.Contain("Kv=1.45"));
            Assert.That(description, Does.Contain("2 м³/ч"));
            Assert.That(description, Does.Contain("400 мбар"));
        }

        #endregion

        #region Значения по умолчанию

        [Test]
        public void Default_IdIsEmptyString()
        {
            // Arrange & Act
            var collector = new Collector();

            // Assert
            Assert.That(collector.Id, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Default_NameIsEmptyString()
        {
            // Arrange & Act
            var collector = new Collector();

            // Assert
            Assert.That(collector.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Default_MaxSettingIs8()
        {
            // Arrange & Act
            var collector = new Collector();

            // Assert
            Assert.That(collector.MaxSetting, Is.EqualTo(8));
        }

        [Test]
        public void Default_CircuitsIsZero()
        {
            // Arrange & Act
            var collector = new Collector();

            // Assert
            Assert.That(collector.Circuits, Is.EqualTo(0));
        }

        #endregion

        #region Типы коллекторов

        [Test]
        public void Collector_CanBeCreatedWithHKVType()
        {
            // Arrange & Act
            var collector = new Collector
            {
                Id = "HKV-D-4",
                Name = "HKV-D 4",
                FullName = "Коллектор HKV-D 4 контура",
                Type = CollectorType.HKV,
                Circuits = 4,
                Kv = 1.2,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };

            // Assert
            Assert.That(collector.Type, Is.EqualTo(CollectorType.HKV));
            Assert.That(collector.IsResidential, Is.True);
        }

        [Test]
        public void Collector_CanBeCreatedWithIVType()
        {
            // Arrange & Act
            var collector = new Collector
            {
                Id = "IV-1.25",
                Name = "IV 1¼\"",
                FullName = "Коллектор IV 1¼\"",
                Type = CollectorType.IV,
                Circuits = 1,
                Kv = 1.45,
                MaxFlowRate = 2.0,
                MaxPressure = 400
            };

            // Assert
            Assert.That(collector.Type, Is.EqualTo(CollectorType.IV));
            Assert.That(collector.IsIndustrial, Is.True);
        }

        #endregion
    }
}