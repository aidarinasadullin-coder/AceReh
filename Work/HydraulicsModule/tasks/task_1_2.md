# Task 1.2: HydraulicParameters (Параметры расчёта)

**Этап:** 1 - Models  
**Приоритет:** Высокий  
**Статус:** Completed  
**Зависимости:** Task 1.1 (Enums)

---

## 1. Цель задачи

Создать класс `HydraulicParameters` — модель входных параметров для гидравлического расчёта.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Расчёт гидравлических параметров контура | Основной класс параметров |
| UC-07 | Загрузка свойств теплоносителя | Поля GlycolConcentration, GlycolType |

---

## 3. Создаваемые файлы

### 3.1. HydraulicParameters.cs

**Путь:** `src/Models/Hydraulics/HydraulicParameters.cs`

**Содержимое:**
```csharp
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Параметры для гидравлического расчёта контура
    /// </summary>
    /// <remarks>
    /// Содержит все входные данные для расчёта гидравлики:
    /// - Параметры контура (длина, шаг укладки)
    /// - Параметры теплоносителя (гликоль, температура)
    /// - Параметры трубы (тип, шероховатость)
    /// - Данные из теплового расчёта (расход, мощность)
    /// </remarks>
    public class HydraulicParameters
    {
        // === Параметры контура ===
        
        /// <summary>
        /// Длина контура (L_HK), м
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-500 м
        /// Формула: L_HK = S × 1000 / lR
        /// Где S — площадь контура, lR — шаг укладки
        /// </remarks>
        public double CircuitLength { get; set; }
        
        /// <summary>
        /// Длина подводки (L_Zul), м
        /// </summary>
        /// <remarks>
        /// Диапазон: 1-100 м
        /// Сумма длин подающей и обратной подводок
        /// </remarks>
        public double SupplyLength { get; set; }
        
        /// <summary>
        /// Шаг укладки (VAHK), см
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-50 см
        /// Рекомендуемые значения: 15, 20, 25, 30 см
        /// </remarks>
        public double PipeSpacing { get; set; }
        
        /// <summary>
        /// Шаг подводки (VAZul), см
        /// </summary>
        /// <remarks>
        /// Условный шаг для расчёта тепла от подводки
        /// Обычно 5 см
        /// </remarks>
        public double SupplySpacing { get; set; } = 5.0;
        
        // === Параметры теплоносителя ===
        
        /// <summary>
        /// Доля гликоля (Glycolanteil), % объёмные
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-90%
        /// По умолчанию: 50%
        /// </remarks>
        public double GlycolConcentration { get; set; } = 50.0;
        
        /// <summary>
        /// Тип гликоля
        /// </summary>
        /// <remarks>
        /// Этиленгликоль или пропиленгликоль
        /// </remarks>
        public GlycolType GlycolType { get; set; } = GlycolType.Ethylene;
        
        /// <summary>
        /// Температура подачи (T_VL), °C
        /// </summary>
        /// <remarks>
        /// Диапазон: 20-90°C
        /// Получается из теплового расчёта
        /// </remarks>
        public double SupplyTemperature { get; set; }
        
        /// <summary>
        /// Температура обратки (T_RL), °C
        /// </summary>
        /// <remarks>
        /// Диапазон: 15-80°C
        /// Получается из теплового расчёта
        /// </remarks>
        public double ReturnTemperature { get; set; }
        
        /// <summary>
        /// Средняя температура теплоносителя, °C
        /// </summary>
        /// <remarks>
        /// Формула: T_mean = (T_VL + T_RL) / 2
        /// Используется для определения свойств гликоля
        /// </remarks>
        public double MeanTemperature => (SupplyTemperature + ReturnTemperature) / 2.0;
        
        /// <summary>
        /// Плотность теплоносителя (ρ), кг/м³
        /// </summary>
        /// <remarks>
        /// Получается из GlycolDataService по температуре и концентрации
        /// </remarks>
        public double Density { get; set; }
        
        /// <summary>
        /// Кинематическая вязкость (ν), мм²/с
        /// </summary>
        /// <remarks>
        /// Получается из GlycolDataService по температуре и концентрации
        /// </remarks>
        public double KinematicViscosity { get; set; }
        
        /// <summary>
        /// Удельная теплоёмкость (c_p), кДж/(кг·К)
        /// </summary>
        /// <remarks>
        /// Получается из GlycolDataService по температуре и концентрации
        /// </remarks>
        public double SpecificHeat { get; set; }
        
        // === Параметры трубы ===
        
        /// <summary>
        /// Тип трубы
        /// </summary>
        /// <remarks>
        /// Только RAUTHERM S (PE-Xa)
        /// </remarks>
        public PipeType? Pipe { get; set; }
        
        /// <summary>
        /// Шероховатость трубы (ε), мм
        /// </summary>
        /// <remarks>
        /// Для PE-Xa: 0.007 мм
        /// </remarks>
        public double Roughness { get; set; } = 0.007;
        
        /// <summary>
        /// Внутренний диаметр трубы (di), мм
        /// </summary>
        /// <remarks>
        /// Вычисляется: di = d - 2 × s
        /// Где d — наружный диаметр, s — толщина стенки
        /// </remarks>
        public double InnerDiameter => Pipe != null 
            ? Pipe.OuterDiameter - 2 * Pipe.WallThickness 
            : 0;
        
        // === Параметры из теплового расчёта ===
        
        /// <summary>
        /// Удельный расход теплоносителя (V_dot), л/(ч·м²)
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.VolumeFlowRate
        /// </remarks>
        public double VolumeFlowRate { get; set; }
        
        /// <summary>
        /// Мощность контура (q_total), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerTotal
        /// </remarks>
        public double PowerPerArea { get; set; }
        
        /// <summary>
        /// Площадь контура (S), м²
        /// </summary>
        /// <remarks>
        /// Вводится пользователем
        /// </remarks>
        public double CircuitArea { get; set; }
        
        /// <summary>
        /// Расход на контур (v), л/ч
        /// </summary>
        /// <remarks>
        /// Формула: v = VolumeFlowRate × CircuitArea
        /// </remarks>
        public double CircuitFlowRate => VolumeFlowRate * CircuitArea;
        
        // === Валидация ===
        
        /// <summary>
        /// Признак валидности параметров
        /// </summary>
        public bool IsValid => Validate().IsValid;
        
        /// <summary>
        /// Валидировать параметры
        /// </summary>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();
            
            if (CircuitLength < 10 || CircuitLength > 500)
                errors.Add($"Длина контура должна быть от 10 до 500 м (текущая: {CircuitLength:F1} м)");
            
            if (SupplyLength < 1 || SupplyLength > 100)
                errors.Add($"Длина подводки должна быть от 1 до 100 м (текущая: {SupplyLength:F1} м)");
            
            if (GlycolConcentration < 10 || GlycolConcentration > 90)
                errors.Add($"Доля гликоля должна быть от 10 до 90% (текущая: {GlycolConcentration:F0}%)");
            
            if (SupplyTemperature < 20 || SupplyTemperature > 90)
                errors.Add($"Температура подачи должна быть от 20 до 90°C (текущая: {SupplyTemperature:F1}°C)");
            
            if (ReturnTemperature < 15 || ReturnTemperature > 80)
                errors.Add($"Температура обратки должна быть от 15 до 80°C (текущая: {ReturnTemperature:F1}°C)");
            
            if (Pipe == null)
                errors.Add("Тип трубы не задан");
            
            if (Density <= 0)
                errors.Add("Плотность теплоносителя должна быть положительной");
            
            if (KinematicViscosity <= 0)
                errors.Add("Кинематическая вязкость должна быть положительной");
            
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

**Файл:** `tests/Models/Hydraulics/HydraulicParametersTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class HydraulicParametersTests
    {
        [Test]
        public void MeanTemperature_CalculatesCorrectly()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                SupplyTemperature = 50,
                ReturnTemperature = 30
            };
            
            // Act & Assert
            Assert.That(parameters.MeanTemperature, Is.EqualTo(40));
        }
        
        [Test]
        public void CircuitFlowRate_CalculatesCorrectly()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                VolumeFlowRate = 10, // л/(ч·м²)
                CircuitArea = 20     // м²
            };
            
            // Act & Assert
            Assert.That(parameters.CircuitFlowRate, Is.EqualTo(200)); // 10 × 20 = 200 л/ч
        }
        
        [Test]
        public void InnerDiameter_CalculatesCorrectly()
        {
            // Arrange
            var pipe = new PipeType
            {
                OuterDiameter = 20,
                WallThickness = 2
            };
            var parameters = new HydraulicParameters { Pipe = pipe };
            
            // Act & Assert
            Assert.That(parameters.InnerDiameter, Is.EqualTo(16)); // 20 - 2×2 = 16 мм
        }
        
        [Test]
        public void Validate_ReturnsValidForCorrectParameters()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }
        
        [Test]
        public void Validate_ReturnsInvalidForIncorrectParameters()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 5, // < 10
                SupplyLength = 200, // > 100
                GlycolConcentration = 5, // < 10
                SupplyTemperature = 100, // > 90
                ReturnTemperature = 10, // < 15
                Pipe = null,
                Density = 0,
                KinematicViscosity = 0
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }
    }
}
```

---

## 5. Критерии приёмки

- [x] Файл `HydraulicParameters.cs` создан
- [x] Класс содержит все свойства из ТЗ
- [x] Вычисляемые свойства (MeanTemperature, CircuitFlowRate, InnerDiameter) работают корректно
- [x] Метод Validate() возвращает корректный результат
- [x] XML-документация для всех свойств и методов
- [x] Unit-тесты проходят успешно
- [x] Код компилируется без предупреждений

---

## 6. Примечания

- Класс должен ссылаться на `PipeType` из `SnowMeltingCalculator.Models.Thermal`
- ValidationResult вынесен в отдельный класс для повторного использования
- Все числовые значения должны иметь значения по умолчанию