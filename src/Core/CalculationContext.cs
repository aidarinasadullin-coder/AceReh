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
    /// Паттерн: Singleton с DI.
    /// Контракт сужен до реально используемой поверхности (этап D2):
    /// публикаторы — ClimateViewModel (климат), ConstructionViewModel (конструкция),
    /// ThermalViewModel (тепловые входы и результат), CircuitsViewModel (итоги гидравлики);
    /// потребитель — CircuitsViewModel (геттеры + событие ContextChanged);
    /// сброс — MainViewModel и ProjectLoadOrchestrator.
    /// Правило инвалидации: изменение любых входных данных сбрасывает downstream-результаты,
    /// чтобы потребители не показывали stale-данные.
    /// </remarks>
    public partial class CalculationContext : ObservableObject
    {
        #region Climate Data

        /// <summary>
        /// Климатические данные (только для чтения)
        /// </summary>
        public IClimateData? Climate { get; private set; }

        /// <summary>
        /// Температура наружного воздуха, °C
        /// </summary>
        public double AirTemperature => Climate?.AirTemperature ?? 0;

        #endregion

        #region Construction Data

        /// <summary>
        /// Данные конструкции (только для чтения)
        /// </summary>
        public IConstructionData? Construction { get; private set; }

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
        /// Температура подачи, °C
        /// </summary>
        public double SupplyTemperature => ThermalResult?.SupplyTemperature ?? 0;

        /// <summary>
        /// Температура обратки, °C
        /// </summary>
        public double ReturnTemperature => ThermalResult?.ReturnTemperature ?? 0;

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
        /// Обновить результаты гидравлического расчёта
        /// </summary>
        /// <param name="results">Результаты расчёта по коллекторам</param>
        /// <param name="source">Источник изменения (имя модуля)</param>
        public void UpdateHydraulics(List<CollectorSummary>? results, string source = "Hydraulics")
        {
            var oldValue = HydraulicsResults;
            HydraulicsResults = results;

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

            OnContextChanged(nameof(Reset), null, null, "System");
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
