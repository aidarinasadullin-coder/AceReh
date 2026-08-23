using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Статус одной канонической мутации теплового состояния (DEC-T02):
    /// <c>Changed | NoChange | Rejected</c>. Отменённый статус (<c>Cancelled</c>)
    /// не существует: у Thermal нет pre-apply сценария отмены.
    /// </summary>
    public enum ThermalMutationStatus
    {
        /// <summary>Изменение применено; выдано ровно одно каноническое завершение.</summary>
        Changed,

        /// <summary>Структурного изменения нет; ноль завершений.</summary>
        NoChange,

        /// <summary>Кандидат отклонён валидацией до замены; состояние атомарно не тронуто.</summary>
        Rejected
    }

    /// <summary>
    /// Поле входных данных, адресуемое одиночной правкой <see cref="ThermalInputEdit"/>.
    /// </summary>
    public enum ThermalInputField
    {
        Mode,
        SupplyTemperature,
        GroundTemperature,
        Pipe,
        PipeSpacing
    }

    /// <summary>
    /// Одиночная типизированная правка одного поля входных данных.
    /// Создаётся только фабричными методами.
    /// </summary>
    public sealed class ThermalInputEdit
    {
        public ThermalInputField Field { get; }
        public OperatingMode ModeValue { get; }
        public double DoubleValue { get; }
        public ThermalPipeSnapshot? PipeValue { get; }
        public int IntValue { get; }

        private ThermalInputEdit(
            ThermalInputField field,
            OperatingMode modeValue,
            double doubleValue,
            ThermalPipeSnapshot? pipeValue,
            int intValue)
        {
            Field = field;
            ModeValue = modeValue;
            DoubleValue = doubleValue;
            PipeValue = pipeValue;
            IntValue = intValue;
        }

        public static ThermalInputEdit ForMode(OperatingMode mode) =>
            new(ThermalInputField.Mode, mode, 0.0, null, 0);

        public static ThermalInputEdit ForSupplyTemperature(double value) =>
            new(ThermalInputField.SupplyTemperature, default, value, null, 0);

        public static ThermalInputEdit ForGroundTemperature(double value) =>
            new(ThermalInputField.GroundTemperature, default, value, null, 0);

        public static ThermalInputEdit ForPipe(ThermalPipeSnapshot? pipe) =>
            new(ThermalInputField.Pipe, default, 0.0, pipe, 0);

        public static ThermalInputEdit ForPipeSpacing(int spacing) =>
            new(ThermalInputField.PipeSpacing, default, 0.0, null, spacing);
    }

    /// <summary>
    /// Результат одной канонической мутации теплового состояния (DEC-T02):
    /// статус, источник и срезы состояния до/после.
    /// </summary>
    public sealed class ThermalMutationResult
    {
        public ThermalMutationStatus Status { get; }
        public ThermalMutationOrigin Origin { get; }
        public ThermalStateSnapshot Before { get; }
        public ThermalStateSnapshot After { get; }

        /// <summary>Ошибки валидации кандидата при <see cref="ThermalMutationStatus.Rejected"/>.</summary>
        public IReadOnlyList<string> Errors { get; }

        public bool IsChanged => Status == ThermalMutationStatus.Changed;
        public bool IsNoChange => Status == ThermalMutationStatus.NoChange;
        public bool IsRejected => Status == ThermalMutationStatus.Rejected;

        public ThermalMutationResult(
            ThermalMutationStatus status,
            ThermalMutationOrigin origin,
            ThermalStateSnapshot before,
            ThermalStateSnapshot after,
            IEnumerable<string>? errors = null)
        {
            Status = status;
            Origin = origin;
            Before = before ?? throw new ArgumentNullException(nameof(before));
            After = after ?? throw new ArgumentNullException(nameof(after));
            Errors = errors?.ToArray() ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Аргументы единственного канонического события завершения мутации:
    /// событие несёт полный результат мутации (статус всегда Changed).
    /// </summary>
    public sealed class ThermalStateChangedEventArgs : EventArgs
    {
        public ThermalMutationResult Mutation { get; }

        public ThermalStateChangedEventArgs(ThermalMutationResult mutation)
        {
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
        }
    }
}
