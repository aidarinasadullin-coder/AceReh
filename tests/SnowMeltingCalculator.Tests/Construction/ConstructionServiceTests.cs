using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SnowMeltingCalculator.Services.Reports.Calculation;
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
            var materialRepoMock = new Mock<IMaterialRepository>();
            var nextId = 100;
            materialRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Material>()))
                .ReturnsAsync((Material m) =>
                {
                    if (m.Id == 0) m.Id = nextId++;
                    return m;
                });
            var templateRepoMock = new Mock<IConstructionTemplateRepository>();
            templateRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            _service = new ConstructionService(
                new ConstructionValidator(),
                materialRepoMock.Object,
                templateRepoMock.Object);
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

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон");
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

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон");
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

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон");
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
        public void CreateFromTemplate_InvalidMaterialId_ThrowsMaterialNotFoundException()
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
            Assert.Throws<MaterialNotFoundException>(() => _service.CreateFromTemplate(template, materials));
        }

        #endregion

        #region ImportProjectMaterialsAsync Tests

        [Test]
        public async Task ImportProjectMaterialsAsync_NewMaterials_AddsToRepository()
        {
            // Arrange
            var repo = new TestMaterialRepository();
            repo.Seed(Material.GetDefaultMaterials());
            var service = new ConstructionService(new ConstructionValidator(), repo, new Mock<IConstructionTemplateRepository>().Object);

            var snapshots = new List<MaterialSnapshot>
            {
                new MaterialSnapshot { Id = 999, Name = "Custom A", Category = MaterialCategory.Concrete, LambdaA = 1.0, LambdaB = 1.1, IsBuiltIn = false },
                new MaterialSnapshot { Id = 998, Name = "Custom B", Category = MaterialCategory.Insulation, LambdaA = 0.5, LambdaB = 0.6, IsBuiltIn = false }
            };

            // Act
            await service.ImportProjectMaterialsAsync(snapshots);

            // Assert
            Assert.That(repo.GetAllMaterials().Any(m => m.Name == "Custom A"), Is.True);
            Assert.That(repo.GetAllMaterials().Any(m => m.Name == "Custom B"), Is.True);
        }

        [Test]
        public async Task ImportProjectMaterialsAsync_ExistingId_Skips()
        {
            // Arrange
            var repo = new TestMaterialRepository();
            repo.Seed(Material.GetDefaultMaterials());
            var service = new ConstructionService(new ConstructionValidator(), repo, new Mock<IConstructionTemplateRepository>().Object);

            var existing = repo.GetAllMaterials().First();
            var countBefore = repo.GetAllMaterials().Count();

            var snapshots = new List<MaterialSnapshot>
            {
                new MaterialSnapshot { Id = existing.Id, Name = "New Name", Category = MaterialCategory.Concrete, LambdaA = 1.0, LambdaB = 1.1, IsBuiltIn = false }
            };

            // Act
            await service.ImportProjectMaterialsAsync(snapshots);

            // Assert
            Assert.That(repo.GetAllMaterials().Count(), Is.EqualTo(countBefore));
        }

        [Test]
        public async Task ImportProjectMaterialsAsync_ExistingNameCaseInsensitive_Skips()
        {
            // Arrange
            var repo = new TestMaterialRepository();
            repo.Seed(Material.GetDefaultMaterials());
            var service = new ConstructionService(new ConstructionValidator(), repo, new Mock<IConstructionTemplateRepository>().Object);

            var existing = repo.GetAllMaterials().First();
            var countBefore = repo.GetAllMaterials().Count();

            var snapshots = new List<MaterialSnapshot>
            {
                new MaterialSnapshot { Id = 999, Name = existing.Name.ToUpperInvariant(), Category = MaterialCategory.Concrete, LambdaA = 1.0, LambdaB = 1.1, IsBuiltIn = false }
            };

            // Act
            await service.ImportProjectMaterialsAsync(snapshots);

            // Assert
            Assert.That(repo.GetAllMaterials().Count(), Is.EqualTo(countBefore));
        }

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

        #endregion

        #region ImportProjectTemplatesAsync Tests

        [Test]
        public async Task ImportProjectTemplatesAsync_AddsNewTemplate()
        {
            // Arrange
            var templateRepo = new TestTemplateRepository();
            templateRepo.Seed(ConstructionTemplate.GetDefaultTemplates());

            var materialRepo = new TestMaterialRepository();
            materialRepo.Seed(Material.GetDefaultMaterials());

            var service = new ConstructionService(
                new ConstructionValidator(),
                materialRepo,
                templateRepo);

            var imported = new ConstructionTemplate
            {
                Name = "Imported User Template",
                Description = "From project",
                HasLoads = false,
                DefaultGroundwaterLevel = 2.0,
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 5, Thickness = 80, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>(),
                MaterialSnapshots = new List<MaterialSnapshot>
                {
                    MaterialSnapshot.FromMaterial(Material.GetDefaultMaterials().First(m => m.Id == 5))
                }
            };

            // Act
            await service.ImportProjectTemplatesAsync(new[] { imported });

            // Assert
            Assert.That(templateRepo.GetAllAsync().Result.Any(t => t.Name == "Imported User Template"), Is.True);
        }

        [Test]
        public async Task ImportProjectTemplatesAsync_SkipsExistingName()
        {
            // Arrange
            var existingTemplate = ConstructionTemplate.GetDefaultTemplates().First();
            var templateRepo = new TestTemplateRepository();
            templateRepo.Seed(ConstructionTemplate.GetDefaultTemplates());

            var materialRepo = new TestMaterialRepository();
            materialRepo.Seed(Material.GetDefaultMaterials());

            var service = new ConstructionService(
                new ConstructionValidator(),
                materialRepo,
                templateRepo);

            var imported = new ConstructionTemplate
            {
                Name = existingTemplate.Name,
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>(),
                LayersBelowPipe = new List<LayerTemplate>(),
                MaterialSnapshots = new List<MaterialSnapshot>()
            };

            var countBefore = templateRepo.GetAllAsync().Result.Count();

            // Act
            await service.ImportProjectTemplatesAsync(new[] { imported });

            // Assert
            Assert.That(templateRepo.GetAllAsync().Result.Count(), Is.EqualTo(countBefore));
        }

        [Test]
        public async Task ImportProjectTemplatesAsync_RemapsMaterialIdByName()
        {
            // Arrange
            var templateRepo = new TestTemplateRepository();
            templateRepo.Seed(ConstructionTemplate.GetDefaultTemplates());

            var materialRepo = new TestMaterialRepository();
            materialRepo.Seed(Material.GetDefaultMaterials());
            materialRepo.AddAsync(new Material
            {
                Name = "Imported Concrete",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.5,
                LambdaB = 1.5,
                IsBuiltIn = false
            }).Wait();
            var localMaterial = materialRepo.GetAllMaterials().First(m => m.Name == "Imported Concrete");

            var service = new ConstructionService(
                new ConstructionValidator(),
                materialRepo,
                templateRepo);

            var imported = new ConstructionTemplate
            {
                Name = "Imported User Template",
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 999, Thickness = 80, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>(),
                MaterialSnapshots = new List<MaterialSnapshot>
                {
                    new MaterialSnapshot
                    {
                        Id = 999,
                        Name = "Imported Concrete",
                        Category = MaterialCategory.Concrete,
                        LambdaA = 1.5,
                        LambdaB = 1.5,
                        IsBuiltIn = false
                    }
                }
            };

            // Act
            await service.ImportProjectTemplatesAsync(new[] { imported });

            // Assert
            var added = templateRepo.GetAllAsync().Result.FirstOrDefault(t => t.Name == "Imported User Template");
            Assert.That(added, Is.Not.Null);
            Assert.That(added!.LayersAbovePipe[0].MaterialId, Is.EqualTo(localMaterial.Id));
        }

        [Test]
        public async Task ImportProjectTemplatesAsync_SkipsWhenMaterialUnresolved()
        {
            // Arrange
            var templateRepo = new TestTemplateRepository();
            templateRepo.Seed(ConstructionTemplate.GetDefaultTemplates());

            var materialRepo = new TestMaterialRepository();
            materialRepo.Seed(Material.GetDefaultMaterials());

            var service = new ConstructionService(
                new ConstructionValidator(),
                materialRepo,
                templateRepo);

            var imported = new ConstructionTemplate
            {
                Name = "Unresolved Template",
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 999, Thickness = 80, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>(),
                MaterialSnapshots = new List<MaterialSnapshot>
                {
                    new MaterialSnapshot
                    {
                        Id = 999,
                        Name = "Missing Material",
                        Category = MaterialCategory.Concrete,
                        LambdaA = 1.5,
                        LambdaB = 1.5,
                        IsBuiltIn = false
                    }
                }
            };

            // Act
            await service.ImportProjectTemplatesAsync(new[] { imported });

            // Assert
            Assert.That(templateRepo.GetAllAsync().Result.Any(t => t.Name == "Unresolved Template"), Is.False);
        }

        #endregion

        #region GetTotalThickness Tests

        [Test]
        public void GetTotalThicknessAbovePipe_MultipleLayers_ReturnsSum()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон");
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
            var constructionVm = CreateConstructionViewModel(_projectStateService.Session);
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            constructionVm.LayersAbovePipe.Clear();
            var asphalt = constructionVm.AvailableMaterials.First(m => m.Name == "Асфальт");
            var tile = constructionVm.AvailableMaterials.First(m => m.Name == "Тротуарная плитка/брусчатка");
            var concrete = constructionVm.AvailableMaterials.First(m => m.Name == "Бетон");

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
                Material = tile,
                Thickness = 50,
                CalculatedLambda = tile.LambdaA,
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

            var constructionVm2 = CreateConstructionViewModel(_projectStateService.Session);
            await constructionVm2.InitializeCommand.ExecuteAsync(null);
            var viewModel2 = CreateResultsViewModel(constructionVm2);

            // Act
            await viewModel2.LoadProjectDataAsync(savedData);

            // Assert
            Assert.That(constructionVm2.LayersAbovePipe.Count, Is.EqualTo(3));
            Assert.That(constructionVm2.LayersAbovePipe[0].Order, Is.EqualTo(0));
            Assert.That(constructionVm2.LayersAbovePipe[1].Order, Is.EqualTo(1));
            Assert.That(constructionVm2.LayersAbovePipe[2].Order, Is.EqualTo(2));
            Assert.That(constructionVm2.LayersAbovePipe[2].Material?.Name, Is.EqualTo("Бетон"));
            Assert.That(constructionVm2.LambdaE, Is.EqualTo(concrete.LambdaA).Within(0.0001));
            Assert.That(constructionVm2.GetConstruction().MaterialAroundPipe?.Name, Is.EqualTo("Бетон"));
        }

        [Test]
        public async Task ProjectData_LayerOrder_RoundTrip_PreservesLambdaE()
        {
            // Arrange
            var constructionVm = CreateConstructionViewModel(_projectStateService.Session);
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            constructionVm.LayersAbovePipe.Clear();
            var asphalt = constructionVm.AvailableMaterials.First(m => m.Name == "Асфальт");
            var concrete = constructionVm.AvailableMaterials.First(m => m.Name == "Бетон");

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
            Assert.That(constructionVm.LayersAbovePipe[1].Material?.Name, Is.EqualTo("Бетон"));
            Assert.That(constructionVm.LayersAbovePipe[1].Order, Is.EqualTo(1));
            Assert.That(constructionVm.LambdaE, Is.EqualTo(concrete.LambdaA).Within(0.0001));
            Assert.That(constructionVm.GetConstruction().MaterialAroundPipe, Is.EqualTo(concrete));

            var viewModel = CreateResultsViewModel(constructionVm);
            var savedData = viewModel.SaveCurrentProject();

            var constructionVm2 = CreateConstructionViewModel(_projectStateService.Session);
            await constructionVm2.InitializeCommand.ExecuteAsync(null);
            var viewModel2 = CreateResultsViewModel(constructionVm2);

            // Act
            await viewModel2.LoadProjectDataAsync(savedData);

            // Assert
            Assert.That(constructionVm2.LayersAbovePipe.Count, Is.EqualTo(2));
            Assert.That(constructionVm2.LayersAbovePipe[0].Order, Is.EqualTo(0));
            Assert.That(constructionVm2.LayersAbovePipe[1].Order, Is.EqualTo(1));
            Assert.That(constructionVm2.LayersAbovePipe[0].Material?.Name, Is.EqualTo("Асфальт"));
            Assert.That(constructionVm2.LayersAbovePipe[1].Material?.Name, Is.EqualTo("Бетон"));
            Assert.That(constructionVm2.LambdaE, Is.EqualTo(concrete.LambdaA).Within(0.0001));
            Assert.That(constructionVm2.GetConstruction().MaterialAroundPipe?.Name, Is.EqualTo("Бетон"));
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

            var constructionVm = CreateConstructionViewModel(_projectStateService.Session);
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            var viewModel = CreateResultsViewModel(constructionVm);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

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
            var constructionVm = CreateConstructionViewModel(_projectStateService.Session);
            await constructionVm.InitializeCommand.ExecuteAsync(null);
            constructionVm.LayersAbovePipe.Clear();
            var concrete = constructionVm.AvailableMaterials.First(m => m.Name == "Бетон");
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

        [Test]
        public async Task ProjectData_CustomMaterials_RoundTrip()
        {
            // Arrange
            var repo = new TestMaterialRepository();
            repo.Seed(Material.GetDefaultMaterials());
            repo.AddAsync(new Material
            {
                Name = "Custom Project Material",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.23,
                LambdaB = 1.45,
                IsBuiltIn = false
            }).Wait();

            var service = new ConstructionService(new ConstructionValidator(), repo, new Mock<IConstructionTemplateRepository>().Object);
            var constructionVm = CreateConstructionViewModel(repo);
            await constructionVm.InitializeCommand.ExecuteAsync(null);

            var viewModel = CreateResultsViewModel(constructionVm, service, repo);

            // Act — сохраняем
            var savedData = viewModel.SaveCurrentProject();

            // Assert — пользовательские материалы сохранены
            Assert.That(savedData.CustomMaterials, Is.Not.Empty);
            Assert.That(savedData.CustomMaterials.Any(m => m.Name == "Custom Project Material"), Is.True);
            Assert.That(savedData.CustomMaterials.All(m => !m.IsBuiltIn), Is.True);
        }

        [Test]
        public async Task ProjectData_Load_ImportsCustomMaterialsBeforeLayers()
        {
            // Arrange
            var repo = new TestMaterialRepository();
            repo.Seed(Material.GetDefaultMaterials());

            var service = new ConstructionService(new ConstructionValidator(), repo, new Mock<IConstructionTemplateRepository>().Object);
            var constructionVm = CreateConstructionViewModel(repo);
            await constructionVm.InitializeCommand.ExecuteAsync(null);

            var viewModel = CreateResultsViewModel(constructionVm, service, repo);

            var projectData = new ProjectData
            {
                Version = "1.1",
                ProjectNumber = "P-MAT",
                ProjectObject = "Material Import Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData
                {
                    Layers = new List<LayerProjectData>
                    {
                        new LayerProjectData
                        {
                            Position = LayerPosition.AbovePipe,
                            MaterialName = "Imported Material",
                            MaterialLambda = 1.5,
                            Thickness = 100,
                            CalculatedLambda = 1.5,
                            Order = 0
                        }
                    }
                },
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData(),
                CustomMaterials = new List<MaterialSnapshot>
                {
                    new MaterialSnapshot
                    {
                        Id = 500,
                        Name = "Imported Material",
                        Category = MaterialCategory.Concrete,
                        LambdaA = 1.5,
                        LambdaB = 1.6,
                        IsBuiltIn = false
                    }
                }
            };

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert — материал импортирован и доступен
            Assert.That(repo.GetAllMaterials().Any(m => m.Name == "Imported Material"), Is.True);
            // Слой загружен с импортированным материалом
            Assert.That(constructionVm.LayersAbovePipe.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task ProjectData_CustomTemplates_RoundTrip()
        {
            // Arrange
            var repo = new TestMaterialRepository();
            repo.Seed(Material.GetDefaultMaterials());
            await repo.AddAsync(new Material
            {
                Name = "Custom Concrete",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.5,
                LambdaB = 1.5,
                IsBuiltIn = false
            });
            var customConcrete = repo.GetAllMaterials().First(m => m.Name == "Custom Concrete");

            var templateRepo = new TestTemplateRepository();
            templateRepo.Seed(ConstructionTemplate.GetDefaultTemplates());

            var service = new ConstructionService(new ConstructionValidator(), repo, templateRepo);
            var constructionVm = CreateConstructionViewModel(repo);
            await constructionVm.InitializeCommand.ExecuteAsync(null);

            constructionVm.Templates.Add(new ConstructionTemplate
            {
                Name = "Custom Project Template",
                Description = "User template",
                HasLoads = false,
                DefaultGroundwaterLevel = 2.0,
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = customConcrete.Id, Thickness = 80, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>()
            });

            var viewModel = CreateResultsViewModel(constructionVm, service, repo);

            // Act
            var savedData = viewModel.SaveCurrentProject();

            // Assert
            Assert.That(savedData.CustomTemplates, Is.Not.Empty);
            Assert.That(savedData.CustomTemplates.Any(t => t.Name == "Custom Project Template"), Is.True);
            Assert.That(savedData.CustomTemplates[0].MaterialSnapshots.Any(m => m.Name == "Custom Concrete"), Is.True);
            Assert.That(savedData.CustomTemplates[0].IsBuiltIn, Is.False);
        }

        [Test]
        public async Task ProjectRoundTrip_CustomTemplateSurvives()
        {
            // Arrange
            var repo = new TestMaterialRepository();
            repo.Seed(Material.GetDefaultMaterials());
            await repo.AddAsync(new Material
            {
                Name = "Custom Concrete",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.5,
                LambdaB = 1.5,
                IsBuiltIn = false
            });
            var customConcrete = repo.GetAllMaterials().First(m => m.Name == "Custom Concrete");

            var templateRepo = new TestTemplateRepository();
            templateRepo.Seed(ConstructionTemplate.GetDefaultTemplates());

            var service = new ConstructionService(new ConstructionValidator(), repo, templateRepo);
            var constructionVm = CreateConstructionViewModel(repo, templateRepo);
            await constructionVm.InitializeCommand.ExecuteAsync(null);

            constructionVm.Templates.Add(new ConstructionTemplate
            {
                Name = "Custom Project Template",
                Description = "User template",
                HasLoads = false,
                DefaultGroundwaterLevel = 2.0,
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = customConcrete.Id, Thickness = 80, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>()
            });

            var viewModel = CreateResultsViewModel(constructionVm, service, repo);
            var savedData = viewModel.SaveCurrentProject();

            // Act — load into a fresh construction view model sharing the same repositories
            var constructionVm2 = CreateConstructionViewModel(repo, templateRepo);
            await constructionVm2.InitializeCommand.ExecuteAsync(null);
            var viewModel2 = CreateResultsViewModel(constructionVm2, service, repo);
            await viewModel2.LoadProjectDataAsync(savedData);

            // Assert
            Assert.That(constructionVm2.Templates.Any(t => t.Name == "Custom Project Template"), Is.True);
        }

        [Test]
        public void ProjectData_DeserializesOldFileWithoutCustomTemplates()
        {
            // Arrange
            var json = "{\"version\":\"1.1\",\"project_number\":\"P-OLD\",\"project_object\":\"Old\",\"created_date\":\"2026-01-01T00:00:00\",\"modified_date\":\"2026-01-01T00:00:00\",\"climate_data\":{},\"construction_data\":{},\"thermal_data\":{},\"hydraulics_data\":{},\"custom_materials\":[],\"is_operating_mode\":true}";
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower };

            // Act
            var data = System.Text.Json.JsonSerializer.Deserialize<ProjectData>(json, options);

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data!.CustomTemplates, Is.Not.Null);
            Assert.That(data.CustomTemplates, Is.Empty);
        }

        private ResultsViewModel CreateResultsViewModel(ConstructionViewModel constructionVm, IConstructionService service, IMaterialRepository repo)
        {
            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var calculationContext = new CalculationContext();
            var climateVm = CreateClimateViewModel();
            var thermalVm = CreateThermalViewModel();
            var circuitsVm = CreateCircuitsViewModel();
            var constructionDefaultStateInitializer = new ConstructionDefaultStateInitializer(
                repo,
                _projectStateService.Session.ConstructionState);

            return new ResultsViewModel(
                _projectStateService,
                _projectStateService.Session,
                _projectStateService,
                new Mock<IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                new Mock<IProjectFileService>().Object,
                calculationStateService,
                repo,
                service,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm,
                new ProjectLoadOrchestrator(
                    climateVm,
                    constructionVm,
                    thermalVm,
                    circuitsVm,
                    calculationStateService,
                    service,
                    calculationContext,
                    _projectStateService.Session,
                    constructionDefaultStateInitializer),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calculationStateService,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
        }

        private ConstructionViewModel CreateConstructionViewModel(IMaterialRepository repo)
        {
            return CreateConstructionViewModel(
                repo,
                new Mock<IConstructionTemplateRepository>().Object,
                _projectStateService.Session);
        }

        private ConstructionViewModel CreateConstructionViewModel(IMaterialRepository repo, IConstructionTemplateRepository templateRepo)
        {
            return CreateConstructionViewModel(repo, templateRepo, _projectStateService.Session);
        }

        private static ConstructionViewModel CreateConstructionViewModel(IMaterialRepository repo, IConstructionTemplateRepository templateRepo, IProjectSession? projectSession)
        {
            var constructionState = projectSession?.ConstructionState;
            var defaultStateInitializer = constructionState == null
                ? null
                : new ConstructionDefaultStateInitializer(repo, constructionState);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                repo,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(),
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                new Mock<IMarkDirtyService>().Object,
                templateRepo,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                constructionState,
                defaultStateInitializer);
        }

        #endregion

        #region Helper Methods

        private ResultsViewModel CreateResultsViewModel(ConstructionViewModel constructionVm)
        {
            var defaultMaterials = Material.GetDefaultMaterials();
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(defaultMaterials);
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(defaultMaterials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns<int>(id => defaultMaterials.FirstOrDefault(m => m.Id == id));

            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var calculationContext = new CalculationContext();
            var climateVm = CreateClimateViewModel();
            var thermalVm = CreateThermalViewModel();
            var circuitsVm = CreateCircuitsViewModel();
            var constructionDefaultStateInitializer = new ConstructionDefaultStateInitializer(
                materialRepositoryMock.Object,
                _projectStateService.Session.ConstructionState);

            return new ResultsViewModel(
                _projectStateService,
                _projectStateService.Session,
                _projectStateService,
                new Mock<IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                new Mock<IProjectFileService>().Object,
                calculationStateService,
                materialRepositoryMock.Object,
                _service,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm,
                new ProjectLoadOrchestrator(
                    climateVm,
                    constructionVm,
                    thermalVm,
                    circuitsVm,
                    calculationStateService,
                    _service,
                    calculationContext,
                    _projectStateService.Session,
                    constructionDefaultStateInitializer),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calculationStateService,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
        }

        private static ConstructionViewModel CreateConstructionViewModel()
        {
            return CreateConstructionViewModel(projectSession: null);
        }

        private static ConstructionViewModel CreateConstructionViewModel(IProjectSession? projectSession)
        {
            var materials = new List<Material>
            {
                new Material { Id = 1, Name = "Песок", LambdaA = 0.4, LambdaB = 2.0 },
                new Material { Id = 5, Name = "Бетон", LambdaA = 1.74, LambdaB = 1.86 },
                new Material { Id = 12, Name = "Тротуарная плитка/брусчатка", LambdaA = 1.2, LambdaB = 1.2 },
                new Material { Id = 11, Name = "Асфальт", LambdaA = 0.75, LambdaB = 0.75 }
            };
            materials.AddRange(Material.GetDefaultMaterials()
                .Where(defaultMaterial => materials.All(material => material.Id != defaultMaterial.Id)));

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materials.SingleOrDefault(material => material.Id == id));

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            var constructionState = projectSession?.ConstructionState;
            var defaultStateInitializer = constructionState == null
                ? null
                : new ConstructionDefaultStateInitializer(materialRepositoryMock.Object, constructionState);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(),
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                new Mock<IMarkDirtyService>().Object,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                constructionState,
                defaultStateInitializer);
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

        /// <summary>
        /// Lightweight in-memory implementation of <see cref="IConstructionTemplateRepository"/> for import tests.
        /// </summary>
        private class TestTemplateRepository : IConstructionTemplateRepository
        {
            private List<ConstructionTemplate> _templates = new();
            private int _nextId = 10;

            public Task<IEnumerable<ConstructionTemplate>> GetAllAsync()
            {
                return Task.FromResult(_templates.AsEnumerable());
            }

            public Task<ConstructionTemplate?> GetByIdAsync(int id)
            {
                return Task.FromResult(_templates.FirstOrDefault(t => t.Id == id));
            }

            public Task<ConstructionTemplate> AddAsync(ConstructionTemplate template)
            {
                template.Id = _nextId++;
                _templates.Add(template);
                return Task.FromResult(template);
            }

            public Task<ConstructionTemplate> UpdateAsync(ConstructionTemplate template)
            {
                throw new NotImplementedException();
            }

            public Task<bool> DeleteAsync(int id)
            {
                throw new NotImplementedException();
            }

            public Task SaveAsync()
            {
                return Task.CompletedTask;
            }

            public void Seed(IEnumerable<ConstructionTemplate> templates)
            {
                _templates = templates.ToList();
            }
        }
    }
}
