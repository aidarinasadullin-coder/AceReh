using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Tests.Climate
{
    /// <summary>
    /// Narrow tests proving ClimateData is a non-owning compatibility projection:
    /// IClimateData is read-only, concrete ClimateData has no public setters,
    /// and only the approved <see cref="ClimateData.ApplyProjection"/> seam mutates values.
    /// </summary>
    [TestFixture]
    public sealed class ClimateDataProjectionTests
    {
        private static readonly string[] ClimateDataPropertyNames =
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

        [Test]
        public void IClimateData_IsReadOnly()
        {
            var properties = typeof(IClimateData).GetProperties()
                .Where(p => ClimateDataPropertyNames.Contains(p.Name))
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(properties, Has.Count.EqualTo(ClimateDataPropertyNames.Length),
                    "IClimateData must expose all eight climate projection properties.");
                Assert.That(properties.Where(p => p.SetMethod != null), Is.Empty,
                    "IClimateData must not expose property setters.");
            });
        }

        [Test]
        public void ClimateData_PropertiesAreNotPubliclySettable()
        {
            var publicSetters = typeof(ClimateData).GetProperties()
                .Where(p => ClimateDataPropertyNames.Contains(p.Name) && p.SetMethod?.IsPublic == true)
                .Select(p => p.Name)
                .ToArray();

            Assert.That(publicSetters, Is.Empty,
                "Concrete ClimateData properties must not have public setters; mutation is internal to the approved projection updater.");
        }

        [Test]
        public void ApplyProjection_UpdatesAllFieldsAndRaisesDataChangedOnce()
        {
            var projection = new ClimateData();
            var source = new ClimateData
            {
                SelectedCity = "Москва",
                SelectedRegion = "Московская область",
                AirTemperature = -15.0,
                ColdFiveDayTemperature = -28.0,
                WindSpeed = 4.5,
                Humidity = 85.0,
                SnowfallIntensity = 0.5,
                Zone = ClimateZone.Zone_M15
            };

            var eventCount = 0;
            ClimateDataChangedEventArgs? capturedArgs = null;
            projection.DataChanged += (sender, args) =>
            {
                eventCount++;
                capturedArgs = args;
            };

            projection.ApplyProjection(source, isValid: true);

            Assert.Multiple(() =>
            {
                Assert.That(projection.SelectedCity, Is.EqualTo("Москва"));
                Assert.That(projection.SelectedRegion, Is.EqualTo("Московская область"));
                Assert.That(projection.AirTemperature, Is.EqualTo(-15.0));
                Assert.That(projection.ColdFiveDayTemperature, Is.EqualTo(-28.0));
                Assert.That(projection.WindSpeed, Is.EqualTo(4.5));
                Assert.That(projection.Humidity, Is.EqualTo(85.0));
                Assert.That(projection.SnowfallIntensity, Is.EqualTo(0.5));
                Assert.That(projection.Zone, Is.EqualTo(ClimateZone.Zone_M15));
                Assert.That(eventCount, Is.EqualTo(1), "ApplyProjection must raise DataChanged exactly once.");
                Assert.That(capturedArgs, Is.Not.Null);
                Assert.That(capturedArgs!.ChangedProperty, Is.EqualTo("Sync"));
                Assert.That(capturedArgs.IsValid, Is.True);
            });
        }

        [Test]
        public void ApplyProjection_ForwardsIsValidToEventArgs()
        {
            var projection = new ClimateData();
            var source = new ClimateData { AirTemperature = -15.0 };

            ClimateDataChangedEventArgs? capturedArgs = null;
            projection.DataChanged += (_, args) => capturedArgs = args;

            projection.ApplyProjection(source, isValid: false);

            Assert.That(capturedArgs, Is.Not.Null);
            Assert.That(capturedArgs!.IsValid, Is.False);
        }

        [Test]
        public void ApplyProjection_NullSource_ThrowsArgumentNullException()
        {
            var projection = new ClimateData();
            Assert.Throws<ArgumentNullException>(() => projection.ApplyProjection(null!));
        }
    }
}
