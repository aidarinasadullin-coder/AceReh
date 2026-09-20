using System.Windows;

using Moq;

using NUnit.Framework;

using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Автосохранение в ResultsViewModel (план 3.1 роадмапа post-1.8, чек
    /// R-2026-09-21-04): восстановление из автоснапшота не привязывает
    /// CurrentFilePath и не пополняет MRU, помечает сессию dirty; запись
    /// автоснапшота идёт через тот же IProjectSaveService, не гасит dirty
    /// и gated состоянием загрузки/расчёта.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ResultsViewModelAutosaveTests
    {
        private ProjectStateService _projectStateService = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<IProjectFileService> _projectFileServiceMock = null!;
        private Mock<IProjectSaveService> _projectSaveServiceMock = null!;
        private ResultsViewModel _viewModel = null!;

        private const string AutosavePath = @"C:\svc\autosave.smc";

        [SetUp]
        public void SetUp()
        {
            ResetAppSettingsHelper.Reset();
            _projectStateService = new ProjectStateService();
            _dialogServiceMock = new Mock<IDialogService>();
            _projectFileServiceMock = new Mock<IProjectFileService>();
            _projectSaveServiceMock = new Mock<IProjectSaveService>();
            _projectSaveServiceMock
                .Setup(s => s.SaveAsync(
                    It.IsAny<IProjectSession>(),
                    It.IsAny<string>(),
                    It.IsAny<ProjectSaveDates>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<object?>.Success(null));
            _viewModel = ResultsViewModelTestGraph.CreateViewModel(
                _projectStateService.Session,
                _dialogServiceMock,
                _projectFileServiceMock,
                new Mock<IConstructionService>(),
                ResultsViewModelTestGraph.CreateClimateViewModel(),
                ResultsViewModelTestGraph.CreateConstructionViewModel(_projectStateService.Session),
                ResultsViewModelTestGraph.CreateThermalViewModel(),
                ResultsViewModelTestGraph.CreateCircuitsViewModel(),
                _projectSaveServiceMock.Object);
        }

        [Test]
        public async Task RestoreFromAutosnapshot_WhenFileBroken_ShowsError_AndReturnsFalse()
        {
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(AutosavePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Failure("Файл повреждён"));

            var restored = await _viewModel.RestoreFromAutosnapshotAsync(AutosavePath);

            Assert.Multiple(() =>
            {
                Assert.That(restored, Is.False);
                Assert.That(_projectStateService.IsDirty, Is.False);
                Assert.That(_projectStateService.CurrentFilePath, Is.Null);
            });
            _dialogServiceMock.Verify(
                d => d.ShowError(
                    It.Is<string>(message => message.Contains("Не удалось восстановить автосохранённый проект")),
                    "Ошибка"),
                Times.Once);
        }

        [Test]
        public async Task RestoreFromAutosnapshot_WhenSuccess_LoadsData_LeavesPathUnbound_AndMarksDirty()
        {
            var projectData = new ProjectDataBuilder()
                .WithNumber("AUTOSAVE-RESTORE")
                .WithDefaultSlices()
                .Build();
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(AutosavePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(projectData));

            var restored = await _viewModel.RestoreFromAutosnapshotAsync(AutosavePath);

            Assert.Multiple(() =>
            {
                // Восстановленный проект не привязан к служебному файлу (S5)
                Assert.That(restored, Is.True);
                Assert.That(_projectStateService.CurrentFilePath, Is.Null);
                // и остаётся dirty: данные не живут ни в каком файле пользователя
                Assert.That(_projectStateService.IsDirty, Is.True);
                Assert.That(_viewModel.ProjectNumber, Is.EqualTo("AUTOSAVE-RESTORE"));
            });

            // Служебный путь в MRU недопустим (S5)
            Assert.That(
                SnowMeltingCalculator.Services.AppSettings.Instance.RecentProjects,
                Does.Not.Contain(AutosavePath));
        }

        [Test]
        public async Task SaveAutosnapshot_WhenClean_SkipsWithoutSaveCall()
        {
            var saved = await _viewModel.SaveAutosnapshotAsync(AutosavePath);

            Assert.That(saved, Is.False);
            _projectSaveServiceMock.Verify(
                s => s.SaveAsync(
                    It.IsAny<IProjectSession>(),
                    It.IsAny<string>(),
                    It.IsAny<ProjectSaveDates>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task SaveAutosnapshot_WhenDirty_SavesThroughSaveService_AndKeepsDirty()
        {
            _projectStateService.MarkDirty();

            var saved = await _viewModel.SaveAutosnapshotAsync(AutosavePath);

            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                // Автосейв не гасит «звёздочку» и не ставит точку чистоты (S1)
                Assert.That(_projectStateService.IsDirty, Is.True);
            });
            _projectSaveServiceMock.Verify(
                s => s.SaveAsync(
                    _projectStateService.Session,
                    AutosavePath,
                    It.IsAny<ProjectSaveDates>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task SaveAutosnapshot_WhenSaveFails_ReturnsFalse()
        {
            _projectStateService.MarkDirty();
            _projectSaveServiceMock
                .Setup(s => s.SaveAsync(
                    It.IsAny<IProjectSession>(),
                    It.IsAny<string>(),
                    It.IsAny<ProjectSaveDates>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<object?>.Failure("диск переполнен"));

            var saved = await _viewModel.SaveAutosnapshotAsync(AutosavePath);

            Assert.That(saved, Is.False);
        }
    }
}
