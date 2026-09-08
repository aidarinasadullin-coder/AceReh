using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Core.Constants;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Канонический владелец теплового состояния проекта (DEC-T01/T02).
    /// Создаётся и хранится <c>ProjectSession</c>; в DI не регистрируется.
    /// Все мутации атомарны: валидация кандидата завершается до замены,
    /// Changed порождает ровно одно каноническое завершение после замены,
    /// NoChange/Rejected не порождают событий и не меняют состояние.
    /// Класс не публикует контекст расчёта и не вызывает dirty-сервис:
    /// подключение потребителей выполняется в Todos 4-8.
    /// </summary>
    public sealed class ProjectSessionThermalState : IProjectSessionThermalState
    {
        // Точные русские формулировки причин пересчёта — посимвольно из
        // охарактеризованного поведения ThermalViewModel (Todo 2 receipt).
        private const string ModeChangedMessage = "Режим работы изменён. Требуется пересчёт.";
        private const string SupplyTemperatureChangedMessage = "Температура подачи изменена. Требуется пересчёт.";
        private const string GroundTemperatureChangedMessage = "Температура грунта изменена. Требуется пересчёт.";
        private const string PipeChangedMessage = "Тип трубы изменён. Требуется пересчёт.";
        private const string PipeSpacingChangedMessage = "Шаг укладки изменён. Требуется пересчёт.";

        private ThermalInputsSnapshot _inputs = ThermalInputsSnapshot.Default;
        private ThermalResultSnapshot? _result;
        private ThermalStatusSnapshot _status = ThermalStatusSnapshot.Default;

        /// <inheritdoc/>
        public ThermalStateSnapshot Snapshot => new(_inputs, _result, _status);

        /// <inheritdoc/>
        public event EventHandler<ThermalStateChangedEventArgs>? Changed;

        /// <inheritdoc/>
        public ThermalMutationResult ApplyInputs(ThermalInputsSnapshot candidate, ThermalMutationOrigin origin)
        {
            if (candidate is null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            var before = Snapshot;

            var errors = ValidateInputs(candidate);
            if (errors.Count > 0)
            {
                return Reject(origin, before, errors);
            }

            var status = ResolveStatusForAppliedInputs(origin, before, candidate, ResolveFirstChangedField(before.Inputs, candidate));
            return Commit(before, origin, candidate, before.Result, status);
        }

        /// <inheritdoc/>
        public ThermalMutationResult ApplyInputEdit(ThermalInputEdit edit, ThermalMutationOrigin origin)
        {
            if (edit is null)
            {
                throw new ArgumentNullException(nameof(edit));
            }

            var before = Snapshot;
            var candidate = BuildCandidate(before.Inputs, edit);

            var errors = ValidateInputs(candidate);
            if (errors.Count > 0)
            {
                return Reject(origin, before, errors);
            }

            var status = ResolveStatusForAppliedInputs(origin, before, candidate, edit.Field);
            return Commit(before, origin, candidate, before.Result, status);
        }

        /// <inheritdoc/>
        public ThermalMutationResult ResetToDefaults(ThermalMutationOrigin origin)
        {
            var before = Snapshot;
            return Commit(before, origin, ThermalInputsSnapshot.Default, null, ThermalStatusSnapshot.Default);
        }

        /// <inheritdoc/>
        public ThermalMutationResult BeginCalculation()
        {
            var before = Snapshot;
            var status = new ThermalStatusSnapshot(
                ThermalCalculationPhase.Calculating,
                string.Empty,
                string.Empty);
            return Commit(before, ThermalMutationOrigin.Calculation, before.Inputs, before.Result, status);
        }

        /// <inheritdoc/>
        public ThermalMutationResult CompleteCalculation(
            ThermalInputsSnapshot calculatedInputs,
            ThermalResultSnapshot result,
            string validationMessage)
        {
            if (calculatedInputs is null)
            {
                throw new ArgumentNullException(nameof(calculatedInputs));
            }

            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var before = Snapshot;
            var status = new ThermalStatusSnapshot(
                ThermalCalculationPhase.Actual,
                string.Empty,
                validationMessage ?? string.Empty);
            return Commit(before, ThermalMutationOrigin.Calculation, calculatedInputs, result, status);
        }

        /// <inheritdoc/>
        public ThermalMutationResult FailCalculation(
            ThermalInputsSnapshot calculatedInputs,
            string validationMessage,
            ThermalResultSnapshot? compatibilityInvalidResult = null)
        {
            if (calculatedInputs is null)
            {
                throw new ArgumentNullException(nameof(calculatedInputs));
            }

            var before = Snapshot;
            var status = new ThermalStatusSnapshot(
                ThermalCalculationPhase.Actual,
                string.Empty,
                validationMessage ?? string.Empty);
            return Commit(before, ThermalMutationOrigin.Calculation, calculatedInputs, compatibilityInvalidResult, status);
        }

        /// <inheritdoc/>
        public ThermalMutationResult Restore(ThermalInputsSnapshot inputs, ThermalResultSnapshot? savedResult)
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            var before = Snapshot;

            // Инвариант канонических входов един для всех путей записи: повреждённые
            // сохранённые данные не отравляют состояние (отклоняются атомарно).
            var errors = ValidateInputs(inputs);
            if (errors.Count > 0)
            {
                return Reject(ThermalMutationOrigin.ProjectLoad, before, errors);
            }

            return Commit(before, ThermalMutationOrigin.ProjectLoad, inputs, savedResult, ThermalStatusSnapshot.Default);
        }

        /// <inheritdoc/>
        public ThermalMutationResult RestoreState(ThermalStateSnapshot snapshot, ThermalMutationOrigin origin)
        {
            // ADR-014: метод существует только для Undo/Redo-применения снимков
            // дневника отмены; другой origin — ошибка программирования.
            if (origin is not (ThermalMutationOrigin.Undo or ThermalMutationOrigin.Redo))
            {
                throw new ArgumentOutOfRangeException(nameof(origin), origin, "RestoreState accepts only Undo/Redo origins.");
            }

            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var before = Snapshot;

            // Инвариант канонических входов един для всех путей записи.
            var errors = ValidateInputs(snapshot.Inputs);
            if (errors.Count > 0)
            {
                return Reject(origin, before, errors);
            }

            // Статус берётся ИЗ снимка (может быть NeedsRecalculation
            // с сообщением — откат правки входов при существовавшем
            // результате); НЕ нормализуется к Default (план, ревью P2-4).
            return Commit(before, origin, snapshot.Inputs, snapshot.Result, snapshot.Status);
        }

        /// <inheritdoc/>
        public ThermalMutationResult InvalidateFromClimate(string message)
        {
            return InvalidateFromUpstream(message, ThermalMutationOrigin.ClimateInvalidation);
        }

        /// <inheritdoc/>
        public ThermalMutationResult InvalidateFromConstruction(string message)
        {
            return InvalidateFromUpstream(message, ThermalMutationOrigin.ConstructionInvalidation);
        }

        /// <inheritdoc/>
        public ThermalMutationResult ApplyNeedsRecalculation(string recalculationMessage, ThermalMutationOrigin origin)
        {
            var before = Snapshot;

            // Мост AMZ-1: входы и результат сохраняются, меняется только статус.
            var status = new ThermalStatusSnapshot(
                ThermalCalculationPhase.NeedsRecalculation,
                recalculationMessage ?? string.Empty,
                before.Status.ValidationMessage);
            var after = new ThermalStateSnapshot(before.Inputs, before.Result, status);

            if (after.Equals(before))
            {
                return new ThermalMutationResult(ThermalMutationStatus.NoChange, origin, before, before);
            }

            _inputs = after.Inputs;
            _result = after.Result;
            _status = status;

            var mutation = new ThermalMutationResult(ThermalMutationStatus.Changed, origin, before, after);
            Changed?.Invoke(this, new ThermalStateChangedEventArgs(mutation));
            return mutation;
        }

        private ThermalMutationResult InvalidateFromUpstream(string message, ThermalMutationOrigin origin)
        {
            var before = Snapshot;

            // Замороженное поведение DEC-T04/характеристика Todo 2 (#14-17):
            // без существующего результата upstream-инвалидация не даёт НИКАКОГО
            // эффекта, включая установку сообщения.
            if (before.Result is null)
            {
                return new ThermalMutationResult(
                    ThermalMutationStatus.NoChange,
                    origin,
                    before,
                    before);
            }

            var status = new ThermalStatusSnapshot(
                ThermalCalculationPhase.NeedsRecalculation,
                message ?? string.Empty,
                before.Status.ValidationMessage);
            return Commit(before, origin, before.Inputs, null, status);
        }

        private ThermalStatusSnapshot ResolveStatusForAppliedInputs(
            ThermalMutationOrigin origin,
            ThermalStateSnapshot before,
            ThermalInputsSnapshot candidate,
            ThermalInputField changedField)
        {
            var inputsChanged = !candidate.Equals(before.Inputs);

            // DEC-T03: правка собственных входов при наличии результата сохраняет
            // последний результат и переводит статус в NeedsRecalculation с точной
            // русской формулировкой причины. Без результата ничего не синтезируется.
            if (origin == ThermalMutationOrigin.User && inputsChanged && before.Result is not null)
            {
                return new ThermalStatusSnapshot(
                    ThermalCalculationPhase.NeedsRecalculation,
                    ResolveChangedFieldMessage(changedField),
                    before.Status.ValidationMessage);
            }

            // Жизненный цикл и системные применения дают чистую базовую линию
            // статуса без пользовательских dirty-последствий (DEC-T03).
            if (NormalizesStatusToActual(origin))
            {
                return ThermalStatusSnapshot.Default;
            }

            return before.Status;
        }

        private ThermalMutationResult Commit(
            ThermalStateSnapshot before,
            ThermalMutationOrigin origin,
            ThermalInputsSnapshot inputs,
            ThermalResultSnapshot? result,
            ThermalStatusSnapshot status)
        {
            var after = new ThermalStateSnapshot(inputs, result, status);

            if (after.Equals(before))
            {
                return new ThermalMutationResult(ThermalMutationStatus.NoChange, origin, before, before);
            }

            _inputs = inputs;
            _result = result;
            _status = status;

            var mutation = new ThermalMutationResult(ThermalMutationStatus.Changed, origin, before, after);
            Changed?.Invoke(this, new ThermalStateChangedEventArgs(mutation));
            return mutation;
        }

        private static ThermalMutationResult Reject(
            ThermalMutationOrigin origin,
            ThermalStateSnapshot before,
            List<string> errors)
        {
            // Атомарность: Before и After — один и тот же экземпляр среза;
            // состояние не тронуто, событий нет.
            return new ThermalMutationResult(ThermalMutationStatus.Rejected, origin, before, before, errors);
        }

        private static bool NormalizesStatusToActual(ThermalMutationOrigin origin)
        {
            return origin == ThermalMutationOrigin.UserReset
                || origin == ThermalMutationOrigin.ProjectLoadReset
                || origin == ThermalMutationOrigin.ProjectLoad
                || origin == ThermalMutationOrigin.Initialization
                || origin == ThermalMutationOrigin.SystemApply;
        }

        private static ThermalInputsSnapshot BuildCandidate(ThermalInputsSnapshot current, ThermalInputEdit edit)
        {
            switch (edit.Field)
            {
                case ThermalInputField.Mode:
                    return new ThermalInputsSnapshot(edit.ModeValue, current.SupplyTemperature, current.GroundTemperature, current.Pipe, current.PipeSpacing);
                case ThermalInputField.SupplyTemperature:
                    return new ThermalInputsSnapshot(current.Mode, edit.DoubleValue, current.GroundTemperature, current.Pipe, current.PipeSpacing);
                case ThermalInputField.GroundTemperature:
                    return new ThermalInputsSnapshot(current.Mode, current.SupplyTemperature, edit.DoubleValue, current.Pipe, current.PipeSpacing);
                case ThermalInputField.Pipe:
                    return new ThermalInputsSnapshot(current.Mode, current.SupplyTemperature, current.GroundTemperature, edit.PipeValue, current.PipeSpacing);
                case ThermalInputField.PipeSpacing:
                    return new ThermalInputsSnapshot(current.Mode, current.SupplyTemperature, current.GroundTemperature, current.Pipe, edit.IntValue);
                default:
                    throw new ArgumentOutOfRangeException(nameof(edit), edit.Field, "Unknown thermal input field.");
            }
        }

        private static ThermalInputField ResolveFirstChangedField(ThermalInputsSnapshot current, ThermalInputsSnapshot candidate)
        {
            if (!candidate.Mode.Equals(current.Mode))
            {
                return ThermalInputField.Mode;
            }

            if (!candidate.SupplyTemperature.Equals(current.SupplyTemperature))
            {
                return ThermalInputField.SupplyTemperature;
            }

            if (!candidate.GroundTemperature.Equals(current.GroundTemperature))
            {
                return ThermalInputField.GroundTemperature;
            }

            var pipeChanged = !(candidate.Pipe is null
                ? current.Pipe is null
                : candidate.Pipe.Equals(current.Pipe));
            if (pipeChanged)
            {
                return ThermalInputField.Pipe;
            }

            return ThermalInputField.PipeSpacing;
        }

        private static string ResolveChangedFieldMessage(ThermalInputField field)
        {
            switch (field)
            {
                case ThermalInputField.Mode:
                    return ModeChangedMessage;
                case ThermalInputField.SupplyTemperature:
                    return SupplyTemperatureChangedMessage;
                case ThermalInputField.GroundTemperature:
                    return GroundTemperatureChangedMessage;
                case ThermalInputField.Pipe:
                    return PipeChangedMessage;
                case ThermalInputField.PipeSpacing:
                    return PipeSpacingChangedMessage;
                default:
                    throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown thermal input field.");
            }
        }

        private static List<string> ValidateInputs(ThermalInputsSnapshot candidate)
        {
            var errors = new List<string>();

            if (!Enum.IsDefined(candidate.Mode))
            {
                errors.Add("OperatingMode must be a defined value.");
            }

            if (double.IsNaN(candidate.SupplyTemperature)
                || candidate.SupplyTemperature < ValidationConstants.MinSupplyTemperature
                || candidate.SupplyTemperature > ValidationConstants.MaxSupplyTemperature)
            {
                errors.Add($"SupplyTemperature must be between {ValidationConstants.MinSupplyTemperature} and {ValidationConstants.MaxSupplyTemperature}.");
            }

            if (double.IsNaN(candidate.GroundTemperature)
                || candidate.GroundTemperature < ValidationConstants.MinGroundTemperature
                || candidate.GroundTemperature > ValidationConstants.MaxGroundTemperature)
            {
                errors.Add($"GroundTemperature must be between {ValidationConstants.MinGroundTemperature} and {ValidationConstants.MaxGroundTemperature}.");
            }

            if (candidate.PipeSpacing < ValidationConstants.MinPipeSpacing
                || candidate.PipeSpacing > ValidationConstants.MaxPipeSpacing)
            {
                errors.Add($"PipeSpacing must be between {ValidationConstants.MinPipeSpacing} and {ValidationConstants.MaxPipeSpacing}.");
            }

            return errors;
        }
    }
}
