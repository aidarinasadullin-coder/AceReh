using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public sealed class ProjectSnapshotFactoryTests
    {
        [Test]
        public void Create_ReadsEachCanonicalSessionValueOnceAndFiltersBuiltIns()
        {
            var climate = new ClimateStateSnapshot("City", "Region", -1, -2, 3, 4, 5, ClimateZone.Zone_M15, true, true, false);
            var construction = new ConstructionStateSnapshot(1, false, Array.Empty<ConstructionLayerSnapshot>(), Array.Empty<ConstructionLayerSnapshot>());
            var thermal = ThermalStateSnapshot.Default;
            var hydraulics = HydraulicsStateSnapshot.Default;
            var session = new Mock<IProjectSession>(MockBehavior.Strict);
            var inputs = new Mock<IProjectSnapshotPersistenceInputs>(MockBehavior.Strict);
            var material = new Material { Id = 7, Name = "Custom", IsBuiltIn = false };
            var builtInMaterial = new Material { Id = 8, Name = "Built-in", IsBuiltIn = true };
            var template = new ConstructionTemplate { Id = 21, Name = "Custom template", IsBuiltIn = false };
            var builtInTemplate = new ConstructionTemplate { Id = 22, Name = "Built-in template", IsBuiltIn = true };
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
            inputs.SetupGet(x => x.Materials).Returns(new[] { material, builtInMaterial });
            inputs.SetupGet(x => x.Templates).Returns(new[] { template, builtInTemplate });

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
                Assert.That(snapshot.CustomMaterials, Has.Count.EqualTo(1));
                Assert.That(snapshot.CustomTemplates, Has.Count.EqualTo(1));
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
    }
}
