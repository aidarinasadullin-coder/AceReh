using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ProjectSaveServiceTests
    {
        // ---- helpers ----------------------------------------------------

        private static DirectoryInfo FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null &&
                   !(Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                     Directory.Exists(Path.Combine(dir.FullName, "tests"))))
            {
                dir = dir.Parent;
            }

            Assert.That(dir, Is.Not.Null, "Repository root containing both src and tests directories was not found.");
            return dir!;
        }

        private static string ReadProductionSource(string relativePath)
        {
            var root = FindRepoRoot();
            var full = Path.Combine(root.FullName, relativePath);
            Assert.That(File.Exists(full), Is.True, $"Expected production source not found: {full}");
            return File.ReadAllText(full);
        }

        private static ProjectSnapshot BuildMinimalSnapshot(
            string projectNumber,
            string projectObject,
            bool isOperatingMode)
        {
            var climate = new ClimateStateSnapshot(
                "City", "Region", -1, -2, 3, 4, 5, ClimateZone.Zone_M15, true, true, false);
            var construction = new ConstructionStateSnapshot(
                1, false, Array.Empty<ConstructionLayerSnapshot>(), Array.Empty<ConstructionLayerSnapshot>());
            var thermal = ThermalStateSnapshot.Default;
            var hydraulics = HydraulicsStateSnapshot.Default;

            return new ProjectSnapshot(
                projectNumber,
                projectObject,
                isOperatingMode,
                climate,
                construction,
                thermal,
                hydraulics);
        }

        // ---- behavioral tests ------------------------------------------

        [Test]
        public async Task SaveAsync_WithValidSession_MapsSnapshotFieldsAndCallsServicesExactlyOnce()
        {
            // arrange
            var sessionMock = new Mock<IProjectSession>();

            var snapshot = BuildMinimalSnapshot("PRJ-2026-001", "Object A", true);
            var factoryMock = new Mock<IProjectSnapshotFactory>();
            factoryMock
                .Setup(f => f.Create(It.IsAny<IProjectSession>()))
                .Returns(snapshot);

            ProjectData? capturedData = null;
            string? capturedPath = null;
            CancellationToken capturedToken = default;

            var fileServiceMock = new Mock<IProjectFileService>();
            fileServiceMock
                .Setup(s => s.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .Callback<string, ProjectData, CancellationToken>((p, d, t) =>
                {
                    capturedPath = p;
                    capturedData = d;
                    capturedToken = t;
                })
                .ReturnsAsync(OperationResult<object?>.Success(new object()));

            var materialRepoMock = new Mock<IMaterialRepository>();
            var service = new ProjectSaveService(factoryMock.Object, materialRepoMock.Object, fileServiceMock.Object);

            var prior = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var dates = new ProjectSaveDates(prior, now);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;
            const string filePath = @"C:\temp\project.3ace";

            // act
            var result = await service.SaveAsync(sessionMock.Object, filePath, dates, ct);

            // assert
            Assert.That(result.IsSuccess, Is.True, "SaveAsync should return the successful OperationResult from the file service.");

            factoryMock.Verify(f => f.Create(It.IsAny<IProjectSession>()), Times.Once,
                "The snapshot factory must be called exactly once.");
            fileServiceMock.Verify(s => s.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()), Times.Once,
                "The file service must be called exactly once.");

            Assert.That(capturedData, Is.Not.Null, "The ProjectData DTO passed to the file service was not captured.");
            Assert.That(capturedPath, Is.EqualTo(filePath), "The file path must be forwarded unchanged to the file service.");

            Assert.That(capturedData!.Version, Is.EqualTo("1.1"), "Version must be mapped from the persistence mapper.");
            Assert.That(capturedData.ProjectNumber, Is.EqualTo("PRJ-2026-001"), "ProjectNumber must be mapped from the snapshot.");
            Assert.That(capturedData.ProjectObject, Is.EqualTo("Object A"), "ProjectObject must be mapped from the snapshot.");
            Assert.That(capturedData.CreatedDate, Is.EqualTo(prior), "CreatedDate must be mapped from ProjectSaveDates.");
            Assert.That(capturedData.ModifiedDate, Is.EqualTo(now), "ModifiedDate must be mapped from ProjectSaveDates.");
            Assert.That(capturedData.IsOperatingMode, Is.True, "IsOperatingMode must be mapped from the snapshot.");
            Assert.That(capturedToken, Is.EqualTo(ct), "The cancellation token must be passed through unchanged.");
        }

        [Test]
        public async Task SaveAsync_WhenFileServiceReturnsFailure_ReturnsOperationResultUnchanged()
        {
            // arrange
            var sessionMock = new Mock<IProjectSession>();
            var snapshot = BuildMinimalSnapshot("PN", "OBJ", false);
            var factoryMock = new Mock<IProjectSnapshotFactory>();
            factoryMock.Setup(f => f.Create(It.IsAny<IProjectSession>())).Returns(snapshot);

            var failed = OperationResult<object?>.Failure("disk full");
            var fileServiceMock = new Mock<IProjectFileService>();
            fileServiceMock
                .Setup(s => s.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failed);

            var service = new ProjectSaveService(factoryMock.Object, new Mock<IMaterialRepository>().Object, fileServiceMock.Object);

            // act
            var result = await service.SaveAsync(
                sessionMock.Object, "p", new ProjectSaveDates(DateTime.UtcNow, DateTime.UtcNow), CancellationToken.None);

            // assert
            Assert.That(result.IsSuccess, Is.False, "A failed OperationResult must be returned as-is.");
            Assert.That(result, Is.SameAs(failed), "The exact failed OperationResult instance must be returned unchanged.");
        }

        [Test]
        public void SaveAsync_WhenFileServiceThrows_ExceptionPropagates()
        {
            // arrange
            var sessionMock = new Mock<IProjectSession>();
            var snapshot = BuildMinimalSnapshot("PN", "OBJ", false);
            var factoryMock = new Mock<IProjectSnapshotFactory>();
            factoryMock.Setup(f => f.Create(It.IsAny<IProjectSession>())).Returns(snapshot);

            var fileServiceMock = new Mock<IProjectFileService>();
            fileServiceMock
                .Setup(s => s.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("write failed"));

            var service = new ProjectSaveService(factoryMock.Object, new Mock<IMaterialRepository>().Object, fileServiceMock.Object);

            // act / assert
            Assert.That(async () => await service.SaveAsync(
                sessionMock.Object, "p", new ProjectSaveDates(DateTime.UtcNow, DateTime.UtcNow), CancellationToken.None),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task SaveAsync_PassesCancellationTokenUnchanged()
        {
            // arrange
            var sessionMock = new Mock<IProjectSession>();
            var snapshot = BuildMinimalSnapshot("PN", "OBJ", false);
            var factoryMock = new Mock<IProjectSnapshotFactory>();
            factoryMock.Setup(f => f.Create(It.IsAny<IProjectSession>())).Returns(snapshot);

            CancellationToken captured = default;
            var fileServiceMock = new Mock<IProjectFileService>();
            fileServiceMock
                .Setup(s => s.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .Callback<string, ProjectData, CancellationToken>((_, _, t) => captured = t)
                .ReturnsAsync(OperationResult<object?>.Success(new object()));

            var service = new ProjectSaveService(factoryMock.Object, new Mock<IMaterialRepository>().Object, fileServiceMock.Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // act
            await service.SaveAsync(sessionMock.Object, "p", new ProjectSaveDates(DateTime.UtcNow, DateTime.UtcNow), ct);

            // assert
            Assert.That(captured, Is.EqualTo(ct), "The supplied cancellation token must reach the file service unchanged.");
        }

        // ---- source guards ---------------------------------------------

        [Test]
        public void ProjectSaveServiceSource_RejectsViewModelAndWpfReferences()
        {
            var source = ReadProductionSource(
                Path.Combine("src", "Services", "Project", "ProjectSaveService.cs"));

            Assert.That(source.Contains("ViewModel"), Is.False,
                "ProjectSaveService.cs must not reference ViewModel types.");
            Assert.That(source.Contains("System.Windows"), Is.False,
                "ProjectSaveService.cs must not reference WPF (System.Windows).");
            Assert.That(source.Contains("DependencyObject"), Is.False,
                "ProjectSaveService.cs must not reference WPF DependencyObject.");
            Assert.That(source.Contains("DependencyProperty"), Is.False,
                "ProjectSaveService.cs must not reference WPF DependencyProperty.");
        }

        [Test]
        public void SaveToFileSourceSlice_RejectsSaveCurrentProject()
        {
            var source = ReadProductionSource(
                Path.Combine("src", "ViewModels", "Results", "ResultsViewModel.cs"));

            var startIdx = source.IndexOf("private async Task<bool> SaveToFile", StringComparison.Ordinal);
            Assert.That(startIdx, Is.GreaterThanOrEqualTo(0), "SaveToFile method slice was not found.");

            var endIdx = source.IndexOf("SaveLegacyFileAsync", startIdx, StringComparison.Ordinal);
            Assert.That(endIdx, Is.GreaterThan(startIdx), "SaveLegacyFileAsync boundary was not found after SaveToFile.");

            var slice = source.Substring(startIdx, endIdx - startIdx);

            Assert.That(slice, Does.Contain("_projectSaveService.SaveAsync"),
                "The SaveToFile slice must delegate to the new save boundary (_projectSaveService.SaveAsync).");
            Assert.That(slice, Does.Not.Contain("SaveCurrentProject"),
                "The SaveToFile slice must not call the legacy SaveCurrentProject path.");
        }
    }
}
