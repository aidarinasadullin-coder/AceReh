using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ProjectSessionLegacyStoreGuardTests
    {
        private static readonly IReadOnlyList<string> LifecycleFieldPatterns = new List<string>
        {
            "ProjectNumber",
            "ProjectObject",
            "CurrentFilePath",
            "IsDirty",
            "IsLoadProjectInProgress",
            "LoadProjectInProgress",
        };

        [Test]
        public void ProjectStateService_HasNoMutableLifecycleBackingFields()
        {
            var forbidden = FindLifecycleBackingFields(typeof(ProjectStateService));

            Assert.That(forbidden, Is.Empty,
                "ProjectStateService must hold only an IProjectSession reference, not duplicate lifecycle state. " +
                "Found: " + string.Join(", ", forbidden));
        }

        [Test]
        public void CalculationStateService_HasNoLocalRestoreGuardBackingField()
        {
            var forbidden = FindLifecycleBackingFields(typeof(CalculationStateService));

            Assert.That(forbidden, Is.Empty,
                "CalculationStateService must delegate IsLoadProjectInProgress to IProjectSession and hold no local guard copy. " +
                "Found: " + string.Join(", ", forbidden));
        }

        private static IEnumerable<string> FindLifecycleBackingFields(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => !f.IsInitOnly)
                .Where(f => LifecycleFieldPatterns.Any(pattern =>
                    f.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                .Select(f => $"{f.Name} ({f.FieldType.Name})");
        }
    }
}
