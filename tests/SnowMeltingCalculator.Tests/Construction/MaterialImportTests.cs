using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
    /// Тесты импорта материала из снимка: сервисный импорт, конфликт имён и диалог при загрузке.
    /// </summary>
    [TestFixture]
    public class MaterialImportTests
    {
        private ConstructionService _service = null!;
        private Mock<IMaterialRepository> _materialRepositoryMock = null!;

        private ConstructionViewModel _viewModel = null!;
        private Mock<IConstructionService> _constructionServiceMock = null!;
        private MockMaterialRepository _materialRepository = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
        private Mock<IConstructionTemplateRepository> _templateRepositoryMock = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<IEditorDialogService> _editorDialogServiceMock = null!;

        [SetUp]
        public void Setup()
        {
            _materialRepositoryMock = new Mock<IMaterialRepository>();
            _service = new ConstructionService(
                new ConstructionValidator(),
                _materialRepositoryMock.Object,
                new Mock<IConstructionTemplateRepository>().Object);

            _constructionServiceMock = new Mock<IConstructionService>();
            _materialRepository = new MockMaterialRepository();
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
                _constructionServiceMock.Object,
                _materialRepository,
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

        #region ConstructionService.ImportMissingMaterialAsync

        [Test]
        public async Task ImportMissingMaterialAsync_FromSnapshot_CreatesMaterial()
        {
            // Arrange
            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Custom Material",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3,
                MaxSupplyTemp = 55,
                MinOutdoorTemp = -20,
                Notes = "snapshot notes"
            };

            _materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            _materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material>());
            _materialRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Material>())).ReturnsAsync((Material m) => m);
            _materialRepositoryMock.Setup(r => r.SaveMaterialsAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ImportMissingMaterialAsync(snapshot);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(snapshot.Name));
            Assert.That(result.Category, Is.EqualTo(snapshot.Category));
            Assert.That(result.LambdaA, Is.EqualTo(snapshot.LambdaA));
            Assert.That(result.LambdaB, Is.EqualTo(snapshot.LambdaB));
            Assert.That(result.IsBuiltIn, Is.False);
            _materialRepositoryMock.Verify(r => r.AddAsync(It.Is<Material>(m =>
                m.Name == snapshot.Name &&
                m.Category == snapshot.Category &&
                m.LambdaA == snapshot.LambdaA)), Times.Once);
            _materialRepositoryMock.Verify(r => r.SaveMaterialsAsync(), Times.Once);
        }

        [Test]
        public async Task ImportMissingMaterialAsync_NameConflict_AppendsSuffix()
        {
            // Arrange
            var existing = new Material { Id = 1, Name = "Custom Material" };
            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Custom Material",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };

            _materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material> { existing });
            _materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material> { existing });
            _materialRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Material>())).ReturnsAsync((Material m) => m);
            _materialRepositoryMock.Setup(r => r.SaveMaterialsAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ImportMissingMaterialAsync(snapshot);

            // Assert
            Assert.That(result.Name, Is.EqualTo("Custom Material (импортирован)"));
        }

        [Test]
        public async Task ImportMissingMaterialAsync_RepeatedNameConflict_AppendsMultipleSuffixes()
        {
            // Arrange
            var existing = new List<Material>
            {
                new Material { Id = 1, Name = "Custom Material" },
                new Material { Id = 2, Name = "Custom Material (импортирован)" }
            };
            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Custom Material",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };

            _materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(existing);
            _materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(existing);
            _materialRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Material>())).ReturnsAsync((Material m) => m);
            _materialRepositoryMock.Setup(r => r.SaveMaterialsAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ImportMissingMaterialAsync(snapshot);

            // Assert
            Assert.That(result.Name, Is.EqualTo("Custom Material (импортирован) (импортирован)"));
        }

        [Test]
        public void ImportMissingMaterialAsync_NullSnapshot_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _service.ImportMissingMaterialAsync(null!));
        }

        #endregion
    }
}
