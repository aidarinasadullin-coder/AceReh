using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Views
{
    [TestFixture]
    public class MainWindowChromeLayoutTests
    {
        [Test]
        public void MainWindowRootGrid_usesWindowResizeBorderThicknessMargin_whenMaximized()
        {
            var xaml = XDocument.Load(FindMainWindowXamlPath());
            var presentation = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml/presentation");

            var rootGrid = xaml.Root!.Elements(presentation + "Grid").Single();
            var style = rootGrid.Element(presentation + "Grid.Style")?.Element(presentation + "Style");

            Assert.That(style, Is.Not.Null);
            Assert.That(style!.Attribute("TargetType")?.Value, Is.EqualTo("Grid"));
            Assert.That(
                style.Elements(presentation + "Setter").Any(setter =>
                    setter.Attribute("Property")?.Value == "Margin" &&
                    setter.Attribute("Value")?.Value == "0"),
                Is.True);

            var maximizedTrigger = style
                .Element(presentation + "Style.Triggers")?
                .Elements(presentation + "DataTrigger")
                .SingleOrDefault(trigger =>
                    trigger.Attribute("Value")?.Value == "Maximized" &&
                    trigger.Attribute("Binding")?.Value.Contains("AncestorType=Window") == true &&
                    trigger.Attribute("Binding")?.Value.Contains("Path=WindowState") == true);

            Assert.That(maximizedTrigger, Is.Not.Null);
            Assert.That(
                maximizedTrigger!
                    .Elements(presentation + "Setter")
                    .Any(setter =>
                        setter.Attribute("Property")?.Value == "Margin" &&
                        setter.Attribute("Value")?.Value == "{x:Static SystemParameters.WindowResizeBorderThickness}"),
                Is.True);
        }

        private static string FindMainWindowXamlPath()
        {
            DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "MainWindow.xaml");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not locate src/MainWindow.xaml from test directory.");
        }
    }
}
