# Task 2.1: IHydraulicCalculator (Интерфейс калькулятора)

**Этап:** 2 - Interfaces  
**Приоритет:** Высокий  
**Статус:** Completed  
**Зависимости:** Task 1.1, Task 1.2, Task 1.3, Task 1.5

---

## 1. Цель задачи

Создать интерфейс `IHydraulicCalculator` — контракт для калькулятора гидравлического расчёта.

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

### 3.1. IHydraulicCalculator.cs

**Путь:** `src/Services/Hydraulics/IHydraulicCalculator.cs`

**Содержимое:**
```csharp
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Интерфейс калькулятора гидравлического расчёта
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для расчёта гидравлических параметров:
    /// - Скорость потока
    /// - Число Рейнольдса
    /// - Режим течения
    /// - Коэффициент трения λ
    /// - Потери давления
    /// 
    /// Формулы взяты из docs/Formulas_Snegotayanie.md, раздел 11.
    /// </remarks>
    public interface IHydraulicCalculator
    {
        /// <summary>
        /// Рассчитать скорость потока
        /// </summary>
        /// <param name="flowRate_L_h">Расход, л/ч</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <returns>Скорость потока, м/с</returns>
        /// <remarks>
        /// Формула: w = v × 1000 / (3600 × π × di² / 4)
        /// Где:
        /// - v — расход, л/ч
        /// - di — внутренний диаметр, мм
        /// 
        /// Рекомендуемый диапазон: 0.2-1.5 м/с
        /// </remarks>
        double CalculateVelocity(double flowRate_L_h, double innerDiameter_mm);
        
        /// <summary>
        /// Рассчитать число Рейнольдса
        /// </summary>
        /// <param name="velocity_m_s">Скорость потока, м/с</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <param name="kinematicViscosity_mm2_s">Кинематическая вязкость, мм²/с</param>
        /// <returns>Число Рейнольдса (безразмерное)</returns>
        /// <remarks>
        /// Формула: Re = 1000 × w × di / ν
        /// Где:
        /// - w — скорость, м/с
        /// - di — внутренний диаметр, мм
        /// - ν — кинематическая вязкость, мм²/с
        /// 
        /// Режимы течения:
        /// - Re &lt; 2300 — ламинарный
        /// - 2300 ≤ Re ≤ 4000 — переходный
        /// - Re &gt; 4000 — турбулентный
        /// </remarks>
        double CalculateReynoldsNumber(
            double velocity_m_s, 
            double innerDiameter_mm, 
            double kinematicViscosity_mm2_s);
        
        /// <summary>
        /// Определить режим течения
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>Режим течения</returns>
        /// <remarks>
        /// Критерии:
        /// - Re &lt; 2300 → Laminar
        /// - 2300 ≤ Re ≤ 4000 → Transitional
        /// - Re &gt; 4000 → Turbulent
        /// </remarks>
        FlowRegime DetermineFlowRegime(double reynoldsNumber);
        
        /// <summary>
        /// Рассчитать коэффициент гидравлического трения λ
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <param name="roughness_mm">Шероховатость трубы, мм</param>
        /// <returns>Коэффициент трения λ (безразмерный)</returns>
        /// <remarks>
        /// Формулы по режимам:
        /// 
        /// Ламинарный (Re &lt; 2300):
        /// λ = 64 / Re (формула Пуазейля)
        /// 
        /// Переходный (2300 ≤ Re ≤ 4000):
        /// Линейная интерполяция между λ_lam и λ_turb
        /// 
        /// Турбулентный (Re &gt; 4000):
        /// 1 / √λ = -2 × lg(ε / (3.7 × di) + 2.51 / (Re × √λ))
        /// (формула Колбрука-Уайта, решается итерационно)
        /// 
        /// Шероховатость PE-Xa: 0.007 мм
        /// </remarks>
        double CalculateFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm);
        
        /// <summary>
        /// Рассчитать удельные потери давления
        /// </summary>
        /// <param name="velocity_m_s">Скорость потока, м/с</param>
        /// <param name="density_kg_m3">Плотность, кг/м³</param>
        /// <param name="frictionFactor">Коэффициент трения λ</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <returns>Удельные потери давления, Па/м</returns>
        /// <remarks>
        /// Формула: R = 1000 × (w² × ρ × λ) / (2 × di)
        /// Где:
        /// - w — скорость, м/с
        /// - ρ — плотность, кг/м³
        /// - λ — коэффициент трения
        /// - di — внутренний диаметр, мм
        /// 
        /// Ограничение: R ≤ 300 Па/м
        /// </remarks>
        double CalculatePressureLossPerMeter(
            double velocity_m_s, 
            double density_kg_m3, 
            double frictionFactor, 
            double innerDiameter_mm);
        
        /// <summary>
        /// Рассчитать потери давления в вентиле коллектора
        /// </summary>
        /// <param name="flowRate_L_h">Расход, л/ч</param>
        /// <param name="density_kg_m3">Плотность, кг/м³</param>
        /// <param name="collectorType">Тип коллектора</param>
        /// <returns>Потери давления в вентиле, Па</returns>
        /// <remarks>
        /// Формулы по типам коллекторов:
        /// 
        /// HKV-D (Kv = 1.2 м³/ч):
        /// Δp = (v / 1000 / 1.2)² × 100000 × ρ
        /// 
        /// IV 1¼" (Kv = 1.45 м³/ч):
        /// Δp = (v / 1000 / 1.45)² × 100000 × ρ
        /// 
        /// IV 1½" (Kv = 1.5 м³/ч):
        /// Δp = (v / 1000 / 1.5)² × 100000 × ρ
        /// </remarks>
        double CalculateValvePressureLoss(
            double flowRate_L_h, 
            double density_kg_m3, 
            CollectorType collectorType);
        
        /// <summary>
        /// Выполнить полный гидравлический расчёт контура
        /// </summary>
        /// <param name="parameters">Параметры расчёта</param>
        /// <returns>Результат расчёта</returns>
        /// <remarks>
        /// Выполняет полный расчёт:
        /// 1. Скорость потока
        /// 2. Число Рейнольдса
        /// 3. Режим течения
        /// 4. Коэффициент трения λ
        /// 5. Удельные потери давления
        /// 6. Потери в трубе
        /// 7. Потери в вентиле
        /// 8. Суммарные потери
        /// </remarks>
        HydraulicResult Calculate(HydraulicParameters parameters);
        
        /// <summary>
        /// Рассчитать балансировку контуров
        /// </summary>
        /// <param name="circuits">Список контуров с результатами расчёта</param>
        /// <returns>Список контуров с рассчитанным дросселированием</returns>
        /// <remarks>
        /// Алгоритм балансировки:
        /// 1. Определить контур с максимальными потерями (Δp_max)
        /// 2. Для каждого контура рассчитать дросселирование:
        ///    zu_drosseln = Δp_max - Δp_контур - Δp_вентиль
        /// 3. Определить настройку вентиля (1-8)
        /// </remarks>
        List<CircuitResult> CalculateBalancing(List<CircuitResult> circuits);
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты (интерфейс)

**Файл:** `tests/Services/Hydraulics/IHydraulicCalculatorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using Moq;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class IHydraulicCalculatorTests
    {
        private Mock<IHydraulicCalculator> _calculatorMock;
        
        [SetUp]
        public void Setup()
        {
            _calculatorMock = new Mock<IHydraulicCalculator>();
        }
        
        [Test]
        public void CalculateVelocity_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateVelocity(100, 16))
                .Returns(0.138);
            
            // Act
            var result = _calculatorMock.Object.CalculateVelocity(100, 16);
            
            // Assert
            Assert.That(result, Is.EqualTo(0.138).Within(0.001));
        }
        
        [Test]
        public void CalculateReynoldsNumber_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateReynoldsNumber(0.5, 16, 2.16))
                .Returns(3704);
            
            // Act
            var result = _calculatorMock.Object.CalculateReynoldsNumber(0.5, 16, 2.16);
            
            // Assert
            Assert.That(result, Is.EqualTo(3704).Within(1));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsCorrectRegime()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.DetermineFlowRegime(2000))
                .Returns(FlowRegime.Laminar);
            _calculatorMock
                .Setup(c => c.DetermineFlowRegime(3000))
                .Returns(FlowRegime.Transitional);
            _calculatorMock
                .Setup(c => c.DetermineFlowRegime(5000))
                .Returns(FlowRegime.Turbulent);
            
            // Act & Assert
            Assert.That(_calculatorMock.Object.DetermineFlowRegime(2000), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(_calculatorMock.Object.DetermineFlowRegime(3000), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(_calculatorMock.Object.DetermineFlowRegime(5000), Is.EqualTo(FlowRegime.Turbulent));
        }
        
        [Test]
        public void CalculateFrictionFactor_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateFrictionFactor(2000, 16, 0.007))
                .Returns(0.032);
            
            // Act
            var result = _calculatorMock.Object.CalculateFrictionFactor(2000, 16, 0.007);
            
            // Assert
            Assert.That(result, Is.EqualTo(0.032).Within(0.001));
        }
        
        [Test]
        public void Calculate_ReturnsValidResult()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                VolumeFlowRate = 10,
                CircuitArea = 20
            };
            
            var expectedResult = new HydraulicResult
            {
                Velocity = 0.5,
                ReynoldsNumber = 3704,
                FlowRegime = FlowRegime.Transitional,
                IsValid = true
            };
            
            _calculatorMock
                .Setup(c => c.Calculate(parameters))
                .Returns(expectedResult);
            
            // Act
            var result = _calculatorMock.Object.Calculate(parameters);
            
            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Velocity, Is.EqualTo(0.5));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `IHydraulicCalculator.cs` создан
- [ ] Интерфейс содержит все методы из ТЗ
- [ ] Все методы имеют XML-документацию с формулами
- [ ] Интерфейс ссылается на модели из Task 1.x
- [ ] Unit-тесты с Mock проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Интерфейс должен быть независимым от реализации
- Все методы должны быть документированы с формулами
- Интерфейс используется для DI и тестирования