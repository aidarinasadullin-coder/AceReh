using NUnit.Framework;
using Moq;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.ViewModels.Construction;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты для ConstructionViewModel
    /// </summary>
    [TestFixture]
    public class ConstructionViewModelTests
    {
        private ConstructionViewModel _viewModel = null!;
        private MockConstructionService _mockService = null!;
        private MockMaterialRepository _mockMaterialRepository = null!;
        private MockConstructionRepository _mockConstructionRepository = null!;
        private Mock<ICalculationStateService> _mockCalculationStateService = null!;

        [SetUp]
        public void Setup()
        {
            _mockService = new MockConstructionService();
            _mockMaterialRepository = new MockMaterialRepository();
            _mockConstructionRepository = new MockConstructionRepository();
            _mockCalculationStateService = new Mock<ICalculationStateService>();
            _mockCalculationStateService.SetupGet(s => s.PipeSpacing).Returns(200);
            var construction = new ConstructionModel();
            _viewModel = new ConstructionViewModel(
                _mockService,
                _mockMaterialRepository,
                _mockConstructionRepository,
                _mockCalculationStateService.Object,
                new SnowMeltingCalculator.Core.CalculationContext(),
                construction);
        }

        #region Initialize Tests

        [Test]
        public async Task Initialize_LoadsMaterials()
        {
            // Act
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.AvailableMaterials.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task Initialize_LoadsTemplates()
        {
            // Act
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Templates.Count, Is.GreaterThan(0));
        }

        #endregion

        #region AddLayer Tests

        [Test]
        public async Task AddLayerAbovePipe_AddsLayerToCollection()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var initialCount = _viewModel.LayersAbovePipe.Count;

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public async Task AddLayerAbovePipe_SetsCorrectPosition()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            var addedLayer = _viewModel.LayersAbovePipe.Last();
            Assert.That(addedLayer.Position, Is.EqualTo(LayerPosition.AbovePipe));
        }

        [Test]
        public async Task AddLayerBelowPipe_AddsLayerToCollection()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var initialCount = _viewModel.LayersBelowPipe.Count;

            // Act
            _viewModel.AddLayerBelowPipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersBelowPipe.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public async Task AddLayerBelowPipe_SetsCorrectPosition()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Act
            _viewModel.AddLayerBelowPipeCommand.Execute(null);

            // Assert
            var addedLayer = _viewModel.LayersBelowPipe.Last();
            Assert.That(addedLayer.Position, Is.EqualTo(LayerPosition.BelowPipe));
        }

        #endregion

        #region RemoveLayer Tests

        [Test]
        public async Task RemoveLayer_RemovesLayerFromCollection()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            var layerToRemove = _viewModel.LayersAbovePipe.First();

            // Act
            _viewModel.RemoveLayerCommand.Execute(layerToRemove);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe, Does.Not.Contain(layerToRemove));
        }

        [Test]
        public async Task RemoveLayer_NullLayer_DoesNothing()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var initialCount = _viewModel.LayersAbovePipe.Count;

            // Act
            _viewModel.RemoveLayerCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(initialCount));
        }

        #endregion

        #region GroundwaterLevel Tests

        [Test]
        public async Task SetGroundwaterLevelBelow1Meter_UpdatesLambdaForBelowPipeLayers()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            var layer = _viewModel.LayersBelowPipe.First();
            var material = layer.Material;
            var expectedLambda = material.LambdaB; // При УГВ < 1м используется λБ

            // Act
            _viewModel.GroundwaterLevel = 0.5;

            // Assert
            Assert.That(layer.CalculatedLambda, Is.EqualTo(expectedLambda));
        }

        [Test]
        public async Task SetGroundwaterLevelAbove1Meter_UsesLambdaAForBelowPipeLayers()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            var layer = _viewModel.LayersBelowPipe.First();
            var material = layer.Material;
            var expectedLambda = material.LambdaA; // При УГВ >= 1м используется λА

            // Act
            _viewModel.GroundwaterLevel = 2.0;

            // Assert
            Assert.That(layer.CalculatedLambda, Is.EqualTo(expectedLambda));
        }

        #endregion

        #region Calculations Tests

        [Test]
        public async Task UpdateCalculations_CalculatesR1Correctly()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5); // Бетон плотный
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            // R = d / λ / 1000 = 100 / 1.5 / 1000 = 0.0667 м²·К/Вт
            var expectedR1 = 100.0 / 1.5 / 1000.0;
            Assert.That(_viewModel.R1Total, Is.EqualTo(expectedR1).Within(0.0001));
        }

        [Test]
        public async Task UpdateCalculations_CalculatesR2Correctly()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersBelowPipe.Clear();

            var sand = _viewModel.AvailableMaterials.First(m => m.Id == 1); // Песок
            _viewModel.LayersBelowPipe.Add(new Layer
            {
                Material = sand,
                Thickness = 150,
                CalculatedLambda = sand.LambdaA,
                Position = LayerPosition.BelowPipe,
                Order = 0
            });

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            // R = d / λ / 1000 = 150 / 0.4 / 1000 = 0.375 м²·К/Вт
            var expectedR2 = 150.0 / 0.4 / 1000.0;
            Assert.That(_viewModel.R2Total, Is.EqualTo(expectedR2).Within(0.0001));
        }

        [Test]
        public async Task UpdateCalculations_CalculatesLambdaEFromFirstLayerAbovePipe()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5); // Бетон плотный
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            Assert.That(_viewModel.LambdaE, Is.EqualTo(concrete.LambdaA));
        }

        [Test]
        public async Task UpdateCalculations_NoLayersAbovePipe_ReturnsDefaultLambdaE()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            Assert.That(_viewModel.LambdaE, Is.EqualTo(1.6)); // Значение по умолчанию
        }

        #endregion

        #region Validation Tests

        [Test]
        public async Task Validate_NoLayers_ReturnsError()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.LayersBelowPipe.Clear();

            // Act
            _viewModel.Validate();

            // Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("хотя бы один слой"));
        }

        [Test]
        public async Task Validate_ThinLayersAbovePipe_ReturnsError()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.HasLoads = true;

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 30, // Меньше минимума (50 мм при нагрузках)
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.Validate();

            // Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Минимальная толщина"));
        }

        [Test]
        public async Task Validate_ValidConstruction_ReturnsTrue()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.ResetToDefaultCommand.Execute(null);

            // Act
            _viewModel.Validate();

            // Assert
            Assert.That(_viewModel.IsValid, Is.True);
        }

        #endregion

        #region Template Tests

        [Test]
        public async Task ApplyTemplate_CreatesLayersFromTemplate()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var template = _viewModel.Templates.First(t => t.Id == 1); // Типовая парковка

            // Act
            _viewModel.SelectedTemplate = template;
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(template.LayersAbovePipe.Count));
            Assert.That(_viewModel.LayersBelowPipe.Count, Is.EqualTo(template.LayersBelowPipe.Count));
        }

        [Test]
        public async Task ApplyTemplate_SetsGroundwaterLevel()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var template = _viewModel.Templates.First(t => t.Id == 3); // Въезд в гараж
            template.DefaultGroundwaterLevel = 1.5;

            // Act
            _viewModel.SelectedTemplate = template;
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.GroundwaterLevel, Is.EqualTo(1.5));
        }

        [Test]
        public async Task ApplyTemplate_SetsHasLoads()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var template = _viewModel.Templates.First(t => t.Id == 1); // Типовая парковка
            template.HasLoads = true;

            // Act
            _viewModel.SelectedTemplate = template;
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasLoads, Is.True);
        }

        #endregion

        #region Reset Tests

        [Test]
        public async Task ResetToDefault_ClearsLayersAndSetsDefaults()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            _viewModel.GroundwaterLevel = 0.5;
            _viewModel.HasLoads = true;

            // Act
            _viewModel.ResetToDefaultCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.GroundwaterLevel, Is.EqualTo(2.0));
            Assert.That(_viewModel.HasLoads, Is.False);
        }

        #endregion

        #region HasUnsavedChanges Tests

        [Test]
        public async Task AddLayer_SetsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.HasUnsavedChanges = false;

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
        }

        [Test]
        public async Task RemoveLayer_SetsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            _viewModel.HasUnsavedChanges = false;
            var layer = _viewModel.LayersAbovePipe.First();

            // Act
            _viewModel.RemoveLayerCommand.Execute(layer);

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
        }

        [Test]
        public async Task ChangeGroundwaterLevel_SetsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.HasUnsavedChanges = false;

            // Act
            _viewModel.GroundwaterLevel = 0.5;

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
        }

        [Test]
        public async Task ResetToDefault_ClearsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.HasUnsavedChanges = true;

            // Act
            _viewModel.ResetToDefaultCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.False);
        }

        #endregion

        #region TotalThickness Tests

        [Test]
        public async Task TotalThicknessAbovePipe_ReturnsCorrectSum()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            _viewModel.LayersAbovePipe.Add(new Layer { Material = concrete, Thickness = 100, Position = LayerPosition.AbovePipe });
            _viewModel.LayersAbovePipe.Add(new Layer { Material = concrete, Thickness = 50, Position = LayerPosition.AbovePipe });

            // Act & Assert
            Assert.That(_viewModel.TotalThicknessAbovePipe, Is.EqualTo(150));
        }

        [Test]
        public async Task TotalThicknessBelowPipe_ReturnsCorrectSum()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersBelowPipe.Clear();

            var sand = _viewModel.AvailableMaterials.First(m => m.Id == 1);
            _viewModel.LayersBelowPipe.Add(new Layer { Material = sand, Thickness = 150, Position = LayerPosition.BelowPipe });
            _viewModel.LayersBelowPipe.Add(new Layer { Material = sand, Thickness = 200, Position = LayerPosition.BelowPipe });

            // Act & Assert
            Assert.That(_viewModel.TotalThicknessBelowPipe, Is.EqualTo(350));
        }

        #endregion
    }

    #region Mock Classes

    /// <summary>
    /// Мок-сервис для тестов ViewModel
    /// </summary>
    internal class MockConstructionService : IConstructionService
    {
        public void CalculateThermalResistances(ConstructionModel construction)
        {
            foreach (var layer in construction.LayersAbovePipe)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }
            foreach (var layer in construction.Layers)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }
        }

        public double CalculateR1(System.Collections.Generic.IEnumerable<Layer> layersAbovePipe)
        {
            return layersAbovePipe.Sum(l => l.CalculatedR);
        }

        public double CalculateR2(System.Collections.Generic.IEnumerable<Layer> layersBelowPipe, double groundwaterLevel)
        {
            return layersBelowPipe.Sum(l => l.CalculatedR);
        }

        public double GetLambdaE(Layer? firstLayerAbovePipe)
        {
            return firstLayerAbovePipe?.Material?.LambdaA ?? 1.6;
        }

        public ValidationResult ValidateConstruction(ConstructionModel construction)
        {
            var validator = new ConstructionValidator();
            return validator.Validate(construction);
        }

        public ConstructionModel CreateFromTemplate(ConstructionTemplate template, System.Collections.Generic.IEnumerable<Material> materials)
        {
            var materialsList = materials.ToList();
            var construction = new ConstructionModel
            {
                GroundwaterLevel = template.DefaultGroundwaterLevel,
                HasLoads = template.HasLoads
            };

            foreach (var layerTemplate in template.LayersAbovePipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material != null)
                {
                    construction.AddLayerAbovePipe(material, layerTemplate.Thickness);
                }
            }

            foreach (var layerTemplate in template.LayersBelowPipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material != null)
                {
                    construction.AddLayerBelowPipe(material, layerTemplate.Thickness);
                }
            }

            return construction;
        }

        public double GetTotalThicknessAbovePipe(ConstructionModel construction)
        {
            return construction.LayersAbovePipe.Sum(l => l.Thickness);
        }

        public double GetTotalThicknessBelowPipe(ConstructionModel construction)
        {
            return construction.Layers.Where(l => l.Position == LayerPosition.BelowPipe).Sum(l => l.Thickness);
        }
    }

    /// <summary>
    /// Мок-репозиторий материалов для тестов
    /// </summary>
    internal class MockMaterialRepository : IMaterialRepository
    {
        private List<Material>? _materials;

        public bool IsLoaded => _materials != null;
        public int MaterialsCount => _materials?.Count ?? 0;

        public Task<System.Collections.Generic.IEnumerable<Material>> LoadMaterialsAsync()
        {
            _materials = Material.GetDefaultMaterials();
            return Task.FromResult<System.Collections.Generic.IEnumerable<Material>>(_materials);
        }

        public Material? GetMaterialById(int id)
        {
            return _materials?.FirstOrDefault(m => m.Id == id);
        }

        public System.Collections.Generic.IEnumerable<Material> GetMaterialsByCategory(MaterialCategory category)
        {
            return _materials?.Where(m => m.Category == category) ?? Enumerable.Empty<Material>();
        }

        public System.Collections.Generic.IEnumerable<Material> GetAllMaterials()
        {
            return _materials ?? Enumerable.Empty<Material>();
        }
    }

    /// <summary>
    /// Мок-репозиторий конструкций для тестов
    /// </summary>
    internal class MockConstructionRepository : IConstructionRepository
    {
        public Task SaveConstructionAsync(ConstructionModel construction, string filePath)
        {
            return Task.CompletedTask;
        }

        public Task<ConstructionModel?> LoadConstructionAsync(string filePath)
        {
            return Task.FromResult<ConstructionModel?>(null);
        }

        public Task SaveToProjectAsync(ConstructionModel construction, int projectId)
        {
            return Task.CompletedTask;
        }

        public Task<ConstructionModel?> LoadFromProjectAsync(int projectId)
        {
            return Task.FromResult<ConstructionModel?>(null);
        }

        public Task<System.Collections.Generic.IEnumerable<string>> GetSavedConstructionsAsync(string directoryPath)
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }

    #endregion
}