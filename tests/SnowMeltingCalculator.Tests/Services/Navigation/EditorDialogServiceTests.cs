using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Services.Navigation;

namespace SnowMeltingCalculator.Tests.Services.Navigation
{
    /// <summary>
    /// Tests for <see cref="EditorDialogService"/> registration and behavior.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class EditorDialogServiceTests
    {
        [Test]
        public void AddNavigationServices_RegistersEditorDialogServiceAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNavigationServices();

            // Act
            var provider = services.BuildServiceProvider();
            var instance1 = provider.GetService<IEditorDialogService>();
            var instance2 = provider.GetService<IEditorDialogService>();

            // Assert
            Assert.That(instance1, Is.Not.Null);
            Assert.That(instance1, Is.InstanceOf<EditorDialogService>());
            Assert.That(instance1, Is.SameAs(instance2));
        }

        [Test]
        public void ShowMaterialEditor_WhenMainWindowIsNull_ReturnsNull()
        {
            // Arrange
            var serviceProviderMock = new Mock<IServiceProvider>();
            var service = new EditorDialogService(serviceProviderMock.Object);

            // Act
            var result = service.ShowMaterialEditor();

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ShowTemplateEditor_WhenViewTypeIsNotRegistered_ReturnsNull()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IEditorDialogService, EditorDialogService>();
            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IEditorDialogService>();

            Window? previousMainWindow = null;
            bool applicationCreated = false;
            if (Application.Current == null)
            {
                applicationCreated = true;
                _ = new Application();
            }
            previousMainWindow = Application.Current!.MainWindow;
            Application.Current.MainWindow = new Window();

            try
            {
                // Act
                var result = service.ShowTemplateEditor();

                // Assert
                Assert.That(result, Is.Null);
            }
            finally
            {
                Application.Current.MainWindow = previousMainWindow;
                if (applicationCreated)
                {
                    Application.Current.Shutdown();
                }
            }
        }
    }
}
