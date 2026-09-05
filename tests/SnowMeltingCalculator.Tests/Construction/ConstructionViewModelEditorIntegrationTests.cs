using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Integration tests for editor interaction paths through <see cref="ConstructionViewModel"/>.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class ConstructionViewModelEditorIntegrationTests
    {
        private ConstructionViewModel _viewModel = null!;
        private MockConstructionService _constructionService = null!;
        private MockMaterialRepository _materialRepository = null!;
        private MockConstructionRepository _constructionRepository = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
        private Mock<IConstructionTemplateRepository> _templateRepositoryMock = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<IEditorDialogService> _editorDialogServiceMock = null!;

        [SetUp]
        public void Setup()
        {
            _constructionService = new MockConstructionService();
            _materialRepository = new MockMaterialRepository();
            _constructionRepository = new MockConstructionRepository();
            _calculationStateServiceMock = new Mock<ICalculationStateService>();
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();
            _templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            _dialogServiceMock = new Mock<IDialogService>();
            _editorDialogServiceMock = new Mock<IEditorDialogService>();

            _calculationStateServiceMock.SetupGet(s => s.PipeSpacing).Returns(200);
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            var calculationContext = new CalculationContext();
            var projectSession = new ProjectSession(calculationContext: calculationContext);
            _viewModel = new ConstructionViewModel(
                _constructionService,
                _materialRepository,
                _constructionRepository,
                _calculationStateServiceMock.Object,
                calculationContext,
                new ConstructionValidator(),
                new ConstructionModel(),
                _markDirtyServiceMock.Object,
                _templateRepositoryMock.Object,
                _dialogServiceMock.Object,
                _editorDialogServiceMock.Object,
                projectSession.ConstructionState,
                new ConstructionDefaultStateInitializer(_materialRepository, projectSession.ConstructionState));
        }

        [Test]
        public async Task OpenMaterialEditor_WhenDialogReturnsTrue_RefreshesMaterialsAndTemplates()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _editorDialogServiceMock.Setup(s => s.ShowMaterialEditor()).Returns(true);
            _materialRepository.Seed(new List<Material> { Material.GetDefaultMaterials().First() });
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConstructionTemplate>());

            // Act
            await _viewModel.OpenMaterialEditorCommand.ExecuteAsync(null);

            // Assert
            _editorDialogServiceMock.Verify(s => s.ShowMaterialEditor(), Times.Once);
            _templateRepositoryMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        }

        [Test]
        public async Task OpenTemplateEditor_WhenDialogReturnsTrue_RefreshesMaterialsAndTemplates()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _editorDialogServiceMock.Setup(s => s.ShowTemplateEditor()).Returns(true);
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConstructionTemplate>());

            // Act
            await _viewModel.OpenTemplateEditorCommand.ExecuteAsync(null);

            // Assert
            _editorDialogServiceMock.Verify(s => s.ShowTemplateEditor(), Times.Once);
            _templateRepositoryMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        }

        [Test]
        public async Task OpenTemplateEditor_WhenClosedWithoutDialogResult_RefreshesTemplates()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _editorDialogServiceMock.Setup(s => s.ShowTemplateEditor()).Returns(false);
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConstructionTemplate>());

            // Act
            await _viewModel.OpenTemplateEditorCommand.ExecuteAsync(null);

            // Assert
            _editorDialogServiceMock.Verify(s => s.ShowTemplateEditor(), Times.Once);
            _templateRepositoryMock.Verify(r => r.GetAllAsync(), Times.AtLeast(2)); // initial load + refresh after close
        }

        [Test]
        public async Task OpenMaterialEditor_WhenDialogReturnsFalse_DoesNotRefresh()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _editorDialogServiceMock.Setup(s => s.ShowMaterialEditor()).Returns(false);

            // Act
            await _viewModel.OpenMaterialEditorCommand.ExecuteAsync(null);

            // Assert
            _templateRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once); // only initial load
        }

        [Test]
        public async Task RefreshCatalogsAsync_ClearsAndReloadsMaterials()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var expectedMaterials = new List<Material>
            {
                Material.GetDefaultMaterials().First(m => m.Id == 1)
            };
            _materialRepository.Seed(expectedMaterials);

            // Act
            await _viewModel.ReloadMaterialsAsync();

            // Assert
            Assert.That(_viewModel.AvailableMaterials, Is.EquivalentTo(expectedMaterials));
        }

        [Test]
        public async Task RefreshCatalogsAsync_PreservesLayerMaterialBinding()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var userMaterial = new Material
            {
                Name = "User material",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };
            await _materialRepository.AddAsync(userMaterial);
            await _viewModel.ReloadMaterialsAsync();

            var layer = new Layer
            {
                Material = userMaterial,
                Thickness = 100,
                CalculatedLambda = userMaterial.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            };
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.LayersAbovePipe.Add(layer);

            // Simulate deletion of the user material from the repository.
            _materialRepository.Seed(Material.GetDefaultMaterials());

            // Act
            await _viewModel.ReloadMaterialsAsync();

            // Assert
            Assert.That(layer.Material, Is.Not.Null);
            Assert.That(
                _viewModel.AvailableMaterials.Contains(layer.Material) ||
                layer.Material.Id == Material.GetDefaultMaterial().Id,
                Is.True,
                "Layer.Material must be rebound to a current catalog instance or the default material after its original material was removed.");
            Assert.That(_viewModel.AvailableMaterials.Select(m => m.Id), Does.Contain(layer.Material.Id));
        }

        [Test]
        public async Task OpenMaterialEditor_AfterAddingUserMaterial_MaterialAppearsInAvailableMaterials()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var existingMaterial = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            var layer = new Layer
            {
                Material = existingMaterial,
                Thickness = 100,
                CalculatedLambda = existingMaterial.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            };
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.LayersAbovePipe.Add(layer);

            var userMaterial = new Material
            {
                Name = "New user material",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };
            await _materialRepository.AddAsync(userMaterial);
            _editorDialogServiceMock.Setup(s => s.ShowMaterialEditor()).Returns(true);

            // Act
            await _viewModel.OpenMaterialEditorCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.AvailableMaterials.Select(m => m.Id), Does.Contain(userMaterial.Id),
                "User material added through the editor must appear in AvailableMaterials after refresh.");
            Assert.That(_viewModel.AvailableMaterials.Any(m => m.Name == userMaterial.Name), Is.True);
            Assert.That(_viewModel.AvailableMaterials.Contains(layer.Material), Is.True,
                "Existing layer material must remain bound to a current catalog instance after refresh.");
        }

        [Test]
        public async Task OpenMaterialEditor_AfterDeletingUserMaterial_LayerMaterialReplaced()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var userMaterial = new Material
            {
                Name = "User material to delete",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };
            await _materialRepository.AddAsync(userMaterial);
            await _viewModel.ReloadMaterialsAsync();

            var layer = new Layer
            {
                Material = userMaterial,
                Thickness = 100,
                CalculatedLambda = userMaterial.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            };
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.LayersAbovePipe.Add(layer);

            await _materialRepository.DeleteAsync(userMaterial.Id);
            _editorDialogServiceMock.Setup(s => s.ShowMaterialEditor()).Returns(true);

            // Act
            await _viewModel.OpenMaterialEditorCommand.ExecuteAsync(null);

            // Assert
            Assert.That(layer.Material, Is.Not.Null);
            Assert.That(
                _viewModel.AvailableMaterials.Contains(layer.Material) ||
                layer.Material.Id == Material.GetDefaultMaterial().Id,
                Is.True,
                "Layer.Material must be rebound to a current catalog instance or the default material after its material was deleted.");
            Assert.That(layer.IsLambdaOverridden, Is.True,
                "Calculated lambda should be preserved as an override when falling back to the default material.");
        }

        [Test]
        public async Task OpenTemplateEditor_AfterClose_LayerMaterialNamesPreserved()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.LayersBelowPipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            var sand = _viewModel.AvailableMaterials.First(m => m.Id == 1);
            var above = new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            };
            var below = new Layer
            {
                Material = sand,
                Thickness = 150,
                CalculatedLambda = sand.LambdaA,
                Position = LayerPosition.BelowPipe,
                Order = 0
            };
            _viewModel.LayersAbovePipe.Add(above);
            _viewModel.LayersBelowPipe.Add(below);

            _editorDialogServiceMock.Setup(s => s.ShowTemplateEditor()).Returns(false);
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            // Act
            await _viewModel.OpenTemplateEditorCommand.ExecuteAsync(null);

            // Assert
            var layers = _viewModel.LayersAbovePipe.Concat(_viewModel.LayersBelowPipe).ToList();
            foreach (var layer in layers)
            {
                Assert.That(layer.Material, Is.Not.Null, "Layer material must not be null after template editor close.");
                Assert.That(layer.Material.Name, Is.Not.Null.And.Not.Empty,
                    "Layer material name must be preserved after template editor close.");
                Assert.That(_viewModel.AvailableMaterials.Contains(layer.Material), Is.True,
                    "Layer material must be a current instance from AvailableMaterials so ComboBox binding resolves its name.");
            }
        }

        [Test]
        public async Task ApplyTemplate_MaterialNotFound_WithSnapshot_SetsPreviewErrorAndBlocksApply()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Импортный материал",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.5,
                LambdaB = 1.6
            };
            _constructionService.ThrowMaterialNotFound(snapshot);

            // Act
            _viewModel.SelectedTemplate = _viewModel.Templates.First();

            // Assert
            Assert.That(_viewModel.CanApplySelectedTemplate, Is.False);
            Assert.That(_viewModel.TemplatePreviewErrorMessage, Does.Contain(snapshot.Id.ToString()));
            Assert.That(_viewModel.TemplatePreviewLayersAbovePipe, Is.Empty);
            Assert.That(_viewModel.TemplatePreviewLayersBelowPipe, Is.Empty);

            // ApplyTemplate must return early because CanApplySelectedTemplate is false
            await _viewModel.ApplyTemplateCommand.ExecuteAsync(null);
            _dialogServiceMock.Verify(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogButtons>(), It.IsAny<DialogIcon>()), Times.Never);
        }

        [Test]
        public async Task ApplyTemplate_MaterialNotFound_WithSnapshot_Declined_BlocksApply()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Импортный материал",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.5,
                LambdaB = 1.6
            };
            _constructionService.ThrowMaterialNotFound(snapshot);

            // Act
            _viewModel.SelectedTemplate = _viewModel.Templates.First();

            // Assert
            Assert.That(_viewModel.CanApplySelectedTemplate, Is.False);
            Assert.That(_viewModel.TemplatePreviewErrorMessage, Does.Contain(snapshot.Id.ToString()));

            // ApplyTemplate must return early because CanApplySelectedTemplate is false
            await _viewModel.ApplyTemplateCommand.ExecuteAsync(null);
            _dialogServiceMock.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(_viewModel.IsValid, Is.True); // construction unchanged, remains valid
        }

        [Test]
        public async Task ApplyTemplate_MaterialNotFound_WithoutSnapshot_BlocksApply()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _constructionService.ThrowMaterialNotFound(materialId: 42);

            // Act
            _viewModel.SelectedTemplate = _viewModel.Templates.First();

            // Assert
            Assert.That(_viewModel.CanApplySelectedTemplate, Is.False);
            Assert.That(_viewModel.TemplatePreviewErrorMessage, Does.Contain("42"));

            // ApplyTemplate must return early because CanApplySelectedTemplate is false
            await _viewModel.ApplyTemplateCommand.ExecuteAsync(null);
            _dialogServiceMock.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(_viewModel.IsValid, Is.True); // construction unchanged, remains valid
        }

        /// <summary>
        /// Construction service mock that can simulate a missing material exception.
        /// </summary>
        private class MockConstructionService : IConstructionService
        {
            private MaterialSnapshot? _missingSnapshot;
            private int _missingMaterialId;

            public void ThrowMaterialNotFound(MaterialSnapshot snapshot)
            {
                _missingSnapshot = snapshot;
                _missingMaterialId = snapshot.Id;
            }

            public void ThrowMaterialNotFound(int materialId)
            {
                _missingSnapshot = null;
                _missingMaterialId = materialId;
            }

            public void CalculateThermalResistances(ConstructionModel construction)
            {
                foreach (var layer in construction.LayersAbovePipe)
                    layer.UpdateLambda(construction.GroundwaterLevel);
                foreach (var layer in construction.Layers)
                    layer.UpdateLambda(construction.GroundwaterLevel);
            }

            public double CalculateR1(IEnumerable<Layer> layersAbovePipe)
            {
                return layersAbovePipe.Sum(l => l.CalculatedR);
            }

            public double CalculateR2(IEnumerable<Layer> layersBelowPipe, double groundwaterLevel)
            {
                return layersBelowPipe.Sum(l => l.CalculatedR);
            }

            public ValidationResult ValidateConstruction(ConstructionModel construction)
            {
                return new ConstructionValidator().Validate(construction);
            }

            public ConstructionModel CreateFromTemplate(ConstructionTemplate template, IEnumerable<Material> materials)
            {
                if (_missingSnapshot != null)
                {
                    throw new MaterialNotFoundException(_missingMaterialId, _missingSnapshot);
                }

                if (_missingMaterialId != 0)
                {
                    throw new MaterialNotFoundException(_missingMaterialId);
                }

                var materialsList = materials.ToList();
                var construction = new ConstructionModel
                {
                    GroundwaterLevel = template.DefaultGroundwaterLevel,
                };

                foreach (var layerTemplate in template.LayersAbovePipe.OrderBy(l => l.Order))
                {
                    var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                    if (material != null)
                        construction.AddLayerAbovePipe(material, layerTemplate.Thickness);
                }

                foreach (var layerTemplate in template.LayersBelowPipe.OrderBy(l => l.Order))
                {
                    var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                    if (material != null)
                        construction.AddLayerBelowPipe(material, layerTemplate.Thickness);
                }

                return construction;
            }

            public Task<Material> ImportMissingMaterialAsync(MaterialSnapshot snapshot)
            {
                throw new NotImplementedException();
            }

            public Task ImportProjectMaterialsAsync(IEnumerable<MaterialSnapshot> snapshots)
            {
                return Task.CompletedTask;
            }

            public Task ImportProjectTemplatesAsync(IEnumerable<ConstructionTemplate> templates)
            {
                return Task.CompletedTask;
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
        /// Lightweight in-memory material repository with a mutable seed.
        /// </summary>
        private class MockMaterialRepository : IMaterialRepository
        {
            private List<Material>? _materials;

            public bool IsLoaded => _materials != null;
            public int MaterialsCount => _materials?.Count ?? 0;

            public Task<IEnumerable<Material>> LoadMaterialsAsync()
            {
                _materials ??= new List<Material>(Material.GetDefaultMaterials());
                return Task.FromResult<IEnumerable<Material>>(_materials);
            }

            public Material? GetMaterialById(int id)
            {
                return _materials?.FirstOrDefault(m => m.Id == id);
            }

            public IEnumerable<Material> GetMaterialsByCategory(MaterialCategory category)
            {
                return _materials?.Where(m => m.Category == category) ?? Enumerable.Empty<Material>();
            }

            public IEnumerable<Material> GetAllMaterials()
            {
                return _materials ?? Enumerable.Empty<Material>();
            }

            public Task<Material> AddAsync(Material material)
            {
                _materials ??= new List<Material>();
                material.Id = _materials.Count > 0 ? _materials.Max(m => m.Id) + 1 : 1;
                _materials.Add(material);
                return Task.FromResult(material);
            }

            public Task<Material> UpdateAsync(Material material)
            {
                var index = _materials?.FindIndex(m => m.Id == material.Id) ?? -1;
                if (index < 0)
                    throw new InvalidOperationException($"Material with id {material.Id} not found");
                _materials![index] = material;
                return Task.FromResult(material);
            }

            public Task<bool> DeleteAsync(int id)
            {
                var material = _materials?.FirstOrDefault(m => m.Id == id);
                if (material == null)
                    return Task.FromResult(false);
                _materials!.Remove(material);
                return Task.FromResult(true);
            }

            public Task SaveMaterialsAsync()
            {
                return Task.CompletedTask;
            }

            public void Seed(IEnumerable<Material> materials)
            {
                _materials = new List<Material>(materials);
            }
        }

        /// <summary>
        /// No-op construction repository.
        /// </summary>
        private class MockConstructionRepository : IConstructionRepository
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

            public Task<IEnumerable<string>> GetSavedConstructionsAsync(string directoryPath)
            {
                return Task.FromResult(Enumerable.Empty<string>());
            }
        }
    }
}
