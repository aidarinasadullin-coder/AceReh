# Task 3.1: HydraulicCalculator (Калькулятор гидравлики)

**Этап:** 3 - Services  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 2.1 (IHydraulicCalculator), Task 2.2 (IGlycolDataService), Task 3.4 (HydraulicValidator)

---

## 1. Цель задачи

Реализовать класс `HydraulicCalculator` — основной калькулятор гидравлического расчёта.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Расчёт гидравлических параметров контура | Метод Calculate() |
| UC-02 | Определение режима течения | Методы DetermineFlowRegime(), CalculateFrictionFactor() |
| UC-03 | Расчёт потерь давления в трубе | Метод CalculatePressureLossPerMeter() |
| UC-04 | Расчёт потерь давления в вентилях | Метод CalculateValvePressureLoss() |
| UC-06 | Расчёт дросселирования контуров | Метод CalculateBalancing() |

---

## 3. Создаваемые файлы

### 3.1. HydraulicCalculator.cs

**Путь:** `src/Services/Hydraulics/HydraulicCalculator.cs`

**Ключевые методы:**

```csharp
namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Реализация калькулятора гидравлического расчёта
    /// </summary>
    public class HydraulicCalculator : IHydraulicCalculator
    {
        private readonly IGlycolDataService _glycolService;
        private readonly HydraulicValidator _validator;
        
        public HydraulicCalculator(IGlycolDataService glycolService)
        {
            _glycolService = glycolService;
            _validator = new HydraulicValidator();
        }
        
        /// <summary>
        /// Рассчитать скорость потока
        /// Формула: w = v × 1000 / (3600 × π × di² / 4)
        /// </summary>
        public double CalculateVelocity(double flowRate_L_h, double innerDiameter_mm)
        {
            // w = v / 3600 / (π × di² / 4 / 1000000)
            // w = v × 1000 / (3600 × π × di² / 4)
            
            double area_mm2 = Math.PI * Math.Pow(innerDiameter_mm, 2) / 4;
            double velocity = flowRate_L_h * 1000 / (3600 * area_mm2);
            
            return velocity;
        }
        
        /// <summary>
        /// Рассчитать число Рейнольдса
        /// Формула: Re = 1000 × w × di / ν
        /// </summary>
        public double CalculateReynoldsNumber(
            double velocity_m_s, 
            double innerDiameter_mm, 
            double kinematicViscosity_mm2_s)
        {
            // Re = w × di / ν
            // При di в мм и ν в мм²/с: Re = 1000 × w × di / ν
            
            double re = 1000 * velocity_m_s * innerDiameter_mm / kinematicViscosity_mm2_s;
            return re;
        }
        
        /// <summary>
        /// Определить режим течения
        /// </summary>
        public FlowRegime DetermineFlowRegime(double reynoldsNumber)
        {
            if (reynoldsNumber < 2300)
                return FlowRegime.Laminar;
            else if (reynoldsNumber <= 4000)
                return FlowRegime.Transitional;
            else
                return FlowRegime.Turbulent;
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения λ
        /// </summary>
        public double CalculateFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            var regime = DetermineFlowRegime(reynoldsNumber);
            
            return regime switch
            {
                FlowRegime.Laminar => CalculateLaminarFrictionFactor(reynoldsNumber),
                FlowRegime.Transitional => CalculateTransitionalFrictionFactor(
                    reynoldsNumber, innerDiameter_mm, roughness_mm),
                FlowRegime.Turbulent => CalculateTurbulentFrictionFactor(
                    reynoldsNumber, innerDiameter_mm, roughness_mm),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        /// <summary>
        /// Ламинарный режим: λ = 64 / Re (формула Пуазейля)
        /// </summary>
        private double CalculateLaminarFrictionFactor(double reynoldsNumber)
        {
            return 64.0 / reynoldsNumber;
        }
        
        /// <summary>
        /// Переходный режим: линейная интерполяция
        /// </summary>
        private double CalculateTransitionalFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            double lambda_lam = 64.0 / 2300; // ≈ 0.0278
            double lambda_turb = CalculateTurbulentFrictionFactor(4000, innerDiameter_mm, roughness_mm);
            
            // Линейная интерполяция
            double ratio = (reynoldsNumber - 2300) / 1700.0;
            return lambda_lam + ratio * (lambda_turb - lambda_lam);
        }
        
        /// <summary>
        /// Турбулентный режим: формула Колбрука-Уайта
        /// 1 / √λ = -2 × lg(ε / (3.7 × di) + 2.51 / (Re × √λ))
        /// </summary>
        private double CalculateTurbulentFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            // Итерационное решение формулы Колбрука-Уайта
            double lambda = 0.02; // Начальное приближение
            
            for (int i = 0; i < 20; i++)
            {
                double newLambda = Math.Pow(
                    -2 * Math.Log10(roughness_mm / (3.7 * innerDiameter_mm) + 
                    2.51 / (reynoldsNumber * Math.Sqrt(lambda))),
                    -2);
                
                if (Math.Abs(newLambda - lambda) < 1e-8)
                    break;
                
                lambda = newLambda;
            }
            
            return lambda;
        }
        
        /// <summary>
        /// Рассчитать удельные потери давления
        /// Формула: R = 1000 × (w² × ρ × λ) / (2 × di)
        /// </summary>
        public double CalculatePressureLossPerMeter(
            double velocity_m_s, 
            double density_kg_m3, 
            double frictionFactor, 
            double innerDiameter_mm)
        {
            // R = (w² × ρ × λ) / (2 × di) × 1000
            // При di в мм: R = 1000 × (w² × ρ × λ) / (2 × di)
            
            double pressureLoss = 1000 * Math.Pow(velocity_m_s, 2) * density_kg_m3 * frictionFactor 
                / (2 * innerDiameter_mm);
            
            return pressureLoss;
        }
        
        /// <summary>
        /// Рассчитать потери давления в вентиле
        /// </summary>
        public double CalculateValvePressureLoss(
            double flowRate_L_h, 
            double density_kg_m3, 
            CollectorType collectorType)
        {
            // Kv для разных типов коллекторов
            double kv = collectorType switch
            {
                CollectorType.HKV => 1.2,
                CollectorType.IV => 1.45, // DN25 по умолчанию
                _ => 1.2
            };
            
            // Δp = (v / 1000 / Kv)² × 100 × ρ  [Па]
            double pressureLoss = Math.Pow(flowRate_L_h / 1000.0 / kv, 2) * 100.0 * density_kg_m3;
            
            return pressureLoss;
        }
        
        /// <summary>
        /// Выполнить полный гидравлический расчёт
        /// </summary>
        public HydraulicResult Calculate(HydraulicParameters parameters)
        {
            // Валидация параметров
            var validationResult = _validator.Validate(parameters);
            if (!validationResult.IsValid)
            {
                return new HydraulicResult
                {
                    IsValid = false,
                    ValidationErrors = validationResult.Errors.ToArray()
                };
            }
            
            // Получение свойств теплоносителя
            var glycolProps = _glycolService.GetProperties(
                parameters.GlycolType,
                parameters.GlycolConcentration,
                parameters.MeanTemperature);
            
            // Расчёт
            double flowRate = parameters.CircuitFlowRate;
            double di = parameters.InnerDiameter;
            
            double velocity = CalculateVelocity(flowRate, di);
            double re = CalculateReynoldsNumber(velocity, di, glycolProps.KinematicViscosity);
            var regime = DetermineFlowRegime(re);
            double lambda = CalculateFrictionFactor(re, di, parameters.Roughness);
            double pressureLossPerMeter = CalculatePressureLossPerMeter(
                velocity, glycolProps.Density, lambda, di);
            
            double circuitPressureLoss = parameters.CircuitLength * pressureLossPerMeter;
            double supplyPressureLoss = parameters.SupplyLength * pressureLossPerMeter;
            double totalPipePressureLoss = circuitPressureLoss + supplyPressureLoss;
            
            double valvePressureLoss = CalculateValvePressureLoss(
                flowRate, glycolProps.Density, CollectorType.HKV);
            
            double totalPressureLoss = totalPipePressureLoss + valvePressureLoss;
            
            // Валидация результата
            var resultValidation = _validator.ValidateResult(new HydraulicResult
            {
                Velocity = velocity,
                ReynoldsNumber = re,
                FlowRegime = regime,
                PressureLossPerMeter = pressureLossPerMeter
            });
            
            return new HydraulicResult
            {
                Velocity = velocity,
                ReynoldsNumber = re,
                FlowRegime = regime,
                FrictionFactor = lambda,
                PressureLossPerMeter = pressureLossPerMeter,
                CircuitPressureLoss = circuitPressureLoss,
                SupplyPressureLoss = supplyPressureLoss,
                TotalPipePressureLoss = totalPipePressureLoss,
                ValvePressureLoss = valvePressureLoss,
                TotalPressureLoss = totalPressureLoss,
                CircuitFlowRate = flowRate,
                IsValid = true,
                Warnings = resultValidation.Warnings.ToArray()
            };
        }
        
        /// <summary>
        /// Рассчитать балансировку контуров
        /// </summary>
        public List<CircuitResult> CalculateBalancing(List<CircuitResult> circuits)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitResult>();
            
            // Найти контур с максимальными потерями
            double maxPressureLoss = circuits.Max(c => c.TotalPressureLoss);
            
            // Рассчитать дросселирование для каждого контура
            foreach (var circuit in circuits)
            {
                circuit.Throttling = maxPressureLoss - circuit.TotalPressureLoss;
                circuit.IsReferenceCircuit = (circuit.TotalPressureLoss == maxPressureLoss);
                
                // Определить настройку вентиля (1-8)
                circuit.RecommendedValveSetting = CalculateValveSetting(circuit.Throttling);
            }
            
            return circuits;
        }
        
        /// <summary>
        /// Определить настройку вентиля по дросселированию
        /// </summary>
        private int CalculateValveSetting(double throttling_Pa)
        {
            // Таблица настроек вентиля (примерная)
            // Настройка 1: минимальное сопротивление
            // Настройка 8: максимальное сопротивление
            
            double throttling_mbar = throttling_Pa / 100;
            
            if (throttling_mbar <= 0)
                return 1;
            else if (throttling_mbar <= 40)
                return 2;
            else if (throttling_mbar <= 80)
                return 3;
            else if (throttling_mbar <= 120)
                return 4;
            else if (throttling_mbar <= 160)
                return 5;
            else if (throttling_mbar <= 200)
                return 6;
            else if (throttling_mbar <= 240)
                return 7;
            else
                return 8;
        }
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/HydraulicCalculatorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using Moq;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class HydraulicCalculatorTests
    {
        private Mock<IGlycolDataService> _glycolServiceMock;
        private HydraulicCalculator _calculator;
        
        [SetUp]
        public void Setup()
        {
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _glycolServiceMock
                .Setup(s => s.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1053,
                    KinematicViscosity = 2.16,
                    SpecificHeat = 3.39
                });
            
            _calculator = new HydraulicCalculator(_glycolServiceMock.Object);
        }
        
        [Test]
        public void CalculateVelocity_ReturnsCorrectValue()
        {
            // Arrange
            double flowRate = 100; // л/ч
            double diameter = 16; // мм
            
            // Act
            double velocity = _calculator.CalculateVelocity(flowRate, diameter);
            
            // Assert
            // w = 100 × 1000 / (3600 × π × 16² / 4) = 0.138 м/с
            Assert.That(velocity, Is.EqualTo(0.138).Within(0.001));
        }
        
        [Test]
        public void CalculateReynoldsNumber_ReturnsCorrectValue()
        {
            // Arrange
            double velocity = 0.5; // м/с
            double diameter = 16; // мм
            double viscosity = 2.16; // мм²/с
            
            // Act
            double re = _calculator.CalculateReynoldsNumber(velocity, diameter, viscosity);
            
            // Assert
            // Re = 1000 × 0.5 × 16 / 2.16 = 3704
            Assert.That(re, Is.EqualTo(3704).Within(1));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsLaminarForLowRe()
        {
            // Act & Assert
            Assert.That(_calculator.DetermineFlowRegime(2000), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(_calculator.DetermineFlowRegime(2299), Is.EqualTo(FlowRegime.Laminar));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsTransitionalForMediumRe()
        {
            // Act & Assert
            Assert.That(_calculator.DetermineFlowRegime(3000), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(_calculator.DetermineFlowRegime(2300), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(_calculator.DetermineFlowRegime(4000), Is.EqualTo(FlowRegime.Transitional));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsTurbulentForHighRe()
        {
            // Act & Assert
            Assert.That(_calculator.DetermineFlowRegime(5000), Is.EqualTo(FlowRegime.Turbulent));
            Assert.That(_calculator.DetermineFlowRegime(4001), Is.EqualTo(FlowRegime.Turbulent));
        }
        
        [Test]
        public void CalculateFrictionFactor_ReturnsCorrectValueForLaminar()
        {
            // Arrange
            double re = 2000;
            double diameter = 16;
            double roughness = 0.007;
            
            // Act
            double lambda = _calculator.CalculateFrictionFactor(re, diameter, roughness);
            
            // Assert
            // Ламинарный: λ = 64 / Re = 64 / 2000 = 0.032
            Assert.That(lambda, Is.EqualTo(0.032).Within(0.0001));
        }
        
        [Test]
        public void CalculatePressureLossPerMeter_ReturnsCorrectValue()
        {
            // Arrange
            double velocity = 0.5; // м/с
            double density = 1053; // кг/м³
            double lambda = 0.04;
            double diameter = 16; // мм
            
            // Act
            double pressureLoss = _calculator.CalculatePressureLossPerMeter(velocity, density, lambda, diameter);
            
            // Assert
            // R = 1000 × (0.5² × 1053 × 0.04) / (2 × 16) = 329 Па/м
            Assert.That(pressureLoss, Is.EqualTo(329).Within(1));
        }
        
        [Test]
        public void CalculateValvePressureLoss_ReturnsCorrectValueForHKV()
        {
            // Arrange
            double flowRate = 200; // л/ч
            double density = 1053; // кг/м³
            
            // Act
            double pressureLoss = _calculator.CalculateValvePressureLoss(flowRate, density, CollectorType.HKV);
            
            // Assert
            // Δp = (200 / 1000 / 1.2)² × 100 × 1053 = 2925 Па (коэффициент 100)
            Assert.That(pressureLoss, Is.EqualTo(2925).Within(10));
        }
        
        [Test]
        public void Calculate_ReturnsValidResult()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                GlycolType = GlycolType.Ethylene,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType { OuterDiameter = 20, WallThickness = 2 },
                Roughness = 0.007,
                VolumeFlowRate = 10,
                CircuitArea = 20,
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = _calculator.Calculate(parameters);
            
            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Velocity, Is.GreaterThan(0));
            Assert.That(result.ReynoldsNumber, Is.GreaterThan(0));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `HydraulicCalculator.cs` создан
- [ ] Реализованы все методы интерфейса `IHydraulicCalculator`
- [ ] Формулы соответствуют `docs/Formulas_Snegotayanie.md`
- [ ] Итерационное решение формулы Колбрука-Уайта сходится
- [ ] Unit-тесты для всех методов проходят успешно
- [ ] XML-документация для всех методов
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Формула Колбрука-Уайта решается итерационно (обычно 5-10 итераций)
- Переходный режим использует линейную интерполяцию
- Валидация выполняется через `HydraulicValidator`