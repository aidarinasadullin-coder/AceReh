using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для FlowRegimeCalculator
    /// </summary>
    [TestFixture]
    public class FlowRegimeCalculatorTests
    {
        #region DetermineFlowRegime Tests

        [Test]
        public void DetermineFlowRegime_Laminar_ReturnsLaminar()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(1000), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(2000), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(2299), Is.EqualTo(FlowRegime.Laminar));
        }

        [Test]
        public void DetermineFlowRegime_Transitional_ReturnsTransitional()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(2300), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(3000), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(4000), Is.EqualTo(FlowRegime.Transitional));
        }

        [Test]
        public void DetermineFlowRegime_Turbulent_ReturnsTurbulent()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(4001), Is.EqualTo(FlowRegime.Turbulent));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(5000), Is.EqualTo(FlowRegime.Turbulent));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(10000), Is.EqualTo(FlowRegime.Turbulent));
        }

        #endregion

        #region IsLaminar/IsTransitional/IsTurbulent Tests

        [Test]
        public void IsLaminar_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.IsLaminar(1000), Is.True);
            Assert.That(FlowRegimeCalculator.IsLaminar(2299), Is.True);
            Assert.That(FlowRegimeCalculator.IsLaminar(3000), Is.False);
            Assert.That(FlowRegimeCalculator.IsLaminar(5000), Is.False);
        }

        [Test]
        public void IsTransitional_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.IsTransitional(1000), Is.False);
            Assert.That(FlowRegimeCalculator.IsTransitional(2300), Is.True);
            Assert.That(FlowRegimeCalculator.IsTransitional(3000), Is.True);
            Assert.That(FlowRegimeCalculator.IsTransitional(4000), Is.True);
            Assert.That(FlowRegimeCalculator.IsTransitional(5000), Is.False);
        }

        [Test]
        public void IsTurbulent_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.IsTurbulent(1000), Is.False);
            Assert.That(FlowRegimeCalculator.IsTurbulent(3000), Is.False);
            Assert.That(FlowRegimeCalculator.IsTurbulent(4001), Is.True);
            Assert.That(FlowRegimeCalculator.IsTurbulent(10000), Is.True);
        }

        #endregion

        #region CalculateLaminarFrictionFactor Tests

        [Test]
        public void CalculateLaminarFrictionFactor_ReturnsCorrectValue()
        {
            // Arrange
            double re = 2000;

            // Act
            double lambda = FlowRegimeCalculator.CalculateLaminarFrictionFactor(re);

            // Assert
            // λ = 64 / Re = 64 / 2000 = 0.032
            Assert.That(lambda, Is.EqualTo(0.032).Within(0.0001));
        }

        [Test]
        public void CalculateLaminarFrictionFactor_ThrowsForInvalidRe()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                FlowRegimeCalculator.CalculateLaminarFrictionFactor(0));
            Assert.Throws<System.ArgumentException>(() =>
                FlowRegimeCalculator.CalculateLaminarFrictionFactor(-100));
        }

        #endregion

        #region CalculateTransitionalFrictionFactor Tests

        [Test]
        public void CalculateTransitionalFrictionFactor_ReturnsInterpolatedValue()
        {
            // Arrange
            double re = 3000; // Середина переходного диапазона
            double diameter = 16;
            double roughness = 0.007;

            // Act
            double lambda = FlowRegimeCalculator.CalculateTransitionalFrictionFactor(re, diameter, roughness);

            // Assert
            // Должно быть между λ_lam ≈ 0.0278 и λ_turb ≈ 0.04
            Assert.That(lambda, Is.GreaterThan(0.027));
            Assert.That(lambda, Is.LessThan(0.04));
        }

        [Test]
        public void CalculateTransitionalFrictionFactor_ThrowsForInvalidRe()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                FlowRegimeCalculator.CalculateTransitionalFrictionFactor(2000, 16, 0.007));
            Assert.Throws<System.ArgumentException>(() =>
                FlowRegimeCalculator.CalculateTransitionalFrictionFactor(5000, 16, 0.007));
        }

        #endregion

        #region CalculateTurbulentFrictionFactor Tests

        [Test]
        public void CalculateTurbulentFrictionFactor_ReturnsCorrectValue()
        {
            // Arrange
            double re = 10000;
            double diameter = 16;
            double roughness = 0.007;

            // Act
            double lambda = FlowRegimeCalculator.CalculateTurbulentFrictionFactor(re, diameter, roughness);

            // Assert
            // Для Re=10000, di=16mm, ε=0.007mm: λ ≈ 0.03-0.04
            Assert.That(lambda, Is.GreaterThan(0.02));
            Assert.That(lambda, Is.LessThan(0.05));
        }

        [Test]
        public void CalculateTurbulentFrictionFactor_ThrowsForInvalidRe()
        {
            // Arrange
            double re = 3000; // Меньше границы турбулентного режима

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                FlowRegimeCalculator.CalculateTurbulentFrictionFactor(re, 16, 0.007));
        }

        #endregion

        #region CalculateFrictionFactor Tests

        [Test]
        public void CalculateFrictionFactor_WorksForAllRegimes()
        {
            // Arrange
            double diameter = 16;
            double roughness = 0.007;

            // Act & Assert - Laminar
            double lambdaLam = FlowRegimeCalculator.CalculateFrictionFactor(2000, diameter, roughness);
            Assert.That(lambdaLam, Is.EqualTo(0.032).Within(0.001));

            // Act & Assert - Transitional
            double lambdaTrans = FlowRegimeCalculator.CalculateFrictionFactor(3000, diameter, roughness);
            Assert.That(lambdaTrans, Is.GreaterThan(0.027));
            Assert.That(lambdaTrans, Is.LessThan(0.04));

            // Act & Assert - Turbulent
            double lambdaTurb = FlowRegimeCalculator.CalculateFrictionFactor(10000, diameter, roughness);
            Assert.That(lambdaTurb, Is.GreaterThan(0.02));
            Assert.That(lambdaTurb, Is.LessThan(0.05));
        }

        #endregion

        #region GetFlowRegimeDescription Tests

        [Test]
        public void GetFlowRegimeDescription_ReturnsCorrectDescription()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.GetFlowRegimeDescription(FlowRegime.Laminar), Does.Contain("Ламинарный"));
            Assert.That(FlowRegimeCalculator.GetFlowRegimeDescription(FlowRegime.Transitional), Does.Contain("Переходный"));
            Assert.That(FlowRegimeCalculator.GetFlowRegimeDescription(FlowRegime.Turbulent), Does.Contain("Турбулентный"));
        }

        #endregion

        #region GetFlowRegimeRecommendation Tests

        [Test]
        public void GetFlowRegimeRecommendation_ReturnsWarningForTransitional()
        {
            // Act
            string recommendation = FlowRegimeCalculator.GetFlowRegimeRecommendation(FlowRegime.Transitional);

            // Assert
            Assert.That(recommendation, Does.Contain("ВНИМАНИЕ"));
            Assert.That(recommendation, Does.Contain("нестабилен"));
        }

        #endregion
    }
}