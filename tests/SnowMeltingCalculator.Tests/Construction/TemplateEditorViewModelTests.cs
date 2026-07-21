using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.ViewModels.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Unit tests for <see cref="TemplateEditorViewModel"/>.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class TemplateEditorViewModelTests
    {
        private TemplateEditorViewModel _viewModel = null!;
        private TestMaterialRepository _materialRepository = null!;
        private TestTemplateRepository _templateRepository = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private ConstructionTemplateValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _materialRepository = new TestMaterialRepository();
            _materialRepository.Seed(Material.GetDefaultMaterials());
            _templateRepository = new TestTemplateRepository();
            var seededTemplates = ConstructionTemplate.GetDefaultTemplates().ToList();
            seededTemplates.Add(new ConstructionTemplate
            {
                Id = 100,
                Name = "Пользовательский шаблон",
                IsBuiltIn = false,
                DefaultGroundwaterLevel = 2.0,
                HasLoads = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new() { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>()
            });
            _templateRepository.Seed(seededTemplates);
            _validator = new ConstructionTemplateValidator(_materialRepository);
            _dialogServiceMock = new Mock<IDialogService>();
            _viewModel = new TemplateEditorViewModel(
                _materialRepository,
                _templateRepository,
                _validator,
                _dialogServiceMock.Object);
        }

        [Test]
        public async Task InitializeAsync_LoadsMaterialsAndTemplates()
        {
            // Arrange
            _templateRepository.Seed(ConstructionTemplate.GetDefaultTemplates());

            // Act
            await _viewModel.InitializeAsync();

            // Assert
            Assert.That(_viewModel.AvailableMaterials.Count, Is.GreaterThan(0));
            Assert.That(_viewModel.Templates.Count, Is.GreaterThan(0));
            Assert.That(_viewModel.IsLoading, Is.False);
        }

        [Test]
        public async Task AddCommand_CreatesNewUserTemplateWithDefaultLayer()
        {
            // Arrange
            await _viewModel.InitializeAsync();

            // Act
            _viewModel.AddCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.EditingTemplate, Is.Not.Null);
            Assert.That(_viewModel.EditingTemplate!.IsBuiltIn, Is.False);
            Assert.That(_viewModel.EditingLayersAbovePipe.Count, Is.EqualTo(1));
            Assert.That(_viewModel.EditingLayersBelowPipe.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task SaveCommand_NewValidTemplate_AddsAndSaves()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.EditingTemplate!.Name = "Тестовый шаблон";
            _viewModel.EditingLayersAbovePipe[0].Material = _materialRepository.GetAllMaterials().First(m => m.Id == 5);
            _viewModel.EditingLayersAbovePipe[0].Thickness = 100;

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Empty);
            Assert.That(_templateRepository.GetAllAsync().Result.Any(t => t.Name == "Тестовый шаблон"), Is.True);
        }

        [Test]
        public async Task SaveCommand_UpdateExisting_UpdatesAndSaves()
        {
            // Arrange
            var existing = new ConstructionTemplate
            {
                Id = 100,
                Name = "Существующий",
                IsBuiltIn = false,
                DefaultGroundwaterLevel = 2.0,
                HasLoads = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new() { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>()
            };
            _templateRepository.Seed(new List<ConstructionTemplate> { existing });
            await _viewModel.InitializeAsync();
            _viewModel.SelectedTemplate = existing;
            _viewModel.EditingTemplate!.Name = "Обновлённый шаблон";

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Empty);
            Assert.That(_templateRepository.GetByIdAsync(100).Result!.Name, Is.EqualTo("Обновлённый шаблон"));
        }

        [Test]
        public async Task SaveCommand_InvalidTemplate_SetsValidationError()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.EditingTemplate!.Name = string.Empty;
            _viewModel.EditingLayersAbovePipe[0].Material = _materialRepository.GetAllMaterials().First(m => m.Id == 5);

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public async Task SaveCommand_UnknownMaterial_SetsValidationError()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.EditingTemplate!.Name = "Шаблон с битым материалом";
            _viewModel.EditingLayersAbovePipe[0].Material = null;
            _viewModel.EditingLayersAbovePipe[0].Thickness = 100;

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public async Task SelectedBuiltInTemplate_ShowsReadOnlyClone()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var builtIn = _viewModel.Templates.First(t => t.IsBuiltIn);

            // Act
            _viewModel.SelectedTemplate = builtIn;

            // Assert
            Assert.That(_viewModel.EditingTemplate, Is.Not.Null);
            Assert.That(_viewModel.EditingTemplate!.Name, Is.EqualTo(builtIn.Name));
            Assert.That(_viewModel.IsBuiltInSelected, Is.True);
            Assert.That(_viewModel.CanEditTemplate, Is.False);
        }

        [Test]
        public async Task DeleteCommand_UserTemplate_DeletesAndSaves()
        {
            // Arrange
            var userTemplate = new ConstructionTemplate
            {
                Id = 200,
                Name = "Пользовательский",
                IsBuiltIn = false,
                DefaultGroundwaterLevel = 2.0,
                HasLoads = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new() { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>()
            };
            _templateRepository.Seed(new List<ConstructionTemplate> { userTemplate });
            await _viewModel.InitializeAsync();
            _viewModel.SelectedTemplate = userTemplate;

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_templateRepository.GetByIdAsync(200).Result, Is.Null);
            Assert.That(_viewModel.SelectedTemplate, Is.Null);
        }

        [Test]
        public async Task DeleteCommand_BuiltInTemplate_BlockedByDialog()
        {
            // Arrange
            var builtIn = ConstructionTemplate.GetDefaultTemplates().First();
            _templateRepository.Seed(new List<ConstructionTemplate> { builtIn });
            await _viewModel.InitializeAsync();
            _viewModel.SelectedTemplate = builtIn;
            var countBefore = _templateRepository.GetAllAsync().Result.Count();

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_templateRepository.GetAllAsync().Result.Count(), Is.EqualTo(countBefore));
            _dialogServiceMock.Verify(d => d.ShowError(It.Is<string>(s => s.Contains("встроенный")), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task AddLayerAbovePipeCommand_AddsLayerToAboveCollection()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            var countBefore = _viewModel.EditingLayersAbovePipe.Count;

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.EditingLayersAbovePipe.Count, Is.EqualTo(countBefore + 1));
        }

        [Test]
        public async Task AddLayerBelowPipeCommand_AddsLayerToBelowCollection()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            var countBefore = _viewModel.EditingLayersBelowPipe.Count;

            // Act
            _viewModel.AddLayerBelowPipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.EditingLayersBelowPipe.Count, Is.EqualTo(countBefore + 1));
        }

        [Test]
        public async Task RemoveLayerAbovePipeCommand_RemovesLayerFromAboveCollection()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            var layer = _viewModel.EditingLayersAbovePipe[1];
            var countBefore = _viewModel.EditingLayersAbovePipe.Count;

            // Act
            _viewModel.RemoveLayerAbovePipeCommand.Execute(layer);

            // Assert
            Assert.That(_viewModel.EditingLayersAbovePipe.Count, Is.EqualTo(countBefore - 1));
        }

        [Test]
        public async Task RemoveLayerBelowPipeCommand_RemovesLayerFromBelowCollection()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            var layer = _viewModel.EditingLayersBelowPipe[0];
            var countBefore = _viewModel.EditingLayersBelowPipe.Count;

            // Act
            _viewModel.RemoveLayerBelowPipeCommand.Execute(layer);

            // Assert
            Assert.That(_viewModel.EditingLayersBelowPipe.Count, Is.EqualTo(countBefore - 1));
        }

        [Test]
        public async Task SaveCommand_SplitsLayersIntoAboveAndBelow()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.EditingTemplate!.Name = "Шаблон с двумя секциями";
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            var saved = _templateRepository.GetAllAsync().Result.First(t => t.Name == "Шаблон с двумя секциями");
            Assert.That(saved.LayersAbovePipe.Count, Is.EqualTo(2));
            Assert.That(saved.LayersBelowPipe.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Lightweight in-memory implementation of <see cref="IMaterialRepository"/>.
        /// </summary>
        private class TestMaterialRepository : IMaterialRepository
        {
            private List<Material>? _materials;

            public bool IsLoaded => _materials != null;
            public int MaterialsCount => _materials?.Count ?? 0;

            public Task<IEnumerable<Material>> LoadMaterialsAsync()
            {
                _materials ??= new List<Material>();
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
                {
                    throw new InvalidOperationException($"Material with id {material.Id} not found");
                }
                _materials![index] = material;
                return Task.FromResult(material);
            }

            public Task<bool> DeleteAsync(int id)
            {
                var material = _materials?.FirstOrDefault(m => m.Id == id);
                if (material == null)
                {
                    return Task.FromResult(false);
                }
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
        /// Lightweight in-memory implementation of <see cref="IConstructionTemplateRepository"/>.
        /// </summary>
        private class TestTemplateRepository : IConstructionTemplateRepository
        {
            private List<ConstructionTemplate> _templates = new();
            private int _nextId = 1;

            public Task<IEnumerable<ConstructionTemplate>> GetAllAsync()
            {
                return Task.FromResult<IEnumerable<ConstructionTemplate>>(_templates);
            }

            public Task<ConstructionTemplate?> GetByIdAsync(int id)
            {
                return Task.FromResult<ConstructionTemplate?>(_templates.FirstOrDefault(t => t.Id == id));
            }

            public Task<ConstructionTemplate> AddAsync(ConstructionTemplate template)
            {
                template.Id = _nextId++;
                _templates.Add(template);
                return Task.FromResult(template);
            }

            public Task<ConstructionTemplate> UpdateAsync(ConstructionTemplate template)
            {
                var index = _templates.FindIndex(t => t.Id == template.Id);
                if (index < 0)
                {
                    throw new InvalidOperationException($"Template with id {template.Id} not found");
                }
                _templates[index] = template;
                return Task.FromResult(template);
            }

            public Task<bool> DeleteAsync(int id)
            {
                var template = _templates.FirstOrDefault(t => t.Id == id);
                if (template == null)
                {
                    return Task.FromResult(false);
                }
                _templates.Remove(template);
                return Task.FromResult(true);
            }

            public Task SaveAsync()
            {
                return Task.CompletedTask;
            }

            public void Seed(IEnumerable<ConstructionTemplate> templates)
            {
                _templates = new List<ConstructionTemplate>(templates);
                var defaults = ConstructionTemplate.GetDefaultTemplates();
                foreach (var template in _templates)
                {
                    if (defaults.Any(d =>
                        d.Id == template.Id &&
                        string.Equals(d.Name, template.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        template.IsBuiltIn = true;
                    }
                }
                _nextId = _templates.Count > 0 ? _templates.Max(t => t.Id) + 1 : 1;
            }
        }
    }
}
