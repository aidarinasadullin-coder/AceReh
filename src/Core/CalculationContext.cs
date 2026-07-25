using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Models.Hydraulics;


namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Состояние расчёта
    /// </summary>
    public enum CalculationState
    {
        /// <summary>
        /// Начальное состояние, данные не загружены
        /// </summary>
        NotInitialized,

        /// <summary>
        /// Климатические данные загружены
        /// </summary>
        ClimateLoaded,

        /// <summary>
        /// Конструкция задана
        /// </summary>
        ConstructionReady,

        /// <summary>
        /// Тепловой расчёт выполнен
        /// </summary>
        ThermalCalculated,

        /// <summary>
        /// Гидравлический расчёт выполнен
        /// </summary>
        HydraulicsCalculated,

        /// <summary>
        /// Ошибка в данных
        /// </summary>
        Error
    }

    /// <summary>
    /// Аргументы события изменения контекста
    /// </summary>
    public class ContextChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Имя изменённого свойства
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Старое значение
        /// </summary>
        public object? OldValue { get; set; }

        /// <summary>
        /// Новое значение
        /// </summary>
        public object? NewValue { get; set; }

        /// <summary>
        /// Источник изменения (имя модуля)
        /// </summary>
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Единый контекст расчёта для синхронизации между модулями
    /// </summary>
    /// <remarks>
    /// Паттерн: Singleton с DI
    /// Назначение: Централизованное хранение всех расчётных параметров и результатов,
    /// обеспечение синхронизации между модулями без событийной модели.
    /// </remarks>
    public partial class CalculationContext : ObservableObject
    {
        #region Observable Properties

        /// <summary>
        /// Текущее состояние расчёта
        /// </summary>
        [ObservableProperty]
        private CalculationState _state = CalculationState.NotInitialized;

        /// <summary>
        /// Сообщение об ошибке (если State == Error)
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        #endregion

        #region Climate Data

        /// <summary>
        /// Климатические данные (только для чтения)
        /// </summary>
        public IClimateData? Climate { get; private set; }

        /// <summary>
        /// Выбранный город
        /// </summary>
        public string? SelectedCity => Climate?.SelectedCity;

        /// <summary>
        /// Температура наружного воздуха, °C
        /// </summary>
        public double AirTemperature => Climate?.AirTemperature ?? 0;

        /// <summary>
        /// Скорость ветра, м/с
        /// </summary>
        public double WindSpeed => Climate?.WindSpeed ?? 0;

        /// <summary>
        /// Интенсивность снегопада, мм/ч
        /// </summary>
        public double SnowfallIntensity => Climate?.SnowfallIntensity ?? 0;

        #endregion

        #region Construction Data

        /// <summary>
        /// Данные конструкции (только для чтения)
        /// </summary>
        public IConstructionData? Construction { get; private set; }

        /// <summary>
        /// Сопротивление слоёв над трубой, м²·К/Вт
        /// </summary>
        public double R1Total => Construction?.R1Total ?? 0;

        /// <summary>
        /// Сопротивление слоёв под трубой, м²·К/Вт
        /// </summary>
        public double R2Total => Construction?.R2Total ?? 0;

        /// <summary>
        /// Теплопроводность стяжки, Вт/м·К
        /// </summary>
        public double LambdaE => Construction?.LambdaE ?? 0;

        #endregion

        #region Thermal Results

        /// <summary>
        /// Результаты теплового расчёта
        /// </summary>
        [ObservableProperty]
        private ThermalCalculationResult? _thermalResult;

        /// <summary>
        /// Признак того, что тепловой расчёт выполнен корректно
        /// </summary>
        public bool IsThermalValid => ThermalResult?.IsValid ?? false;

        /// <summary>
        /// Мощность вверх, Вт/м²
        /// </summary>
        public double PowerUp => ThermalResult?.PowerUp ?? 0;

        /// <summary>
        /// Мощность вниз (потери), Вт/м²
        /// </summary>
        public double PowerDown => ThermalResult?.PowerDown ?? 0;

        /// <summary>
        /// Суммарная мощность, Вт/м²
        /// </summary>
        public double PowerTotal => ThermalResult?.PowerTotal ?? 0;

        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        public double SupplyTemperature => ThermalResult?.SupplyTemperature ?? 0;

        /// <summary>
        /// Температура обратки, °C
        /// </summary>
        public double ReturnTemperature => ThermalResult?.ReturnTemperature ?? 0;

        /// <summary>
        /// Температурный перепад, К
        /// </summary>
        public double DeltaT => ThermalResult?.DeltaT ?? 0;

        /// <summary>
        /// Входные параметры теплового расчёта (включая выбранную трубу)
        /// </summary>
        public ThermalInputs? ThermalInputs { get; private set; }

        #endregion

        #region Hydraulics Results

        /// <summary>
        /// Результаты гидравлического расчёта по коллекторам
        /// </summary>
        [ObservableProperty]
        private List<CollectorSummary>? _hydraulicsResults;

        /// <summary>
        /// Признак того, что гидравлический расчёт выполнен
        /// </summary>
        public bool IsHydraulicsValid => HydraulicsResults != null && HydraulicsResults.Count > 0;

        /// <summary>
        /// Входные данные гидравлического расчёта
        /// </summary>
        public HydraulicInputData? Hydraulics { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения контекста
        /// </summary>
        public event EventHandler<ContextChangedEventArgs>? ContextChanged;

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновить климатические данные
        /// </summary>
        /// <param name="climate">Новые климатические данные</param>
        /// <param name="source">Источник изменения (имя модуля)</param>
        public void UpdateClimate(IClimateData climate, string source = "Climate")
        {
            var oldValue = Climate;
            Climate = climate;

            // Сброс результатов при изменении климатических данных
            ThermalResult = null;
            HydraulicsResults = null;

            State = CalculationState.ClimateLoaded;

            OnContextChanged(nameof(Climate), oldValue, climate, source);
        }

        /// <summary>
        /// Обновить данные конструкции
        /// </summary>
        /// <param name="construction">Новые данные конструкции</param>
        /// <param name="source">Источник изменения (имя модуля)</param>
        public void UpdateConstruction(IConstructionData construction, string source = "Construction")
        {
            var oldValue = Construction;
            Construction = construction;

            // Сброс результатов при изменении конструкции
            ThermalResult = null;
            HydraulicsResults = null;

            State = CalculationState.ConstructionReady;

            OnContextChanged(nameof(Construction), oldValue, construction, source);
        }

        /// <summary>
        /// Обновить результаты теплового расчёта
        /// </summary>
        /// <param name="result">Результаты расчёта</param>
        /// <param name="source">Источник изменения (имя модуля)</param>
        public void UpdateThermal(ThermalCalculationResult result, string source = "Thermal")
        {
            var oldValue = ThermalResult;
            ThermalResult = result;

            if (result.IsValid)
            {
                State = CalculationState.ThermalCalculated;
                ErrorMessage = string.Empty;
            }
            else
            {
                State = CalculationState.Error;
                ErrorMessage = string.Join("; ", result.ValidationErrors ?? Array.Empty<string>());
            }

            // Сброс гидравлических результатов при изменении теплового расчёта
            HydraulicsResults = null;

            OnContextChanged(nameof(ThermalResult), oldValue, result, source);
        }

        /// <summary>
        /// Обновить входные параметры теплового расчёта
        /// </summary>
        /// <param name="inputs">Входные параметры теплового расчёта</param>
        /// <param name="source">Источник изменения (имя модуля)</param>
        public void UpdateThermalInputs(ThermalInputs inputs, string source = "Thermal")
        {
            var oldValue = ThermalInputs;
            ThermalInputs = inputs;

            // Изменение входных параметров теплового расчёта инвалидирует
            // гидравлические результаты: они рассчитаны от прежних тепловых данных.
            // Тот же принцип, что в UpdateClimate/UpdateConstruction/UpdateThermal.
            // Пересчёт выполняется потребителем через UpdateThermal -> гидравлику.
            HydraulicsResults = null;

            OnContextChanged(nameof(ThermalInputs), oldValue, inputs, source);
        }

        /// <summary>
        /// Обновить входные данные гидравлического расчёта
        /// </summary>
        /// <param name="inputs">Входные данные гидравлического расчёта</param>
        /// <param name="source">Источник изменения (имя модуля)</param>
        public void UpdateHydraulics(HydraulicInputData inputs, string source = "Hydraulics")
        {
            var oldValue = Hydraulics;
            Hydraulics = inputs;

            OnContextChanged(nameof(Hydraulics), oldValue, inputs, source);
        }

        /// <summary>
        /// Обновить результаты гидравлического расчёта
        /// </summary>
        /// <param name="results">Результаты расчёта по коллекторам</param>
        /// <param name="source">Источник изменения (имя модуля)</param>
        public void UpdateHydraulics(List<CollectorSummary> results, string source = "Hydraulics")
        {
            var oldValue = HydraulicsResults;
            HydraulicsResults = results;

            if (results != null && results.Count > 0)
            {
                var hasErrors = results.Any(r => !r.IsValid);
                if (hasErrors)
                {
                    State = CalculationState.Error;
                    ErrorMessage = string.Join("; ", results
                        .Where(r => r.Warnings != null && r.Warnings.Length > 0)
                        .SelectMany(r => r.Warnings));
                }
                else
                {
                    State = CalculationState.HydraulicsCalculated;
                    ErrorMessage = string.Empty;
                }
            }

            OnContextChanged(nameof(HydraulicsResults), oldValue, results, source);
        }

        /// <summary>
        /// Сбросить контекст в начальное состояние
        /// </summary>
        public void Reset()
        {
            Climate = null;
            Construction = null;
            ThermalResult = null;
            HydraulicsResults = null;
            Hydraulics = null;
            State = CalculationState.NotInitialized;
            ErrorMessage = string.Empty;

            OnContextChanged(nameof(Reset), null, null, "System");
        }

        #endregion

        #region Validation

        /// <summary>
        /// Проверить валидность всех данных
        /// </summary>
        /// <returns>true если все данные валидны</returns>
        public bool Validate()
        {
            return GetValidationErrors().Count == 0;
        }

        /// <summary>
        /// Получить список ошибок валидации
        /// </summary>
        /// <returns>Список ошибок</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            // Проверка климатических данных
            if (Climate == null)
            {
                errors.Add("Климатические данные не заданы");
            }
            else if (string.IsNullOrEmpty(Climate.SelectedCity))
            {
                errors.Add("Город не выбран");
            }

            // Проверка конструкции
            if (Construction == null)
            {
                errors.Add("Конструкция не задана");
            }

            // Проверка теплового расчёта
            if (ThermalResult == null)
            {
                errors.Add("Тепловой расчёт не выполнен");
            }
            else if (!ThermalResult.IsValid)
            {
                errors.AddRange(ThermalResult.ValidationErrors ?? Array.Empty<string>());
            }

            return errors;
        }

        /// <summary>
        /// Проверить готовность к тепловому расчёту
        /// </summary>
        /// <returns>true если можно выполнять тепловой расчёт</returns>
        public bool IsReadyForThermalCalculation()
        {
            return Climate != null &&
                   !string.IsNullOrEmpty(Climate.SelectedCity) &&
                   Construction != null;
        }

        /// <summary>
        /// Проверить готовность к гидравлическому расчёту
        /// </summary>
        /// <returns>true если можно выполнять гидравлический расчёт</returns>
        public bool IsReadyForHydraulicsCalculation()
        {
            return IsReadyForThermalCalculation() &&
                   ThermalResult != null &&
                   ThermalResult.IsValid;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Вызвать событие изменения контекста
        /// </summary>
        private void OnContextChanged(string propertyName, object? oldValue, object? newValue, string source)
        {
            ContextChanged?.Invoke(this, new ContextChangedEventArgs
            {
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                Source = source
            });
        }

        #endregion
    }
}