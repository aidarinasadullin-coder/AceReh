using System;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Реконструкция Results-owned строк контуров из канонического
    /// HydraulicsState-снапшота (Phase 9, ST-026): Results владеет своими
    /// проекциями и больше не мутирует объекты CircuitsViewModel-модуля.
    /// </summary>
    /// <remarks>
    /// Маппинг — инверсия замороженной characterization-связки
    /// <c>CircuitsViewModel.CaptureCanonicalCollectors</c> /
    /// <c>CircuitsViewModel.ApplyLifecycleSnapshotToAdapter</c>; порядок
    /// инициализатора сохранён дословно, чтобы self-healing строки
    /// (<c>CircuitArea</c> при <c>OnPipeSpacing_cmChanged</c>) дал идентичное
    /// конечное состояние. Дедупликация с адаптерным зеркалом модуля —
    /// отдельная задача очистки (см. slice-3 receipt).
    /// </remarks>
    public static class HydraulicCircuitRowProjection
    {
        /// <summary>
        /// Создать новую строку Results-owned проекции из канонического снапшота контура.
        /// </summary>
        public static CircuitRow CreateRow(HydraulicCircuitSnapshot circuit)
        {
            if (circuit is null)
            {
                throw new ArgumentNullException(nameof(circuit));
            }

            return new CircuitRow
            {
                CircuitNumber = circuit.CircuitNumber,
                CircuitLength = circuit.CircuitLength,
                SupplyLength = circuit.SupplyLength,
                SupplySpacing_cm = circuit.SupplySpacingCm,
                SupplyHeatPercent = circuit.SupplyHeatPercent,
                PipeSpacing_cm = circuit.PipeSpacingCm,
                Power = circuit.OperatingResult?.Power ?? 0,
                FlowRate = circuit.OperatingResult?.FlowRate ?? 0,
                Velocity = circuit.OperatingResult?.Velocity ?? 0,
                Throttling = circuit.OperatingResult?.Throttling ?? 0,
                ValveTurns = circuit.OperatingResult?.ValveTurns ?? 0,
                OperatingResult = ToDomainResult(circuit.OperatingResult),
                DesignResult = ToDomainResult(circuit.DesignResult)
            };
        }

        private static CircuitTemperatureResult ToDomainResult(HydraulicCircuitResultSnapshot? snapshot)
        {
            if (snapshot is null)
            {
                return new CircuitTemperatureResult();
            }

            return new CircuitTemperatureResult
            {
                DpRohr = snapshot.DpRohr,
                DpVerteiler = snapshot.DpVerteiler,
                DpVent = snapshot.DpVent,
                ZuDrosseln = snapshot.Throttling,
                FlowRegime = snapshot.FlowRegime,
                Density = snapshot.Density,
                KinematicViscosity = snapshot.KinematicViscosity,
                ReynoldsNumber = snapshot.ReynoldsNumber,
                FrictionFactor = snapshot.FrictionFactor,
                PressureLossPerMeter = snapshot.PressureLossPerMeter
            };
        }
    }
}
