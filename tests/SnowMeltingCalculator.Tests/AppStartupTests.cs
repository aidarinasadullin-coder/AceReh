using NUnit.Framework;

namespace SnowMeltingCalculator.Tests;

[TestFixture]
public class AppStartupTests
{
    [TestCase(new[] { @"C:\Projects\Snow Melt\winter design.smc" }, @"C:\Projects\Snow Melt\winter design.smc")]
    [TestCase(new[] { "--safe-mode", @"C:\Projects\WINTER.SMC" }, @"C:\Projects\WINTER.SMC")]
    [TestCase(new[] { "notes.txt", @"C:\Projects\first.smc", @"C:\Projects\second.smc" }, @"C:\Projects\first.smc")]
    [TestCase(new[] { "notes.txt", "", "   " }, null)]
    public void SelectStartupProjectPath_WhenArgumentsAreProvided_ReturnsFirstSmcPath(
        string[] arguments,
        string? expectedPath)
    {
        var selectedPath = App.SelectStartupProjectPath(arguments);

        Assert.That(selectedPath, Is.EqualTo(expectedPath));
    }
}
