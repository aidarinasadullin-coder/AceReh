using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ClimateStateTests
    {
        private ProjectSession _session = null!;
        private IProjectSessionClimateState _climate = null!;

        [SetUp]
        public void Setup()
        {
            _session = new ProjectSession();
            _climate = _session.ClimateState;
        }

        [Test]
        public void ProjectSession_OwnsSingleRetainedClimateStateInstance()
        {
            var first = _session.ClimateState;
            var second = _session.ClimateState;

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
            Assert.That(_climate, Is.SameAs(first));
        }

        [Test]
        public void InitialSnapshot_HasExpectedDefaults()
        {
            var snapshot = _climate.Snapshot;

            Assert.That(snapshot.SelectedCity, Is.EqualTo(string.Empty));
            Assert.That(snapshot.SelectedRegion, Is.EqualTo(string.Empty));
            Assert.That(snapshot.AirTemperature, Is.EqualTo(-15.0));
            Assert.That(snapshot.ColdFiveDayTemperature, Is.EqualTo(0.0));
            Assert.That(snapshot.WindSpeed, Is.EqualTo(5.0));
            Assert.That(snapshot.Humidity, Is.EqualTo(70.0));
            Assert.That(snapshot.SnowfallIntensity, Is.EqualTo(0.0));
            Assert.That(snapshot.Zone, Is.EqualTo(ClimateZone.Zone_M15));
            Assert.That(snapshot.IsHighRequirements, Is.False);
            Assert.That(snapshot.IsCitySelected, Is.False);
            Assert.That(snapshot.HasUserModifications, Is.False);
        }

        [Test]
        public void ApplyCitySelection_User_ChangesFieldsMarksDirtyAndRaisesChanged()
        {
            var city = CreateCity();
            var events = CaptureChangedEvents();

            var result = _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ClimateMutationOrigin.User));
            Assert.That(result.NewSnapshot.SelectedCity, Is.EqualTo(city.Name));
            Assert.That(result.NewSnapshot.AirTemperature, Is.EqualTo(-10.0));
            Assert.That(result.NewSnapshot.ColdFiveDayTemperature, Is.EqualTo(city.T5Days092));
            Assert.That(result.NewSnapshot.HasUserModifications, Is.True);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Origin, Is.EqualTo(ClimateMutationOrigin.User));
            Assert.That(_session.IsDirty, Is.True);
        }

        [Test]
        public void ApplyCitySelection_Load_ChangesFieldsDoesNotMarkDirty()
        {
            var city = CreateCity();

            var result = _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.Load);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.HasUserModifications, Is.False);
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ApplyCitySelection_SameCity_IsNoOp_NoEvent_NoDirty()
        {
            var city = CreateCity();
            _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);
            _session.MarkClean();
            var events = CaptureChangedEvents();

            var result = _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);

            Assert.That(result.IsChanged, Is.False);
            Assert.That(events, Is.Empty);
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ApplyCitySelection_NullCity_ClearsCityFields()
        {
            var city = CreateCity();
            _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);
            var events = CaptureChangedEvents();

            var result = _climate.ApplyCitySelection(null, false, ClimateMutationOrigin.ProjectLoadReset);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.IsCitySelected, Is.False);
            Assert.That(result.NewSnapshot.SelectedCity, Is.EqualTo(string.Empty));
            Assert.That(events, Has.Count.EqualTo(1));
        }

        [Test]
        public void ApplyIndividualEdit_User_ChangesValueMarksDirtyAndRaisesChanged()
        {
            var edit = new ClimateEdit(ClimateEditField.WindSpeed, 5.0);
            var events = CaptureChangedEvents();

            var result = _climate.ApplyIndividualEdit(edit, ClimateMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.WindSpeed, Is.EqualTo(5.0));
            Assert.That(result.NewSnapshot.HasUserModifications, Is.True);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(_session.IsDirty, Is.True);
        }

        [Test]
        public void ApplyIndividualEdit_SystemApply_ChangesValueDoesNotMarkDirty()
        {
            var edit = new ClimateEdit(ClimateEditField.Humidity, 50.0);

            var result = _climate.ApplyIndividualEdit(edit, ClimateMutationOrigin.SystemApply);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.Humidity, Is.EqualTo(50.0));
            Assert.That(result.NewSnapshot.HasUserModifications, Is.False);
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ApplyIndividualEdit_SameValue_IsNoOp_NoEvent_NoDirty()
        {
            _climate.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.AirTemperature, -20.0), ClimateMutationOrigin.User);
            _session.MarkClean();
            var events = CaptureChangedEvents();

            var result = _climate.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.AirTemperature, -20.0), ClimateMutationOrigin.User);

            Assert.That(result.IsChanged, Is.False);
            Assert.That(events, Is.Empty);
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ApplyIndividualEdit_InvalidValue_DoesNotChangeState()
        {
            var oldSnapshot = _climate.Snapshot;
            var events = CaptureChangedEvents();

            var result = _climate.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.AirTemperature, 999.0), ClimateMutationOrigin.User);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsChanged, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(_climate.Snapshot, Is.EqualTo(oldSnapshot));
            Assert.That(events, Is.Empty);
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ApplyIndividualEdit_IsHighRequirements_True_ChangesZone()
        {
            var city = CreateCity(t5days: -30.0);
            _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);

            var result = _climate.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.IsHighRequirements, 1.0), ClimateMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.Zone, Is.EqualTo(ClimateZone.Zone_M20_Plus));
        }

        [Test]
        public void ApplyProjectSnapshot_Load_ChangesStateDoesNotMarkDirty()
        {
            var data = new ClimateProjectData
            {
                SelectedCity = "Perm",
                Region = "Perm Krai",
                AirTemperature = -25.0,
                WindSpeed = 3.0,
                Humidity = 70.0,
                SnowfallIntensity = 1.5,
                SelectedZone = ClimateZone.Zone_M15,
                IsHighRequirements = false
            };

            var result = _climate.ApplyProjectSnapshot(data, null, ClimateMutationOrigin.Load);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.SelectedCity, Is.EqualTo("Perm"));
            Assert.That(result.NewSnapshot.ColdFiveDayTemperature, Is.EqualTo(-25.0));
            Assert.That(result.NewSnapshot.HasUserModifications, Is.False);
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ApplyProjectSnapshot_User_ChangesStateAndMarksDirty()
        {
            var data = new ClimateProjectData
            {
                SelectedCity = "Perm",
                Region = "Perm Krai",
                AirTemperature = -25.0,
                WindSpeed = 3.0,
                Humidity = 70.0,
                SnowfallIntensity = 1.5,
                SelectedZone = ClimateZone.Zone_M15,
                IsHighRequirements = false
            };

            var result = _climate.ApplyProjectSnapshot(data, null, ClimateMutationOrigin.User);

            Assert.That(result.NewSnapshot.HasUserModifications, Is.True);
            Assert.That(_session.IsDirty, Is.True);
        }

        [Test]
        public void ApplyProjectSnapshot_SameData_IsNoOp()
        {
            var data = new ClimateProjectData
            {
                SelectedCity = "Perm",
                Region = "Perm Krai",
                AirTemperature = -25.0,
                WindSpeed = 3.0,
                Humidity = 70.0,
                SnowfallIntensity = 1.5,
                SelectedZone = ClimateZone.Zone_M15,
                IsHighRequirements = false
            };
            _climate.ApplyProjectSnapshot(data, null, ClimateMutationOrigin.Load);
            var events = CaptureChangedEvents();

            var result = _climate.ApplyProjectSnapshot(data, null, ClimateMutationOrigin.Load);

            Assert.That(result.IsChanged, Is.False);
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void ResetToDefaults_Reset_ChangesStateDoesNotMarkDirty()
        {
            var city = CreateCity();
            _climate.ApplyCitySelection(city, true, ClimateMutationOrigin.User);
            _session.MarkClean();
            var events = CaptureChangedEvents();

            var result = _climate.ResetToDefaults(ClimateMutationOrigin.ProjectLoadReset);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.IsCitySelected, Is.False);
            Assert.That(result.NewSnapshot.IsHighRequirements, Is.False);
            Assert.That(result.NewSnapshot.HasUserModifications, Is.False);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ResetToDefaults_User_ChangesStateAndMarksDirty()
        {
            var city = CreateCity();
            _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);
            _session.MarkClean();

            var result = _climate.ResetToDefaults(ClimateMutationOrigin.User);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.HasUserModifications, Is.True);
            Assert.That(_session.IsDirty, Is.True);
        }

        [Test]
        public void ResetToCityData_WithCity_ChangesScalarsToCityData()
        {
            var city = CreateCity(t5days: -30.0, wind: 4.0, humidity: 60.0);
            _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);
            _climate.ApplyIndividualEdit(new ClimateEdit(ClimateEditField.AirTemperature, -10.0), ClimateMutationOrigin.User);
            _session.MarkClean();

            var result = _climate.ResetToCityData(city, ClimateMutationOrigin.ProjectLoadReset);

            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.NewSnapshot.AirTemperature, Is.EqualTo(-15.0));
            Assert.That(result.NewSnapshot.WindSpeed, Is.EqualTo(4.0));
            Assert.That(result.NewSnapshot.Humidity, Is.EqualTo(60.0));
            Assert.That(result.NewSnapshot.HasUserModifications, Is.False);
            Assert.That(_session.IsDirty, Is.False);
        }

        [Test]
        public void ResetToCityData_NullCity_ReturnsNoOp()
        {
            var result = _climate.ResetToCityData(null, ClimateMutationOrigin.ProjectLoadReset);

            Assert.That(result.IsChanged, Is.False);
        }

        [Test]
        public void Snapshot_Equality_CoversAllFields()
        {
            var city = CreateCity();
            _climate.ApplyCitySelection(city, false, ClimateMutationOrigin.User);
            var snapshot = _climate.Snapshot;

            var equalSnapshot = snapshot with { };
            var differentSnapshot = snapshot with { AirTemperature = snapshot.AirTemperature + 1.0 };

            Assert.That(equalSnapshot, Is.EqualTo(snapshot));
            Assert.That(differentSnapshot, Is.Not.EqualTo(snapshot));
        }

        [Test]
        public void Changed_Event_IncludesOriginAndSnapshots()
        {
            var events = CaptureChangedEvents();

            _climate.ApplyCitySelection(CreateCity(), false, ClimateMutationOrigin.Restore);

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Origin, Is.EqualTo(ClimateMutationOrigin.Restore));
            Assert.That(events[0].OldSnapshot, Is.Not.EqualTo(events[0].NewSnapshot));
            Assert.That(events[0].NewSnapshot, Is.EqualTo(_climate.Snapshot));
        }

        private static CityInfo CreateCity(double t5days = -25.0, double wind = 3.0, double humidity = 70.0)
        {
            return new CityInfo
            {
                Name = "Yekaterinburg",
                Region = "Sverdlovsk Oblast",
                T5Days092 = t5days,
                WindAvgTempLe8 = wind,
                Humidity15hCold = humidity,
                TColdDays098 = t5days - 5.0,
                TAbsMin = t5days - 10.0,
                Period_0_Days = 180,
                Period_8_Days = 220,
                Period_10_Days = 240
            };
        }

        private List<ClimateStateChangedEventArgs> CaptureChangedEvents()
        {
            var events = new List<ClimateStateChangedEventArgs>();
            _climate.Changed += (_, e) => events.Add(e);
            return events;
        }
    }
}
