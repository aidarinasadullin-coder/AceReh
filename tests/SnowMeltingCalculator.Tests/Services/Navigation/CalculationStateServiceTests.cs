// ================================================================================
// REHAU Снеготаяние - Тесты для CalculationStateService
// ================================================================================

using NUnit.Framework;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Navigation
{
    /// <summary>
    /// Тесты для CalculationStateService
    /// </summary>
    [TestFixture]
    public class CalculationStateServiceTests
    {
        private CalculationStateService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new CalculationStateService();
        }

        #region Тепловой расчёт - Начальное состояние

        [Test]
        public void ThermalNeedsRecalculation_Initially_False()
        {
            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
        }

        [Test]
        public void ThermalIsCalculating_Initially_False()
        {
            // Assert
            Assert.That(_service.ThermalIsCalculating, Is.False);
        }

        [Test]
        public void ThermalValidationMessage_Initially_Empty()
        {
            // Assert
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo(string.Empty));
        }

        #endregion

        #region Тепловой расчёт - SetThermalNeedsRecalculation

        [Test]
        public void SetThermalNeedsRecalculation_SetsFlagToTrue()
        {
            // Act
            _service.SetThermalNeedsRecalculation("Test message");

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.True);
        }

        [Test]
        public void SetThermalNeedsRecalculation_SetsMessage()
        {
            // Act
            _service.SetThermalNeedsRecalculation("Test message");

            // Assert
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo("Test message"));
        }

        [Test]
        public void SetThermalNeedsRecalculation_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.SetThermalNeedsRecalculation("Test message");

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Thermal"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.NeedsRecalculation));
            Assert.That(eventArgs.Message, Is.EqualTo("Test message"));
        }

        #endregion

        #region Тепловой расчёт - SetThermalCalculating

        [Test]
        public void SetThermalCalculating_SetsIsCalculatingToTrue()
        {
            // Act
            _service.SetThermalCalculating();

            // Assert
            Assert.That(_service.ThermalIsCalculating, Is.True);
        }

        [Test]
        public void SetThermalCalculating_ResetsNeedsRecalculation()
        {
            // Arrange
            _service.SetThermalNeedsRecalculation("Test message");

            // Act
            _service.SetThermalCalculating();

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
        }

        [Test]
        public void SetThermalCalculating_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.SetThermalCalculating();

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Thermal"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.Calculating));
            Assert.That(eventArgs.Message, Is.Null);
        }

        #endregion

        #region Тепловой расчёт - ResetThermalState

        [Test]
        public void ResetThermalState_ResetsIsCalculating()
        {
            // Arrange
            _service.SetThermalCalculating();

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(_service.ThermalIsCalculating, Is.False);
        }

        [Test]
        public void ResetThermalState_ResetsNeedsRecalculation()
        {
            // Arrange
            _service.SetThermalNeedsRecalculation("Test message");

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
        }

        [Test]
        public void ResetThermalState_ClearsMessage()
        {
            // Arrange
            _service.SetThermalNeedsRecalculation("Test message");

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ResetThermalState_RaisesStateChangedEvent()
        {
            // Arrange: статус сначала уводится из дефолта, чтобы нормализация к
            // Actual дала ровно одно каноническое завершение (мост AMZ-1).
            _service.SetThermalNeedsRecalculation("Test message");
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Thermal"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.Actual));
            Assert.That(eventArgs.Message, Is.Null);
        }

        [Test]
        public void ResetThermalState_WhenAlreadyActual_IsIdempotentlySilent()
        {
            // Arrange
            var eventCount = 0;
            _service.StateChanged += (_, _) => eventCount++;

            // Act
            _service.ResetThermalState();

            // Assert: NoChange-завершения не транслируются в события.
            Assert.That(eventCount, Is.Zero);
        }

        #endregion

        #region Тепловой расчёт - Каноническое делегирование (AMZ-1)

        [Test]
        public void Getters_MapLiveCanonicalSnapshot()
        {
            var session = new ProjectSession();
            var service = new CalculationStateService(session);

            Assert.Multiple(() =>
            {
                Assert.That(service.ThermalNeedsRecalculation, Is.False);
                Assert.That(service.ThermalValidationMessage, Is.Empty);
                Assert.That(service.PipeSpacing, Is.EqualTo(200));
            });

            session.ThermalState.ApplyInputEdit(
                ThermalInputEdit.ForPipeSpacing(250), ThermalMutationOrigin.User);

            Assert.That(service.PipeSpacing, Is.EqualTo(250),
                "PipeSpacing getter reads the live canonical inputs snapshot.");

            session.ThermalState.Restore(
                new ThermalInputsSnapshot(OperatingMode.Melting, 50.0, 10.0, null, 250),
                ThermalResultSnapshot.FromResult(new ThermalCalculationResult { PowerTotal = 5.0, IsValid = true }));

            Assert.Multiple(() =>
            {
                Assert.That(service.ThermalNeedsRecalculation, Is.False,
                    "Restore normalizes the canonical phase to Actual.");
                Assert.That(service.PipeSpacing, Is.EqualTo(250));
            });

            session.ThermalState.InvalidateFromClimate("Климатические данные изменены. Требуется пересчёт.");

            Assert.Multiple(() =>
            {
                Assert.That(service.ThermalNeedsRecalculation, Is.True);
                Assert.That(service.ThermalValidationMessage,
                    Is.EqualTo("Климатические данные изменены. Требуется пересчёт."));
            });
        }

        [Test]
        public void CanonicalCompletion_TranslatesExactlyOncePerLegacySurface()
        {
            var session = new ProjectSession();
            var service = new CalculationStateService(session);
            var stateEvents = 0;
            var spacingEvents = 0;
            ModuleStateChangedEventArgs? last = null;
            service.StateChanged += (_, args) => { stateEvents++; last = args; };
            service.PipeSpacingChanged += (_, _) => spacingEvents++;

            // Правка шага с существующим результатом: ровно одно PipeSpacingChanged
            // и ровно одно StateChanged(NeedsRecalculation, точное сообщение).
            session.ThermalState.Restore(
                new ThermalInputsSnapshot(OperatingMode.Melting, 50.0, 10.0, null, 200),
                ThermalResultSnapshot.FromResult(new ThermalCalculationResult { PowerTotal = 9.0, IsValid = true }));
            stateEvents = 0;

            session.ThermalState.ApplyInputEdit(
                ThermalInputEdit.ForPipeSpacing(300), ThermalMutationOrigin.User);

            Assert.Multiple(() =>
            {
                Assert.That(spacingEvents, Is.EqualTo(1));
                Assert.That(stateEvents, Is.EqualTo(1));
                Assert.That(last!.State, Is.EqualTo(ModuleState.NeedsRecalculation));
                Assert.That(last.Message, Is.EqualTo("Шаг укладки изменён. Требуется пересчёт."));
            });

            // Правка собственного поля без результата не синтезирует статус:
            // ноль StateChanged (замороженная строка characterization OwnInputEdit_
            // ChangedWithoutResult_MarksDirtyOnceWithoutRecalculationEvent).
            var freshSession = new ProjectSession();
            var freshService = new CalculationStateService(freshSession);
            var freshEvents = 0;
            freshService.StateChanged += (_, _) => freshEvents++;

            freshSession.ThermalState.ApplyInputEdit(
                ThermalInputEdit.ForSupplyTemperature(60.0), ThermalMutationOrigin.User);

            Assert.That(freshEvents, Is.Zero);
        }

        [Test]
        public void SetPipeSpacing_CanonicalDelegation_NoOpSilent_GuardPreserved()
        {
            var session = new ProjectSession();
            var service = new CalculationStateService(session);
            var spacingEvents = 0;
            var stateEvents = 0;
            service.PipeSpacingChanged += (_, _) => spacingEvents++;
            service.StateChanged += (_, args) =>
            {
                if (args.Module == "Thermal")
                {
                    stateEvents++;
                }
            };

            service.SetPipeSpacing(150, "ThermalViewModel");

            Assert.Multiple(() =>
            {
                Assert.That(service.PipeSpacing, Is.EqualTo(150),
                    "SetPipeSpacing delegates to the canonical inputs snapshot.");
                Assert.That(spacingEvents, Is.EqualTo(1));
                Assert.That(stateEvents, Is.Zero,
                    "Compatibility spacing writer never synthesizes a status event.");
                Assert.That(session.IsDirty, Is.False,
                    "Compatibility spacing writer never marks dirty.");
            });

            // Повтор то же значения — no-op, ноль событий (refresh-idempotence).
            service.SetPipeSpacing(150, "ThermalViewModel");
            Assert.That(spacingEvents, Is.EqualTo(1));

            // Guard сохранён.
            Assert.Throws<InvalidOperationException>(
                () => service.SetPipeSpacing(150, "rogue source"));
        }

        [Test]
        public void ParameterlessConstructor_CreatesIsolatedConsistentSession()
        {
            var shared = new ProjectSession();
            var serviceA = new CalculationStateService(shared);
            var serviceB = new CalculationStateService(shared);
            var isolated = new CalculationStateService();

            serviceA.SetThermalNeedsRecalculation("только A");

            Assert.Multiple(() =>
            {
                Assert.That(serviceB.ThermalNeedsRecalculation, Is.True,
                    "Services sharing one session observe one canonical state.");
                Assert.That(isolated.ThermalNeedsRecalculation, Is.False,
                    "Parameterless service owns an isolated session.");
                Assert.That(isolated.PipeSpacing, Is.EqualTo(200));
            });
        }

        #endregion

        #region Гидравлический расчёт - Начальное состояние

        [Test]
        public void HydraulicsIsCalculating_Initially_False()
        {
            // Assert
            Assert.That(_service.HydraulicsIsCalculating, Is.False);
        }

        #endregion

        #region Гидравлический расчёт - SetHydraulicsCalculating

        [Test]
        public void SetHydraulicsCalculating_SetsFlagToTrue()
        {
            // Act
            _service.SetHydraulicsCalculating();

            // Assert
            Assert.That(_service.HydraulicsIsCalculating, Is.True);
        }

        [Test]
        public void SetHydraulicsCalculating_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.SetHydraulicsCalculating();

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Hydraulics"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.Calculating));
            Assert.That(eventArgs.Message, Is.Null);
        }

        #endregion

        #region Гидравлический расчёт - SetHydraulicsError

        [Test]
        public void SetHydraulicsError_OutsideCalculation_IsRejectedWithoutChangingMessage()
        {
            // Act
            _service.SetHydraulicsError("test");

            // Assert
            Assert.That(_service.HydraulicsValidationMessage, Is.EqualTo(string.Empty));
        }

        [Test]
        public void SetHydraulicsError_ResetsIsCalculating()
        {
            // Arrange - переводим в Calculating
            _service.SetHydraulicsCalculating();
            Assert.That(_service.HydraulicsIsCalculating, Is.True);

            // Act
            _service.SetHydraulicsError("test");

            // Assert - _hydraulicsIsCalculating должен быть сброшен защитно
            Assert.That(_service.HydraulicsIsCalculating, Is.False);
        }

        [Test]
        public void SetHydraulicsError_OutsideCalculation_DoesNotRaiseStateChanged()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.SetHydraulicsError("test");

            // Assert
            Assert.That(eventArgs, Is.Null);
        }

        [Test]
        public void SetHydraulicsError_OutsideCalculation_LeavesStateAndEventsUnchanged()
        {
            // Arrange - подписка на событие + подсчёт количества вызовов
            ModuleStateChangedEventArgs? eventArgs = null;
            var eventCount = 0;
            _service.StateChanged += (sender, args) =>
            {
                eventCount++;
                eventArgs = args;
            };

            // Act
            _service.SetHydraulicsError("test");

            Assert.That(_service.HydraulicsValidationMessage, Is.EqualTo(string.Empty));
            Assert.That(eventCount, Is.Zero);
            Assert.That(eventArgs, Is.Null);
        }

        [Test]
        public void SetHydraulicsError_DoesNotTouchThermalState()
        {
            // Arrange
            ModuleStateChangedEventArgs? lastEventArgs = null;
            _service.StateChanged += (sender, args) => lastEventArgs = args;

            // Act
            _service.SetHydraulicsError("test");

            // Assert - ThermalValidationMessage остался пустым, ThermalNeedsRecalculation не выставлен
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo(string.Empty));
            Assert.That(_service.ThermalIsCalculating, Is.False);
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
            Assert.That(lastEventArgs, Is.Null);
        }

        #endregion

        #region Гидравлический расчёт - ResetHydraulicsState

        [Test]
        public void ResetHydraulicsState_ResetsIsCalculating()
        {
            // Arrange
            _service.SetHydraulicsCalculating();

            // Act
            _service.ResetHydraulicsState();

            // Assert
            Assert.That(_service.HydraulicsIsCalculating, Is.False);
        }

        [Test]
        public void ResetHydraulicsState_WhenInputsAreAlreadyActual_DoesNotRaiseStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.ResetHydraulicsState();

            // Assert
            Assert.That(eventArgs, Is.Null);
        }

        #endregion

        #region Интеграционные тесты

        [Test]
        public void ThermalAndHydraulics_States_AreIndependent()
        {
            // Act
            _service.SetThermalNeedsRecalculation("Thermal needs recalc");
            _service.SetHydraulicsCalculating();

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.True);
            Assert.That(_service.ThermalIsCalculating, Is.False);
            Assert.That(_service.HydraulicsIsCalculating, Is.True);
        }

        [Test]
        public void FullWorkflow_Thermal()
        {
            // Arrange
            var eventCount = 0;
            _service.StateChanged += (sender, args) => eventCount++;

            // Act & Assert - Initial state
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
            Assert.That(_service.ThermalIsCalculating, Is.False);

            // Act - Set needs recalculation
            _service.SetThermalNeedsRecalculation("Parameters changed");
            Assert.That(_service.ThermalNeedsRecalculation, Is.True);
            Assert.That(_service.ThermalIsCalculating, Is.False);
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo("Parameters changed"));

            // Act - Start calculating
            _service.SetThermalCalculating();
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
            Assert.That(_service.ThermalIsCalculating, Is.True);

            // Act - Reset
            _service.ResetThermalState();
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
            Assert.That(_service.ThermalIsCalculating, Is.False);
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo(string.Empty));

            // Assert - Event raised 3 times
            Assert.That(eventCount, Is.EqualTo(3));
        }

        [Test]
        public void FullWorkflow_Hydraulics()
        {
            // Arrange
            var eventCount = 0;
            _service.StateChanged += (sender, args) => eventCount++;

            // Act & Assert - Initial state
            Assert.That(_service.HydraulicsIsCalculating, Is.False);

            // Act - Start calculating
            _service.SetHydraulicsCalculating();
            Assert.That(_service.HydraulicsIsCalculating, Is.True);

            // Act - Reset
            _service.ResetHydraulicsState();
            Assert.That(_service.HydraulicsIsCalculating, Is.False);

            // Assert - Event raised 2 times
            Assert.That(eventCount, Is.EqualTo(2));
        }

        #endregion
    }
}
