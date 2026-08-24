using System;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Maps the canonical hydraulics snapshot to and from the existing .smc DTOs.
    /// The mapper preserves the Version 1.1 wire shape, including both legacy
    /// FlowRegime fields and nullable result/summary values.
    /// </summary>
    public static class HydraulicsPersistenceMapper
    {
        public static HydraulicsProjectData BuildHydraulicsProjectData(HydraulicsStateSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new HydraulicsProjectData
            {
                GlycolType = snapshot.GlobalInputs.GlycolType,
                GlycolConcentration = snapshot.GlobalInputs.GlycolConcentration,
                SupplySpacingCm = snapshot.GlobalInputs.SupplySpacingCm,
                SupplyHeatPercent = snapshot.GlobalInputs.SupplyHeatPercent,
                Collectors = snapshot.Collectors.Select(BuildCollectorProjectData).ToList()
            };
        }

        public static HydraulicsStateSnapshot BuildRestoreCandidate(HydraulicsProjectData? data)
        {
            if (data is null)
            {
                return HydraulicsStateSnapshot.Default;
            }

            var inputs = new HydraulicGlobalInputsSnapshot(
                data.GlycolType,
                data.GlycolConcentration,
                data.SupplySpacingCm,
                data.SupplyHeatPercent);

            var collectors = (data.Collectors ?? new())
                .Select(BuildCollectorSnapshot)
                .ToList();

            return new HydraulicsStateSnapshot(inputs, collectors, HydraulicsStatusSnapshot.Default);
        }

        private static CollectorProjectData BuildCollectorProjectData(HydraulicCollectorSnapshot snapshot)
        {
            return new CollectorProjectData
            {
                CollectorNumber = snapshot.CollectorNumber,
                CollectorType = snapshot.CollectorType,
                ValveType = snapshot.ValveType,
                Circuits = snapshot.Circuits.Select(BuildCircuitProjectData).ToList(),
                Summary = snapshot.Summary is null ? null : new CollectorSummaryProjectData
                {
                    CircuitCount = snapshot.Summary.CircuitCount,
                    TotalPipeLength = snapshot.Summary.TotalPipeLength,
                    TotalPower = snapshot.Summary.TotalPower,
                    TotalFlowRate = snapshot.Summary.TotalFlowRate,
                    PressureLoss_Operating_Pa = snapshot.Summary.PressureLoss_Operating_Pa,
                    PressureLoss_Cold_Pa = snapshot.Summary.PressureLoss_Cold_Pa,
                    Kv = snapshot.Summary.Kv,
                    CollectorType = snapshot.Summary.CollectorType
                }
            };
        }

        private static CircuitProjectData BuildCircuitProjectData(HydraulicCircuitSnapshot snapshot)
        {
            return new CircuitProjectData
            {
                CircuitNumber = snapshot.CircuitNumber,
                CircuitLength = snapshot.CircuitLength,
                SupplyLength = snapshot.SupplyLength,
                SupplySpacingCm = snapshot.SupplySpacingCm,
                SupplyHeatPercent = snapshot.SupplyHeatPercent,
                PipeSpacingCm = snapshot.PipeSpacingCm,
                Power = snapshot.OperatingResult?.Power ?? 0,
                FlowRate = snapshot.OperatingResult?.FlowRate ?? 0,
                Velocity = snapshot.OperatingResult?.Velocity ?? 0,
                FlowRegimeDescription = GetFlowRegimeDescription(snapshot.OperatingResult?.FlowRegime ?? FlowRegime.Laminar),
                Throttling = snapshot.OperatingResult?.Throttling ?? 0,
                ValveTurns = snapshot.OperatingResult?.ValveTurns ?? 0,
                OperatingResult = BuildResultProjectData(snapshot.OperatingResult),
                DesignResult = BuildResultProjectData(snapshot.DesignResult)
            };
        }

        private static CircuitResultProjectData? BuildResultProjectData(HydraulicCircuitResultSnapshot? snapshot)
        {
            if (snapshot is null)
            {
                return null;
            }

            return new CircuitResultProjectData
            {
                Power = snapshot.Power,
                FlowRate = snapshot.FlowRate,
                Velocity = snapshot.Velocity,
                DpRohr = snapshot.DpRohr,
                DpVerteiler = snapshot.DpVerteiler,
                DpVent = snapshot.DpVent,
                DpGesamt = snapshot.DpGesamt,
                Throttling = snapshot.Throttling,
                ValveTurns = snapshot.ValveTurns,
                FlowRegime = snapshot.FlowRegime.ToString(),
                FlowRegimeString = snapshot.FlowRegime.ToString(),
                Density = snapshot.Density,
                KinematicViscosity = snapshot.KinematicViscosity,
                ReynoldsNumber = snapshot.ReynoldsNumber,
                FrictionFactor = snapshot.FrictionFactor,
                PressureLossPerMeter = snapshot.PressureLossPerMeter
            };
        }

        private static HydraulicCollectorSnapshot BuildCollectorSnapshot(CollectorProjectData data)
        {
            var circuits = (data.Circuits ?? new())
                .Select(BuildCircuitSnapshot)
                .ToList();

            var summary = data.Summary is null ? null : new HydraulicCollectorSummarySnapshot(
                data.Summary.CircuitCount,
                data.Summary.TotalPipeLength,
                data.Summary.TotalPower,
                data.Summary.TotalFlowRate,
                data.Summary.PressureLoss_Operating_Pa,
                data.Summary.PressureLoss_Cold_Pa,
                data.Summary.Kv,
                data.Summary.CollectorType);

            return new HydraulicCollectorSnapshot(data.CollectorNumber, data.CollectorType, data.ValveType, circuits, summary);
        }

        private static HydraulicCircuitSnapshot BuildCircuitSnapshot(CircuitProjectData data)
        {
            var operatingResult = BuildResultSnapshot(data.OperatingResult);
            if (operatingResult is not null && operatingResult.Power == 0 && operatingResult.FlowRate == 0
                && (data.Power != 0 || data.FlowRate != 0))
            {
                operatingResult = new HydraulicCircuitResultSnapshot(
                    data.Power,
                    data.FlowRate,
                    operatingResult.Velocity,
                    operatingResult.DpRohr,
                    operatingResult.DpVerteiler,
                    operatingResult.DpVent,
                    operatingResult.DpGesamt,
                    operatingResult.Throttling,
                    operatingResult.ValveTurns,
                    operatingResult.Density,
                    operatingResult.KinematicViscosity,
                    operatingResult.ReynoldsNumber,
                    operatingResult.FrictionFactor,
                    operatingResult.PressureLossPerMeter,
                    operatingResult.FlowRegime);
            }

            return new HydraulicCircuitSnapshot(
                data.CircuitNumber,
                data.CircuitLength,
                data.SupplyLength,
                data.SupplySpacingCm,
                data.SupplyHeatPercent,
                data.PipeSpacingCm,
                operatingResult,
                BuildResultSnapshot(data.DesignResult));
        }

        private static HydraulicCircuitResultSnapshot? BuildResultSnapshot(CircuitResultProjectData? data)
        {
            if (data is null)
            {
                return null;
            }

            return new HydraulicCircuitResultSnapshot(
                data.Power,
                data.FlowRate,
                data.Velocity,
                data.DpRohr,
                data.DpVerteiler,
                data.DpVent,
                data.DpGesamt,
                data.Throttling,
                data.ValveTurns,
                data.Density,
                data.KinematicViscosity,
                data.ReynoldsNumber,
                data.FrictionFactor,
                data.PressureLossPerMeter,
                ResolveFlowRegime(data));
        }

        private static FlowRegime ResolveFlowRegime(CircuitResultProjectData data)
        {
            if (Enum.TryParse<FlowRegime>(data.FlowRegimeString, true, out var flowRegime)
                || Enum.TryParse(data.FlowRegime, true, out flowRegime))
            {
                return flowRegime;
            }

            return FlowRegime.Laminar;
        }

        private static string GetFlowRegimeDescription(FlowRegime flowRegime)
        {
            return flowRegime switch
            {
                FlowRegime.Laminar => "Ламинарный",
                FlowRegime.Transitional => "Переходный",
                FlowRegime.Turbulent => "Турбулентный",
                _ => string.Empty
            };
        }
    }
}
