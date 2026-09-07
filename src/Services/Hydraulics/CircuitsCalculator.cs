using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Реализация калькулятора контуров
    /// </summary>
    public class CircuitsCalculator : ICircuitsCalculator
    {
        private readonly IGlycolDataService _glycolService;

        public CircuitsCalculator(IGlycolDataService glycolService)
        {
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
        }

        public double CalculateCircuitPower(CircuitRow circuit, double q_up, double q_down, double pipeSpacing_cm)
        {
            if (circuit == null)
                throw new ArgumentNullException(nameof(circuit));

            if (q_up < 0)
                throw new ArgumentException("Мощность вверх не может быть отрицательной", nameof(q_up));

            if (q_down < 0)
                throw new ArgumentException("Мощность вниз не может быть отрицательной", nameof(q_down));

            if (pipeSpacing_cm <= 0)
                throw new ArgumentException("Шаг укладки должен быть положительным", nameof(pipeSpacing_cm));

            double lengthPerArea = circuit.CircuitLength / (100.0 / pipeSpacing_cm);
            double supplyLengthPerArea = circuit.SupplyLength / (100.0 / circuit.SupplySpacing_cm);
            double supplyHeatFactor = circuit.SupplyHeatPercent / 100.0;
            double power = (lengthPerArea + supplyLengthPerArea * supplyHeatFactor) * (q_up + q_down);

            return power;
        }

        public double CalculateFlowRate(double power, double deltaT, double density, double specificHeat)
        {
            if (power <= 0)
                throw new ArgumentException("Мощность должна быть положительной", nameof(power));

            if (deltaT <= 0)
                throw new ArgumentException("Температурный перепад должен быть положительным", nameof(deltaT));

            if (density <= 0)
                throw new ArgumentException("Плотность должна быть положительной", nameof(density));

            if (specificHeat <= 0)
                throw new ArgumentException("Удельная теплоёмкость должна быть положительной", nameof(specificHeat));

            double flowRate_m3h = power * 3.6 / (density * specificHeat * deltaT);
            double flowRate_lh = flowRate_m3h * 1000;

            return flowRate_lh;
        }

        public CircuitTemperatureResult CalculateAtTemperature(
            CircuitRow circuit,
            double temperature,
            GlycolProperties glycolProps,
            double innerDiameter,
            double kv,
            ValveType valveType)
        {
            if (circuit == null)
                throw new ArgumentNullException(nameof(circuit));

            if (glycolProps == null)
                throw new ArgumentNullException(nameof(glycolProps));

            if (innerDiameter <= 0)
                throw new ArgumentException("Внутренний диаметр должен быть положительным", nameof(innerDiameter));

            if (kv <= 0)
                throw new ArgumentException("Kv должен быть положительным", nameof(kv));

            var result = new CircuitTemperatureResult
            {
                Temperature = temperature,
                Density = glycolProps.Density / 1000.0,
                KinematicViscosity = glycolProps.KinematicViscosity
            };

            double velocity = circuit.FlowRate * 4000 / (3600 * Math.PI * Math.Pow(innerDiameter, 2));
            circuit.Velocity = velocity;

            double reynolds = 1000 * velocity * innerDiameter / glycolProps.KinematicViscosity;
            result.ReynoldsNumber = reynolds;

            result.FlowRegime = FlowRegimeCalculator.DetermineFlowRegime(reynolds);

            double frictionFactor = FlowRegimeCalculator.CalculateFrictionFactor(reynolds, innerDiameter);
            result.FrictionFactor = frictionFactor;

            double density_g_cm3 = glycolProps.Density / 1000.0;
            double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * density_g_cm3 * frictionFactor
                / (2 * innerDiameter) * 100;
            result.PressureLossPerMeter = pressureLossPerMeter;

            double dpRohr = (circuit.CircuitLength + circuit.SupplyLength) * pressureLossPerMeter;
            result.DpRohr = dpRohr;

            if (valveType == ValveType.HKV_D)
            {
                result.DpVerteiler = Math.Pow(circuit.FlowRate / 1000.0 / 1.2, 2) * 100000 * density_g_cm3;
                result.DpVent = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
            }
            else
            {
                result.DpVerteiler = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
                result.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
            }

            return result;
        }

        public List<CircuitRow> CalculateAllCircuits(
            List<CircuitRow> circuits,
            HydraulicInputData inputData,
            double pipeSpacing_cm,
            double powerUp,
            double powerDown,
            double operatingTemperature,
            double designTemperature,
            double deltaT,
            double innerDiameter)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitRow>();

            if (inputData == null)
                throw new ArgumentNullException(nameof(inputData));

            if (pipeSpacing_cm <= 0)
                throw new ArgumentException("Шаг укладки должен быть положительным", nameof(pipeSpacing_cm));

            var glycolPropsOperating = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                operatingTemperature);

            var glycolPropsDesign = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                designTemperature);

            double kv = ValveTurnsCalculator.GetDefaultKv(inputData.ValveType);

            foreach (var circuit in circuits)
            {
                if (!circuit.IsActive)
                    continue;

                circuit.Power = CalculateCircuitPower(circuit, powerUp, powerDown, pipeSpacing_cm);

                circuit.FlowRate = CalculateFlowRate(
                    circuit.Power,
                    deltaT,
                    glycolPropsOperating.Density,
                    glycolPropsOperating.SpecificHeat);

                circuit.OperatingResult = CalculateAtTemperature(
                    circuit,
                    operatingTemperature,
                    glycolPropsOperating,
                    innerDiameter,
                    kv,
                    inputData.ValveType);

                circuit.DesignResult = CalculateAtTemperature(
                    circuit,
                    designTemperature,
                    glycolPropsDesign,
                    innerDiameter,
                    kv,
                    inputData.ValveType);
            }

            return circuits;
        }

        public List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitRow>();

            var activeCircuits = circuits.Where(c => c.IsActive && c.OperatingResult != null).ToList();

            if (activeCircuits.Count == 0)
                return circuits;

            double maxDpGesamt = activeCircuits.Max(c => c.OperatingResult?.DpGesamt ?? 0);
            double maxTurns = ValveTurnsCalculator.GetMaxTurns(valveType);

            foreach (var circuit in activeCircuits)
            {
                double dpGesamt = circuit.OperatingResult?.DpGesamt ?? 0;
                circuit.IsReferenceCircuit = Math.Abs(dpGesamt - maxDpGesamt) < 0.01;

                if (circuit.IsReferenceCircuit)
                {
                    circuit.Throttling = 0;
                    circuit.ValveTurns = maxTurns;
                    circuit.ValveTurnsWarning = null;
                }
                else
                {
                    if (valveType == ValveType.HKV_D)
                    {
                        circuit.Throttling = maxDpGesamt - ((circuit.OperatingResult?.DpRohr ?? 0) + (circuit.OperatingResult?.DpVent ?? 0));
                    }
                    else
                    {
                        circuit.Throttling = maxDpGesamt - ((circuit.OperatingResult?.DpRohr ?? 0) + (circuit.OperatingResult?.DpVerteiler ?? 0));
                    }

                    double density_g_cm3 = circuit.OperatingResult?.Density ?? 0;
                    double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling, density_g_cm3);
                    var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, valveType);
                    circuit.ValveTurns = turns;
                    circuit.ValveTurnsWarning = warning;
                }
            }

            // ВАЖНО: DpVent НЕ пересчитывается с Kv из балансировки!
            // DpVent должен показывать потери в вентиле с дефолтным Kv (до настройки).
            // Это соответствует ожиданиям пользователя: DpVent = потери при полностью открытом вентиле.
            // 
            // Для балансировки используется Throttling = maxDpGesamt - (DpRohr + DpVerteiler),
            // а не DpGesamt, поэтому DpVent не влияет на балансировку.
            // 
            // Ранее здесь был код, который пересчитывал DpVent с Kv из балансировки,
            // что приводило к неправильным значениям DpVent и DpGesamt в UI.

            return circuits;
        }

        public CollectorSummary CalculateCollectorSummary(
            List<CircuitRow> circuits,
            int collectorNumber,
            ValveType valveType)
        {
            if (circuits == null || circuits.Count == 0)
                return new CollectorSummary { CollectorNumber = collectorNumber };

            var activeCircuits = circuits.Where(c => c.IsActive).ToList();

            if (activeCircuits.Count == 0)
                return new CollectorSummary { CollectorNumber = collectorNumber };

            var summary = new CollectorSummary
            {
                CollectorNumber = collectorNumber,
                CircuitCount = activeCircuits.Count,
                ValveType = valveType,
                Kv = ValveTurnsCalculator.GetDefaultKv(valveType),
                TotalPipeLength = activeCircuits.Sum(c => c.TotalLength),
                TotalPower = activeCircuits.Sum(c => c.Power),
                TotalFlowRate = activeCircuits.Sum(c => c.FlowRate),
                PressureLoss_Operating_Pa = activeCircuits.Max(c => c.OperatingResult?.DpGesamt ?? 0),
                PressureLoss_Cold_Pa = activeCircuits.Max(c => c.DesignResult?.DpGesamt ?? 0)
            };

            var referenceCircuit = activeCircuits.FirstOrDefault(c => c.IsReferenceCircuit);
            if (referenceCircuit != null)
            {
                summary.ReferenceCircuitNumber = referenceCircuit.CircuitNumber;
            }

            var warnings = new List<string>();
            if (summary.PressureLoss_Cold_Pa > CollectorSummary.MaxAllowedPressure_Pa)
            {
                warnings.Add($"Превышение давления: {summary.PressureLoss_Cold_Pa / 100.0:F1} мбар > {CollectorSummary.MaxAllowedPressure_Pa / 100.0:F0} мбар");
            }

            summary.Warnings = warnings.ToArray();
            summary.IsValid = warnings.Count == 0;

            return summary;
        }

        private double CalculateKvForThrottling(double flowRate, double throttling, double density_g_cm3)
        {
            if (throttling <= 0)
                return 0;

            if (density_g_cm3 <= 0)
                throw new ArgumentException("Плотность должна быть положительной", nameof(density_g_cm3));

            double flowRate_m3h = flowRate / 1000.0;
            double throttling_bar = throttling / 100000.0;

            return flowRate_m3h / Math.Sqrt(throttling_bar / density_g_cm3);
        }
    }
}