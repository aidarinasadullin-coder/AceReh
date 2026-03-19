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
    }
}