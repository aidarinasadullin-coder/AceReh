using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Services.Project
{
    public enum HydraulicsMutationStatus { Changed, NoChange, Rejected }

    public sealed class HydraulicsMutationResult
    {
        public HydraulicsMutationStatus Status { get; }
        public HydraulicsMutationOrigin Origin { get; }
        public HydraulicsStateSnapshot Before { get; }
        public HydraulicsStateSnapshot After { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsChanged => Status == HydraulicsMutationStatus.Changed;
        public bool IsNoChange => Status == HydraulicsMutationStatus.NoChange;
        public bool IsRejected => Status == HydraulicsMutationStatus.Rejected;
        public HydraulicsMutationResult(HydraulicsMutationStatus status, HydraulicsMutationOrigin origin, HydraulicsStateSnapshot before, HydraulicsStateSnapshot after, IEnumerable<string>? errors = null)
        {
            Status = status; Origin = origin; Before = before ?? throw new ArgumentNullException(nameof(before)); After = after ?? throw new ArgumentNullException(nameof(after));
            Errors = Array.AsReadOnly((errors ?? Array.Empty<string>()).ToArray());
        }
    }

    public sealed class HydraulicsStateChangedEventArgs : EventArgs
    {
        public HydraulicsStateSnapshot OldSnapshot { get; }
        public HydraulicsStateSnapshot NewSnapshot { get; }
        public HydraulicsMutationOrigin Origin { get; }
        public HydraulicsStateChangedEventArgs(HydraulicsStateSnapshot oldSnapshot, HydraulicsStateSnapshot newSnapshot, HydraulicsMutationOrigin origin)
        {
            OldSnapshot = oldSnapshot ?? throw new ArgumentNullException(nameof(oldSnapshot)); NewSnapshot = newSnapshot ?? throw new ArgumentNullException(nameof(newSnapshot)); Origin = origin;
        }
    }

    public sealed class ProjectSessionHydraulicsState : IProjectSessionHydraulicsState
    {
        private readonly IMarkDirtyService? _markDirtyService;
        private HydraulicsStateSnapshot _snapshot = HydraulicsStateSnapshot.Default;

        public ProjectSessionHydraulicsState(IMarkDirtyService? markDirtyService = null) => _markDirtyService = markDirtyService;
        public HydraulicsStateSnapshot Snapshot => _snapshot;
        public event EventHandler<HydraulicsStateChangedEventArgs>? Changed;

        public HydraulicsMutationResult ApplyGlobalInputs(HydraulicGlobalInputsSnapshot candidate, HydraulicsMutationOrigin origin)
        {
            if (candidate is null) throw new ArgumentNullException(nameof(candidate));
            var errors = Validate(candidate);
            var status = origin == HydraulicsMutationOrigin.SystemApply
                ? HydraulicsStatusSnapshot.Default
                : _snapshot.Status;
            return errors.Count == 0 ? Commit(new(candidate, _snapshot.Collectors, status), origin) : Reject(origin, errors);
        }

        public HydraulicsMutationResult ReplaceCollectors(IEnumerable<HydraulicCollectorSnapshot> collectors, HydraulicsMutationOrigin origin)
        {
            if (collectors is null) throw new ArgumentNullException(nameof(collectors));
            var candidate = collectors.ToArray();
            return Commit(new(_snapshot.GlobalInputs, candidate, _snapshot.Status), origin);
        }

        public HydraulicsMutationResult BeginCalculation() =>
            Commit(new(_snapshot.GlobalInputs, _snapshot.Collectors, new(HydraulicsCalculationPhase.Calculating, string.Empty)), HydraulicsMutationOrigin.Calculation);

        public HydraulicsMutationResult CompleteCalculation(IReadOnlyList<HydraulicCollectorSnapshot> results, IReadOnlyDictionary<int, HydraulicCollectorSummarySnapshot> summaryByCollector, HydraulicsMutationOrigin origin = HydraulicsMutationOrigin.Calculation)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));
            if (summaryByCollector is null) throw new ArgumentNullException(nameof(summaryByCollector));
            var collectors = results.Select(c => new HydraulicCollectorSnapshot(c.CollectorNumber, c.CollectorType, c.ValveType, c.Circuits, summaryByCollector.TryGetValue(c.CollectorNumber, out var summary) ? summary : c.Summary)).ToArray();
            return Commit(new(_snapshot.GlobalInputs, collectors, HydraulicsStatusSnapshot.Default), origin);
        }

        public HydraulicsMutationResult FailCalculation(string message)
        {
            if (_snapshot.Status.Phase != HydraulicsCalculationPhase.Calculating)
                return Reject(HydraulicsMutationOrigin.Calculation, new[] { "FailCalculation requires an active calculation." });
            return Commit(new(_snapshot.GlobalInputs, _snapshot.Collectors, new(HydraulicsCalculationPhase.Error, message)), HydraulicsMutationOrigin.Calculation);
        }

        public HydraulicsMutationResult Restore(HydraulicsStateSnapshot snapshot, HydraulicsMutationOrigin origin)
        {
            if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
            if (origin != HydraulicsMutationOrigin.ProjectLoad) return Reject(origin, new[] { "Restore accepts only ProjectLoad origin." });
            return Commit(new(snapshot.GlobalInputs, snapshot.Collectors, snapshot.Status), origin);
        }

        public HydraulicsMutationResult ResetToDefaults(HydraulicsMutationOrigin origin) => Commit(HydraulicsStateSnapshot.Default, origin);

        private HydraulicsMutationResult Commit(HydraulicsStateSnapshot candidate, HydraulicsMutationOrigin origin)
        {
            var before = _snapshot;
            if (candidate.Equals(before)) return new(HydraulicsMutationStatus.NoChange, origin, before, before);
            _snapshot = candidate;
            if (origin == HydraulicsMutationOrigin.User) _markDirtyService?.MarkDirty();
            Changed?.Invoke(this, new(before, candidate, origin));
            return new(HydraulicsMutationStatus.Changed, origin, before, candidate);
        }

        private HydraulicsMutationResult Reject(HydraulicsMutationOrigin origin, IEnumerable<string> errors) => new(HydraulicsMutationStatus.Rejected, origin, _snapshot, _snapshot, errors);

        private static List<string> Validate(HydraulicGlobalInputsSnapshot candidate)
        {
            var errors = new List<string>();
            if (!Enum.IsDefined(candidate.GlycolType)) errors.Add("GlycolType must be a defined value.");
            if (double.IsNaN(candidate.GlycolConcentration) || double.IsInfinity(candidate.GlycolConcentration) || candidate.GlycolConcentration < 0 || candidate.GlycolConcentration > 100) errors.Add("GlycolConcentration must be between 0 and 100.");
            if (double.IsNaN(candidate.SupplySpacingCm) || double.IsInfinity(candidate.SupplySpacingCm) || candidate.SupplySpacingCm <= 0) errors.Add("SupplySpacingCm must be positive.");
            if (double.IsNaN(candidate.SupplyHeatPercent) || double.IsInfinity(candidate.SupplyHeatPercent) || candidate.SupplyHeatPercent < 0 || candidate.SupplyHeatPercent > 100) errors.Add("SupplyHeatPercent must be between 0 and 100.");
            return errors;
        }
    }
}
