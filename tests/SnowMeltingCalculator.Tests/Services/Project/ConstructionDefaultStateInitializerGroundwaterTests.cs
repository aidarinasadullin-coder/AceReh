using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Tests.Construction;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Дефолты конструкции согласованы с УГВ: заводской сброс — «сухие» λА,
    /// влажный УГВ — λБ под трубой (план 2026-09-04, D1+D6).
    /// </summary>
    [TestFixture]
    public sealed class ConstructionDefaultStateInitializerGroundwaterTests
    {
        [Test]
        public async Task Apply_DefaultOverload_UsesFactoryGroundwaterLevel()
        {
            var materialRepository = new MockMaterialRepository();
            await materialRepository.LoadMaterialsAsync();
            var state = new ProjectSessionConstructionState();
            var initializer = new ConstructionDefaultStateInitializer(materialRepository, state);

            var result = initializer.Apply(ConstructionMutationOrigin.Reset);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsChanged, Is.True);
                Assert.That(state.Snapshot.GroundwaterLevel, Is.EqualTo(2.0).Within(1e-9));
                Assert.That(state.Snapshot.LayersBelowPipe, Has.Count.EqualTo(6));
                Assert.That(
                    state.Snapshot.LayersBelowPipe.Select(l => l.CalculatedLambda),
                    Is.EqualTo(state.Snapshot.LayersBelowPipe.Select(
                        l => materialRepository.GetMaterialById(l.MaterialId)!.LambdaA)));
            });
        }

        [Test]
        public async Task Apply_WetGroundwaterLevel_BelowPipeLayersUseLambdaB()
        {
            var materialRepository = new MockMaterialRepository();
            await materialRepository.LoadMaterialsAsync();
            var state = new ProjectSessionConstructionState();
            var initializer = new ConstructionDefaultStateInitializer(materialRepository, state);

            initializer.Apply(0.5, ConstructionMutationOrigin.Reset);

            var above = state.Snapshot.LayersAbovePipe.Single();
            Assert.That(above.CalculatedLambda, Is.EqualTo(materialRepository.GetMaterialById(above.MaterialId)!.LambdaA));
            Assert.That(
                state.Snapshot.LayersBelowPipe.Select(l => l.CalculatedLambda),
                Is.EqualTo(state.Snapshot.LayersBelowPipe.Select(
                    l => materialRepository.GetMaterialById(l.MaterialId)!.LambdaB)));
        }
    }
}
