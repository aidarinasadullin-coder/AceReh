using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Shell;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    [TestFixture]
    public sealed class ResultsStabilizationPhase1ContractsTests
    {
        private static string SourcePath(string relativePath)
        {
            var directory = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "SnowMeltingCalculator.sln")))
            {
                directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
            }

            return Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public async Task ResolveView_CachedResultsEntryRefreshesProjectionBeforeReturningSameView()
        {
            var projectStateService = new ProjectStateService();
            var results = ResultsViewModelTestHelpers.CreateResultsViewModel(
                projectStateService,
                ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors());
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(results);
            var window = CreateUninitializedMainWindow(results, projectStateService);
            var cachedView = new ContentControl { DataContext = results };
            SetCachedView(window, NavigationTarget.Results, cachedView);
            var refreshCount = 0;
            SetField(window, "_refreshResultsOnNavigate", (Action)(() =>
            {
                refreshCount++;
                results.LoadHydraulicsDataOnNavigate();
            }));

            var firstView = InvokeResolveView(window, NavigationTarget.Results);
            Assert.That(refreshCount, Is.EqualTo(1));
            var firstCity = results.SelectedCity;
            results.SelectedCity = "Изменённый город";
            var secondView = InvokeResolveView(window, NavigationTarget.Results);

            Assert.Multiple(() =>
            {
                Assert.That(firstView, Is.SameAs(cachedView));
                Assert.That(secondView, Is.SameAs(firstView), "Cached Results navigation must reuse the View instance.");
                Assert.That(cachedView.DataContext, Is.SameAs(results),
                    "Cached Results navigation must preserve the ResultsViewModel DataContext.");
                Assert.That(refreshCount, Is.EqualTo(2), "Each Results entry must invoke exactly one refresh.");
                Assert.That(results.SelectedCity, Is.EqualTo(firstCity),
                    "Each Results cache hit must refresh the projection before returning it.");
            });
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void ResolveView_ResultsRefreshFailureUsesExistingFallbackWithoutCachingResultsView()
        {
            var projectStateService = new ProjectStateService();
            var results = ResultsViewModelTestHelpers.CreateResultsViewModel(
                projectStateService,
                ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors());
            var window = CreateUninitializedMainWindow(results, projectStateService, out var dialog);
            var fallbackView = new object();
            SetCachedView(window, NavigationTarget.Climate, fallbackView);
            SetField(window, "_refreshResultsOnNavigate", (Action)(() => throw new InvalidOperationException("refresh failed")));

            var resolvedView = InvokeResolveView(window, NavigationTarget.Results);

            Assert.Multiple(() =>
            {
                Assert.That(resolvedView, Is.SameAs(fallbackView));
                Assert.That(GetCachedViews(window), Does.Not.ContainKey(NavigationTarget.Results));
            });
            dialog.Verify(service => service.ShowError(
                Moq.It.Is<string>(message => message.Contains("refresh failed", StringComparison.Ordinal)),
                "Ошибка навигации"), Moq.Times.Once);
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void ResolveView_NonResultsNavigationReturnsCachedNonResultsView()
        {
            var projectStateService = new ProjectStateService();
            var results = ResultsViewModelTestHelpers.CreateResultsViewModel(
                projectStateService,
                ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors());
            var window = CreateUninitializedMainWindow(results, projectStateService);
            var cachedView = new object();
            SetCachedView(window, NavigationTarget.Climate, cachedView);

            var firstView = InvokeResolveView(window, NavigationTarget.Climate);
            var secondView = InvokeResolveView(window, NavigationTarget.Climate);

            Assert.That(firstView, Is.SameAs(cachedView));
            Assert.That(secondView, Is.SameAs(firstView),
                "Non-Results module views must preserve their existing cache behavior.");
        }

        [Test]
        public void ResolveView_ResultsRefreshRemainsInsideExistingFailureBoundaryAndHasOneCallSite()
        {
            var source = File.ReadAllText(SourcePath("src/MainWindow.xaml.cs"));
            var methodBody = ExtractMethodBody(source, "ResolveView");
            var refreshIndex = methodBody.IndexOf("LoadHydraulicsDataOnNavigate();", StringComparison.Ordinal);
            var cacheIndex = methodBody.IndexOf("_moduleViewCache.TryGetValue(target", StringComparison.Ordinal);
            var tryIndex = methodBody.IndexOf("try", StringComparison.Ordinal);

            Assert.That(refreshIndex, Is.GreaterThan(tryIndex),
                "Results refresh must remain inside the existing navigation failure boundary.");
            Assert.That(refreshIndex, Is.GreaterThan(cacheIndex),
                "The fix must preserve the existing cache lookup and refresh on Results entry, including cache hits.");
            Assert.That(CountOccurrences(methodBody, "LoadHydraulicsDataOnNavigate();"), Is.EqualTo(1),
                "One Results entry must invoke exactly one refresh.");
            Assert.That(methodBody, Does.Contain("catch (Exception ex)"));
            Assert.That(methodBody, Does.Contain("_dialogService.ShowError("));
            Assert.That(methodBody, Does.Contain("NavigationTarget.Climate"));
        }

        [Test]
        public void LoadHydraulicsDataOnNavigate_DelegatesToExactlyOneCanonicalRefreshAll()
        {
            var source = File.ReadAllText(SourcePath("src/ViewModels/Results/ResultsViewModel.cs"));
            var methodBody = ExtractMethodBody(source, "LoadHydraulicsDataOnNavigate");

            Assert.That(methodBody, Does.Contain("RefreshAll();"),
                "Navigation refresh must delegate to the one canonical RefreshAll boundary.");
            Assert.Multiple(() =>
            {
                Assert.That(CountOccurrences(methodBody, "RefreshAll();"), Is.EqualTo(1));
                Assert.That(methodBody, Does.Not.Contain("LoadClimateData();"));
                Assert.That(methodBody, Does.Not.Contain("LoadConstructionData();"));
                Assert.That(methodBody, Does.Not.Contain("LoadThermalData();"));
                Assert.That(methodBody, Does.Not.Contain("LoadHydraulicsData();"));
            });
        }

        [Test]
        public void RefreshAll_UsesCanonicalSynchronizationOrderExactlyOnce()
        {
            var source = File.ReadAllText(SourcePath("src/ViewModels/Results/ResultsViewModel.cs"));
            var methodBody = ExtractMethodBody(source, "public void RefreshAll");
            var canonicalCalls = new[]
            {
                "LoadClimateData();",
                "LoadConstructionData();",
                "LoadThermalData();",
                "LoadHydraulicsData();",
                "CheckDataReadiness();",
                "RecalculateKpi();",
                "RebuildHydraulicSummaryCards();",
                "UpdateCollectorEquipmentItems();"
            };

            var previousIndex = -1;
            foreach (var call in canonicalCalls)
            {
                var index = methodBody.IndexOf(call, StringComparison.Ordinal);
                Assert.That(index, Is.GreaterThan(previousIndex), $"{call} must follow the canonical refresh order.");
                Assert.That(CountOccurrences(methodBody, call), Is.EqualTo(1), $"{call} must run exactly once.");
                previousIndex = index;
            }
        }

        [Test]
        public void LoadProjectDataAsync_RefreshesAfterModuleRestoreBeforeProjectChangedAndMarkClean()
        {
            var source = File.ReadAllText(SourcePath("src/ViewModels/Results/ResultsViewModel.cs"));
            var methodBody = ExtractMethodBody(source, "public async Task LoadProjectDataAsync");
            var restoreIndex = methodBody.IndexOf("await _projectLoadOrchestrator.RestoreModulesFromProjectAsync(data);", StringComparison.Ordinal);
            var refreshIndex = methodBody.IndexOf("RefreshAll();", StringComparison.Ordinal);
            var projectChangedIndex = methodBody.IndexOf("ProjectChanged?.Invoke(this, data);", StringComparison.Ordinal);
            var markCleanIndex = methodBody.IndexOf("_projectStateService.MarkClean();", StringComparison.Ordinal);

            Assert.Multiple(() =>
            {
                Assert.That(refreshIndex, Is.GreaterThan(restoreIndex));
                Assert.That(projectChangedIndex, Is.GreaterThan(refreshIndex));
                Assert.That(markCleanIndex, Is.GreaterThan(projectChangedIndex));
                Assert.That(CountOccurrences(methodBody, "RefreshAll();"), Is.EqualTo(1));
            });
        }

        [TestCase("ExportPdf")]
        [TestCase("PreviewPdf")]
        [TestCase("PrintPdf")]
        [TestCase("ExportOperatingMarkdownReport")]
        [TestCase("ExportDesignColdMarkdownReport")]
        [TestCase("ExportExcel")]
        public void ResultsCommands_RefreshBeforeConsumingOrExportingData(string commandName)
        {
            var source = File.ReadAllText(SourcePath("src/ViewModels/Results/ResultsViewModel.cs"));
            var methodBody = ExtractMethodBody(source, commandName);
            var refreshIndex = methodBody.IndexOf("RefreshAll();", StringComparison.Ordinal);

            Assert.That(methodBody, Does.Contain("RefreshAll();"),
                $"{commandName} must refresh current module projections before readiness/build/save/export.");
            Assert.That(CountOccurrences(methodBody, "RefreshAll();"), Is.EqualTo(1),
                $"{commandName} must invoke the canonical refresh exactly once.");

            var firstConsumeIndex = commandName switch
            {
                "ExportPdf" => methodBody.IndexOf("if (!IsDataReady)", StringComparison.Ordinal),
                "PreviewPdf" => methodBody.IndexOf("if (!IsDataReady)", StringComparison.Ordinal),
                "PrintPdf" => methodBody.IndexOf("if (!IsDataReady)", StringComparison.Ordinal),
                "ExportOperatingMarkdownReport" => methodBody.IndexOf("ExportMarkdownReportAsync(", StringComparison.Ordinal),
                "ExportDesignColdMarkdownReport" => methodBody.IndexOf("ExportMarkdownReportAsync(", StringComparison.Ordinal),
                "ExportExcel" => methodBody.IndexOf("StatusMessage =", StringComparison.Ordinal),
                _ => -1
            };

            Assert.That(firstConsumeIndex, Is.GreaterThan(refreshIndex),
                $"{commandName} must refresh before its first readiness or data-consumption path.");
        }

        private static string ExtractMethodBody(string source, string methodName)
        {
            var marker = $" {methodName}(";
            var methodStart = source.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(methodStart, Is.GreaterThanOrEqualTo(0), $"Could not locate {methodName} in the source.");

            var bodyStart = source.IndexOf('{', methodStart);
            Assert.That(bodyStart, Is.GreaterThanOrEqualTo(0), $"Could not locate {methodName} body.");

            var depth = 0;
            for (var index = bodyStart; index < source.Length; index++)
            {
                if (source[index] == '{') depth++;
                if (source[index] == '}' && --depth == 0)
                    return source.Substring(bodyStart, index - bodyStart + 1);
            }

            Assert.Fail($"Could not close {methodName} body.");
            return string.Empty;
        }

        private static MainWindow CreateUninitializedMainWindow(
            ResultsViewModel results,
            ProjectStateService projectStateService)
        {
            return CreateUninitializedMainWindow(results, projectStateService, out _);
        }

        private static MainWindow CreateUninitializedMainWindow(
            ResultsViewModel results,
            ProjectStateService projectStateService,
            out Moq.Mock<SnowMeltingCalculator.Services.Navigation.IDialogService> dialog)
        {
#pragma warning disable SYSLIB0050
            var window = (MainWindow)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(MainWindow));
#pragma warning restore SYSLIB0050
            var climate = GetField<ClimateViewModel>(results, "_climateViewModel");
            var construction = GetField<SnowMeltingCalculator.ViewModels.Construction.ConstructionViewModel>(results, "_constructionViewModel");
            var thermal = GetField<ThermalViewModel>(results, "_thermalViewModel");
            var circuits = GetField<CircuitsViewModel>(results, "_circuitsViewModel");
            var calculationState = GetField<SnowMeltingCalculator.Services.Navigation.ICalculationStateService>(results, "_calculationStateService");
            dialog = new Moq.Mock<SnowMeltingCalculator.Services.Navigation.IDialogService>();
            var mainViewModel = new MainViewModel(
                climate,
                thermal,
                construction,
                circuits,
                results,
                calculationState,
                projectStateService,
                dialog.Object,
                new SnowMeltingCalculator.Core.CalculationContext());
            SetField(window, "_viewModel", mainViewModel);
            SetField(window, "_projectStateService", projectStateService);
            SetField(window, "_dialogService", dialog.Object);
            SetField(window, "_moduleViewCache", new System.Collections.Generic.Dictionary<NavigationTarget, object>());
            return window;
        }

        private static object InvokeResolveView(MainWindow window, NavigationTarget target)
        {
            var method = typeof(MainWindow).GetMethod("ResolveView", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return method.Invoke(window, new object[] { target })!;
        }

        private static T GetField<T>(object instance, string fieldName)
        {
            return (T)instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
        }

        private static void SetCachedView(MainWindow window, NavigationTarget target, object view)
        {
            GetCachedViews(window)[target] = view;
        }

        private static System.Collections.Generic.Dictionary<NavigationTarget, object> GetCachedViews(MainWindow window) =>
            GetField<System.Collections.Generic.Dictionary<NavigationTarget, object>>(window, "_moduleViewCache");

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
