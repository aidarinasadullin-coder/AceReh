using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Climate
{
    [TestFixture]
    public sealed class ClimateStateLegacyStoreGuardTests
    {
        private static readonly string[] ClimateDataProperties =
        {
            "SelectedCity",
            "SelectedRegion",
            "AirTemperature",
            "ColdFiveDayTemperature",
            "WindSpeed",
            "Humidity",
            "SnowfallIntensity",
            "Zone"
        };

        private static readonly string[] ClimateViewModelMutationBoundaries =
        {
            "public void Reset()",
            "private void ResetToCityData()",
            "public void SetClimateParameters(ClimateParameters parameters)",
            "partial void OnSelectedCityChanged(CityInfo? value)",
            "partial void OnIsHighRequirementsChanged(bool value)",
            "partial void OnAirTemperatureChanged(double value)",
            "partial void OnWindSpeedChanged(double value)",
            "partial void OnHumidityChanged(double value)",
            "partial void OnSnowfallIntensityChanged(double value)"
        };

        [Test]
        public void ClimateStateLegacyStoreGuard_CapturesExactCurrentWriterAndProjectionInventory()
        {
            var serviceRegistrationSource = ReadSource("src/Configuration/ServiceCollectionExtensions.cs");
            var projectSessionSource = ReadSource("src/Services/Project/ProjectSession.cs");
            var climateViewModelSource = ReadSource("src/ViewModels/Climate/ClimateViewModel.cs");
            var climateDataSource = ReadSource("src/Models/Climate/ClimateData.cs");
            var calculationContextSource = ReadSource("src/Core/CalculationContext.cs");
            var orchestratorSource = ReadSource("src/Services/Project/ProjectLoadOrchestrator.cs");
            var resultsSource = ReadSource("src/ViewModels/Results/ResultsViewModel.cs");
            var saveCurrentProjectSource = ExtractSaveCurrentProjectMethod(resultsSource);

            Assert.Multiple(() =>
            {
                Assert.That(serviceRegistrationSource, Does.Not.Contain("AddSingleton<IProjectSessionClimateState"));
                Assert.That(serviceRegistrationSource, Does.Not.Contain("AddTransient<IProjectSessionClimateState"));
                Assert.That(serviceRegistrationSource, Does.Not.Contain("AddScoped<IProjectSessionClimateState"));
                Assert.That(serviceRegistrationSource, Does.Not.Contain("AddSingleton<ProjectSessionClimateState"));
                Assert.That(serviceRegistrationSource, Does.Not.Contain("AddTransient<ProjectSessionClimateState"));
                Assert.That(serviceRegistrationSource, Does.Not.Contain("AddScoped<ProjectSessionClimateState"));
                Assert.That(projectSessionSource, Does.Contain("private readonly ProjectSessionClimateState _climateState;"));
                Assert.That(climateViewModelSource, Does.Not.Contain("private ProjectSessionClimateState"));
                Assert.That(climateViewModelSource, Does.Not.Contain("private readonly ProjectSessionClimateState"));
                Assert.That(orchestratorSource, Does.Not.Contain("private ProjectSessionClimateState"));
                Assert.That(orchestratorSource, Does.Not.Contain("private readonly ProjectSessionClimateState"));
                Assert.That(resultsSource, Does.Not.Contain("private ProjectSessionClimateState"));
                Assert.That(resultsSource, Does.Not.Contain("private readonly ProjectSessionClimateState"));
                Assert.That(climateViewModelSource, Does.Contain("private readonly IProjectSessionClimateState _climateState;"));
                Assert.That(climateViewModelSource, Does.Contain("_climateState.Changed += OnClimateStateChanged;"));
                Assert.That(climateViewModelSource, Does.Not.Contain("_markDirtyService"));
                Assert.That(climateViewModelSource, Does.Not.Contain("_calculationContext"));
                Assert.That(climateViewModelSource, Does.Not.Contain(".MarkDirty()"));
                Assert.That(climateViewModelSource, Does.Not.Contain(".UpdateClimate("));
                Assert.That(ClimateViewModelMutationBoundaries.Where(boundary => !climateViewModelSource.Contains(boundary, StringComparison.Ordinal)),
                    Is.Empty, "A legacy ClimateViewModel mutation boundary disappeared from the inventory.");
                Assert.That(GetClimateDataPublicWritableProperties(climateDataSource), Is.Empty,
                    "ClimateData must not expose public writable setters after projection migration; setters are internal and reachable only through the approved projection updater.");
                Assert.That(climateDataSource, Does.Contain("internal void ApplyProjection(IClimateData source, bool isValid = true, bool publishDataChanged = true)"),
                    "ClimateData must expose the approved projection updater as the single internal mutation seam.");
                Assert.That(climateViewModelSource, Does.Not.Contain("SyncToClimateData"),
                    "ClimateViewModel must not retain a legacy projection publication path outside canonical ClimateState completion.");
                Assert.That(calculationContextSource, Does.Contain("public void UpdateClimate(IClimateData climate, string source = \"Climate\")"));
                Assert.That(calculationContextSource, Does.Contain("Climate = climate;"));
                Assert.That(calculationContextSource, Does.Contain("OnContextChanged(nameof(Climate), oldValue, climate, source);"));
                Assert.That(GetDirectClimateViewModelWrites(orchestratorSource), Is.EqualTo(new[] { "SearchQuery", "SearchQuery" }),
                    "Task 7 permits only the UI search text; project climate values must not be assigned through ClimateViewModel.");
                Assert.That(orchestratorSource, Does.Contain("public void ResetModules()"));
                Assert.That(orchestratorSource, Does.Contain("_climateState.ResetToDefaults(ClimateMutationOrigin.ProjectLoadReset);"));
                Assert.That(orchestratorSource, Does.Contain("_climateState.ApplyProjectSnapshot(data.ClimateData, city, ClimateMutationOrigin.Load);"));
                Assert.That(orchestratorSource, Does.Not.Contain("_climateViewModel.BeginLoadProject();"),
                    "Task 7 removed the ClimateViewModel load guard bypass.");
                Assert.That(orchestratorSource, Does.Not.Contain("_climateViewModel.EndLoadProject();"),
                    "Task 7 removed the ClimateViewModel load guard bypass.");
                Assert.That(orchestratorSource, Does.Not.Contain("_climateViewModel.Reset();"),
                    "Task 7 reset must use ClimateState, not ClimateViewModel.Reset().");
                Assert.That(orchestratorSource, Does.Not.Contain("_climateViewModel.SyncToClimateData();"),
                    "Task 7 canonical completion owns projection publication.");
                Assert.That(orchestratorSource, Does.Not.Contain("_climateViewModel.HasUserModifications = false;"),
                    "Task 7 canonical non-user origin owns modification state.");
                Assert.That(GetDirectClimateViewModelWrites(resultsSource), Is.Empty,
                    "ResultsViewModel is a Climate projection/read site and must not gain direct ClimateViewModel setters.");
                Assert.That(saveCurrentProjectSource, Does.Contain("public ProjectData SaveCurrentProject()"));
                Assert.That(saveCurrentProjectSource, Does.Contain("data.ClimateData = new ClimateProjectData"));
                Assert.That(saveCurrentProjectSource, Does.Contain("var climateSnapshot = _projectSession.ClimateState.Snapshot;"));
                Assert.That(saveCurrentProjectSource, Does.Contain("SelectedCity = climateSnapshot.SelectedCity,"));
                Assert.That(saveCurrentProjectSource, Does.Contain("Region = climateSnapshot.SelectedRegion,"));
                Assert.That(saveCurrentProjectSource, Does.Contain("AirTemperature = climateSnapshot.AirTemperature,"));
                Assert.That(saveCurrentProjectSource, Does.Contain("WindSpeed = climateSnapshot.WindSpeed,"));
                Assert.That(saveCurrentProjectSource, Does.Contain("Humidity = climateSnapshot.Humidity,"));
                Assert.That(saveCurrentProjectSource, Does.Contain("SnowfallIntensity = climateSnapshot.SnowfallIntensity,"));
                Assert.That(saveCurrentProjectSource, Does.Contain("SelectedZone = climateSnapshot.Zone,"));
                Assert.That(saveCurrentProjectSource, Does.Contain("IsHighRequirements = climateSnapshot.IsHighRequirements"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("SelectedCity = _climateViewModel"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("Region = _climateViewModel"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("AirTemperature = _climateViewModel"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("WindSpeed = _climateViewModel"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("Humidity = _climateViewModel"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("SnowfallIntensity = _climateViewModel"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("SelectedZone = _climateViewModel"));
                Assert.That(saveCurrentProjectSource, Does.Not.Contain("IsHighRequirements = _climateViewModel"));
            });
        }

        [Test]
        public void ClimateStateLegacyStoreGuard_RejectsNewDirectClimateViewModelSetterInForbiddenCallers()
        {
            const string resultsFixture = "_climateViewModel.AirTemperature = -20.0;";
            const string orchestratorFixture = "_climateViewModel.Humidity = data.ClimateData.Humidity;";

            Assert.Multiple(() =>
            {
                Assert.That(GetDirectClimateViewModelWrites(resultsFixture), Is.EqualTo(new[] { "AirTemperature" }));
                Assert.That(GetDirectClimateViewModelWrites(orchestratorFixture), Is.EqualTo(new[] { "Humidity" }));
                Assert.That(GetDirectClimateViewModelWrites(resultsFixture), Is.Not.Empty,
                    "A ResultsViewModel direct Climate setter must fail the projection-only guard.");
            });
        }

        [Test]
        public void ClimateStateLegacyStoreGuard_RejectsDirectConcreteClimateDataSetterOutsideUpdater()
        {
            const string forbiddenProjection = "data.AirTemperature = -20.0;";
            const string forbiddenCast = "((ClimateData)_climateData).AirTemperature = -20.0;";

            Assert.Multiple(() =>
            {
                Assert.That(GetDirectClimateDataWrites(forbiddenProjection), Is.EqualTo(new[] { "AirTemperature" }),
                    "A direct concrete ClimateData property assignment must be detected by the projection guard.");
                Assert.That(GetDirectClimateDataWrites(forbiddenCast), Is.EqualTo(new[] { "AirTemperature" }),
                    "A cast-to-concrete ClimateData property assignment outside the approved updater must be detected.");
            });
        }

        private static string ExtractSaveCurrentProjectMethod(string resultsSource)
        {
            var start = resultsSource.IndexOf("public ProjectData SaveCurrentProject()", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(-1), "SaveCurrentProject method not found in ResultsViewModel.");

            var bodyStart = resultsSource.IndexOf('{', start);
            Assert.That(bodyStart, Is.GreaterThan(-1), "SaveCurrentProject method body start not found.");

            var depth = 0;
            for (var i = bodyStart; i < resultsSource.Length; i++)
            {
                if (resultsSource[i] == '{')
                {
                    depth++;
                }
                else if (resultsSource[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return resultsSource[start..(i + 1)];
                    }
                }
            }

            Assert.Fail("Could not extract SaveCurrentProject method body: unbalanced braces.");
            return string.Empty;
        }

        private static string ReadSource(string relativePath)
        {
            var directory = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "SnowMeltingCalculator.sln")))
            {
                directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
            }

            Assert.That(directory, Is.Not.Empty, "Could not locate the repository root from the test output directory.");
            return File.ReadAllText(Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string[] GetClimateDataPublicWritableProperties(string source)
        {
            return Regex.Matches(source, @"public\s+[A-Za-z_][A-Za-z0-9_<>?]*\s+(?<property>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;\s*set;\s*\}")
                .Select(match => match.Groups["property"].Value)
                .ToArray();
        }

        private static string[] GetDirectClimateDataWrites(string source)
        {
            var properties = string.Join("|", ClimateDataProperties);
            var pattern = $@"(?:\)|[A-Za-z0-9_])\s*\.\s*(?<property>{properties})\s*=";
            return Regex.Matches(source, pattern)
                .Select(match => match.Groups["property"].Value)
                .ToArray();
        }

        private static string[] GetDirectClimateViewModelWrites(string source)
        {
            return Regex.Matches(source, @"\b_climateViewModel\.(?<property>[A-Za-z_][A-Za-z0-9_]*)\s*=(?!=)")
                .Select(match => match.Groups["property"].Value)
                .ToArray();
        }
    }
}
