# Task 1.2: Создать HydraulicInputData.cs

**Этап:** 1 - Модели данных  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Нет

---

## 1. Цель задачи

Создать класс `HydraulicInputData` — модель входных данных для гидравлического расчёта контуров.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Ввод параметров контуров | Основной класс входных данных |
| UC-07 | Интеграция с ThermalModule и ClimateModule | Получение данных из модулей |

---

## 3. Создаваемые файлы

### 3.1. HydraulicInputData.cs

**Путь:** `src/Models/Hydraulics/HydraulicInputData.cs`

**Содержимое:**
```csharp
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Входные данные для гидравлического расчёта контуров
    /// </summary>
    /// <remarks>
    /// Содержит данные из ThermalModule, ClimateModule и от пользователя.
    /// Используется для расчёта таблицы контуров.
    /// </remarks>
    public class HydraulicInputData
    {
        // === Данные из ThermalModule ===
        
        /// <summary>
        /// Мощность вверх (q_up), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerUp
        /// </remarks>
        public double PowerUp { get; set; }
        
        /// <summary>
        /// Мощность вниз (q_down), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerDown
        /// </remarks>
        public double PowerDown { get; set; }
        
        /// <summary>
        /// Температура подачи (T_supply), °C
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.SupplyTemperature
        /// </remarks>
        public double SupplyTemperature { get; set; }
        
        /// <summary>
        /// Температура обратки (T_return), °C
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.ReturnTemperature
        /// </remarks>
        public double ReturnTemperature { get; set; }
        
        /// <summary>
        /// Внутренний диаметр трубы (d_inner), мм
        /// </summary>
        /// <remarks>
        /// Вычисляется: d_inner = D_ext - 2 × s
        /// Где D_ext — наружный диаметр, s — толщина стенки
        /// </remarks>
        public double InnerDiameter { get; set; }
        
        /// <summary>
        /// Шаг укладки (VA_hk), мм
        /// </summary>
        /// <remarks>
        /// Получается из ThermalViewModel.PipeSpacing
        /// Стандартные значения: 150, 200, 250, 300 мм
        /// </remarks>
        public double PipeSpacing_mm { get; set; }
        
        // === Данные из ClimateModule ===
        
        /// <summary>
        /// Температура холодной пятидневки (t_cold), °C
        /// </summary>
        /// <remarks>
        /// Получается из ClimateData.ColdFiveDayTemperature
        /// Используется для расчёта при "холодном пуске"
        /// </remarks>
        public double ColdFiveDayTemperature { get; set; }
        
        // === Данные от пользователя ===
        
        /// <summary>
        /// Тип гликоля
        /// </summary>
        /// <remarks>
        /// Этиленгликоль или пропиленгликоль
        /// По умолчанию: этиленгликоль
        /// </remarks>
        public GlycolType GlycolType { get; set; } = GlycolType.Ethylene;
        
        /// <summary>
        /// Концентрация гликоля, %
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-90%
        /// По умолчанию: 50%
        /// </remarks>
        public double GlycolConcentration { get; set; } = 50.0;
        
        /// <summary>
        /// Шаг подводки (VA_zul), см
        /// </summary>
        /// <remarks>
        /// По умолчанию: 5 см
        /// </remarks>
        public double SupplySpacing_cm { get; set; } = 5.0;
        
        /// <summary>
        /// Доля тепла от подводок (q_zul), %
        /// </summary>
        /// <remarks>
        /// По умолчанию: 10%
        /// Диапазон: 0-20%
        /// </remarks>
        public double SupplyHeatPercent { get; set; } = 10.0;

        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        /// <remarks>
        /// По умолчанию: HKV_D
        /// Определяет kv-значение для расчёта потерь на клапане
        /// </remarks>
        public ValveType ValveType { get; set; } = ValveType.HKV_D;

        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Рабочая температура (T_operating), °C
        /// </summary>
        /// <remarks>
        /// Формула: T_operating = (T_supply + T_return) / 2
        /// </remarks>
        public double OperatingTemperature => (SupplyTemperature + ReturnTemperature) / 2.0;
        
        /// <summary>
        /// Расчётная температура (T_design), °C
        /// </summary>
        /// <remarks>
        /// Равна температуре холодной пятидневки
        /// </remarks>
        public double DesignTemperature => ColdFiveDayTemperature;
        
        /// <summary>
        /// Температурный перепад (ΔT), К
        /// </summary>
        /// <remarks>
        /// Формула: ΔT = T_supply - T_return
        /// </remarks>
        public double DeltaT => SupplyTemperature - ReturnTemperature;
        
        /// <summary>
        /// Шаг укладки (VA_hk), см
        /// </summary>
        /// <remarks>
        /// Формула: VA_hk_cm = VA_hk_mm / 10
        /// </remarks>
        public double PipeSpacing_cm => PipeSpacing_mm / 10.0;
        
        // === Валидация ===
        
        /// <summary>
        /// Признак валидности данных
        /// </summary>
        public bool IsValid => Validate().IsValid;
        
        /// <summary>
        /// Валидировать входные данные
        /// </summary>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();
            
            if (PowerUp <= 0)
                errors.Add("Мощность вверх должна быть положительной");
            
            if (PowerDown < 0)
                errors.Add("Мощность вниз не может быть отрицательной");
            
            if (SupplyTemperature <= ReturnTemperature)
                errors.Add("Температура подачи должна быть больше температуры обратки");
            
            if (InnerDiameter <= 0)
                errors.Add("Внутренний диаметр трубы должен быть положительным");
            
            if (PipeSpacing_mm <= 0)
                errors.Add("Шаг укладки должен быть положительным");
            
            if (GlycolConcentration < 10 || GlycolConcentration > 90)
                errors.Add($"Концентрация гликоля должна быть от 10 до 90% (текущая: {GlycolConcentration:F0}%)");
            
            if (SupplySpacing_cm <= 0)
                errors.Add("Шаг подводки должен быть положительным");
            
            if (SupplyHeatPercent < 0 || SupplyHeatPercent > 20)
                errors.Add($"Доля тепла от подводок должна быть от 0 до 20% (текущая: {SupplyHeatPercent:F0}%)");
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }
    
    /// <summary>
    /// Результат валидации
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Признак валидности
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Список ошибок
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// Список предупреждений
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Models/Hydraulics/HydraulicInputDataTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class HydraulicInputDataTests
    {
        [Test]
        public void OperatingTemperature_CalculatesCorrectly()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                SupplyTemperature = 50,
                ReturnTemperature = 30
            };
            
            // Act & Assert
            Assert.That(data.OperatingTemperature, Is.EqualTo(40));
        }
        
        [Test]
        public void DesignTemperature_EqualsColdFiveDayTemperature()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                ColdFiveDayTemperature = -30
            };
            
            // Act & Assert
            Assert.That(data.DesignTemperature, Is.EqualTo(-30));
        }
        
        [Test]
        public void DeltaT_CalculatesCorrectly()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                SupplyTemperature = 50,
                ReturnTemperature = 30
            };
            
            // Act & Assert
            Assert.That(data.DeltaT, Is.EqualTo(20));
        }
        
        [Test]
        public void PipeSpacing_cm_ConvertsFromMm()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PipeSpacing_mm = 200
            };
            
            // Act & Assert
            Assert.That(data.PipeSpacing_cm, Is.EqualTo(20));
        }
        
        [Test]
        public void Validate_ReturnsValidForCorrectData()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 256,
                PowerDown = 5,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16,
                PipeSpacing_mm = 200,
                ColdFiveDayTemperature = -30,
                GlycolConcentration = 50,
                SupplyHeatPercent = 10
            };
            
            // Act
            var result = data.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }
        
        [Test]
        public void Validate_ReturnsInvalidForIncorrectData()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 0, // Невалидно
                PowerDown = -1, // Невалидно
                SupplyTemperature = 30,
                ReturnTemperature = 50, // Невалидно: подача < обратки
                InnerDiameter = 0, // Невалидно
                PipeSpacing_mm = 0, // Невалидно
                GlycolConcentration = 5, // Невалидно: < 10
                SupplyHeatPercent = 25 // Невалидно: > 20
            };
            
            // Act
            var result = data.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }
        
        [Test]
        public void DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var data = new HydraulicInputData();
            
            // Assert
            Assert.That(data.GlycolType, Is.EqualTo(GlycolType.Ethylene));
            Assert.That(data.GlycolConcentration, Is.EqualTo(50.0));
            Assert.That(data.SupplySpacing_cm, Is.EqualTo(5.0));
            Assert.That(data.SupplyHeatPercent, Is.EqualTo(10.0));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `HydraulicInputData.cs` создан в `src/Models/Hydraulics/`
- [ ] Класс содержит все свойства из ТЗ
- [ ] Вычисляемые свойства (OperatingTemperature, DesignTemperature, DeltaT, PipeSpacing_cm) работают корректно
- [ ] Метод Validate() возвращает корректный результат
- [ ] XML-документация для всех свойств и методов
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Класс должен быть независимым от других сервисов
- Вычисляемые свойства не должны выбрасывать исключения
- Значения по умолчанию соответствуют ТЗ
- Файл размещается в `src/Models/Hydraulics/`

---

## 7. Связанные задачи

- Task 2.1: Создать ICircuitsCalculator.cs — использовать HydraulicInputData
- Task 3.2: Создать CircuitsCalculator.cs — использовать HydraulicInputData
- Task 4.1: Создать CircuitsViewModel.cs — использовать HydraulicInputData

---

*Дата создания: 2026-03-17*