// ================================================================================
// REHAU Снеготаяние - Каноническая граница применения тепловых команд
// ================================================================================
//
// Phase 4 Todos 5+6+7 merged boundary (AMZ-1). Реализация DEC-T04A:
// - переводит пользовательские команды в замкнутый набор мутаций состояния;
//   changed user edit => одна ApplyInputEdit + один MarkDirty; no-op/rejected
//   => нулевой эффект;
// - оркестрирует расчёт в точном порядке DEC-T05, включая матрицу отказов;
// - владеет единственными upstream-подписками Climate/Construction и
//   транслирует их в InvalidateFromClimate/Construction ровно один раз,
//   только если эффект требуется (DEC-T04);
// - пользовательский/жизненный сброс сохраняет наблюдаемое поведение ST-013/
//   ST-015: каноническое состояние не мутируется, событий нет.
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Sealed-реализация <see cref="IThermalStateCoordinator"/>. Создаётся один раз
    /// на композицию DI либо изолированно legacy-конструктором адаптера.
    /// </summary>
    public sealed class ThermalStateCoordinator : IThermalStateCoordinator
    {
        /// <summary>Точное сообщение инвалидации от климата (замороженная формулировка).</summary>
        public const string ClimateInvalidationMessage = "Климатические данные изменены. Требуется пересчёт.";

        /// <summary>Точное сообщение инвалидации от конструкции (замороженная формулировка).</summary>
        public const string ConstructionInvalidationMessage = "Данные конструкции изменены. Требуется пересчёт.";

        private readonly IProjectSessionThermalState _state;
        private readonly CalculationContext _calculationContext;
        private readonly IMarkDirtyService _markDirtyService;
        private readonly IThermalCalculator _calculator;
        private readonly IClimateData _climateData;
        private readonly IConstructionData _constructionData;
        private readonly IValidator<ThermalInputs> _thermalValidator;
        private readonly IValidator<ThermalCalculationResult> _thermalResultValidator;

        private readonly ClimateData? _climateDataImpl;
        private readonly EventHandler<ClimateDataChangedEventArgs> _climateUpstreamHandler;
        private readonly EventHandler<ConstructionDataChangedEventArgs> _constructionUpstreamHandler;
        private bool _disposed;
        private bool _isCalculating;

        /// <summary>
        /// Создать координатор. Все зависимости обязательны; состояние должно быть
        /// reference-identical с <c>IProjectSession.ThermalState</c>.
        /// </summary>
        public ThermalStateCoordinator(
            IProjectSessionThermalState state,
            CalculationContext calculationContext,
            IMarkDirtyService markDirtyService,
            IThermalCalculator calculator,
            IClimateData climateData,
            IConstructionData constructionData,
            IValidator<ThermalInputs> thermalValidator,
            IValidator<ThermalCalculationResult> thermalResultValidator)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            _markDirtyService = markDirtyService ?? throw new ArgumentNullException(nameof(markDirtyService));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _climateData = climateData ?? throw new ArgumentNullException(nameof(climateData));
            _constructionData = constructionData ?? throw new ArgumentNullException(nameof(constructionData));
            _thermalValidator = thermalValidator ?? throw new ArgumentNullException(nameof(thermalValidator));
            _thermalResultValidator = thermalResultValidator ?? throw new ArgumentNullException(nameof(thermalResultValidator));

            // Единственные upstream-подписки приложения (перенесены из ThermalViewModel
            // атомарно, DEC-T04A). Совместимость: пользовательские изменения климата/
            // конструкции публикуются ровно на этих поверхностях; lifecycle-источники
            // их не поднимают (замерено characterization Phase 4 Todo 2).
            _climateUpstreamHandler = OnClimateUpstream;
            _constructionUpstreamHandler = OnConstructionUpstream;
            _climateDataImpl = _climateData as ClimateData;
            if (_climateDataImpl != null)
            {
                _climateDataImpl.DataChanged += _climateUpstreamHandler;
            }

            _constructionData.DataChanged += _constructionUpstreamHandler;
        }

        /// <inheritdoc />
        public IProjectSessionThermalState State => _state;

        /// <inheritdoc />
        public event EventHandler<ThermalStateChangedEventArgs>? Completion;

        /// <inheritdoc />
        public event EventHandler? UpstreamObserved;

        /// <inheritdoc />
        public bool IsCalculating => _isCalculating;

        /// <inheritdoc />
        public ThermalMutationResult ApplyInputEdit(ThermalInputEdit edit)
        {
            var mutation = _state.ApplyInputEdit(edit, ThermalMutationOrigin.User);

            // Ровно один dirty-intent на изменённую логическую правку пользователя;
            // no-op/rejected не создают намерений (DEC-T03).
            if (mutation.IsChanged)
            {
                _markDirtyService.MarkDirty();
                Completion?.Invoke(this, new ThermalStateChangedEventArgs(mutation));
            }

            return mutation;
        }

        /// <inheritdoc />
        public void Reset()
        {
            // Наследуемое поведение ST-013/ST-015: сброс адаптера не трогает
            // канонические значения статуса/шага/результата и не создаёт событий.
            // Каноническая замена происходит только через CalculateAsync/LoadResult.
        }

        /// <inheritdoc />
        public async Task<ThermalCalculationOutcome> CalculateAsync(ThermalInputs inputs)
        {
            if (_isCalculating)
            {
                // Реентерабельность DEC-T05: второй вызов во время расчёта — no-op.
                return new ThermalCalculationOutcome(null, string.Empty);
            }

            _isCalculating = true;
            try
            {
                // 3. BeginCalculation: фаза Calculating, сообщения очищены.
                Publish(_state.BeginCalculation());

                // 4. Публикация рассчитанных входов ровно один раз.
                _calculationContext.UpdateThermalInputs(inputs, "Thermal");

                // 5. Один вызов калькулятора (в фоне, как и прежде Task.Run в
                // ThermalViewModel: замороженный reentrancy-тест блокируется внутри
                // Calculate и требует возврата управления вызывающему потоку).
                ThermalCalculationResult result;
                try
                {
                    result = await Task.Run(() =>
                        _calculator.Calculate(inputs, _climateData, _constructionData));
                }
                catch (Exception ex)
                {
                    // Точный текст ошибки + нулевой результат + совместимая
                    // невалидная публикация контекста ровно один раз.
                    var failureMessage = $"Ошибка расчёта: {ex.Message}";
                    Publish(_state.FailCalculation(
                        ToInputsSnapshot(inputs),
                        failureMessage));
                    _calculationContext.UpdateThermal(
                        new ThermalCalculationResult
                        {
                            IsValid = false,
                            ValidationErrors = new[] { failureMessage }
                        },
                        "Thermal");
                    return new ThermalCalculationOutcome(null, failureMessage);
                }

                // 6. Сохранить канонический результат (валидный или нет) с собранным
                // сообщением пост-валидации.
                var validationMessage = ComposeResultValidationMessage(result);
                if (result != null)
                {
                    Publish(_state.CompleteCalculation(
                        ToInputsSnapshot(inputs),
                        ThermalResultSnapshot.FromResult(result)!,
                        validationMessage));

                    // 7. Публикация результата ровно один раз (включая невалидный).
                    _calculationContext.UpdateThermal(result, "Thermal");
                }
                else
                {
                    Publish(_state.FailCalculation(
                        ToInputsSnapshot(inputs),
                        validationMessage));
                }

                return new ThermalCalculationOutcome(result, validationMessage);
            }
            finally
            {
                _isCalculating = false;
            }
        }

        /// <inheritdoc />
        public void LoadResult(ThermalCalculationResult result, ThermalInputs inputs)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            // Восстановительный канонический писатель (DEC-T08): Restore нормализует
            // статус к дефолту без dirty; публикации идут в замороженном порядке:
            // сначала входы, затем результат.
            // Шаг укладки при финализации восстанавливается с канонического среза:
            // оркестратор применяет SetPipeSpacing ДО LoadResult, поэтому канон
            // авторитетнее возможного устаревшего эха адаптера (no-op SetPipeSpacing
            // не порождает эхо-события — замороженное поведение ST-015).
            var candidate = ToInputsSnapshot(inputs);
            var canonicalSpacing = _state.Snapshot.Inputs.PipeSpacing;
            if (candidate.PipeSpacing != canonicalSpacing)
            {
                candidate = new ThermalInputsSnapshot(
                    candidate.Mode,
                    candidate.SupplyTemperature,
                    candidate.GroundTemperature,
                    candidate.Pipe,
                    canonicalSpacing);
            }

            Publish(_state.Restore(
                candidate,
                ThermalResultSnapshot.FromResult(result)));
            _calculationContext.UpdateThermalInputs(inputs, "Thermal");
            _calculationContext.UpdateThermal(result, "Thermal");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_climateDataImpl != null)
            {
                _climateDataImpl.DataChanged -= _climateUpstreamHandler;
            }

            _constructionData.DataChanged -= _constructionUpstreamHandler;
        }

        private void OnClimateUpstream(object? sender, ClimateDataChangedEventArgs e)
        {
            // Инвалидация ровно один раз, только если эффект требуется
            // (результат существовал); иначе состояние вернёт NoChange — тихо.
            var mutation = _state.InvalidateFromClimate(ClimateInvalidationMessage);
            UpstreamObserved?.Invoke(this, EventArgs.Empty);
            if (mutation.IsChanged)
            {
                Completion?.Invoke(this, new ThermalStateChangedEventArgs(mutation));
            }
        }

        private void OnConstructionUpstream(object? sender, ConstructionDataChangedEventArgs e)
        {
            var mutation = _state.InvalidateFromConstruction(ConstructionInvalidationMessage);
            UpstreamObserved?.Invoke(this, EventArgs.Empty);
            if (mutation.IsChanged)
            {
                Completion?.Invoke(this, new ThermalStateChangedEventArgs(mutation));
            }
        }

        private void Publish(ThermalMutationResult mutation)
        {
            if (mutation.IsChanged)
            {
                Completion?.Invoke(this, new ThermalStateChangedEventArgs(mutation));
            }
        }

        private string ComposeResultValidationMessage(ThermalCalculationResult? result)
        {
            var messages = new List<string>();
            var resultValidation = result != null
                ? _thermalResultValidator.Validate(result)
                : ValidationResult.Success();

            if (result != null && !result.IsValid && result.ValidationErrors.Length > 0)
            {
                messages.AddRange(result.ValidationErrors);
            }

            if (!resultValidation.IsValid)
            {
                messages.AddRange(resultValidation.Errors.Select(error => error.Message));
            }

            return string.Join("; ", messages);
        }

        private static ThermalInputsSnapshot ToInputsSnapshot(ThermalInputs inputs)
        {
            return new ThermalInputsSnapshot(
                inputs.Mode,
                inputs.SupplyTemperature,
                inputs.GroundTemperature,
                ThermalPipeSnapshot.FromPipeType(inputs.Pipe),
                (int)inputs.PipeSpacing);
        }
    }
}
