using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Tests.Models.Climate
{
    /// <summary>
    /// Тесты для перечисления MatchType
    /// </summary>
    [TestFixture]
    public class MatchTypeTests
    {
        [Test]
        public void MatchType_HasCorrectValues()
        {
            // Assert
            Assert.That((int)MatchType.StartsWith, Is.EqualTo(0));
            Assert.That((int)MatchType.Contains, Is.EqualTo(1));
            Assert.That((int)MatchType.Region, Is.EqualTo(2));
        }

        [Test]
        public void MatchType_HasThreeValues()
        {
            // Arrange
            var values = System.Enum.GetValues<MatchType>();

            // Assert
            Assert.That(values.Length, Is.EqualTo(3));
        }

        [Test]
        public void MatchType_NamesAreCorrect()
        {
            // Assert
            Assert.That(MatchType.StartsWith.ToString(), Is.EqualTo("StartsWith"));
            Assert.That(MatchType.Contains.ToString(), Is.EqualTo("Contains"));
            Assert.That(MatchType.Region.ToString(), Is.EqualTo("Region"));
        }

        [Test]
        public void MatchType_CanParseFromString()
        {
            // Act & Assert
            Assert.That(System.Enum.Parse<MatchType>("StartsWith"), Is.EqualTo(MatchType.StartsWith));
            Assert.That(System.Enum.Parse<MatchType>("Contains"), Is.EqualTo(MatchType.Contains));
            Assert.That(System.Enum.Parse<MatchType>("Region"), Is.EqualTo(MatchType.Region));
        }

        [Test]
        public void MatchType_PriorityOrderIsCorrect()
        {
            // Assert - StartsWith has highest priority (lowest value)
            Assert.That((int)MatchType.StartsWith, Is.LessThan((int)MatchType.Contains));
            Assert.That((int)MatchType.Contains, Is.LessThan((int)MatchType.Region));
        }
    }
}