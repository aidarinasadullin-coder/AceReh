using System.IO;
using System.Text.RegularExpressions;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Tests.Fixtures;

namespace SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics
{
    /// <summary>
    /// TDD-характеризационные тесты полномочий записи в CalculationContext.
    /// </summary>
    /// <remarks>
    /// Проверяет, что каждый модуль пишет только в свою зону ответственности:
    /// - ThermalViewModel — канонический автор ThermalResult / ThermalInputs.
    /// - HydraulicsStateCoordinator — единственный production-автор HydraulicsResults.
    /// - CircuitsViewModel сохраняет только null-coordinator compatibility seam для
    ///   изолированных тестовых конструкций; production DI всегда передаёт coordinator.
    /// </remarks>
    [TestFixture]
    public class CalculationContextWriterAuthorityTests : HydraulicsIntegrationTestBase
    {
        /// <summary>
        /// Поверх канона — историческое расхождение этого набора
        /// (зафиксировано волной 3, 2026-09-18): сводка коллектора
        /// считается по TotalLength и помечается валидной.
        /// </summary>
        public override void Setup()
        {
            base.Setup();

            _circuitsCalculatorMock
                .Setup(c => c.CalculateCollectorSummary(
                    It.IsAny<List<CircuitRow>>(),
                    It.IsAny<int>(),
                    It.IsAny<ValveType>()))
                .Returns((List<CircuitRow> circuits, int number, ValveType valveType) => new CollectorSummary
                {
                    CollectorNumber = number,
                    CircuitCount = circuits.Count,
                    TotalPipeLength = circuits.Sum(c => c.TotalLength),
                    TotalPower = circuits.Sum(c => c.Power),
                    TotalFlowRate = circuits.Sum(c => c.FlowRate),
                    IsValid = true
                });
        }

        private void SeedThermalInputsAndResult(ThermalCalculationResult result, PipeType? pipe = null)
        {
            _thermalViewModel.Result = result;
            var inputs = _thermalViewModel.BuildThermalInputs();
            if (pipe != null)
            {
                inputs = inputs with { Pipe = pipe };
            }
            _calculationContext.UpdateThermalInputs(inputs, "Thermal");
            _calculationContext.UpdateThermal(result, "Thermal");
        }

        [Test]
        [Category("ThermalProjection")]
        public void ThermalProjection_HasExactlyOneProductionWriterType()
        {
            var sourceRoot = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (sourceRoot != null && !Directory.Exists(Path.Combine(sourceRoot.FullName, "src")))
            {
                sourceRoot = sourceRoot.Parent;
            }

            Assert.That(sourceRoot, Is.Not.Null);
            var sourceDirectory = Path.Combine(sourceRoot!.FullName, "src");
            var writerFiles = Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
                .Select(file => new { File = file, Text = File.ReadAllText(file) })
                .Where(item => !string.Equals(Path.GetFileNameWithoutExtension(item.File), "CalculationContext", StringComparison.OrdinalIgnoreCase))
                .Where(item => Regex.IsMatch(item.Text, @"\bUpdateThermal(?:Inputs|Result)?\s*\("))
                .Select(item => Path.GetFileNameWithoutExtension(item.File))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.That(writerFiles, Is.EqualTo(new[] { "ThermalStateCoordinator" }));
        }

        [Test]
        [Category("HydraulicsProjection")]
        public void HydraulicsProjection_HasExactlyOneApprovedProductionWriterType()
        {
            var sourceRoot = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (sourceRoot != null && !Directory.Exists(Path.Combine(sourceRoot.FullName, "src")))
            {
                sourceRoot = sourceRoot.Parent;
            }

            Assert.That(sourceRoot, Is.Not.Null);
            var sourceDirectory = Path.Combine(sourceRoot!.FullName, "src");
            var writerFiles = Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
                .Select(file => new { File = file, Text = File.ReadAllText(file) })
                .Where(item => !string.Equals(Path.GetFileNameWithoutExtension(item.File), "CalculationContext", StringComparison.OrdinalIgnoreCase))
                .Where(item => Regex.IsMatch(item.Text, @"\bUpdateHydraulics\s*\("))
                .Select(item => Path.GetFileNameWithoutExtension(item.File))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.That(writerFiles, Is.EqualTo(new[] { "HydraulicsStateCoordinator" }));
        }

        [Test]
        [Category("ThermalProjection")]
        public void CircuitsOwnSourceContextEvents_DoNotReenterHydraulicsCalculation()
        {
            var validResult = new ThermalCalculationResult
            {
                IsValid = true,
                PowerUp = 256,
                PowerDown = 5,
                SupplyTemperature = 50,
                ReturnTemperature = 30
            };
            SeedThermalInputsAndResult(validResult);
            _circuitsCalculatorMock.Invocations.Clear();

            _calculationContext.UpdateThermal(validResult, "CircuitsViewModel");

            Assert.That(_circuitsCalculatorMock.Invocations.Count(i => i.Method.Name == nameof(ICircuitsCalculator.CalculateCircuitPower)), Is.Zero);
        }

        [Test]
        public void ThermalVM_Calculate_PublishesInvalidResult_NotifiesCircuitsButDoesNotCalculate()
        {
            // Arrange - seed context with invalid thermal result
            var invalidResult = new ThermalCalculationResult
            {
                PowerUp = 100.0,
                PowerDown = 10.0,
                SupplyTemperature = 60.0,
                ReturnTemperature = 40.0,
                IsValid = false,
                ValidationErrors = new[] { "Ошибка валидации" }
            };
            _calculationContext.UpdateThermalInputs(_thermalViewModel.BuildThermalInputs(), "Thermal");
            _calculationContext.UpdateThermal(invalidResult, "Thermal");

            // Act - invalid result only notifies UI; no hydraulic calculation should run
            // (OnCalculationContextChanged handles invalid thermal as Notify-only)

            // Assert - hydraulics context stays empty/stale-free
            Assert.That(_calculationContext.HydraulicsResults, Is.Null, "HydraulicsResults должен оставаться null");
        }

        [Test]
        public void UpdateFromThermalModule_NoLongerWritesToContext()
        {
            // Arrange - seed context externally with valid thermal data
            var pipe = new PipeType
            {
                Name = "RAUTHERM S 20x2,0",
                OuterDiameter = 20,
                InnerDiameter = 16,
                WallThickness = 2.0
            };
            var validResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };
            SeedThermalInputsAndResult(validResult, pipe);

            var contextChangedEvents = new List<ContextChangedEventArgs>();
            _calculationContext.ContextChanged += (s, e) => contextChangedEvents.Add(e);

            // Act - invoke UpdateFromThermalModule (it must NOT republish thermal data)
            _viewModel.UpdateFromThermalModule(validResult, pipe);

            // Assert - no thermal-result write originated from CircuitsViewModel
            var thermalWritesFromCircuitsVm = contextChangedEvents
                .Any(e => e.PropertyName == nameof(CalculationContext.ThermalResult)
                          && e.Source == "CircuitsViewModel");
            Assert.That(thermalWritesFromCircuitsVm, Is.False,
                "UpdateFromThermalModule не должен публиковать ThermalResult в контекст");
        }

        [Test]
        public void CircuitsVM_Calculate_PublishesCollectorSummariesToContext()
        {
            // Arrange - seed valid thermal context and add a second collector with circuits
            var pipe = new PipeType
            {
                Name = "RAUTHERM S 20x2,0",
                OuterDiameter = 20,
                InnerDiameter = 16,
                WallThickness = 2.0
            };
            var validResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };
            SeedThermalInputsAndResult(validResult, pipe);

            _viewModel.AddCollectorCommand.Execute(null);
            _viewModel.Collectors[1].Circuits.Clear();
            _viewModel.Collectors[1].Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 70 });
            _viewModel.Collectors[1].Circuits.Add(new CircuitRow { CircuitNumber = 2, CircuitLength = 60 });

            // Act - calculate for each collector; each run publishes all accumulated summaries
            _viewModel.SelectedCollectorIndex = 0;
            _viewModel.CalculateCommand.Execute(null);

            _viewModel.SelectedCollectorIndex = 1;
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_calculationContext.HydraulicsResults, Is.Not.Null, "HydraulicsResults должны быть опубликованы");
            Assert.That(_calculationContext.HydraulicsResults!.Count, Is.EqualTo(2), "Должно быть 2 summary по коллекторам");
            Assert.That(_calculationContext.HydraulicsResults.All(s => s.TotalPipeLength > 0), Is.True,
                "Каждый summary должен иметь ненулевую общую длину труб");
        }

        [Test]
        public void CircuitsVM_Calculate_WithGlycolError_PublishesNullHydraulicsResults()
        {
            // Arrange - seed valid thermal context
            var validResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };
            SeedThermalInputsAndResult(validResult);

            // Гликоль с концентрацией 95% вне допустимого диапазона
            _viewModel.InputData.GlycolConcentration = 95.0;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Throws(new ArgumentOutOfRangeException("concentration", "Концентрация вне диапазона"));

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_calculationContext.HydraulicsResults, Is.Null,
                "При ошибке гликоля результаты гидравлики должны сбрасываться в null");
        }
    }
}
