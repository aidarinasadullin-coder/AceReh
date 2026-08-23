// ================================================================================
// Phase 4 Todos 5+6+7 (AMZ-1) - AutomationId selector contract.
// ================================================================================
//
// Pins the UI automation surface for the agent-operated WPF QA harness:
// exactly the agreed AutomationIds on exactly the agreed element types across
// ThermalView, CircuitsView and ResultsView. Includes negative synthetic
// fixtures proving duplicate/missing IDs are rejected by the same validator.
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Thermal;

[TestFixture]
public sealed class ThermalAutomationIdSelectorContractTests
{
    private const string IdAttribute = "AutomationProperties.AutomationId";

    private static string RepoRoot => Path.Combine(
        Path.GetDirectoryName(typeof(ThermalAutomationIdSelectorContractTests).Assembly.Location)!,
        "..", "..", "..", "..", "..");

    private static string ViewPath(string relative) => Path.Combine(RepoRoot, relative);

    private static readonly (string File, string Id, string ControlType)[] Contract =
    {
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalMode", "ComboBox"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalSupplyTemperature", "TextBox"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalGroundTemperature", "TextBox"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalPipe", "ComboBox"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalPipeSpacing", "ComboBox"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalCalculate", "Button"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalReset", "Button"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalRecalcMessage", "TextBlock"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalDeltaT", "TextBlock"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalPowerTotal", "TextBlock"),
        (@"src\Views\Thermal\ThermalView.xaml", "ThermalResultStatus", "TextBlock"),
        (@"src\Views\Hydraulics\CircuitsView.xaml", "HydraulicsPipeSpacing", "TextBlock"),
        (@"src\Views\Hydraulics\CircuitsView.xaml", "HydraulicsSupplyTemperature", "TextBlock"),
        (@"src\Views\Hydraulics\CircuitsView.xaml", "HydraulicsReturnTemperature", "TextBlock"),
        (@"src\Views\Results\ResultsView.xaml", "ResultsThermalPower", "TextBlock"),
        (@"src\Views\Results\ResultsView.xaml", "ResultsSupplyTemperature", "TextBlock"),
        (@"src\Views\Results\ResultsView.xaml", "ResultsReturnTemperature", "TextBlock")
    };

    [TestCaseSource(nameof(ContractRows))]
    public void Contract_IdAppearsExactlyOnceWithRequiredControlType(string file, string id, string controlType)
    {
        var document = LoadView(file);
        var matches = FindById(document, id).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(matches, Has.Count.EqualTo(1),
                $"{id} must exist exactly once in {file}.");
            Assert.That(matches[0].Element.Name.LocalName, Is.EqualTo(controlType),
                $"{id} must be attached to a {controlType} element.");
        });
    }

    private static IEnumerable<TestCaseData> ContractRows()
    {
        foreach (var row in Contract)
        {
            yield return new TestCaseData(row.File, row.Id, row.ControlType)
                .SetArgDisplayNames($"{Path.GetFileName(row.File)}::{row.Id}");
        }
    }

    [TestCase(@"src\Views\Thermal\ThermalView.xaml")]
    [TestCase(@"src\Views\Hydraulics\CircuitsView.xaml")]
    [TestCase(@"src\Views\Results\ResultsView.xaml")]
    public void Contract_NoDuplicateAutomationIdsWithinAView(string file)
    {
        var document = LoadView(file);
        var ids = FindById(document, null)
            .Select(match => match.Id)
            .ToList();

        Assert.That(ids, Is.Unique,
            $"Every AutomationId inside {file} must be unique.");
    }

    [Test]
    public void NegativeFixture_DuplicateId_IsRejected()
    {
        const string xaml = """
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Button AutomationProperties.AutomationId="ThermalCalculate" />
                <Button AutomationProperties.AutomationId="ThermalCalculate" />
            </StackPanel>
            """;

        var document = XDocument.Parse(xaml);
        var matches = FindById(document, "ThermalCalculate").ToList();

        Assert.That(matches, Has.Count.EqualTo(2),
            "Synthetic duplicate fixture: validator must observe two occurrences.");

        Assert.Throws<AssertionException>(() =>
            Assert.That(matches, Has.Count.EqualTo(1)),
            "Duplicate/missing IDs must be rejected by the selector-contract validation.");
    }

    [Test]
    public void NegativeFixture_MissingId_IsRejected()
    {
        const string xaml = """
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Button />
            </StackPanel>
            """;

        var document = XDocument.Parse(xaml);
        var matches = FindById(document, "ThermalCalculate").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(matches, Is.Empty,
                "Synthetic missing fixture: validator observes zero occurrences.");

            Assert.Throws<AssertionException>(() =>
                Assert.That(matches, Has.Count.EqualTo(1)),
                "Missing required IDs must be rejected by the selector-contract validation.");
        });
    }

    #region Helpers

    private static XDocument LoadView(string relativePath)
    {
        var path = ViewPath(relativePath);
        Assert.That(File.Exists(path), Is.True, $"View file must exist: {relativePath}");
        return XDocument.Load(path);
    }

    private static IEnumerable<(XElement Element, string Id)> FindById(XDocument document, string? id)
    {
        return document
            .Descendants()
            .Select(element => (Element: element, Id: element.Attribute(IdAttribute)?.Value))
            .Where(pair => pair.Id is not null)
            .Select(pair => (pair.Element, Id: pair.Id!))
            .Where(pair => id is null || pair.Id == id);
    }

    #endregion
}
