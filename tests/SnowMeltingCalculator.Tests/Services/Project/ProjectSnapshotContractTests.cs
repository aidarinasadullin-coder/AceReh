using System;
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
    /// get-only property shape, exclusion of runtime/UI/date state, canonical
    /// module snapshot types and absence of a second writable owner.
    /// Dates are intentionally NOT part of ProjectSnapshot; they remain
    /// explicit save-operation inputs for later save tasks (4/5).
    /// DEC-006 (2026-09-03): catalogs live only globally — the snapshot no
    /// longer carries custom materials/templates at all, and this contract
    /// pins their absence.
    /// </summary>
    [TestFixture]
    public class ProjectSnapshotContractTests
    {
        private static readonly string[] ForbiddenPropertyNamePatterns =
        {
            "CurrentFilePath",
            "FilePath",
            "Dirty",
            "LoadProjectInProgress",
            "Restore",
            "CreatedDate",
            "ModifiedDate",
            "CustomMaterial",
            "CustomTemplate",
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

            return new ConstructionStateSnapshot(1.5, above, below);
        }

        private static ProjectSnapshot CreateSnapshot() => new(
            "ПР-001",
            "Тестовый объект",
            isOperatingMode: true,
            CreateClimateSnapshot(),
            CreateConstructionSnapshot(),
            ThermalStateSnapshot.Default,
            HydraulicsStateSnapshot.Default);

        // ------------------------------------------------------------------
        // Required null rejection.
        // ------------------------------------------------------------------

        [Test]
        public void Constructor_WhenProjectNumberIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot(null, "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("projectNumber"));
        }

        [Test]
        public void Constructor_WhenProjectObjectIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", null, true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("projectObject"));
        }

        [Test]
        public void Constructor_WhenClimateSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, null, CreateConstructionSnapshot(), ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("climateStateSnapshot"));
        }

        [Test]
        public void Constructor_WhenConstructionSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), null, ThermalStateSnapshot.Default, HydraulicsStateSnapshot.Default),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("constructionStateSnapshot"));
        }

        [Test]
        public void Constructor_WhenThermalSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), null, HydraulicsStateSnapshot.Default),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("thermalStateSnapshot"));
        }

        [Test]
        public void Constructor_WhenHydraulicsSnapshotIsNull_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new ProjectSnapshot("ПР-001", "Объект", true, CreateClimateSnapshot(), CreateConstructionSnapshot(), ThermalStateSnapshot.Default, null),
                Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("hydraulicsStateSnapshot"));
        }

        // ------------------------------------------------------------------
        // Property shape: get-only, canonical module types, no runtime/UI/date.
        // ------------------------------------------------------------------

        [Test]
        public void PublicProperties_AllContractTypes_AreGetOnly()
        {
            var writable = typeof(ProjectSnapshot)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => $"{typeof(ProjectSnapshot).Name}.{p.Name}")
                .ToArray();

            Assert.That(writable, Is.Empty, "ProjectSnapshot must expose only get-only public properties.");
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
                "ProjectSnapshot must exclude paths, dirty flags, restore guards, dates and embedded catalogs; found: " + string.Join(", ", violations));
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
        // DEC-006: catalogs live only globally — no catalog members anywhere
        // in the snapshot contract (properties, fields or nested types).
        // ------------------------------------------------------------------

        [Test]
        public void ProjectSnapshot_CarriesNoCustomCatalogMembers()
        {
            var type = typeof(ProjectSnapshot);
            var memberNames = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(memberNames.Where(n => n.Contains("CustomMaterial", StringComparison.Ordinal)), Is.Empty,
                    "ProjectSnapshot must not carry CustomMaterial members (DEC-006).");
                Assert.That(memberNames.Where(n => n.Contains("CustomTemplate", StringComparison.Ordinal)), Is.Empty,
                    "ProjectSnapshot must not carry CustomTemplate members (DEC-006).");
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
                hydraulics);

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
