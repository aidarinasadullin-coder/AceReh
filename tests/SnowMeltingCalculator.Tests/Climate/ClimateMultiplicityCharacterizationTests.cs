using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;

namespace SnowMeltingCalculator.Tests.Climate;

[TestFixture]
public class ClimateMultiplicityCharacterizationTests
{
    [Test]
    public void ClimateMultiplicity_SelectedCity_EmitsOneCompatibilityUpdatePerLegacyPropertyMutation()
    {
        var probe = CreateProbe();

        probe.ResetCounters();
        probe.ViewModel.SelectedCity = Moscow;

        AssertCounts(probe, markDirtyCalls: 1, climateDataChanged: 1, viewModelDataChanged: 1, contextChanged: 1);
        Assert.That(probe.ViewModel.SelectedCity?.Name, Is.EqualTo("Москва"));
        Assert.That(probe.ClimateData.SelectedCity, Is.EqualTo("Москва"));
        Assert.That(probe.ClimateData.AirTemperature, Is.EqualTo(-15));
    }

    [Test]
    public void ClimateMultiplicity_ScalarEdit_EmitsSingleCompatibilityUpdate()
    {
        var probe = CreateProbe();

        probe.ResetCounters();
        probe.ViewModel.AirTemperature = -20;

        AssertCounts(probe, markDirtyCalls: 1, climateDataChanged: 1, viewModelDataChanged: 1, contextChanged: 1);
        Assert.That(probe.ViewModel.AirTemperature, Is.EqualTo(-20));
        Assert.That(probe.ClimateData.AirTemperature, Is.EqualTo(-20));
    }

    [Test]
    public void ClimateMultiplicity_HighRequirementsToggle_EmitsNestedScalarAndToggleUpdates()
    {
        var probe = CreateProbe();
        probe.ViewModel.SelectedCity = Moscow;

        probe.ResetCounters();
        probe.ViewModel.IsHighRequirements = true;

        AssertCounts(probe, markDirtyCalls: 1, climateDataChanged: 1, viewModelDataChanged: 1, contextChanged: 1);
        Assert.That(probe.ViewModel.SelectedZone, Is.EqualTo(ClimateZone.Zone_M20_Plus));
        Assert.That(probe.ClimateData.AirTemperature, Is.EqualTo(-20));
    }

    [Test]
    public void ClimateMultiplicity_ChangedUserReset_EmitsOneCompletionAndCompatibilityUpdateAndMarksDirty()
    {
        var probe = CreateProbe();
        probe.ViewModel.SelectedCity = Moscow;
        probe.ViewModel.AirTemperature = -20;

        probe.ResetCounters();
        probe.ViewModel.Reset();

        AssertCounts(probe, markDirtyCalls: 1, climateDataChanged: 1, viewModelDataChanged: 1, contextChanged: 1, completions: 1);
        Assert.That(probe.ViewModel.SelectedCity, Is.Null);
        Assert.That(probe.ClimateData.SelectedCity, Is.Empty);
        Assert.That(probe.ClimateData.AirTemperature, Is.EqualTo(-15));
    }

    [Test]
    public void ClimateMultiplicity_ChangedUserResetToCityData_EmitsOneCompletionAndCompatibilityUpdateAndMarksDirty()
    {
        var probe = CreateProbe();
        probe.ViewModel.SelectedCity = Moscow;
        probe.ViewModel.AirTemperature = -20;
        probe.ViewModel.WindSpeed = 10;
        probe.ViewModel.Humidity = 90;

        probe.ResetCounters();
        probe.ViewModel.ResetToCityDataCommand.Execute(null);

        AssertCounts(probe, markDirtyCalls: 1, climateDataChanged: 1, viewModelDataChanged: 1, contextChanged: 1, completions: 1);
        Assert.That(probe.ViewModel.AirTemperature, Is.EqualTo(-15));
        Assert.That(probe.ViewModel.WindSpeed, Is.EqualTo(4.5));
        Assert.That(probe.ViewModel.Humidity, Is.EqualTo(85));
        Assert.That(probe.ClimateData.SelectedCity, Is.EqualTo("Москва"));
    }

    [Test]
    public void ClimateMultiplicity_SameValueScalarEdit_IsACompatibilityNoOp()
    {
        var probe = CreateProbe();

        probe.ResetCounters();
        probe.ViewModel.AirTemperature = -15;

        AssertCounts(probe, markDirtyCalls: 0, climateDataChanged: 0, viewModelDataChanged: 0, contextChanged: 0, completions: 0);
        Assert.That(probe.ViewModel.AirTemperature, Is.EqualTo(-15));
    }

    [Test]
    public void ClimateMultiplicity_SameCitySelection_IsACompatibilityNoOp()
    {
        var probe = CreateProbe();
        probe.ViewModel.SelectedCity = Moscow;

        probe.ResetCounters();
        probe.ViewModel.SelectedCity = Moscow;

        AssertCounts(probe, markDirtyCalls: 0, climateDataChanged: 0, viewModelDataChanged: 0, contextChanged: 0);
        Assert.That(probe.ClimateData.SelectedCity, Is.EqualTo("Москва"));
    }

    [Test]
    public async Task ClimateMultiplicity_LoadAndSecondLoad_OnlyRefreshTheCityList()
    {
        var probe = CreateProbe();

        probe.ResetCounters();
        await probe.ViewModel.LoadDataAsync();
        AssertCounts(probe, markDirtyCalls: 0, climateDataChanged: 0, viewModelDataChanged: 0, contextChanged: 0);
        Assert.That(probe.Service.LoadCalls, Is.EqualTo(1));
        Assert.That(probe.ViewModel.FilteredCities.Select(city => city.Name), Is.EquivalentTo(new[] { "Москва", "Сочи" }));

        probe.ResetCounters();
        await probe.ViewModel.LoadDataAsync();
        AssertCounts(probe, markDirtyCalls: 0, climateDataChanged: 0, viewModelDataChanged: 0, contextChanged: 0);
        Assert.That(probe.Service.LoadCalls, Is.EqualTo(2));
        Assert.That(probe.ViewModel.FilteredCities.Count, Is.EqualTo(2));
    }

    [Test]
    public void ClimateMultiplicity_RepeatedReset_IsCanonicalNoOp()
    {
        var probe = CreateProbe();

        probe.ResetCounters();
        probe.ViewModel.Reset();
        AssertCounts(probe, markDirtyCalls: 0, climateDataChanged: 0, viewModelDataChanged: 0, contextChanged: 0);

        probe.ResetCounters();
        probe.ViewModel.Reset();
        AssertCounts(probe, markDirtyCalls: 0, climateDataChanged: 0, viewModelDataChanged: 0, contextChanged: 0);
        Assert.That(probe.ViewModel.SelectedCity, Is.Null);
        Assert.That(probe.ClimateData.SelectedCity, Is.Empty);
    }

    private static readonly CityInfo Moscow = new()
    {
        Name = "Москва",
        Region = "Московская область",
        T5Days092 = -28,
        WindAvgTempLe8 = 4.5,
        Humidity15hCold = 85
    };

    private static ClimateProbe CreateProbe()
    {
        var service = new CountingClimateDataService();
        var climateData = new ClimateData();
        var dirty = new Mock<IMarkDirtyService>();
        var context = new CalculationContext();
        var viewModel = new ClimateViewModel(service, climateData, new ClimateValidator(), dirty.Object, context);
        return new ClimateProbe(viewModel, viewModel.ClimateState, climateData, context, dirty, service);
    }

    private static void AssertCounts(ClimateProbe probe, int markDirtyCalls, int climateDataChanged, int viewModelDataChanged, int contextChanged, int? completions = null)
    {
        Assert.That(probe.MarkDirtyCalls, Is.EqualTo(markDirtyCalls), nameof(probe.MarkDirtyCalls));
        Assert.That(probe.ClimateDataChanged, Is.EqualTo(climateDataChanged), nameof(probe.ClimateDataChanged));
        Assert.That(probe.ViewModelDataChanged, Is.EqualTo(viewModelDataChanged), nameof(probe.ViewModelDataChanged));
        Assert.That(probe.ContextChanged, Is.EqualTo(contextChanged), nameof(probe.ContextChanged));
        if (completions.HasValue)
        {
            Assert.That(probe.Completions, Is.EqualTo(completions.Value), nameof(probe.Completions));
        }
    }

    private sealed class ClimateProbe
    {
        private int _markDirtyCalls;
        private int _climateDataChanged;
        private int _viewModelDataChanged;
        private int _contextChanged;
        private int _completions;

        public ClimateProbe(ClimateViewModel viewModel, IProjectSessionClimateState climateState, ClimateData climateData, CalculationContext context, Mock<IMarkDirtyService> dirty, CountingClimateDataService service)
        {
            ViewModel = viewModel;
            ClimateData = climateData;
            Service = service;
            dirty.Setup(item => item.MarkDirty()).Callback(() => _markDirtyCalls++);
            climateData.DataChanged += (_, _) => _climateDataChanged++;
            viewModel.DataChanged += (_, _) => _viewModelDataChanged++;
            context.ContextChanged += (_, _) => _contextChanged++;
            climateState.Changed += (_, _) => _completions++;
        }

        public ClimateViewModel ViewModel { get; }
        public ClimateData ClimateData { get; }
        public CountingClimateDataService Service { get; }
        public int MarkDirtyCalls => _markDirtyCalls;
        public int ClimateDataChanged => _climateDataChanged;
        public int ViewModelDataChanged => _viewModelDataChanged;
        public int ContextChanged => _contextChanged;
        public int Completions => _completions;

        public void ResetCounters()
        {
            _markDirtyCalls = 0;
            _climateDataChanged = 0;
            _viewModelDataChanged = 0;
            _contextChanged = 0;
            _completions = 0;
        }
    }

    private sealed class CountingClimateDataService : IClimateDataService
    {
        public bool IsLoaded => true;
        public int CitiesCount => 2;
        public int LoadCalls { get; private set; }

        public Task LoadClimateDataAsync()
        {
            LoadCalls++;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<CityInfo>>(Array.Empty<CityInfo>());
        public Task<IEnumerable<CityInfo>> SearchCitiesWithPriorityAsync(string query, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<CityInfo>>(Array.Empty<CityInfo>());
        public Task<IEnumerable<CityInfo>> GetRecentCitiesAsync(int limit = 10, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<CityInfo>>(Array.Empty<CityInfo>());
        public Task SaveToHistoryAsync(CityInfo city, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public CityInfo? GetCityByName(string name) => null;
        public IEnumerable<CityInfo> GetAllCities() => new[] { Moscow, new CityInfo { Name = "Сочи", Region = "Краснодарский край", T5Days092 = -5 } };
        public ClimateZone DetermineZone(double t5days, bool isHighRequirements = false) => isHighRequirements ? ClimateZone.Zone_M20_Plus : t5days >= -27 ? ClimateZone.Zone_M10 : t5days > -37 ? ClimateZone.Zone_M15 : ClimateZone.Zone_M20;
        public (string highlightedName, string highlightedRegion, MatchType matchType) HighlightMatch(CityInfo city, string query) => (city.Name, city.Region, MatchType.Contains);
    }
}
