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
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Unit tests for <see cref="MaterialEditorViewModel"/>.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class MaterialEditorViewModelTests
    {
        private MaterialEditorViewModel _viewModel = null!;
        private TestMaterialRepository _materialRepository = null!;
        private Mock<IConstructionTemplateRepository> _templateRepositoryMock = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private MaterialCrudValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _materialRepository = new TestMaterialRepository();
            var seededMaterials = Material.GetDefaultMaterials().ToList();
            seededMaterials.Add(new Material
            {
                Id = 100,
                Name = "Пользовательский материал",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.0,
                LambdaB = 1.2,
                IsBuiltIn = false
            });
            _materialRepository.Seed(seededMaterials);
            _validator = new MaterialCrudValidator(_materialRepository);
            _templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            _dialogServiceMock = new Mock<IDialogService>();
            _viewModel = new MaterialEditorViewModel(
                _materialRepository,
                _templateRepositoryMock.Object,
                _validator,
                _dialogServiceMock.Object);
        }

        [Test]
        public async Task InitializeAsync_LoadsMaterialsIntoCollection()
        {
            // Act
            await _viewModel.InitializeAsync();

            // Assert
            Assert.That(_viewModel.Materials.Count, Is.GreaterThan(0));
            Assert.That(_viewModel.IsLoading, Is.False);
        }

        [Test]
        public void AddCommand_CreatesNewUserMaterialAndSelectsItForEdit()
        {
            // Act
            _viewModel.AddCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.EditingMaterial, Is.Not.Null);
            Assert.That(_viewModel.EditingMaterial!.IsBuiltIn, Is.False);
            Assert.That(_viewModel.EditingMaterial.Id, Is.EqualTo(0));
        }

        [Test]
        public async Task SaveCommand_NewValidMaterial_AddsAndSaves()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.EditingMaterial!.Name = "Тестовый материал";
            _viewModel.EditingMaterial.LambdaA = 1.0;
            _viewModel.EditingMaterial.LambdaB = 1.2;
            _viewModel.EditingMaterial.Category = MaterialCategory.Concrete;

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Empty);
            Assert.That(_materialRepository.GetAllMaterials().Any(m => m.Name == "Тестовый материал"), Is.True);
        }

        [Test]
        public async Task SaveCommand_UpdateExisting_UpdatesAndSaves()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var existing = _materialRepository.GetAllMaterials().First(m => !m.IsBuiltIn);
            _viewModel.SelectedMaterial = existing;
            _viewModel.EditingMaterial!.Name = "Обновлённый пользовательский";

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Empty);
            Assert.That(_materialRepository.GetMaterialById(existing.Id)!.Name, Is.EqualTo("Обновлённый пользовательский"));
        }

        [Test]
        public async Task SaveCommand_InvalidMaterial_SetsValidationError()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            _viewModel.AddCommand.Execute(null);
            _viewModel.EditingMaterial!.Name = string.Empty;

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.ErrorMessage, Is.Not.Empty);
            Assert.That(_materialRepository.GetAllMaterials().Count(), Is.EqualTo(Material.GetDefaultMaterials().Count + 1));
        }

        [Test]
        public void SelectedBuiltInMaterial_ShowsReadOnlyClone()
        {
            // Arrange
            var builtIn = _materialRepository.GetAllMaterials().First(m => m.IsBuiltIn);

            // Act
            _viewModel.SelectedMaterial = builtIn;

            // Assert
            Assert.That(_viewModel.EditingMaterial, Is.Not.Null);
            Assert.That(_viewModel.EditingMaterial!.Name, Is.EqualTo(builtIn.Name));
            Assert.That(_viewModel.IsBuiltInSelected, Is.True);
            Assert.That(_viewModel.CanEditMaterial, Is.False);
        }

        [Test]
        public void AddCommand_ActivatesSaveCommand()
        {
            // Act
            _viewModel.AddCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.EditingMaterial, Is.Not.Null);
            Assert.That(_viewModel.CanEditMaterial, Is.True);
            Assert.That(_viewModel.IsBuiltInSelected, Is.False);
            Assert.That(_viewModel.SaveCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void AddCommand_EnablesEditingAndSave()
        {
            // Act
            _viewModel.AddCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.EditingMaterial, Is.Not.Null);
            Assert.That(_viewModel.CanEditMaterial, Is.True);
            Assert.That(_viewModel.IsBuiltInSelected, Is.False);
            Assert.That(_viewModel.SaveCommand.CanExecute(null), Is.True);
        }

        [Test]
        public async Task DeleteCommand_UserMaterial_DeletesAndSaves()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var userMaterial = _materialRepository.GetAllMaterials().First(m => !m.IsBuiltIn);
            _viewModel.SelectedMaterial = userMaterial;
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConstructionTemplate>());

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_materialRepository.GetMaterialById(userMaterial.Id), Is.Null);
            Assert.That(_viewModel.SelectedMaterial, Is.Null);
        }

        [Test]
        public async Task DeleteCommand_BuiltInMaterial_BlockedByDialog()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var builtIn = _materialRepository.GetAllMaterials().First(m => m.IsBuiltIn);
            _viewModel.SelectedMaterial = builtIn;
            var countBefore = _materialRepository.GetAllMaterials().Count();

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_materialRepository.GetAllMaterials().Count(), Is.EqualTo(countBefore));
            _dialogServiceMock.Verify(d => d.ShowError(It.Is<string>(s => s.Contains("встроенный")), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task DeleteCommand_ReferencedByTemplate_BlockedByDialog()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var material = _materialRepository.GetAllMaterials().First(m => !m.IsBuiltIn);
            _viewModel.SelectedMaterial = material;
            var template = new ConstructionTemplate
            {
                Id = 1,
                Name = "Шаблон с материалом",
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new() { MaterialId = material.Id, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>()
            };
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConstructionTemplate> { template });

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_materialRepository.GetMaterialById(material.Id), Is.Not.Null);
            _dialogServiceMock.Verify(d => d.ShowError(It.Is<string>(s => s.Contains("шаблонах")), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task DeleteCommand_ReferencedByConstruction_BlockedByDialog()
        {
            // Arrange
            var construction = new ConstructionModel();
            var material = _materialRepository.GetAllMaterials().First(m => !m.IsBuiltIn);
            construction.AddLayerAbovePipe(material, 50);

            _viewModel = new MaterialEditorViewModel(
                _materialRepository,
                _templateRepositoryMock.Object,
                _validator,
                _dialogServiceMock.Object,
                construction);
            await _viewModel.InitializeAsync();
            _viewModel.SelectedMaterial = material;
            _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConstructionTemplate>());

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_materialRepository.GetMaterialById(material.Id), Is.Not.Null);
            _dialogServiceMock.Verify(d => d.ShowError(It.Is<string>(s => s.Contains("конструкции")), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Lightweight in-memory implementation of <see cref="IMaterialRepository"/> for editor tests.
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
                var defaults = Material.GetDefaultMaterials();
                foreach (var material in _materials)
                {
                    if (defaults.Any(d =>
                        d.Id == material.Id &&
                        string.Equals(d.Name, material.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        material.IsBuiltIn = true;
                    }
                }
            }
        }
    }
}
