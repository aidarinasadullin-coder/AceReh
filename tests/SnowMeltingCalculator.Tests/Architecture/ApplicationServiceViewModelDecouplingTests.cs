using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Architecture
{
    /// <summary>
    /// Phase 9 slice 5 (INV-008): application services SHALL NOT depend on
    /// concrete ViewModels. Static architecture test — scans every concrete
    /// class in the application-service namespaces and rejects constructor
    /// parameters typed as concrete ViewModel classes.
    /// Application-owned adapter interfaces (IProjectLoad*Adapter,
    /// IReport*Source) are the approved seam; they bind to the same
    /// singleton module adapters via DI.
    /// </summary>
    [TestFixture]
    public class ApplicationServiceViewModelDecouplingTests
    {
        private static readonly string[] ApplicationServiceNamespaces =
        {
            "SnowMeltingCalculator.Services"
        };

        private static readonly string[] ViewModelNamespacePrefix =
        {
            "SnowMeltingCalculator.ViewModels"
        };

        [Test]
        public void ApplicationServices_HaveNoConcreteViewModelConstructorDependencies()
        {
            var productionAssembly = typeof(global::SnowMeltingCalculator.Services.Project.ProjectLoadOrchestrator).Assembly;

            var violations =
                (from type in productionAssembly.GetTypes()
                 where type.IsClass
                       && !type.IsAbstract
                       && !type.IsCompilerGenerated()
                 where ApplicationServiceNamespaces.Any(prefix =>
                     type.Namespace is not null
                     && (type.Namespace == prefix || type.Namespace.StartsWith(prefix + ".", StringComparison.Ordinal)))
                 from constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 from parameter in constructor.GetParameters()
                 where ViewModelNamespacePrefix.Any(vmPrefix =>
                     parameter.ParameterType.Namespace is not null
                     && parameter.ParameterType.Namespace.StartsWith(vmPrefix, StringComparison.Ordinal))
                 select $"{type.FullName}({constructor.GetParameters().Length} params): parameter '{parameter.Name}' is concrete ViewModel type '{parameter.ParameterType.FullName}'")
                    .ToList();

            Assert.That(violations, Is.Empty,
                "INV-008: application services must not depend on concrete ViewModels. " +
                "Use the application-owned adapter interfaces (IProjectLoad*Adapter / IReport*Source). " +
                "Violations:\n" + string.Join("\n", violations));
        }
    }

    internal static class TypeReflectionExtensions
    {
        public static bool IsCompilerGenerated(this Type type) =>
            type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null
            || type.FullName?.Contains("<", StringComparison.Ordinal) == true;
    }
}
