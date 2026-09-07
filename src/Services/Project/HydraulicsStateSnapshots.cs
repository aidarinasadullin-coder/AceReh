using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Project
{
    public enum HydraulicsCalculationPhase
    {
        Actual,
        Calculating,
        Error
    }

    public sealed class HydraulicGlobalInputsSnapshot : IEquatable<HydraulicGlobalInputsSnapshot>
    {
        public static HydraulicGlobalInputsSnapshot Default { get; } =
            new(GlycolType.Ethylene, 50.0, 5.0, 10.0);

        public GlycolType GlycolType { get; }
        public double GlycolConcentration { get; }
        public double SupplySpacingCm { get; }
        public double SupplyHeatPercent { get; }

        public HydraulicGlobalInputsSnapshot(GlycolType glycolType, double glycolConcentration, double supplySpacingCm, double supplyHeatPercent)
        {
            GlycolType = glycolType;
            GlycolConcentration = glycolConcentration;
            SupplySpacingCm = supplySpacingCm;
            SupplyHeatPercent = supplyHeatPercent;
        }

        public bool Equals(HydraulicGlobalInputsSnapshot? other) => other is not null
            && GlycolType == other.GlycolType
            && GlycolConcentration.Equals(other.GlycolConcentration)
            && SupplySpacingCm.Equals(other.SupplySpacingCm)
            && SupplyHeatPercent.Equals(other.SupplyHeatPercent);
        public override bool Equals(object? obj) => obj is HydraulicGlobalInputsSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(GlycolType, GlycolConcentration, SupplySpacingCm, SupplyHeatPercent);
    }

    public sealed class HydraulicCircuitResultSnapshot : IEquatable<HydraulicCircuitResultSnapshot>
    {
        public double Power { get; }
        public double FlowRate { get; }
        public double Velocity { get; }
        public double DpRohr { get; }
        public double DpVerteiler { get; }
        public double DpVent { get; }
        public double DpGesamt { get; }
        public double Throttling { get; }
        public double ValveTurns { get; }
        public double Density { get; }
        public double KinematicViscosity { get; }
        public double ReynoldsNumber { get; }
        public double FrictionFactor { get; }
        public double PressureLossPerMeter { get; }
        public FlowRegime FlowRegime { get; }

        public HydraulicCircuitResultSnapshot(double power, double flowRate, double velocity, double dpRohr, double dpVerteiler, double dpVent, double dpGesamt, double throttling, double valveTurns, double density, double kinematicViscosity, double reynoldsNumber, double frictionFactor, double pressureLossPerMeter, FlowRegime flowRegime = FlowRegime.Laminar)
        {
            Power = power; FlowRate = flowRate; Velocity = velocity; DpRohr = dpRohr; DpVerteiler = dpVerteiler;
            DpVent = dpVent; DpGesamt = dpGesamt; Throttling = throttling; ValveTurns = valveTurns; Density = density;
            KinematicViscosity = kinematicViscosity; ReynoldsNumber = reynoldsNumber; FrictionFactor = frictionFactor;
            PressureLossPerMeter = pressureLossPerMeter; FlowRegime = flowRegime;
        }

        public bool Equals(HydraulicCircuitResultSnapshot? other) => other is not null
            && Power.Equals(other.Power) && FlowRate.Equals(other.FlowRate) && Velocity.Equals(other.Velocity)
            && DpRohr.Equals(other.DpRohr) && DpVerteiler.Equals(other.DpVerteiler) && DpVent.Equals(other.DpVent)
            && DpGesamt.Equals(other.DpGesamt) && Throttling.Equals(other.Throttling) && ValveTurns.Equals(other.ValveTurns)
            && Density.Equals(other.Density) && KinematicViscosity.Equals(other.KinematicViscosity)
            && ReynoldsNumber.Equals(other.ReynoldsNumber) && FrictionFactor.Equals(other.FrictionFactor)
            && PressureLossPerMeter.Equals(other.PressureLossPerMeter) && FlowRegime == other.FlowRegime;
        public override bool Equals(object? obj) => obj is HydraulicCircuitResultSnapshot other && Equals(other);
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Power); hash.Add(FlowRate); hash.Add(Velocity); hash.Add(DpRohr); hash.Add(DpVerteiler); hash.Add(DpVent);
            hash.Add(DpGesamt); hash.Add(Throttling); hash.Add(ValveTurns); hash.Add(Density); hash.Add(KinematicViscosity);
            hash.Add(ReynoldsNumber); hash.Add(FrictionFactor); hash.Add(PressureLossPerMeter);
            hash.Add(FlowRegime);
            return hash.ToHashCode();
        }
    }

    public sealed class HydraulicCollectorSummarySnapshot : IEquatable<HydraulicCollectorSummarySnapshot>
    {
        public int CircuitCount { get; }
        public double TotalPipeLength { get; }
        public double TotalPower { get; }
        public double TotalFlowRate { get; }
        public double PressureLoss_Operating_Pa { get; }
        public double PressureLoss_Cold_Pa { get; }
        public double Kv { get; }
        public string CollectorType { get; }

        public HydraulicCollectorSummarySnapshot(int circuitCount, double totalPipeLength, double totalPower, double totalFlowRate, double pressureLossOperatingPa, double pressureLossColdPa, double kv, string? collectorType)
        {
            CircuitCount = circuitCount; TotalPipeLength = totalPipeLength; TotalPower = totalPower; TotalFlowRate = totalFlowRate;
            PressureLoss_Operating_Pa = pressureLossOperatingPa; PressureLoss_Cold_Pa = pressureLossColdPa; Kv = kv;
            CollectorType = collectorType ?? string.Empty;
        }

        public bool Equals(HydraulicCollectorSummarySnapshot? other) => other is not null
            && CircuitCount == other.CircuitCount && TotalPipeLength.Equals(other.TotalPipeLength) && TotalPower.Equals(other.TotalPower)
            && TotalFlowRate.Equals(other.TotalFlowRate) && PressureLoss_Operating_Pa.Equals(other.PressureLoss_Operating_Pa)
            && PressureLoss_Cold_Pa.Equals(other.PressureLoss_Cold_Pa) && Kv.Equals(other.Kv)
            && string.Equals(CollectorType, other.CollectorType, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is HydraulicCollectorSummarySnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(CircuitCount, TotalPipeLength, TotalPower, TotalFlowRate, PressureLoss_Operating_Pa, PressureLoss_Cold_Pa, Kv, CollectorType);
    }

    public sealed class HydraulicCircuitSnapshot : IEquatable<HydraulicCircuitSnapshot>
    {
        public int CircuitNumber { get; }
        public double CircuitLength { get; }
        public double SupplyLength { get; }
        public double SupplySpacingCm { get; }
        public double SupplyHeatPercent { get; }
        public double PipeSpacingCm { get; }
        public HydraulicCircuitResultSnapshot? OperatingResult { get; }
        public HydraulicCircuitResultSnapshot? DesignResult { get; }

        public HydraulicCircuitSnapshot(int circuitNumber, double circuitLength, double supplyLength, double supplySpacingCm, double supplyHeatPercent, double pipeSpacingCm, HydraulicCircuitResultSnapshot? operatingResult = null, HydraulicCircuitResultSnapshot? designResult = null)
        {
            CircuitNumber = circuitNumber; CircuitLength = circuitLength; SupplyLength = supplyLength; SupplySpacingCm = supplySpacingCm;
            SupplyHeatPercent = supplyHeatPercent; PipeSpacingCm = pipeSpacingCm; OperatingResult = operatingResult; DesignResult = designResult;
        }

        public bool Equals(HydraulicCircuitSnapshot? other) => other is not null
            && CircuitNumber == other.CircuitNumber && CircuitLength.Equals(other.CircuitLength) && SupplyLength.Equals(other.SupplyLength)
            && SupplySpacingCm.Equals(other.SupplySpacingCm) && SupplyHeatPercent.Equals(other.SupplyHeatPercent)
            && PipeSpacingCm.Equals(other.PipeSpacingCm) && Equals(OperatingResult, other.OperatingResult) && Equals(DesignResult, other.DesignResult);
        public override bool Equals(object? obj) => obj is HydraulicCircuitSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(CircuitNumber, CircuitLength, SupplyLength, SupplySpacingCm, SupplyHeatPercent, PipeSpacingCm, OperatingResult, DesignResult);
    }

    public sealed class HydraulicCollectorSnapshot : IEquatable<HydraulicCollectorSnapshot>
    {
        public int CollectorNumber { get; }
        public string CollectorType { get; }
        public ValveType ValveType { get; }
        public IReadOnlyList<HydraulicCircuitSnapshot> Circuits { get; }
        public HydraulicCollectorSummarySnapshot? Summary { get; }

        public HydraulicCollectorSnapshot(int collectorNumber, string? collectorType, ValveType valveType, IEnumerable<HydraulicCircuitSnapshot>? circuits, HydraulicCollectorSummarySnapshot? summary = null)
        {
            CollectorNumber = collectorNumber; CollectorType = collectorType ?? string.Empty; ValveType = valveType;
            Circuits = Array.AsReadOnly((circuits ?? Array.Empty<HydraulicCircuitSnapshot>()).ToArray()); Summary = summary;
        }

        public bool Equals(HydraulicCollectorSnapshot? other) => other is not null
            && CollectorNumber == other.CollectorNumber && string.Equals(CollectorType, other.CollectorType, StringComparison.Ordinal)
            && ValveType == other.ValveType && Circuits.SequenceEqual(other.Circuits) && Equals(Summary, other.Summary);
        public override bool Equals(object? obj) => obj is HydraulicCollectorSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(CollectorNumber, CollectorType, ValveType, Circuits.Count, Summary);
    }

    public sealed class HydraulicsStatusSnapshot : IEquatable<HydraulicsStatusSnapshot>
    {
        public static HydraulicsStatusSnapshot Default { get; } = new(HydraulicsCalculationPhase.Actual, string.Empty);
        public HydraulicsCalculationPhase Phase { get; }
        public string ValidationMessage { get; }
        public HydraulicsStatusSnapshot(HydraulicsCalculationPhase phase, string? validationMessage)
        {
            Phase = phase; ValidationMessage = validationMessage ?? string.Empty;
        }
        public bool Equals(HydraulicsStatusSnapshot? other) => other is not null && Phase == other.Phase && string.Equals(ValidationMessage, other.ValidationMessage, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is HydraulicsStatusSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Phase, ValidationMessage);
    }

    public sealed class HydraulicsStateSnapshot : IEquatable<HydraulicsStateSnapshot>
    {
        public static HydraulicsStateSnapshot Default { get; } = new(HydraulicGlobalInputsSnapshot.Default, Array.Empty<HydraulicCollectorSnapshot>(), HydraulicsStatusSnapshot.Default);
        public HydraulicGlobalInputsSnapshot GlobalInputs { get; }
        public IReadOnlyList<HydraulicCollectorSnapshot> Collectors { get; }
        public HydraulicsStatusSnapshot Status { get; }

        public HydraulicsStateSnapshot(HydraulicGlobalInputsSnapshot globalInputs, IEnumerable<HydraulicCollectorSnapshot>? collectors, HydraulicsStatusSnapshot status)
        {
            GlobalInputs = globalInputs ?? throw new ArgumentNullException(nameof(globalInputs));
            Collectors = Array.AsReadOnly((collectors ?? Array.Empty<HydraulicCollectorSnapshot>()).ToArray());
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }
        public bool Equals(HydraulicsStateSnapshot? other) => other is not null && GlobalInputs.Equals(other.GlobalInputs) && Collectors.SequenceEqual(other.Collectors) && Status.Equals(other.Status);
        public override bool Equals(object? obj) => obj is HydraulicsStateSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(GlobalInputs, Collectors.Count, Status);
    }

    /// <summary>
    /// Честный признак «расчёт гидравлики выполнен для текущих данных»:
    /// есть контуры с длиной &gt; 0 и у всех коллекторов посчитан Summary.
    /// Правка ввода пользователем (ReplaceCollectors c User/UserReset) обнуляет
    /// результаты в каноне — предикат гаснет реактивно до пересчёта (ADR-012).
    /// Общий для статуса вкладки «Гидравлика» (MainViewModel) и гейта вкладки
    /// «Результаты» (ResultsViewModel.CheckDataReadiness).
    /// </summary>
    public static class HydraulicsStateSnapshotExtensions
    {
        public static bool IsCalculated(this HydraulicsStateSnapshot snapshot) =>
            snapshot.Collectors.Count > 0
            && snapshot.Collectors.All(c => c.Summary is not null)
            && snapshot.Collectors.Any(c => c.Circuits.Any(cr => cr.CircuitLength > 0));
    }
}
