using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты для ConstructionService
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ConstructionServiceTests
    {
        private ConstructionService _service = null!;
        private ProjectStateService _projectStateService = null!;

        [SetUp]
        public void Setup()
        {
            _service = new ConstructionService(new ConstructionValidator());
            _projectStateService = new ProjectStateService();
        }

        #region CalculateR1 Tests

        [Test]
        public void CalculateR1_SingleLayer_ReturnsCorrectValue()
        {
            // Arrange
            var material = Material.GetDefaultMaterial();
            var layer = new Layer
            {
                Material = material,
                Thickness = 100, // 100 мм
                CalculatedLambda = 1.5, // Вт/м·К
                Position = LayerPosition.AbovePipe
            };

            // Act
            var r1 = _service.CalculateR1(new[] { layer });

            // Assert
            // R = d / λ / 1000 = 100 / 1.5 / 1000 = 0.0667 м²·К/Вт
            Assert.That(r1, Is.EqualTo(0.0667).Within(0.0001));
        }

        [Test]
        public void CalculateR1_MultipleLayers_ReturnsSum()
        {
            // Arrange
            var layers = new[]
            {
                new Layer { Material = Material.GetDefaultMaterial(), Thickness = 50, CalculatedLambda = 1.5, Position = LayerPosition.AbovePipe },
                new Layer { Material = Material.GetDefaultMaterial(), Thickness = 100, CalculatedLambda = 1.2, Position = LayerPosition.AbovePipe }
            };

            // Act
            var r1 = _service.CalculateR1(layers);

            // Assert
            // R1 = 50/1.5/1000 + 100/1.2/1000 = 0.0333 + 0.0833 = 0.1167 м²·К/Вт
            Assert.That(r1, Is.EqualTo(0.1167).Within(0.0001));
        }

        [Test]
        public void CalculateR1_EmptyCollection_ReturnsZero()
        {
            // Act
            var r1 = _service.CalculateR1(Enumerable.Empty<Layer>());

            // Assert
            Assert.That(r1, Is.EqualTo(0));
        }

        [Test]
        public void CalculateR1_ZeroLambda_ThrowsInvalidOperationException()
        {
            // Arrange
            var layer = new Layer
            {
                Material = Material.GetDefaultMaterial(),
                Thickness = 100,
                CalculatedLambda = 0, // Некорректное значение
                Position = LayerPosition.AbovePipe
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CalculateR1(new[] { layer }));
        }

        [Test]
        public void CalculateR1_NegativeLambda_ThrowsInvalidOperationException()
        {
            // Arrange
            var layer = new Layer
            {
                Material = Material.GetDefaultMaterial(),
                Thickness = 100,
                CalculatedLambda = -1.5, // Некорректное значение
                Position = LayerPosition.AbovePipe
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CalculateR1(new[] { layer }));
        }

        #endregion

        #region CalculateR2 Tests

        [Test]
        public void CalculateR2_SingleLayer_ReturnsCorrectValue()
        {
            // Arrange
            var material = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            var layer = new Layer
            {
                Material = material,
                Thickness = 150, // 150 мм
                CalculatedLambda = material.LambdaA, // 0.4 Вт/м·К
                Position = LayerPosition.BelowPipe
            };

            // Act
            var r2 = _service.CalculateR2(new[] { layer }, groundwaterLevel: 2.0);

            // Assert
            // R = d / λ / 1000 = 150 / 0.4 / 1000 = 0.375 м²·К/Вт
            Assert.That(r2, Is.EqualTo(0.375).Within(0.0001));
        }

        [Test]
        public void CalculateR2_HighGroundwater_UsesLambdaB()
        {
            // Arrange
            var material = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            var layer = new Layer
            {
                Material = material,
                Thickness = 150,
                Position = LayerPosition.BelowPipe
            };

            // Act - УГВ < 1м, должна использоваться λБ
            var r2 = _service.CalculateR2(new[] { layer }, groundwaterLevel: 0.5);

            // Assert
            // При УГВ < 1м используется λБ = 2.0 для песка
            // R = 150 / 2.0 / 1000 = 0.075 м²·К/Вт
            Assert.That(r2, Is.EqualTo(0.075).Within(0.0001));
        }

        [Test]
        public void CalculateR2_LowGroundwater_UsesLambdaA()
        {
            // Arrange
            var material = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            var layer = new Layer
            {
                Material = material,
                Thickness = 150,
                Position = LayerPosition.BelowPipe
            };

            // Act - УГВ >= 1м, должна использоваться λА
            var r2 = _service.CalculateR2(new[] { layer }, groundwaterLevel: 2.0);

            // Assert
            // При УГВ >= 1м используется λА = 0.4 для песка
            // R = 150 / 0.4 / 1000 = 0.375 м²·К/Вт
            Assert.That(r2, Is.EqualTo(0.375).Within(0.0001));
        }

        [Test]
        public void CalculateR2_NegativeGroundwater_ThrowsArgumentException()
        {
            // Arrange
            var layer = new Layer
            {
                Material = Material.GetDefaultMaterial(),
                Thickness = 100,
                Position = LayerPosition.BelowPipe
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.CalculateR2(new[] { layer }, groundwaterLevel: -1.0));
        }

        #endregion

        #region ValidateConstruction Tests

        [Test]
        public void ValidateConstruction_ValidConstruction_ReturnsValidResult()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction
            {
                GroundwaterLevel = 2.0,
                HasLoads = false
            };

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateConstruction_NoLayers_ReturnsInvalidResult()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ValidateConstruction_ThinLayerAbovePipe_ReturnsInvalidResult()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction
            {
                HasLoads = false
            };

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 30); // Меньше минимума (40 мм)

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Минимальная толщина")), Is.True);
        }

        [Test]
        public void ValidateConstruction_WithLoads_RequiresThickerLayer()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction
            {
                HasLoads = true
            };

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 40); // Меньше минимума при нагрузках (50 мм)

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region CreateFromTemplate Tests

        [Test]
        public void CreateFromTemplate_ValidTemplate_ReturnsConstruction()
        {
            // Arrange
            var template = ConstructionTemplate.GetDefaultTemplates().First();
            var materials = Material.GetDefaultMaterials();

            // Act
            var construction = _service.CreateFromTemplate(template, materials);

            // Assert
            Assert.That(construction, Is.Not.Null);
            Assert.That(construction.LayersAbovePipe.Count, Is.EqualTo(template.LayersAbovePipe.Count));
            Assert.That(construction.HasLoads, Is.EqualTo(template.HasLoads));
        }

        [Test]
        public void CreateFromTemplate_InvalidMaterialId_ThrowsInvalidOperationException()
        {
            // Arrange
            var template = new ConstructionTemplate
            {
                Id = 999,
                Name = "Test",
                LayersAbovePipe = new System.Collections.Generic.List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 9999, Thickness = 100, Position = LayerPosition.AbovePipe }
                }
            };
            var materials = Material.GetDefaultMaterials();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CreateFromTemplate(template, materials));
        }

        #endregion

        #region GetTotalThickness Tests

        [Test]
        public void GetTotalThicknessAbovePipe_MultipleLayers_ReturnsSum()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);
            construction.AddLayerAbovePipe(concrete, 100);

            // Act
            var thickness = _service.GetTotalThicknessAbovePipe(construction);

            // Assert
            Assert.That(thickness, Is.EqualTo(150));
        }

        [Test]
        public void GetTotalThicknessBelowPipe_MultipleLayers_ReturnsSum()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();
            var sand = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            construction.AddLayerBelowPipe(sand, 150);
            construction.AddLayerBelowPipe(sand, 200);

            // Act
            var thickness = _service.GetTotalThicknessBelowPipe(construction);

            // Assert
            Assert.That(thickness, Is.EqualTo(350));
        }

        #endregion

        #region MaterialAroundPipe Tests

        [Test]
        public void MaterialAroundPipe_IsLastAbovePipeLayer()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();
            var concrete = Material.GetDefaultMaterials().First(m => m.Id == 5);
            var asphalt = Material.GetDefaultMaterials().First(m => m.Id == 11); // Асфальт

            // Physical top-to-bottom: asphalt on surface, then concrete near pipe
            construction.LayersAbovePipe.Add(new Layer
            {
                Material = asphalt,
                Thickness = 50,
                CalculatedLambda = asphalt.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });
            construction.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 1
            });

            // Act & Assert
            Assert.That(construction.MaterialAroundPipe, Is.EqualTo(concrete));
            Assert.That(construction.LambdaE, Is.EqualTo(concrete.LambdaA));
        }

        #endregion

        #region ProjectData Round-Trip Tests

        [Test]
        public async Task ProjectData_Load_ReindexesOrder()
        {
            // Arrange
            var constructionVm = CreateConstructionViewModel();
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            constructionVm.LayersAbovePipe.Clear();
            var asphalt = constructionVm.AvailableMaterials.First(m => m.Name == "Асфальт");
            var asphaltConcrete = constructionVm.AvailableMaterials.First(m => m.Name == "Асфальтобетон");
            var concrete = constructionVm.AvailableMaterials.First(m => m.Name == "Бетон плотный");

            constructionVm.LayersAbovePipe.Add(new Layer
            {
                Material = asphalt,
                Thickness = 50,
                CalculatedLambda = asphalt.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });
            constructionVm.LayersAbovePipe.Add(new Layer
            {
                Material = asphaltConcrete,
                Thickness = 50,
                CalculatedLambda = asphaltConcrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 1
            });
            constructionVm.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 2
            });
            constructionVm.UpdateCalculations();

            var viewModel = CreateResultsViewModel(constructionVm);
            var savedData = viewModel.SaveCurrentProject();

            var constructionVm2 = CreateConstructionViewModel();
            await constructionVm2.InitializeCommand.ExecuteAsync(null);
            var viewModel2 = CreateResultsViewModel(constructionVm2);

            // Act
            viewModel2.LoadProjectData(savedData);

            // Assert
            Assert.That(constructionVm2.LayersAbovePipe.Count, Is.EqualTo(3));
            Assert.That(constructionVm2.LayersAbovePipe[0].Order, Is.EqualTo(0));
            Assert.That(constructionVm2.LayersAbovePipe[1].Order, Is.EqualTo(1));
            Assert.That(constructionVm2.LayersAbovePipe[2].Order, Is.EqualTo(2));
            Assert.That(constructionVm2.LayersAbovePipe[2].Material?.Name, Is.EqualTo("Бетон плотный"));
            Assert.That(constructionVm2.LambdaE, Is.EqualTo(concrete.LambdaA).Within(0.0001));
            Assert.That(constructionVm2.GetConstruction().MaterialAroundPipe?.Name, Is.EqualTo("Бетон плотный"));
        }

        [Test]
        public async Task ProjectData_LayerOrder_RoundTrip_PreservesLambdaE()
        {
            // Arrange
            var constructionVm = CreateConstructionViewModel();
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            constructionVm.LayersAbovePipe.Clear();
            var asphalt = constructionVm.AvailableMaterials.First(m => m.Name == "Асфальт");
            var concrete = constructionVm.AvailableMaterials.First(m => m.Name == "Бетон плотный");

            // Physical top-to-bottom: asphalt surface layer above concrete near pipe
            constructionVm.LayersAbovePipe.Add(new Layer
            {
                Material = asphalt,
                Thickness = 50,
                CalculatedLambda = asphalt.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });
            constructionVm.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 1
            });
            constructionVm.UpdateCalculations();

            // Assert collection order and LambdaE before round-trip
            Assert.That(constructionVm.LayersAbovePipe[0].Material?.Name, Is.EqualTo("Асфальт"));
            Assert.That(constructionVm.LayersAbovePipe[0].Order, Is.EqualTo(0));
            Assert.That(constructionVm.LayersAbovePipe[1].Material?.Name, Is.EqualTo("Бетон плотный"));
            Assert.That(constructionVm.LayersAbovePipe[1].Order, Is.EqualTo(1));
            Assert.That(constructionVm.LambdaE, Is.EqualTo(concrete.LambdaA).Within(0.0001));
            Assert.That(constructionVm.GetConstruction().MaterialAroundPipe, Is.EqualTo(concrete));

            var viewModel = CreateResultsViewModel(constructionVm);
            var savedData = viewModel.SaveCurrentProject();

            var constructionVm2 = CreateConstructionViewModel();
            await constructionVm2.InitializeCommand.ExecuteAsync(null);
            var viewModel2 = CreateResultsViewModel(constructionVm2);

            // Act
            viewModel2.LoadProjectData(savedData);

            // Assert
            Assert.That(constructionVm2.LayersAbovePipe.Count, Is.EqualTo(2));
            Assert.That(constructionVm2.LayersAbovePipe[0].Order, Is.EqualTo(0));
            Assert.That(constructionVm2.LayersAbovePipe[1].Order, Is.EqualTo(1));
            Assert.That(constructionVm2.LayersAbovePipe[0].Material?.Name, Is.EqualTo("Асфальт"));
            Assert.That(constructionVm2.LayersAbovePipe[1].Material?.Name, Is.EqualTo("Бетон плотный"));
            Assert.That(constructionVm2.LambdaE, Is.EqualTo(concrete.LambdaA).Within(0.0001));
            Assert.That(constructionVm2.GetConstruction().MaterialAroundPipe?.Name, Is.EqualTo("Бетон плотный"));
        }

        [Test]
        public async Task ProjectData_Load_v1_0_MigratesAbovePipeOrder()
        {
            // Arrange
            var concrete = Material.GetDefaultMaterial();
            var asphalt = Material.GetDefaultMaterials().First(m => m.Id == 11);
            var projectData = new ProjectData
            {
                Version = "1.0",
                ProjectNumber = "P-v1",
                ProjectObject = "Migration Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData
                {
                    Layers = new List<LayerProjectData>
                    {
                        new LayerProjectData
                        {
                            Position = LayerPosition.AbovePipe,
                            MaterialName = concrete.Name,
                            MaterialLambda = concrete.LambdaA,
                            Thickness = 100,
                            CalculatedLambda = concrete.LambdaA,
                            Order = 0
                        },
                        new LayerProjectData
                        {
                            Position = LayerPosition.AbovePipe,
                            MaterialName = asphalt.Name,
                            MaterialLambda = asphalt.LambdaA,
                            Thickness = 50,
                            CalculatedLambda = asphalt.LambdaA,
                            Order = 1
                        }
                    }
                },
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData()
            };

            var constructionVm = CreateConstructionViewModel();
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            var viewModel = CreateResultsViewModel(constructionVm);

            // Act
            viewModel.LoadProjectData(projectData);

            // Assert
            Assert.That(constructionVm.LayersAbovePipe.Count, Is.EqualTo(2));
            Assert.That(constructionVm.LayersAbovePipe[0].Material?.Name, Is.EqualTo(asphalt.Name));
            Assert.That(constructionVm.LayersAbovePipe[1].Material?.Name, Is.EqualTo(concrete.Name));
            Assert.That(constructionVm.GetConstruction().MaterialAroundPipe?.Name, Is.EqualTo(concrete.Name));
        }

        [Test]
        public async Task ProjectData_Save_v1_1_SetsVersion()
        {
            // Arrange
            var constructionVm = CreateConstructionViewModel();
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            constructionVm.LayersAbovePipe.Clear();
            var concrete = constructionVm.AvailableMaterials.First(m => m.Name == "Бетон плотный");
            constructionVm.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });
            constructionVm.UpdateCalculations();

            var viewModel = CreateResultsViewModel(constructionVm);

            // Act
            var savedData = viewModel.SaveCurrentProject();

            // Assert
            Assert.That(savedData.Version, Is.EqualTo("1.1"));
            Assert.That(savedData.ConstructionData.Layers, Is.Not.Empty);
            foreach (var layer in savedData.ConstructionData.Layers)
            {
                Assert.That(layer.Order, Is.GreaterThanOrEqualTo(0));
            }
        }

        #endregion

        #region Helper Methods

        private ResultsViewModel CreateResultsViewModel(ConstructionViewModel constructionVm)
        {
            return new ResultsViewModel(
                _projectStateService,
                _projectStateService,
                new Mock<IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<IProjectFileService>().Object,
                new Mock<IConstructionVisualizationImageService>().Object,
                new CalculationStateService(),
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
        }

        private static ConstructionViewModel CreateConstructionViewModel()
        {
            var materials = new List<Material>
            {
                new Material { Id = 1, Name = "Песок", LambdaA = 0.4, LambdaB = 2.0 },
                new Material { Id = 5, Name = "Бетон плотный", LambdaA = 1.5, LambdaB = 1.5 },
                new Material { Id = 7, Name = "Асфальтобетон", LambdaA = 1.5, LambdaB = 1.5 },
                new Material { Id = 11, Name = "Асфальт", LambdaA = 0.75, LambdaB = 0.75 }
            };

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(),
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                new Mock<IMarkDirtyService>().Object);
        }

        private static ClimateViewModel CreateClimateViewModel()
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(Enumerable.Empty<CityInfo>());
            climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                .Returns((double t, bool high) =>
                {
                    if (high) return ClimateZone.Zone_M20_Plus;
                    if (t >= -27) return ClimateZone.Zone_M10;
                    if (t > -37) return ClimateZone.Zone_M15;
                    return ClimateZone.Zone_M20;
                });

            return new ClimateViewModel(
                climateServiceMock.Object,
                new ClimateData(),
                new ClimateValidator(),
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());
        }

        private static ThermalViewModel CreateThermalViewModel()
        {
            var climateData = new ClimateData();
            var constructionData = new ConstructionData();
            return new ThermalViewModel(
                new Mock<IThermalCalculator>().Object,
                climateData,
                constructionData,
                new CalculationStateService(),
                new CalculationContext(),
                new ThermalValidator(new ThermalCalculator(), climateData, constructionData),
                new ThermalResultValidator(),
                new Mock<IMarkDirtyService>().Object);
        }

        private static CircuitsViewModel CreateCircuitsViewModel()
        {
            var calculatorMock = new Mock<ICircuitsCalculator>();
            calculatorMock.Setup(c => c.CalculateCircuitPower(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateFlowRate(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateCollectorSummary(It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>())).Returns(new CollectorSummary());
            calculatorMock.Setup(c => c.CalculateAtTemperature(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<GlycolProperties>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<ValveType>())).Returns(new CircuitTemperatureResult());
            calculatorMock.Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitRow>>(), It.IsAny<ValveType>())).Returns((List<CircuitRow> circuits, ValveType _) => circuits);

            var glycolMock = new Mock<IGlycolDataService>();
            glycolMock.Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>())).Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });

            var selectorMock = new Mock<ICollectorTypeSelector>();
            selectorMock.Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>())).Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });

            var validatorMock = new Mock<ICircuitsValidator>();

            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                new CalculationStateService(),
                validatorMock.Object,
                selectorMock.Object,
                new CalculationContext(),
                new Mock<IMarkDirtyService>().Object);
        }

        #endregion
    }
}