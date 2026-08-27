using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Phase 6 Task 3 contract tests for the immutable project snapshot
    /// (src/Services/Project/ProjectSnapshot.cs): required null rejection,
    /// defensive-copy isolation at every collection level, get-only property
    /// shape, exclusion of runtime/UI/date state, canonical module snapshot
    /// types and absence of a second writable owner.
    /// Dates are intentionally NOT part of ProjectSnapshot; they remain
    /// explicit save-operation inputs for later save tasks (4/5).
    /// </summary>
    [TestFixture]
    public class ProjectSnapshotContractTests
    {
        private static readonly Type[] SnapshotContractTypes =
        {
            typeof(ProjectSnapshot),
            typeof(ProjectCustomMaterialRecord),
            typeof(ProjectTemplateRecord),
            typeof(ProjectTemplateLayerRecord),
        };

        private static readonly string[] ForbiddenPropertyNamePatterns =
        {
            "CurrentFilePath",
            "FilePath",
            "Dirty",
            "LoadProjectInProgress",
            "Restore",
            "CreatedDate",
            "ModifiedDate",
        };

        // ------------------------------------------------------------------
        // Helpers: real production snapshot types only, no fakes.
        // ------------------------------------------------------------------

        private static ClimateStateSnapshot CreateClimateSnapshot() => new(
            "Москва",
            "Московская область",
            -15.0,
            -28.0,
            5.0,
            70.0,
            0.0,
            ClimateZone.Zone_M15,
            IsHighRequirements: false,
            IsCitySelected: true,
            HasUserModifications: false);

        private static ConstructionLayerSnapshot CreateLayer(LayerPosition position, int order) =>
            new(Guid.NewGuid(), 5, "Пенополистирол ЭППС", 100.0 + order, 0.041, false, position, order);

        private static ConstructionStateSnapshot CreateConstructionSnapshot()
        {
            var above = new[] { CreateLayer(LayerPosition.AbovePipe, 0) };
            var below = new[]
            {
                CreateLayer(LayerPosition.BelowPipe, 0),
                CreateLayer(LayerPosition.BelowPipe, 1),
            };

            return new ConstructionStateSnapshot(1.5, hasLoads: false, above, below);
        }

        private static ProjectCustomMaterialRecord CreateMaterialRecord(int id = 100) =>
            new(id, $"Тестовый материал {id}", MaterialCategory.Insulation, 0.35, 0.41, null, null, null, isBuiltIn: false);

        private static ProjectTemplateLayerRecord CreateTemplateLayerRecord(
            double thickness = 50.0,
            LayerPosition position = LayerPosition.AbovePipe,
            int order = 0) =>
            new(5, thickness, position, order);

        private static ProjectTemplateRecord CreateTemplateRecord(
            IEnumerable<ProjectTemplateLayerRecord> layersAbovePipe,
            IEnumerable<ProjectTemplateLayerRecord> layersBelowPipe,
            IEnumerable<ProjectCustomMaterialRecord> materialSnapshots) =>
            new(1, "Тестовый шаблон", "Описание шаблона", layersAbovePipe, layersBelowPipe, hasLoads: false, 1.2, isBuiltIn: false, materialSnapshots);

        private static ProjectSnapshot CreateSnapshot(
            IEnumerable<ProjectCustomMaterialRecord>? customMaterials = null,
            IEnumerable<ProjectTemplateRecord>? customTemplates = null)
        {
            return new ProjectSnapshot(
                "ПР-001",
                "Тестовый объект",
                isOperatingMode: true,
                CreateClimateSnapshot(),
                CreateConstructionSnapshot(),
                ThermalStateSnapshot.Default,
                HydraulicsStateSnapshot.Default,
                customMaterials ?? Array.Empty<ProjectCustomMaterialRecord>(),
                customTemplates ?? Array.Empty<ProjectTemplateRecord>());
        }

        // ------------------------------------------------------------------
        // Required null rejection.
        // ------------------------------------------------------------------

        [Test]
        public void Constructor_WhenProjectNumberIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot(null, "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default, Array.Empty<ProjectCustomMaterialRecord>(), Array.Empty<ProjectTemplateRecord>()),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("projectNumber"));
        }

        [Test]
        public void Constructor_WhenProjectObjectIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", null, true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default, Array.Empty<ProjectCustomMaterialRecord>(), Array.Empty<ProjectTemplateRecord>()),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("projectObject"));
        }

        [Test]
        public void Constructor_WhenClimateSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, null, CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default, Array.Empty<ProjectCustomMaterialRecord>(), Array.Empty<ProjectTemplateRecord>()),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("climateStateSnapshot"));
        }

        [Test]
        public void Constructor_WhenConstructionSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), null, ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default, Array.Empty<ProjectCustomMaterialRecord>(), Array.Empty<ProjectTemplateRecord>()),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("constructionStateSnapshot"));
        }

        [Test]
        public void Constructor_WhenThermalSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), null, HydraulicsStateSnapshot.Default, Array.Empty<ProjectCustomMaterialRecord>(), Array.Empty<ProjectTemplateRecord>()),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("thermalStateSnapshot"));
        }

        [Test]
        public void Constructor_WhenHydraulicsSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, null, Array.Empty<ProjectCustomMaterialRecord>(), Array.Empty<ProjectTemplateRecord>()),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("hydraulicsStateSnapshot"));
        }

        [Test]
        public void Constructor_WhenMaterialsCollectionIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default, null, Array.Empty<ProjectTemplateRecord>()),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("customMaterials"));
        }

        [Test]
        public void Constructor_WhenTemplatesCollectionIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default, Array.Empty<ProjectCustomMaterialRecord>(), null),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("customTemplates"));
        }

        [Test]
        public void Constructor_WhenMaterialsContainsNullElement_ThrowsArgumentException()
        {
            var materials = new List<ProjectCustomMaterialRecord> { CreateMaterialRecord(101), null! };

            Assert.That(
                () => CreateSnapshot(customMaterials: materials),
                Throws.ArgumentException.With.Message.Contains("customMaterials"));
        }

        [Test]
        public void Constructor_WhenTemplatesContainsNullElement_ThrowsArgumentException()
        {
            var templates = new List<ProjectTemplateRecord> { null! };

            Assert.That(
                () => CreateSnapshot(customTemplates: templates),
                Throws.ArgumentException.With.Message.Contains("customTemplates"));
        }

        // ------------------------------------------------------------------
        // Defensive-copy isolation.
        // ------------------------------------------------------------------

        [Test]
        public void CustomMaterials_SourceListMutatedAfterConstruction_SnapshotStaysUnchanged()
        {
            var source = new List<ProjectCustomMaterialRecord> { CreateMaterialRecord(101), CreateMaterialRecord(102) };
            var snapshot = CreateSnapshot(customMaterials: source);
            var expected = snapshot.CustomMaterials.ToArray();

            source.Add(CreateMaterialRecord(103));
            source.RemoveAt(0);
            source.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.CustomMaterials.Count, Is.EqualTo(2));
                Assert.That(snapshot.CustomMaterials, Is.EqualTo(expected));
                Assert.That(((ICollection<ProjectCustomMaterialRecord>)snapshot.CustomMaterials).IsReadOnly, Is.True);
                Assert.That(
                    () => ((ICollection<ProjectCustomMaterialRecord>)snapshot.CustomMaterials).Add(CreateMaterialRecord(999)),
                    Throws.TypeOf<NotSupportedException>());
            });
        }

        [Test]
        public void CustomTemplates_SourceListMutatedAfterConstruction_SnapshotStaysUnchanged()
        {
            var template = CreateTemplateRecord(
                new[] { CreateTemplateLayerRecord() },
                Array.Empty<ProjectTemplateLayerRecord>(),
                Array.Empty<ProjectCustomMaterialRecord>());
            var source = new List<ProjectTemplateRecord> { template };
            var snapshot = CreateSnapshot(customTemplates: source);
            var expected = snapshot.CustomTemplates.ToArray();

            source.Add(template);
            source.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.CustomTemplates.Count, Is.EqualTo(1));
                Assert.That(snapshot.CustomTemplates, Is.EqualTo(expected));
                Assert.That(((ICollection<ProjectTemplateRecord>)snapshot.CustomTemplates).IsReadOnly, Is.True);
                Assert.That(
                    () => ((ICollection<ProjectTemplateRecord>)snapshot.CustomTemplates).Add(template),
                    Throws.TypeOf<NotSupportedException>());
            });
        }

        [Test]
        public void CustomTemplates_NestedLayerSourcesMutatedAfterConstruction_SnapshotStaysUnchanged()
        {
            var nestedAbove = new List<ProjectTemplateLayerRecord> { CreateTemplateLayerRecord(order: 0) };
            var templatesSource = new List<ProjectTemplateRecord>
            {
                CreateTemplateRecord(nestedAbove, Array.Empty<ProjectTemplateLayerRecord>(), Array.Empty<ProjectCustomMaterialRecord>()),
            };
            var snapshot = CreateSnapshot(customTemplates: templatesSource);

            nestedAbove.Add(CreateTemplateLayerRecord(thickness: 77.0, order: 5));
            templatesSource.Add(CreateTemplateRecord(Array.Empty<ProjectTemplateLayerRecord>(), Array.Empty<ProjectTemplateLayerRecord>(), Array.Empty<ProjectCustomMaterialRecord>()));

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.CustomTemplates.Count, Is.EqualTo(1));
                Assert.That(snapshot.CustomTemplates[0].LayersAbovePipe.Count, Is.EqualTo(1));
                Assert.That(snapshot.CustomTemplates[0].LayersAbovePipe[0].Thickness, Is.EqualTo(50.0));
            });
        }

        [Test]
        public void TemplateRecord_SourceCollectionsMutatedAfterConstruction_RecordStaysUnchanged()
        {
            var above = new List<ProjectTemplateLayerRecord> { CreateTemplateLayerRecord(position: LayerPosition.AbovePipe, order: 0) };
            var below = new List<ProjectTemplateLayerRecord> { CreateTemplateLayerRecord(position: LayerPosition.BelowPipe, order: 0) };
            var materials = new List<ProjectCustomMaterialRecord> { CreateMaterialRecord(201) };

            var template = CreateTemplateRecord(above, below, materials);
            var aboveExpected = template.LayersAbovePipe.ToArray();
            var belowExpected = template.LayersBelowPipe.ToArray();
            var materialsExpected = template.MaterialSnapshots.ToArray();

            above.Add(CreateTemplateLayerRecord(order: 1));
            below.Clear();
            materials.Add(CreateMaterialRecord(202));

            Assert.Multiple(() =>
            {
                Assert.That(template.LayersAbovePipe, Is.EqualTo(aboveExpected));
                Assert.That(template.LayersBelowPipe, Is.EqualTo(belowExpected));
                Assert.That(template.MaterialSnapshots, Is.EqualTo(materialsExpected));
                Assert.That(
                    () => ((ICollection<ProjectTemplateLayerRecord>)template.LayersAbovePipe).Add(CreateTemplateLayerRecord()),
                    Throws.TypeOf<NotSupportedException>());
            });
        }

        // ------------------------------------------------------------------
        // Property shape: get-only, canonical module types, no runtime/UI/date.
        // ------------------------------------------------------------------

        [Test]
        public void PublicProperties_AllContractTypes_AreGetOnly()
        {
            foreach (var type in SnapshotContractTypes)
            {
                var writable = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .Select(p => $"{type.Name}.{p.Name}")
                    .ToArray();

                Assert.That(writable, Is.Empty, $"{type.Name} must expose only get-only public properties.");
            }
        }

        [Test]
        public void ModuleProperties_UseExactlyTheFourCanonicalSnapshotTypes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(typeof(ProjectSnapshot).GetProperty(nameof(ProjectSnapshot.ClimateStateSnapshot))!.PropertyType, Is.EqualTo(typeof(ClimateStateSnapshot)));
                Assert.That(typeof(ProjectSnapshot).GetProperty(nameof(ProjectSnapshot.ConstructionStateSnapshot))!.PropertyType, Is.EqualTo(typeof(ConstructionStateSnapshot)));
                Assert.That(typeof(ProjectSnapshot).GetProperty(nameof(ProjectSnapshot.ThermalStateSnapshot))!.PropertyType, Is.EqualTo(typeof(ThermalStateSnapshot)));
                Assert.That(typeof(ProjectSnapshot).GetProperty(nameof(ProjectSnapshot.HydraulicsStateSnapshot))!.PropertyType, Is.EqualTo(typeof(HydraulicsStateSnapshot)));
            });
        }

        [Test]
        public void PublicProperties_ExcludeLifecycleRuntimeUiAndDateNames()
        {
            var names = typeof(ProjectSnapshot)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToArray();

            var violations = ForbiddenPropertyNamePatterns
                .Where(pattern => names.Any(name => name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            Assert.That(
                violations,
                Is.Empty,
                "ProjectSnapshot must exclude paths, dirty flags, restore guards and dates; found: " + string.Join(", ", violations));
        }

        [Test]
        public void PublicPropertyTypes_DoNotReferenceViewModelsOrWpf()
        {
            var offenders = typeof(ProjectSnapshot)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(p => ExpandToReferencedTypes(p.PropertyType))
                .Where(t => t.Namespace is not null)
                .Where(t => t.Namespace!.StartsWith("SnowMeltingCalculator.ViewModels", StringComparison.Ordinal)
                            || t.Namespace.StartsWith("System.Windows", StringComparison.Ordinal))
                .Select(t => t.FullName)
                .Distinct()
                .ToArray();

            Assert.That(offenders, Is.Empty, "ProjectSnapshot property graph must not depend on ViewModels or WPF.");
        }

        // ------------------------------------------------------------------
        // Ownership guard: no second writable owner inside the contract itself.
        // ------------------------------------------------------------------

        [Test]
        public void ProjectSnapshot_IsSealedWithoutWritableStateOrLifecycleMutators()
        {
            var type = typeof(ProjectSnapshot);

            var writableFields = type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsInitOnly)
                .Select(f => f.Name)
                .ToArray();

            var mutatorMethods = type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.Name)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(type.IsSealed, Is.True, "ProjectSnapshot must be sealed.");
                Assert.That(type.GetInterfaces(), Is.Empty, "ProjectSnapshot must implement no mutable lifecycle ownership interface.");
                Assert.That(writableFields, Is.Empty, "All instance fields must be readonly.");
                Assert.That(type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Empty, "ProjectSnapshot must declare no events.");
                Assert.That(mutatorMethods, Is.Empty, "ProjectSnapshot must expose no lifecycle mutator methods.");
            });
        }

        // ------------------------------------------------------------------
        // Value round-trip with the four canonical snapshots as provided.
        // ------------------------------------------------------------------

        [Test]
        public void Constructor_StoresIdentityModeAndCanonicalSnapshotsAsProvided()
        {
            var climate = CreateClimateSnapshot();
            var construction = CreateConstructionSnapshot();
            var thermal = ThermalStateSnapshot.Default;
            var hydraulics = HydraulicsStateSnapshot.Default;

            var snapshot = new ProjectSnapshot(
                "ПР-042",
                "Администрация",
                isOperatingMode: false,
                climate,
                construction,
                thermal,
                hydraulics,
                Array.Empty<ProjectCustomMaterialRecord>(),
                Array.Empty<ProjectTemplateRecord>());

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ProjectNumber, Is.EqualTo("ПР-042"));
                Assert.That(snapshot.ProjectObject, Is.EqualTo("Администрация"));
                Assert.That(snapshot.IsOperatingMode, Is.False);
                Assert.That(snapshot.ClimateStateSnapshot, Is.SameAs(climate));
                Assert.That(snapshot.ConstructionStateSnapshot, Is.SameAs(construction));
                Assert.That(snapshot.ThermalStateSnapshot, Is.SameAs(thermal));
                Assert.That(snapshot.HydraulicsStateSnapshot, Is.SameAs(hydraulics));
            });
        }

        [Test]
        public void Constructor_EmptyCollections_ProduceEmptyLists()
        {
            var snapshot = CreateSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.CustomMaterials, Is.Empty);
                Assert.That(snapshot.CustomTemplates, Is.Empty);
            });
        }

        private static IEnumerable<Type> ExpandToReferencedTypes(Type type)
        {
            yield return type;

            if (type.IsArray && type.GetElementType() is { } elementType)
            {
                foreach (var nested in ExpandToReferencedTypes(elementType))
                {
                    yield return nested;
                }
            }

            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var nested in ExpandToReferencedTypes(argument))
                {
                    yield return nested;
                }
            }
        }
    }
}
