using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Реализация канонического климатического состояния проекта.
    /// Создаётся и хранится владельцем <see cref="ProjectSession"/>; не регистрируется в DI.
    /// </summary>
    public sealed class ProjectSessionClimateState : IProjectSessionClimateState
    {
        private readonly IMarkDirtyService? _markDirtyService;
        private readonly ClimateData? _climateData;
        private readonly CalculationContext? _calculationContext;

        private string _selectedCity = string.Empty;
        private string _selectedRegion = string.Empty;
        private double _airTemperature = -15.0;
        private double _coldFiveDayTemperature;
        private double _windSpeed = 5.0;
        private double _humidity = 70.0;
        private double _snowfallIntensity;
        private ClimateZone _zone = ClimateZone.Zone_M15;
        private bool _isHighRequirements;
        private bool _isCitySelected;
        private bool _hasUserModifications;
        private int _period0Days;

        public ProjectSessionClimateState(
            IMarkDirtyService? markDirtyService = null,
            IClimateData? climateData = null,
            CalculationContext? calculationContext = null)
        {
            _markDirtyService = markDirtyService;
            _climateData = climateData as ClimateData;
            _calculationContext = calculationContext;
        }

        public ClimateStateSnapshot Snapshot => new(
            _selectedCity,
            _selectedRegion,
            _airTemperature,
            _coldFiveDayTemperature,
            _windSpeed,
            _humidity,
            _snowfallIntensity,
            _zone,
            _isHighRequirements,
            _isCitySelected,
            _hasUserModifications,
            _period0Days);

        public event EventHandler<ClimateStateChangedEventArgs>? Changed;

        public ClimateMutationResult ApplyCitySelection(CityInfo? city, ClimateMutationOrigin origin)
        {
            var oldSnapshot = Snapshot;

            var newCity = city?.Name ?? string.Empty;
            var newRegion = city?.Region ?? string.Empty;
            var t5Days = city?.T5Days092 ?? 0.0;
            // Повышенные требования сбрасываются при смене города (план 2026-09-12, B5):
            // новый город всегда начинается с чистой автоматики.
            var newAirTemperature = city == null ? -15.0 : ClimateZoneRules.AutoAirTemperature(t5Days);
            var newColdFiveDayTemperature = city?.T5Days092 ?? 0.0;
            var newWindSpeed = city?.WindMaxJan ?? 0.0;
            var newHumidity = city?.Humidity15hCold ?? 0.0;
            var newSnowfallIntensity = 0.0;
            var newZone = ClimateZoneRules.FromTemperature(newAirTemperature);
            var newIsCitySelected = city != null;
            var newPeriod0Days = city?.Period_0_Days ?? 0;
            var newHasUserModifications = origin == ClimateMutationOrigin.User;

            var anyChange = false;
            anyChange |= SetProperty(ref _selectedCity, newCity);
            anyChange |= SetProperty(ref _selectedRegion, newRegion);
            anyChange |= SetProperty(ref _airTemperature, newAirTemperature);
            anyChange |= SetProperty(ref _coldFiveDayTemperature, newColdFiveDayTemperature);
            anyChange |= SetProperty(ref _windSpeed, newWindSpeed);
            anyChange |= SetProperty(ref _humidity, newHumidity);
            anyChange |= SetProperty(ref _snowfallIntensity, newSnowfallIntensity);
            anyChange |= SetProperty(ref _zone, newZone);
            anyChange |= SetProperty(ref _isHighRequirements, false);
            anyChange |= SetProperty(ref _isCitySelected, newIsCitySelected);
            anyChange |= SetProperty(ref _period0Days, newPeriod0Days);
            anyChange |= SetProperty(ref _hasUserModifications, newHasUserModifications);

            return CompleteMutation(oldSnapshot, origin, true, anyChange);
        }

        public ClimateMutationResult ApplyIndividualEdit(ClimateEdit edit, ClimateMutationOrigin origin)
        {
            var oldSnapshot = Snapshot;
            var errors = new List<string>();

            var isValid = Validate(edit, errors);
            if (!isValid)
            {
                return new ClimateMutationResult(
                    origin,
                    isChanged: false,
                    isValid: false,
                    errors,
                    oldSnapshot,
                    oldSnapshot);
            }

            var anyChange = false;
            var newHasUserModifications = origin == ClimateMutationOrigin.User;

            switch (edit.Field)
            {
                case ClimateEditField.AirTemperature:
                    anyChange |= SetProperty(ref _airTemperature, edit.Value);
                    if (!_isCitySelected)
                    {
                        anyChange |= SetProperty(ref _coldFiveDayTemperature, edit.Value);
                    }
                    // Зона следует за итоговой температурой (план 2026-09-12, B4):
                    // ручная правка держит строку «Зона:» и PDF согласованными.
                    anyChange |= SetProperty(ref _zone, ClimateZoneRules.FromTemperature(_airTemperature));
                    break;
                case ClimateEditField.ColdFiveDayTemperature:
                    anyChange |= SetProperty(ref _coldFiveDayTemperature, edit.Value);
                    break;
                case ClimateEditField.WindSpeed:
                    anyChange |= SetProperty(ref _windSpeed, edit.Value);
                    break;
                case ClimateEditField.Humidity:
                    anyChange |= SetProperty(ref _humidity, edit.Value);
                    break;
                case ClimateEditField.SnowfallIntensity:
                    anyChange |= SetProperty(ref _snowfallIntensity, edit.Value);
                    break;
                case ClimateEditField.IsHighRequirements:
                    var newHigh = edit.Value != 0.0;
                    anyChange |= SetProperty(ref _isHighRequirements, newHigh);
                    // Ступень вниз от автоматики города (план 2026-09-12, B1–B3):
                    // вкл → «автоматика + ступень»; выкл → возврат автоматики.
                    if (_isCitySelected)
                    {
                        var auto = ClimateZoneRules.AutoAirTemperature(_coldFiveDayTemperature);
                        var target = newHigh ? ClimateZoneRules.StepDown(auto) : auto;
                        if (target.HasValue)
                        {
                            anyChange |= SetProperty(ref _airTemperature, target.Value);
                        }
                    }

                    anyChange |= SetProperty(ref _zone, ClimateZoneRules.FromTemperature(_airTemperature));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edit), edit.Field, "Unknown climate edit field.");
            }

            anyChange |= SetProperty(ref _hasUserModifications, newHasUserModifications);

            return CompleteMutation(oldSnapshot, origin, true, anyChange);
        }

        public ClimateMutationResult ApplyProjectSnapshot(ClimateProjectData data, CityInfo? city, ClimateMutationOrigin origin)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var oldSnapshot = Snapshot;
            var newHasUserModifications = origin == ClimateMutationOrigin.User;
            var newIsCitySelected = !string.IsNullOrEmpty(data.SelectedCity);
            var coldFiveDay = city?.T5Days092 ?? data.AirTemperature;
            var newPeriod0Days = city?.Period_0_Days ?? 0;
            // Зона нормализуется по сохранённой температуре: M20_Plus из старых
            // .smc и расхождения legacy-записей приводятся к текущей семантике
            // (план 2026-09-12, B5 — совместимость .smc).
            var newZone = ClimateZoneRules.FromTemperature(data.AirTemperature);

            var anyChange = false;
            anyChange |= SetProperty(ref _selectedCity, data.SelectedCity ?? string.Empty);
            anyChange |= SetProperty(ref _selectedRegion, data.Region ?? string.Empty);
            anyChange |= SetProperty(ref _airTemperature, data.AirTemperature);
            anyChange |= SetProperty(ref _coldFiveDayTemperature, coldFiveDay);
            anyChange |= SetProperty(ref _windSpeed, data.WindSpeed);
            anyChange |= SetProperty(ref _humidity, data.Humidity);
            anyChange |= SetProperty(ref _snowfallIntensity, data.SnowfallIntensity);
            anyChange |= SetProperty(ref _zone, newZone);
            anyChange |= SetProperty(ref _isHighRequirements, data.IsHighRequirements);
            anyChange |= SetProperty(ref _isCitySelected, newIsCitySelected);
            anyChange |= SetProperty(ref _period0Days, newPeriod0Days);
            anyChange |= SetProperty(ref _hasUserModifications, newHasUserModifications);

            return CompleteMutation(oldSnapshot, origin, true, anyChange);
        }

        public ClimateMutationResult ResetToDefaults(ClimateMutationOrigin origin)
        {
            var oldSnapshot = Snapshot;
            var newHasUserModifications = origin == ClimateMutationOrigin.User;
            var anyChange = false;
            anyChange |= SetProperty(ref _selectedCity, string.Empty);
            anyChange |= SetProperty(ref _selectedRegion, string.Empty);
            anyChange |= SetProperty(ref _airTemperature, -15.0);
            anyChange |= SetProperty(ref _coldFiveDayTemperature, 0.0);
            anyChange |= SetProperty(ref _windSpeed, 5.0);
            anyChange |= SetProperty(ref _humidity, 70.0);
            anyChange |= SetProperty(ref _snowfallIntensity, 0.0);
            anyChange |= SetProperty(ref _zone, ClimateZone.Zone_M15);
            anyChange |= SetProperty(ref _isHighRequirements, false);
            anyChange |= SetProperty(ref _isCitySelected, false);
            anyChange |= SetProperty(ref _period0Days, 0);
            anyChange |= SetProperty(ref _hasUserModifications, newHasUserModifications);

            return CompleteMutation(oldSnapshot, origin, true, anyChange);
        }

        public ClimateMutationResult ResetToCityData(CityInfo? city, ClimateMutationOrigin origin)
        {
            var oldSnapshot = Snapshot;

            if (city == null)
            {
                return new ClimateMutationResult(
                    origin,
                    isChanged: false,
                    isValid: true,
                    Array.Empty<string>(),
                    oldSnapshot,
                    oldSnapshot);
            }

            var newHasUserModifications = origin == ClimateMutationOrigin.User;
            // Чистая автоматика города: повышенные требования сбрасываются вместе
            // с ручными правками (план 2026-09-12, B5).
            var newAirTemperature = ClimateZoneRules.AutoAirTemperature(city.T5Days092);

            var anyChange = false;
            anyChange |= SetProperty(ref _airTemperature, newAirTemperature);
            anyChange |= SetProperty(ref _coldFiveDayTemperature, city.T5Days092);
            anyChange |= SetProperty(ref _windSpeed, city.WindMaxJan);
            anyChange |= SetProperty(ref _humidity, city.Humidity15hCold);
            anyChange |= SetProperty(ref _snowfallIntensity, 0.0);
            anyChange |= SetProperty(ref _zone, ClimateZoneRules.FromTemperature(newAirTemperature));
            anyChange |= SetProperty(ref _isHighRequirements, false);
            anyChange |= SetProperty(ref _period0Days, city.Period_0_Days);
            anyChange |= SetProperty(ref _hasUserModifications, newHasUserModifications);

            return CompleteMutation(oldSnapshot, origin, true, anyChange);
        }

        public ClimateMutationResult ApplySnapshot(ClimateStateSnapshot snapshot, ClimateMutationOrigin origin)
        {
            // ADR-014: метод существует только для Undo/Redo-применения снимков
            // дневника отмены; другой origin — ошибка программирования.
            if (origin is not (ClimateMutationOrigin.Undo or ClimateMutationOrigin.Redo))
            {
                throw new ArgumentOutOfRangeException(nameof(origin), origin, "ApplySnapshot accepts only Undo/Redo origins.");
            }

            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var oldSnapshot = Snapshot;

            // Прямое присваивание всех 12 полей снимка: record несёт
            // HasUserModifications/Period0Days, поэтому «Load-ветка»
            // (пересчёт по городу) воспроизводила бы потерю этих полей
            // (план undo/redo, ревью P2-2).
            var anyChange = false;
            anyChange |= SetProperty(ref _selectedCity, snapshot.SelectedCity);
            anyChange |= SetProperty(ref _selectedRegion, snapshot.SelectedRegion);
            anyChange |= SetProperty(ref _airTemperature, snapshot.AirTemperature);
            anyChange |= SetProperty(ref _coldFiveDayTemperature, snapshot.ColdFiveDayTemperature);
            anyChange |= SetProperty(ref _windSpeed, snapshot.WindSpeed);
            anyChange |= SetProperty(ref _humidity, snapshot.Humidity);
            anyChange |= SetProperty(ref _snowfallIntensity, snapshot.SnowfallIntensity);
            anyChange |= SetProperty(ref _zone, snapshot.Zone);
            anyChange |= SetProperty(ref _isHighRequirements, snapshot.IsHighRequirements);
            anyChange |= SetProperty(ref _isCitySelected, snapshot.IsCitySelected);
            anyChange |= SetProperty(ref _period0Days, snapshot.Period0Days);
            anyChange |= SetProperty(ref _hasUserModifications, snapshot.HasUserModifications);

            return CompleteMutation(oldSnapshot, origin, true, anyChange);
        }

        private ClimateMutationResult CompleteMutation(
            ClimateStateSnapshot oldSnapshot,
            ClimateMutationOrigin origin,
            bool isValid,
            bool isChanged)
        {
            var newSnapshot = Snapshot;

            if (isChanged)
            {
                if (_climateData != null)
                {
                    _climateData.ApplyProjection(newSnapshot, isValid, PublishesCompatibility(origin));
                    _calculationContext?.UpdateClimate(_climateData, "Climate");
                }

                Changed?.Invoke(this, new ClimateStateChangedEventArgs(origin, oldSnapshot, newSnapshot));

                if (origin == ClimateMutationOrigin.User || origin == ClimateMutationOrigin.UserReset)
                {
                    _markDirtyService?.MarkDirty();
                }
            }

            return new ClimateMutationResult(
                origin,
                isChanged,
                isValid,
                Array.Empty<string>(),
                oldSnapshot,
                newSnapshot);
        }

        private static bool PublishesCompatibility(ClimateMutationOrigin origin)
        {
            return origin == ClimateMutationOrigin.User || origin == ClimateMutationOrigin.UserReset;
        }

        private static bool Validate(ClimateEdit edit, List<string> errors)
        {
            switch (edit.Field)
            {
                case ClimateEditField.AirTemperature:
                case ClimateEditField.ColdFiveDayTemperature:
                    return ValidateRange(edit.Value, ValidationConstants.MinAirTemperature, ValidationConstants.MaxAirTemperature, edit.Field, errors);
                case ClimateEditField.WindSpeed:
                    return ValidateRange(edit.Value, ValidationConstants.MinWindSpeed, ValidationConstants.MaxWindSpeed, edit.Field, errors);
                case ClimateEditField.Humidity:
                    return ValidateRange(edit.Value, ValidationConstants.MinHumidity, ValidationConstants.MaxHumidity, edit.Field, errors);
                case ClimateEditField.SnowfallIntensity:
                    return ValidateRange(edit.Value, ValidationConstants.MinSnowfallIntensity, ValidationConstants.MaxSnowfallIntensity, edit.Field, errors);
                case ClimateEditField.IsHighRequirements:
                    if (edit.Value != 0.0 && edit.Value != 1.0)
                    {
                        errors.Add($"{edit.Field} must be 0.0 (false) or 1.0 (true).");
                        return false;
                    }
                    return true;
                default:
                    errors.Add($"Unknown climate edit field: {edit.Field}.");
                    return false;
            }
        }

        private static bool ValidateRange(double value, double min, double max, ClimateEditField field, List<string> errors)
        {
            if (value < min || value > max)
            {
                errors.Add($"{field} must be between {min} and {max}.");
                return false;
            }
            return true;
        }

        private static bool SetProperty(ref string field, string value)
        {
            if (string.Equals(field, value, StringComparison.Ordinal))
            {
                return false;
            }

            field = value;
            return true;
        }

        private static bool SetProperty<T>(ref T field, T value) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            return true;
        }
    }
}
