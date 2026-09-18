using System.Windows;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Тесты команды открытия проекта в ResultsViewModel: промпт замены
    /// грязного проекта и guard'ы LoadProjectFromPathAsync (волна A плана
    /// 2026-09-18 «чистка раздутых тестов»; только перенос дословно).
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ResultsViewModelOpenProjectPromptTests
    {
        private ProjectStateService _projectStateService = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<IProjectFileService> _projectFileServiceMock = null!;
        private Mock<IConstructionService> _constructionServiceMock = null!;
        private ResultsViewModel _viewModel = null!;

        private const string TestFilePath = ResultsViewModelTestGraph.TestFilePath;

        [SetUp]
        public void SetUp()
        {
            _projectStateService = new ProjectStateService();
            _dialogServiceMock = new Mock<IDialogService>();
            _projectFileServiceMock = new Mock<IProjectFileService>();
            _constructionServiceMock = new Mock<IConstructionService>();
            _viewModel = CreateViewModel();
        }

        private ResultsViewModel CreateViewModel()
        {
            return ResultsViewModelTestGraph.CreateViewModel(
                _projectStateService.Session,
                _dialogServiceMock,
                _projectFileServiceMock,
                _constructionServiceMock,
                ResultsViewModelTestGraph.CreateClimateViewModel(),
                ResultsViewModelTestGraph.CreateConstructionViewModel(_projectStateService.Session),
                ResultsViewModelTestGraph.CreateThermalViewModel(),
                ResultsViewModelTestGraph.CreateCircuitsViewModel());
        }

        [Test]
        public async Task OpenProject_LeavesGlobalCatalogReadOnly_AndCarriesNoCatalogs()
        {
            // Given: a project on the existing restore boundary and a wired
            // construction service. DEC-006 (2026-09-03): catalogs live only
            // globally — the wire DTO cannot carry custom catalogs at all, so
            // opening a project must never route anything to global CRUD.
            var projectData = new ProjectDataBuilder()
                .WithNumber("CATALOG-READ-ONLY")
                .WithDefaultSlices()
                .WithThermal(thermal => thermal.Result = new ThermalResultProjectData { IsValid = true })
                .Build();

            Assert.Multiple(() =>
            {
                Assert.That(typeof(ProjectData).GetProperty("CustomMaterials"), Is.Null,
                    "DEC-006: the wire DTO must not carry CustomMaterials.");
                Assert.That(typeof(ProjectData).GetProperty("CustomTemplates"), Is.Null,
                    "DEC-006: the wire DTO must not carry CustomTemplates.");
            });

            var viewModel = CreateViewModel();

            // When: opening the project through the existing restore boundary.
            await viewModel.LoadProjectDataAsync(projectData);

            // Then: no global catalog CRUD/import operation is invoked.
            _constructionServiceMock.Verify(
                service => service.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()),
                Times.Never);
            _constructionServiceMock.Verify(
                service => service.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>()),
                Times.Never);
            _constructionServiceMock.Verify(
                service => service.ImportMissingMaterialAsync(It.IsAny<MaterialSnapshot>()),
                Times.Never);
        }

        [Test]
        public async Task OpenProject_WhenDirty_ShowsReplacePrompt()
        {
            // Arrange
            _projectStateService.MarkDirty();
            _dialogServiceMock
                .Setup(d => d.ShowOpenFileDialog(It.IsAny<string>()))
                .Returns(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Question))
                .Returns(DialogResult.Yes);

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show("Текущий проект будет заменён. Продолжить?", "Открытие проекта", DialogButtons.YesNo, DialogIcon.Question),
                Times.Once);
            _projectFileServiceMock.Verify(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task OpenProject_WhenClean_DoesNotShowPrompt()
        {
            // Arrange
            _projectStateService.MarkClean();
            _dialogServiceMock
                .Setup(d => d.ShowOpenFileDialog(It.IsAny<string>()))
                .Returns(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogButtons>(), It.IsAny<DialogIcon>()),
                Times.Never);
            _projectFileServiceMock.Verify(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task OpenProject_WhenDirtyAndUserPicksNo_DoesNotLoad()
        {
            // Arrange
            _projectStateService.MarkDirty();
            _viewModel.ProjectNumber = "PRJ-001";
            _dialogServiceMock
                .Setup(d => d.ShowOpenFileDialog(It.IsAny<string>()))
                .Returns(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Question))
                .Returns(DialogResult.No);

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show("Текущий проект будет заменён. Продолжить?", "Открытие проекта", DialogButtons.YesNo, DialogIcon.Question),
                Times.Once);
            Assert.That(_viewModel.ProjectNumber, Is.EqualTo("PRJ-001"));
            Assert.That(_projectStateService.CurrentFilePath, Is.Null);
            Assert.That(_projectStateService.IsDirty, Is.True);
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenNullPath_DoesNothing()
        {
            // Arrange
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.LoadProjectFromPathAsync(null!);

            // Assert
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenEmptyPath_DoesNothing()
        {
            // Arrange
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.LoadProjectFromPathAsync(string.Empty);

            // Assert
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenFileNotFound_ShowsError()
        {
            // Arrange
            const string errorMessage = "Файл не найден";
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Failure(errorMessage));

            // Act
            await _viewModel.LoadProjectFromPathAsync(TestFilePath);

            // Assert
            _dialogServiceMock.Verify(
                d => d.ShowError($"Не удалось открыть проект: {errorMessage}", "Ошибка"),
                Times.Once);
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.Null);
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenSuccess_LoadsDataAndSetsCurrentFilePath()
        {
            // Arrange
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.LoadProjectFromPathAsync(TestFilePath);

            // Assert
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }
    }
}
