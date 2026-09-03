using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public sealed class ProjectSnapshotFactoryTests
    {
        [Test]
        public void Create_ReadsEachCanonicalSessionValueOnce_AndCarriesNoCatalogs()
        {
            var climate = new ClimateStateSnapshot("City", "Region", -1, -2, 3, 4, 5, ClimateZone.Zone_M15, true, true, false);
            var construction = new ConstructionStateSnapshot(1, false, Array.Empty<ConstructionLayerSnapshot>(), Array.Empty<ConstructionLayerSnapshot>());
            var thermal = ThermalStateSnapshot.Default;
            var hydraulics = HydraulicsStateSnapshot.Default;
            var session = new Mock<IProjectSession>(MockBehavior.Strict);
            var inputs = new Mock<IProjectSnapshotPersistenceInputs>(MockBehavior.Strict);
            var climateState = new Mock<IProjectSessionClimateState>(MockBehavior.Strict);
            var constructionState = new Mock<IProjectSessionConstructionState>(MockBehavior.Strict);
            var thermalState = new Mock<IProjectSessionThermalState>(MockBehavior.Strict);
            var hydraulicsState = new Mock<IProjectSessionHydraulicsState>(MockBehavior.Strict);

            session.SetupGet(x => x.ProjectNumber).Returns("PR-1");
            session.SetupGet(x => x.ProjectObject).Returns("Object");
            session.SetupGet(x => x.ClimateState).Returns(climateState.Object);
            session.SetupGet(x => x.ConstructionState).Returns(constructionState.Object);
            session.SetupGet(x => x.ThermalState).Returns(thermalState.Object);
            session.SetupGet(x => x.HydraulicsState).Returns(hydraulicsState.Object);
            climateState.SetupGet(x => x.Snapshot).Returns(climate);
            constructionState.SetupGet(x => x.Snapshot).Returns(construction);
            thermalState.SetupGet(x => x.Snapshot).Returns(thermal);
            hydraulicsState.SetupGet(x => x.Snapshot).Returns(hydraulics);
            inputs.SetupGet(x => x.IsOperatingMode).Returns(false);

            var snapshot = new ProjectSnapshotFactory(inputs.Object).Create(session.Object);

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ProjectNumber, Is.EqualTo("PR-1"));
                Assert.That(snapshot.ProjectObject, Is.EqualTo("Object"));
                Assert.That(snapshot.IsOperatingMode, Is.False);
                Assert.That(snapshot.ClimateStateSnapshot, Is.SameAs(climate));
                Assert.That(snapshot.ConstructionStateSnapshot, Is.SameAs(construction));
                Assert.That(snapshot.ThermalStateSnapshot, Is.SameAs(thermal));
                Assert.That(snapshot.HydraulicsStateSnapshot, Is.SameAs(hydraulics));
            });
            session.VerifyGet(x => x.ProjectNumber, Times.Once);
            session.VerifyGet(x => x.ProjectObject, Times.Once);
            session.VerifyGet(x => x.ClimateState, Times.Once);
            session.VerifyGet(x => x.ConstructionState, Times.Once);
            session.VerifyGet(x => x.ThermalState, Times.Once);
            session.VerifyGet(x => x.HydraulicsState, Times.Once);
            climateState.VerifyGet(x => x.Snapshot, Times.Once);
            constructionState.VerifyGet(x => x.Snapshot, Times.Once);
            thermalState.VerifyGet(x => x.Snapshot, Times.Once);
            hydraulicsState.VerifyGet(x => x.Snapshot, Times.Once);
        }

        [Test]
        public void Create_NullInputsThrowArgumentNullException()
        {
            var inputs = new Mock<IProjectSnapshotPersistenceInputs>().Object;
            var factory = new ProjectSnapshotFactory(inputs);

            Assert.Multiple(() =>
            {
                Assert.That(() => factory.Create(null!), Throws.ArgumentNullException);
                Assert.That(() => new ProjectSnapshotFactory(null!), Throws.ArgumentNullException);
            });
        }

        [Test]
        public void Create_ToProjectData_HashPinStaysStable_AcrossCatalogEmbeddingRemoval()
        {
            // Phase 11 / DEC-006: pins the exact ProjectData bytes produced
            // through the save chain after custom catalogs left the wire. Any
            // shape/value drift in the snapshot or the mapper changes this
            // hash and fails the pin.
            var climate = new ClimateStateSnapshot("Пин-Сити", "Region", -20, -35, 5, 70, 2, ClimateZone.Zone_M15, true, true, false);
            var layersAbove = new[]
            {
                new ConstructionLayerSnapshot(Guid.NewGuid(), 5, "Асфальт", 80, 0.81, true, LayerPosition.AbovePipe, 0)
            };
            var construction = new ConstructionStateSnapshot(0.9, true, layersAbove, Array.Empty<ConstructionLayerSnapshot>());
            var session = new Mock<IProjectSession>(MockBehavior.Strict);
            var inputs = new Mock<IProjectSnapshotPersistenceInputs>(MockBehavior.Strict);
            var climateState = new Mock<IProjectSessionClimateState>(MockBehavior.Strict);
            var constructionState = new Mock<IProjectSessionConstructionState>(MockBehavior.Strict);
            var thermalState = new Mock<IProjectSessionThermalState>(MockBehavior.Strict);
            var hydraulicsState = new Mock<IProjectSessionHydraulicsState>(MockBehavior.Strict);

            session.SetupGet(x => x.ProjectNumber).Returns("PIN-1");
            session.SetupGet(x => x.ProjectObject).Returns("Hash pin object");
            session.SetupGet(x => x.ClimateState).Returns(climateState.Object);
            session.SetupGet(x => x.ConstructionState).Returns(constructionState.Object);
            session.SetupGet(x => x.ThermalState).Returns(thermalState.Object);
            session.SetupGet(x => x.HydraulicsState).Returns(hydraulicsState.Object);
            climateState.SetupGet(x => x.Snapshot).Returns(climate);
            constructionState.SetupGet(x => x.Snapshot).Returns(construction);
            thermalState.SetupGet(x => x.Snapshot).Returns(ThermalStateSnapshot.Default);
            hydraulicsState.SetupGet(x => x.Snapshot).Returns(HydraulicsStateSnapshot.Default);
            inputs.SetupGet(x => x.IsOperatingMode).Returns(true);

            var snapshot = new ProjectSnapshotFactory(inputs.Object).Create(session.Object);
            var catalog = new Mock<IMaterialRepository>();
            catalog.Setup(r => r.GetAllMaterials()).Returns(Array.Empty<global::SnowMeltingCalculator.Models.Construction.Material>);
            var dates = new ProjectSaveDates(
                new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 11, 0, 0, DateTimeKind.Utc));

            var data = ProjectPersistenceMapper.ToProjectData(snapshot, dates, catalog.Object);

            var json = JsonSerializer.Serialize(data);
            using var sha256 = SHA256.Create();
            var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(json)));

            Assert.That(hash, Is.EqualTo(
                "FBD2010C0C8BF0F1552BE48F4CFAFF30A35ACFA57CA42D7DA0F39A2729B1B7B5"));
        }
    }
}
