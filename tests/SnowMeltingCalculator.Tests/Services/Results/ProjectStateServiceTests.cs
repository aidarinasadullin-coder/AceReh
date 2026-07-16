// ================================================================================
// REHAU Снеготаяние - Тесты для ProjectStateService
// ================================================================================

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Services.Results
{
    /// <summary>
    /// Тесты для ProjectStateService
    /// </summary>
    [TestFixture]
    public class ProjectStateServiceTests
    {
        private ProjectStateService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new ProjectStateService();
        }

        #region Начальное состояние

        [Test]
        public void IsDirty_Initially_False()
        {
            // Assert
            Assert.That(_service.IsDirty, Is.False);
        }

        [Test]
        public void CurrentFilePath_Initially_Null()
        {
            // Assert
            Assert.That(_service.CurrentFilePath, Is.Null);
        }

        [Test]
        public void ProjectNumber_Initially_Empty()
        {
            // Assert
            Assert.That(_service.ProjectNumber, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ProjectObject_Initially_Empty()
        {
            // Assert
            Assert.That(_service.ProjectObject, Is.EqualTo(string.Empty));
        }

        #endregion

        #region MarkDirty

        [Test]
        public void MarkDirty_SetsIsDirtyToTrue()
        {
            // Act
            _service.MarkDirty();

            // Assert
            Assert.That(_service.IsDirty, Is.True);
        }

        [Test]
        public void MarkDirty_RaisesPropertyChanged_ForIsDirty()
        {
            // Arrange
            var changedProperties = new List<string>();
            _service.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

            // Act
            _service.MarkDirty();

            // Assert
            Assert.That(changedProperties, Contains.Item(nameof(_service.IsDirty)));
        }

        [Test]
        public void MarkDirty_DoesNotRaisePropertyChanged_WhenAlreadyDirty()
        {
            // Arrange
            _service.MarkDirty();
            var changedProperties = new List<string>();
            _service.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

            // Act
            _service.MarkDirty();

            // Assert
            Assert.That(changedProperties, Is.Empty);
        }

        #endregion

        #region MarkClean

        [Test]
        public void MarkClean_SetsIsDirtyToFalse()
        {
            // Arrange
            _service.MarkDirty();

            // Act
            _service.MarkClean();

            // Assert
            Assert.That(_service.IsDirty, Is.False);
        }

        [Test]
        public void MarkClean_RaisesPropertyChanged_ForIsDirty()
        {
            // Arrange
            _service.MarkDirty();
            var changedProperties = new List<string>();
            _service.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

            // Act
            _service.MarkClean();

            // Assert
            Assert.That(changedProperties, Contains.Item(nameof(_service.IsDirty)));
        }

        [Test]
        public void MarkClean_DoesNotRaisePropertyChanged_WhenAlreadyClean()
        {
            // Arrange
            var changedProperties = new List<string>();
            _service.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

            // Act
            _service.MarkClean();

            // Assert
            Assert.That(changedProperties, Is.Empty);
        }

        #endregion

        #region CurrentFilePath

        [Test]
        public void CurrentFilePath_Setter_RaisesPropertyChanged_ForCurrentFilePath()
        {
            // Arrange
            var changedProperties = new List<string>();
            _service.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

            // Act
            _service.CurrentFilePath = "C:\\test.smc";

            // Assert
            Assert.That(changedProperties, Contains.Item(nameof(_service.CurrentFilePath)));
        }

        [Test]
        public void CurrentFilePath_Setter_DoesNotRaisePropertyChanged_WhenValueUnchanged()
        {
            // Arrange
            _service.CurrentFilePath = "C:\\test.smc";
            var changedProperties = new List<string>();
            _service.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

            // Act
            _service.CurrentFilePath = "C:\\test.smc";

            // Assert
            Assert.That(changedProperties, Is.Empty);
        }

        #endregion

        #region Интеграция с DI

        [Test]
        public void DependencyInjection_ResolvesSameInstance_ForProjectInfoServiceAndProjectStateService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ProjectStateService>();
            services.AddSingleton<IProjectInfoService>(sp => sp.GetRequiredService<ProjectStateService>());
            services.AddSingleton<IProjectStateService>(sp => sp.GetRequiredService<ProjectStateService>());
            services.AddSingleton<IMarkDirtyService>(sp => sp.GetRequiredService<ProjectStateService>());

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var projectInfoService = serviceProvider.GetRequiredService<IProjectInfoService>();
            var projectStateService = serviceProvider.GetRequiredService<IProjectStateService>();

            // Assert
            Assert.That(projectInfoService, Is.SameAs(projectStateService));
        }

        [Test]
        public void DependencyInjection_ResolvesSameInstance_ForProjectStateServiceAndMarkDirtyService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ProjectStateService>();
            services.AddSingleton<IProjectInfoService>(sp => sp.GetRequiredService<ProjectStateService>());
            services.AddSingleton<IProjectStateService>(sp => sp.GetRequiredService<ProjectStateService>());
            services.AddSingleton<IMarkDirtyService>(sp => sp.GetRequiredService<ProjectStateService>());

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var projectStateService = serviceProvider.GetRequiredService<IProjectStateService>();
            var markDirtyService = serviceProvider.GetRequiredService<IMarkDirtyService>();

            // Assert
            Assert.That(projectStateService, Is.SameAs(markDirtyService));
        }

        [Test]
        public void ApplicationServices_ResolvesProjectStateService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddApplicationServices();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var projectStateService = serviceProvider.GetRequiredService<IProjectStateService>();

            // Assert
            Assert.That(projectStateService, Is.Not.Null);
        }

        #endregion
    }
}
