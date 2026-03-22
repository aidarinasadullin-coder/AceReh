using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    /// <summary>
    /// Тесты для CircuitRow - сценарии ввода длины и площади контура
    /// </summary>
    [TestFixture]
    public class CircuitRowTests
    {
        #region Тесты базовых свойств

        [Test]
        public void Constructor_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var circuit = new CircuitRow();

            // Assert
            Assert.That(circuit.CircuitLength, Is.EqualTo(0));
            Assert.That(circuit.CircuitArea, Is.EqualTo(0));
            Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(20.0));
            Assert.That(circuit.IsLengthUserInput, Is.False);
            Assert.That(circuit.IsAreaUserInput, Is.False);
            Assert.That(circuit.IsLengthReadOnly, Is.False);
            Assert.That(circuit.IsAreaReadOnly, Is.False);
        }

        #endregion

        #region Сценарий 2.1: Пользователь ввёл длину

        [Test]
        public void OnCircuitLengthChanged_WhenUserEntersLength_AreaIsCalculated()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };

            // Act - пользователь вводит длину 100 м
            circuit.CircuitLength = 100;

            // Assert
            Assert.That(circuit.IsLengthUserInput, Is.True, "Должен быть установлен флаг IsLengthUserInput");
            Assert.That(circuit.IsAreaUserInput, Is.False, "Флаг IsAreaUserInput должен быть сброшен");
            
            // Формула: S = L / (100 / VA_hk) = 100 / (100 / 20) = 100 / 5 = 20 м²
            Assert.That(circuit.CircuitArea, Is.EqualTo(20.0).Within(0.01), "Площадь должна быть вычислена");
            
            // Поле площади должно быть заблокировано
            Assert.That(circuit.IsAreaReadOnly, Is.True, "Поле площади должно быть заблокировано");
            Assert.That(circuit.IsLengthReadOnly, Is.False, "Поле длины должно быть активно");
        }

        [Test]
        public void OnCircuitLengthChanged_WithDifferentPipeSpacing_AreaIsCorrect()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 25.0 };

            // Act - пользователь вводит длину 100 м
            circuit.CircuitLength = 100;

            // Assert
            // Формула: S = L / (100 / VA_hk) = 100 / (100 / 25) = 100 / 4 = 25 м²
            Assert.That(circuit.CircuitArea, Is.EqualTo(25.0).Within(0.01));
        }

        [Test]
        public void OnCircuitLengthChanged_WhenUserEntersLengthMultipleTimes_AreaIsRecalculated()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };

            // Act - пользователь вводит длину несколько раз
            circuit.CircuitLength = 100;
            var firstArea = circuit.CircuitArea;
            
            circuit.CircuitLength = 150;

            // Assert
            Assert.That(circuit.IsLengthUserInput, Is.True);
            Assert.That(circuit.CircuitArea, Is.EqualTo(30.0).Within(0.01));
            Assert.That(circuit.CircuitArea, Is.Not.EqualTo(firstArea));
        }

        #endregion

        #region Сценарий 2.2: Пользователь ввёл площадь

        [Test]
        public void OnCircuitAreaChanged_WhenUserEntersArea_LengthIsCalculated()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };

            // Act - пользователь вводит площадь 20 м²
            circuit.CircuitArea = 20;

            // Assert
            Assert.That(circuit.IsAreaUserInput, Is.True, "Должен быть установлен флаг IsAreaUserInput");
            Assert.That(circuit.IsLengthUserInput, Is.False, "Флаг IsLengthUserInput должен быть сброшен");
            
            // Формула: L = S × (100 / VA_hk) = 20 × (100 / 20) = 20 × 5 = 100 м
            Assert.That(circuit.CircuitLength, Is.EqualTo(100.0).Within(0.01), "Длина должна быть вычислена");
            
            // Поле длины должно быть заблокировано
            Assert.That(circuit.IsLengthReadOnly, Is.True, "Поле длины должно быть заблокировано");
            Assert.That(circuit.IsAreaReadOnly, Is.False, "Поле площади должно быть активно");
        }

        [Test]
        public void OnCircuitAreaChanged_WithDifferentPipeSpacing_LengthIsCorrect()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 25.0 };

            // Act - пользователь вводит площадь 25 м²
            circuit.CircuitArea = 25;

            // Assert
            // Формула: L = S × (100 / VA_hk) = 25 × (100 / 25) = 25 × 4 = 100 м
            Assert.That(circuit.CircuitLength, Is.EqualTo(100.0).Within(0.01));
        }

        #endregion

        #region Сценарий 2.3: Пользователь очистил длину

        [Test]
        public void OnCircuitLengthChanged_WhenUserClearsLength_BothFieldsAreActive()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.CircuitLength = 100; // Сначала вводим длину

            // Act - пользователь очищает длину
            circuit.CircuitLength = 0;

            // Assert
            Assert.That(circuit.IsLengthUserInput, Is.False, "Флаг IsLengthUserInput должен быть сброшен");
            Assert.That(circuit.IsAreaUserInput, Is.False, "Флаг IsAreaUserInput должен быть сброшен");
            Assert.That(circuit.IsLengthReadOnly, Is.False, "Поле длины должно быть активно");
            Assert.That(circuit.IsAreaReadOnly, Is.False, "Поле площади должно быть активно");
        }

        #endregion

        #region Сценарий 2.4: Пользователь очистил площадь

        [Test]
        public void OnCircuitAreaChanged_WhenUserClearsArea_BothFieldsAreActive()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.CircuitArea = 20; // Сначала вводим площадь

            // Act - пользователь очищает площадь
            circuit.CircuitArea = 0;

            // Assert
            Assert.That(circuit.IsLengthUserInput, Is.False, "Флаг IsLengthUserInput должен быть сброшен");
            Assert.That(circuit.IsAreaUserInput, Is.False, "Флаг IsAreaUserInput должен быть сброшен");
            Assert.That(circuit.IsLengthReadOnly, Is.False, "Поле длины должно быть активно");
            Assert.That(circuit.IsAreaReadOnly, Is.False, "Поле площади должно быть активно");
        }

        #endregion

        #region Сценарий: Переключение между вводом длины и площади

        [Test]
        public void Switching_FromLengthToArea_UpdatesFlagsCorrectly()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.CircuitLength = 100; // Сначала вводим длину

            // Act - пользователь переключается на ввод площади
            circuit.CircuitArea = 30;

            // Assert
            Assert.That(circuit.IsAreaUserInput, Is.True, "Флаг IsAreaUserInput должен быть установлен");
            Assert.That(circuit.IsLengthUserInput, Is.False, "Флаг IsLengthUserInput должен быть сброшен");
            Assert.That(circuit.CircuitLength, Is.EqualTo(150.0).Within(0.01), "Длина должна быть пересчитана");
            Assert.That(circuit.IsLengthReadOnly, Is.True, "Поле длины должно быть заблокировано");
            Assert.That(circuit.IsAreaReadOnly, Is.False, "Поле площади должно быть активно");
        }

        [Test]
        public void Switching_FromAreaToLength_UpdatesFlagsCorrectly()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.CircuitArea = 20; // Сначала вводим площадь

            // Act - пользователь переключается на ввод длины
            circuit.CircuitLength = 150;

            // Assert
            Assert.That(circuit.IsLengthUserInput, Is.True, "Флаг IsLengthUserInput должен быть установлен");
            Assert.That(circuit.IsAreaUserInput, Is.False, "Флаг IsAreaUserInput должен быть сброшен");
            Assert.That(circuit.CircuitArea, Is.EqualTo(30.0).Within(0.01), "Площадь должна быть пересчитана");
            Assert.That(circuit.IsAreaReadOnly, Is.True, "Поле площади должно быть заблокировано");
            Assert.That(circuit.IsLengthReadOnly, Is.False, "Поле длины должно быть активно");
        }

        #endregion

        #region Сценарий: Изменение шага укладки

        [Test]
        public void OnPipeSpacingChanged_WhenLengthWasEntered_AreaIsRecalculated()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.CircuitLength = 100; // Пользователь ввёл длину

            // Act - изменяем шаг укладки
            circuit.PipeSpacing_cm = 25.0;

            // Assert
            // Формула: S = L / (100 / VA_hk) = 100 / (100 / 25) = 100 / 4 = 25 м²
            Assert.That(circuit.CircuitArea, Is.EqualTo(25.0).Within(0.01), "Площадь должна быть пересчитана");
            Assert.That(circuit.IsLengthUserInput, Is.True, "Флаг IsLengthUserInput должен остаться");
        }

        [Test]
        public void OnPipeSpacingChanged_WhenAreaWasEntered_LengthIsRecalculated()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.CircuitArea = 20; // Пользователь ввёл площадь

            // Act - изменяем шаг укладки
            circuit.PipeSpacing_cm = 25.0;

            // Assert
            // Формула: L = S × (100 / VA_hk) = 20 × (100 / 25) = 20 × 4 = 80 м
            Assert.That(circuit.CircuitLength, Is.EqualTo(80.0).Within(0.01), "Длина должна быть пересчитана");
            Assert.That(circuit.IsAreaUserInput, Is.True, "Флаг IsAreaUserInput должен остаться");
        }

        [Test]
        public void OnPipeSpacingChanged_WhenNoUserInput_NoRecalculation()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.CircuitLength = 100;
            circuit.CircuitArea = 20;
            // Сбрасываем флаги вручную (имитация очистки)
            circuit.IsLengthUserInput = false;
            circuit.IsAreaUserInput = false;

            // Act - изменяем шаг укладки
            circuit.PipeSpacing_cm = 25.0;

            // Assert - значения не должны измениться
            Assert.That(circuit.CircuitLength, Is.EqualTo(100.0).Within(0.01));
            Assert.That(circuit.CircuitArea, Is.EqualTo(20.0).Within(0.01));
        }

        #endregion

        #region Граничные случаи

        [Test]
        public void OnCircuitLengthChanged_ZeroValue_ClearsFlags()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };

            // Act
            circuit.CircuitLength = 0;

            // Assert
            Assert.That(circuit.IsLengthUserInput, Is.False);
            Assert.That(circuit.IsAreaUserInput, Is.False);
        }

        [Test]
        public void OnCircuitAreaChanged_ZeroValue_ClearsFlags()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };

            // Act
            circuit.CircuitArea = 0;

            // Assert
            Assert.That(circuit.IsLengthUserInput, Is.False);
            Assert.That(circuit.IsAreaUserInput, Is.False);
        }

        [Test]
        public void IsLengthReadOnly_WhenAreaIsZero_ReturnsFalse()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.IsAreaUserInput = true;
            circuit.CircuitArea = 0;

            // Assert
            Assert.That(circuit.IsLengthReadOnly, Is.False, "Поле длины должно быть активно, если площадь = 0");
        }

        [Test]
        public void IsAreaReadOnly_WhenLengthIsZero_ReturnsFalse()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            circuit.IsLengthUserInput = true;
            circuit.CircuitLength = 0;

            // Assert
            Assert.That(circuit.IsAreaReadOnly, Is.False, "Поле площади должно быть активно, если длина = 0");
        }

        #endregion

        #region Тесты формул

        [Test]
        public void Formula_AreaFromLength_IsCorrect()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 15.0 };

            // Act
            circuit.CircuitLength = 100;

            // Assert
            // S = L / (100 / VA_hk) = 100 / (100 / 15) = 100 / 6.667 = 15 м²
            var expectedArea = 100.0 / (100.0 / 15.0);
            Assert.That(circuit.CircuitArea, Is.EqualTo(expectedArea).Within(0.01));
        }

        [Test]
        public void Formula_LengthFromArea_IsCorrect()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 15.0 };

            // Act
            circuit.CircuitArea = 15;

            // Assert
            // L = S × (100 / VA_hk) = 15 × (100 / 15) = 15 × 6.667 = 100 м
            var expectedLength = 15.0 * (100.0 / 15.0);
            Assert.That(circuit.CircuitLength, Is.EqualTo(expectedLength).Within(0.01));
        }

        [Test]
        public void Formula_RoundTrip_IsConsistent()
        {
            // Arrange
            var circuit = new CircuitRow { PipeSpacing_cm = 20.0 };
            var originalLength = 100.0;

            // Act - вводим длину, затем очищаем и вводим площадь
            circuit.CircuitLength = originalLength;
            var calculatedArea = circuit.CircuitArea;
            
            // Очищаем оба поля
            circuit.CircuitLength = 0;
            circuit.CircuitArea = 0; // Очищаем площадь, чтобы избежать конфликта
            
            // Вводим площадь
            circuit.CircuitArea = calculatedArea;

            // Assert
            Assert.That(circuit.CircuitLength, Is.EqualTo(originalLength).Within(0.01), 
                "Круговой расчёт должен давать исходное значение");
        }

        #endregion

        #region Тесты CircuitNumber (ObservableProperty)

        [Test]
        public void CircuitNumber_DefaultValue_IsZero()
        {
            // Arrange & Act
            var circuit = new CircuitRow();

            // Assert
            Assert.That(circuit.CircuitNumber, Is.EqualTo(0));
        }

        [Test]
        public void CircuitNumber_CanBeSetAndRetrieved()
        {
            // Arrange
            var circuit = new CircuitRow();

            // Act
            circuit.CircuitNumber = 5;

            // Assert
            Assert.That(circuit.CircuitNumber, Is.EqualTo(5));
        }

        [Test]
        public void CircuitNumber_RaisesPropertyChangedEvent()
        {
            // Arrange
            var circuit = new CircuitRow();
            var eventRaised = false;
            string? changedPropertyName = null;

            circuit.PropertyChanged += (sender, e) =>
            {
                eventRaised = true;
                changedPropertyName = e.PropertyName;
            };

            // Act
            circuit.CircuitNumber = 10;

            // Assert
            Assert.That(eventRaised, Is.True, "Событие PropertyChanged должно быть вызвано");
            Assert.That(changedPropertyName, Is.EqualTo(nameof(CircuitRow.CircuitNumber)));
        }

        [Test]
        public void CircuitNumber_DoesNotRaiseEvent_WhenValueUnchanged()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitNumber = 5 };
            var eventRaised = false;

            circuit.PropertyChanged += (sender, e) =>
            {
                eventRaised = true;
            };

            // Act - устанавливаем то же значение
            circuit.CircuitNumber = 5;

            // Assert
            Assert.That(eventRaised, Is.False, "Событие PropertyChanged не должно вызываться при установке того же значения");
        }

        [Test]
        public void CircuitNumber_CanBeSetToLargeValue()
        {
            // Arrange
            var circuit = new CircuitRow();

            // Act
            circuit.CircuitNumber = 12;

            // Assert
            Assert.That(circuit.CircuitNumber, Is.EqualTo(12));
        }

        [Test]
        public void CircuitNumber_CanBeSetToNegativeValue()
        {
            // Arrange
            var circuit = new CircuitRow();

            // Act
            circuit.CircuitNumber = -1;

            // Assert
            Assert.That(circuit.CircuitNumber, Is.EqualTo(-1));
        }

        [Test]
        public void CircuitNumber_MultipleChanges_RaisesMultipleEvents()
        {
            // Arrange
            var circuit = new CircuitRow();
            var eventCount = 0;

            circuit.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(CircuitRow.CircuitNumber))
                {
                    eventCount++;
                }
            };

            // Act
            circuit.CircuitNumber = 1;
            circuit.CircuitNumber = 2;
            circuit.CircuitNumber = 3;

            // Assert
            Assert.That(eventCount, Is.EqualTo(3), "Событие должно вызываться при каждом изменении значения");
        }

        #endregion

        #region Тесты CircuitTemperatureResult

        [Test]
        public void CircuitTemperatureResult_CircuitPipeLoss_mbar_ConvertsCorrectly()
        {
            // Arrange
            var result = new CircuitTemperatureResult
            {
                CircuitPipeLoss = 23360.0 // 233.6 мбар в Па
            };

            // Act & Assert
            Assert.That(result.CircuitPipeLoss_mbar, Is.EqualTo(233.6).Within(0.01), 
                "CircuitPipeLoss_mbar должен быть CircuitPipeLoss / 100");
        }

        [Test]
        public void CircuitTemperatureResult_ValveLoss_mbar_ConvertsCorrectly()
        {
            // Arrange
            var result = new CircuitTemperatureResult
            {
                ValveLoss = 5730.0 // 57.3 мбар в Па
            };

            // Act & Assert
            Assert.That(result.ValveLoss_mbar, Is.EqualTo(57.3).Within(0.01), 
                "ValveLoss_mbar должен быть ValveLoss / 100");
        }

        [Test]
        public void CircuitTemperatureResult_TotalLoss_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitTemperatureResult
            {
                DpRohr = 23360.0,      // 233.6 мбар
                DpVerteiler = 1000.0,  // 10 мбар
                DpVent = 5730.0        // 57.3 мбар
            };

            // Act & Assert
            var expectedTotal = 23360.0 + 1000.0 + 5730.0;
            Assert.That(result.TotalLoss, Is.EqualTo(expectedTotal).Within(0.01), 
                "TotalLoss должен быть суммой DpRohr + DpVerteiler + DpVent");
        }

        [Test]
        public void CircuitTemperatureResult_TotalLoss_mbar_ConvertsCorrectly()
        {
            // Arrange
            var result = new CircuitTemperatureResult
            {
                DpRohr = 23360.0,
                DpVerteiler = 1000.0,
                DpVent = 5730.0
            };

            // Act & Assert
            var expectedTotal_mbar = (23360.0 + 1000.0 + 5730.0) / 100.0;
            Assert.That(result.TotalLoss_mbar, Is.EqualTo(expectedTotal_mbar).Within(0.01), 
                "TotalLoss_mbar должен быть TotalLoss / 100");
        }

        [Test]
        public void CircuitTemperatureResult_ZeroLosses_ReturnsZero_mbar()
        {
            // Arrange
            var result = new CircuitTemperatureResult();

            // Act & Assert
            Assert.That(result.DpRohr, Is.EqualTo(0.0).Within(0.01));
            Assert.That(result.DpVerteiler, Is.EqualTo(0.0).Within(0.01));
            Assert.That(result.DpVent, Is.EqualTo(0.0).Within(0.01));
            Assert.That(result.DpGesamt, Is.EqualTo(0.0).Within(0.01));
            Assert.That(result.TotalLoss, Is.EqualTo(0.0).Within(0.01));
            Assert.That(result.TotalLoss_mbar, Is.EqualTo(0.0).Within(0.01));
        }

        [Test]
        public void CircuitTemperatureResult_FrictionFactor_CanBeSet()
        {
            // Arrange
            var result = new CircuitTemperatureResult();

            // Act
            result.FrictionFactor = 0.0423;

            // Assert
            Assert.That(result.FrictionFactor, Is.EqualTo(0.0423).Within(0.0001));
        }

        [Test]
        public void CircuitTemperatureResult_ReynoldsNumber_CanBeSet()
        {
            // Arrange
            var result = new CircuitTemperatureResult();

            // Act
            result.ReynoldsNumber = 3551;

            // Assert
            Assert.That(result.ReynoldsNumber, Is.EqualTo(3551));
        }

        [Test]
        public void CircuitTemperatureResult_PressureLossPerMeter_CanBeSet()
        {
            // Arrange
            var result = new CircuitTemperatureResult();

            // Act
            result.PressureLossPerMeter = 592.0;

            // Assert
            Assert.That(result.PressureLossPerMeter, Is.EqualTo(592.0).Within(0.01));
        }

        #endregion

        #region Тесты DisplayMode и CurrentResult

        [Test]
        public void DisplayMode_DefaultValue_IsOperatingTemperature()
        {
            // Arrange & Act
            var circuit = new CircuitRow();

            // Assert
            Assert.That(circuit.DisplayMode, Is.EqualTo(HydraulicMode.OperatingTemperature));
        }

        [Test]
        public void DisplayMode_CanBeChanged()
        {
            // Arrange
            var circuit = new CircuitRow();

            // Act
            circuit.DisplayMode = HydraulicMode.DesignTemperature;

            // Assert
            Assert.That(circuit.DisplayMode, Is.EqualTo(HydraulicMode.DesignTemperature));
        }

        [Test]
        public void CurrentResult_WhenOperatingMode_ReturnsOperatingResult()
        {
            // Arrange
            var circuit = new CircuitRow();
            circuit.OperatingResult = new CircuitTemperatureResult
            {
                Temperature = 32.5,
                ReynoldsNumber = 10000,
                FrictionFactor = 0.03
            };
            circuit.DesignResult = new CircuitTemperatureResult
            {
                Temperature = -28.0,
                ReynoldsNumber = 5000,
                FrictionFactor = 0.04
            };

            // Act
            circuit.DisplayMode = HydraulicMode.OperatingTemperature;

            // Assert
            Assert.That(circuit.CurrentResult.Temperature, Is.EqualTo(32.5));
            Assert.That(circuit.CurrentResult.ReynoldsNumber, Is.EqualTo(10000));
            Assert.That(circuit.CurrentResult.FrictionFactor, Is.EqualTo(0.03));
        }

        [Test]
        public void CurrentResult_WhenDesignMode_ReturnsDesignResult()
        {
            // Arrange
            var circuit = new CircuitRow();
            circuit.OperatingResult = new CircuitTemperatureResult
            {
                Temperature = 32.5,
                ReynoldsNumber = 10000,
                FrictionFactor = 0.03
            };
            circuit.DesignResult = new CircuitTemperatureResult
            {
                Temperature = -28.0,
                ReynoldsNumber = 5000,
                FrictionFactor = 0.04
            };

            // Act
            circuit.DisplayMode = HydraulicMode.DesignTemperature;

            // Assert
            Assert.That(circuit.CurrentResult.Temperature, Is.EqualTo(-28.0));
            Assert.That(circuit.CurrentResult.ReynoldsNumber, Is.EqualTo(5000));
            Assert.That(circuit.CurrentResult.FrictionFactor, Is.EqualTo(0.04));
        }

        [Test]
        public void CurrentResult_RaisesPropertyChanged_WhenDisplayModeChanges()
        {
            // Arrange
            var circuit = new CircuitRow();
            circuit.OperatingResult = new CircuitTemperatureResult { Temperature = 32.5 };
            circuit.DesignResult = new CircuitTemperatureResult { Temperature = -28.0 };

            var propertyChanged = false;
            string? changedPropertyName = null;
            circuit.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(CircuitRow.CurrentResult))
                {
                    propertyChanged = true;
                    changedPropertyName = e.PropertyName;
                }
            };

            // Act
            circuit.DisplayMode = HydraulicMode.DesignTemperature;

            // Assert
            Assert.That(propertyChanged, Is.True, "PropertyChanged для CurrentResult должен быть вызван");
            Assert.That(changedPropertyName, Is.EqualTo(nameof(CircuitRow.CurrentResult)));
        }

        [Test]
        public void FlowRegimeDescription_ReturnsCorrectDescription()
        {
            // Arrange
            var circuit = new CircuitRow();
            circuit.OperatingResult = new CircuitTemperatureResult
            {
                FlowRegime = FlowRegime.Turbulent
            };

            // Act & Assert
            Assert.That(circuit.FlowRegimeDescription, Is.EqualTo("Турбулентный"));
        }

        [Test]
        public void FlowRegimeDescription_WhenDesignMode_ReturnsDesignFlowRegime()
        {
            // Arrange
            var circuit = new CircuitRow();
            circuit.OperatingResult = new CircuitTemperatureResult
            {
                FlowRegime = FlowRegime.Turbulent
            };
            circuit.DesignResult = new CircuitTemperatureResult
            {
                FlowRegime = FlowRegime.Laminar
            };

            // Act
            circuit.DisplayMode = HydraulicMode.DesignTemperature;

            // Assert
            Assert.That(circuit.FlowRegimeDescription, Is.EqualTo("Ламинарный"));
        }

        #endregion
    }
}