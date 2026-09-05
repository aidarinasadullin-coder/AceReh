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
        private Mock<IConstructionRepository> _constructionRepositoryMock = null!;
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
            _constructionRepositoryMock = new Mock<IConstructionRepository>();
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
                _constructionRepositoryMock.Object,
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

        #region ConstructionViewModel import on load

        [Test]
        public async Task LoadConstruction_MissingMaterialWithSnapshot_Accepted_ImportsAndRetries()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Импортный материал",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };
            var importedMaterial = new Material { Id = 100, Name = "Импортный материал (импортирован)" };

            var callCount = 0;
            _constructionRepositoryMock.Setup(r => r.LoadConstructionAsync(It.IsAny<string>()))
                .Returns(() =>
                {
                    if (callCount++ == 0)
                    {
                        throw new MaterialNotFoundException(999, snapshot);
                    }
                    return Task.FromResult<ConstructionModel?>(new ConstructionModel
                    {
                        GroundwaterLevel = 2.0,
                    });
                });

            _constructionServiceMock
                .Setup(s => s.ImportMissingMaterialAsync(It.Is<MaterialSnapshot>(s => s.Id == 999)))
                .ReturnsAsync(importedMaterial);

            _dialogServiceMock
                .Setup(d => d.Show(It.Is<string>(s => s.Contains(snapshot.Name)), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Question))
                .Returns(DialogResult.Yes);

            // Act
            await _viewModel.LoadConstructionCommand.ExecuteAsync(null);

            // Assert
            _constructionServiceMock.Verify(s => s.ImportMissingMaterialAsync(It.Is<MaterialSnapshot>(s => s.Id == 999)), Times.Once);
            _dialogServiceMock.Verify(d => d.Show(It.Is<string>(s => s.Contains(snapshot.Name)), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Question), Times.Once);
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("успешно"));
        }

        [Test]
        public async Task LoadConstruction_MissingMaterialWithSnapshot_Declined_ShowsErrorAndDoesNotImport()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Импортный материал",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };

            _constructionRepositoryMock
                .Setup(r => r.LoadConstructionAsync(It.IsAny<string>()))
                .Throws(new MaterialNotFoundException(999, snapshot));

            _dialogServiceMock
                .Setup(d => d.Show(It.Is<string>(s => s.Contains(snapshot.Name)), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Question))
                .Returns(DialogResult.No);

            // Act
            await _viewModel.LoadConstructionCommand.ExecuteAsync(null);

            // Assert
            _constructionServiceMock.Verify(s => s.ImportMissingMaterialAsync(It.IsAny<MaterialSnapshot>()), Times.Never);
            _dialogServiceMock.Verify(d => d.ShowError(It.Is<string>(s => s.Contains(snapshot.Name)), It.IsAny<string>()), Times.Once);
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("не найден"));
        }

        [Test]
        public async Task LoadConstruction_MissingMaterialWithoutSnapshot_ShowsError()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            _constructionRepositoryMock
                .Setup(r => r.LoadConstructionAsync(It.IsAny<string>()))
                .Throws(new MaterialNotFoundException(999));

            // Act
            await _viewModel.LoadConstructionCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(d => d.ShowError(It.Is<string>(s => s.Contains("999")), It.IsAny<string>()), Times.Once);
            Assert.That(_viewModel.IsValid, Is.False);
        }

        [Test]
        public async Task StandaloneLoadConstruction_ImportFailure_ThroughRealServicePreservesCanonicalState()
        {
            var calculationContext = new CalculationContext();
            var projectSession = new ProjectSession(calculationContext: calculationContext);
            var completionCount = 0;
            var contextPublicationCount = 0;
            projectSession.ConstructionState.Changed += (_, _) => completionCount++;
            calculationContext.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    contextPublicationCount++;
                }
            };
            var realService = new ConstructionService(
                new ConstructionValidator(),
                _materialRepositoryMock.Object,
                _templateRepositoryMock.Object);
            var viewModel = new ConstructionViewModel(
                realService,
                _materialRepository,
                _constructionRepositoryMock.Object,
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
            await viewModel.InitializeCommand.ExecuteAsync(null);
            completionCount = 0;
            contextPublicationCount = 0;
            _markDirtyServiceMock.Invocations.Clear();
            var before = projectSession.ConstructionState.Snapshot;
            var snapshot = new MaterialSnapshot
            {
                Id = 999,
                Name = "Import failure material",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.2,
                LambdaB = 1.3
            };
            _constructionRepositoryMock
                .Setup(repository => repository.LoadConstructionAsync(It.IsAny<string>()))
                .Throws(new MaterialNotFoundException(snapshot.Id, snapshot));
            _dialogServiceMock
                .Setup(dialog => dialog.Show(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    DialogButtons.YesNo,
                    DialogIcon.Question))
                .Returns(DialogResult.Yes);
            _materialRepositoryMock.Setup(repository => repository.LoadMaterialsAsync())
                .ReturnsAsync(Array.Empty<Material>());
            _materialRepositoryMock.Setup(repository => repository.GetAllMaterials())
                .Returns(Array.Empty<Material>());
            _materialRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<Material>()))
                .ThrowsAsync(new IOException("catalog import write failed"));

            await viewModel.LoadConstructionCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(projectSession.ConstructionState.Snapshot, Is.EqualTo(before));
                Assert.That(viewModel.IsValid, Is.False);
                Assert.That(viewModel.ValidationMessage, Does.Contain("catalog import write failed"));
                Assert.That(completionCount, Is.Zero);
                Assert.That(contextPublicationCount, Is.Zero);
            });
            _materialRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Material>()), Times.Once);
            _markDirtyServiceMock.Verify(service => service.MarkDirty(), Times.Never);
        }

        #endregion
    }
}
