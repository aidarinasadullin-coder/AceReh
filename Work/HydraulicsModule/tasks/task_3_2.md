# Task 3.2: FlowRegimeCalculator (Расчёт режима течения)

**Этап:** 3 - Services  
**Приоритет:** Средний  
**Статус:** Не начато  
**Зависимости:** Task 1.1 (Enums)

---

## 1. Цель задачи

Создать вспомогательный класс `FlowRegimeCalculator` для расчёта режима течения и коэффициента трения.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-02 | Определение режима течения | Все методы класса |

---

## 3. Создаваемые файлы

### 3.1. FlowRegimeCalculator.cs

**Путь:** `src/Services/Hydraulics/FlowRegimeCalculator.cs`

**Содержимое:**
```csharp
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Калькулятор режима течения и коэффициента трения
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для:
    /// - Определения режима течения по числу Рейнольдса
    /// - Расчёта коэффициента трения λ для разных режимов
    /// 
    /// Режимы течения:
    /// - Ламинарный: Re &lt; 2300
    /// - Переходный: 2300 ≤ Re ≤ 4000
    /// - Турбулентный: Re &gt; 4000
    /// </remarks>
    public static class FlowRegimeCalculator
    {
        /// <summary>
        /// Граница ламинарного режима
        /// </summary>
        public const double LaminarBoundary = 2300;
        
        /// <summary>
        /// Граница турбулентного режима
        /// </summary>
        public const double TurbulentBoundary = 4000;
        
        /// <summary>
        /// Шероховатость PE-Xa труб, мм
        /// </summary>
        public const double PEXaRoughness = 0.007;
        
        /// <summary>
        /// Определить режим течения по числу Рейнольдса
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>Режим течения</returns>
        public static FlowRegime DetermineFlowRegime(double reynoldsNumber)
        {
            if (reynoldsNumber < LaminarBoundary)
                return FlowRegime.Laminar;
            else if (reynoldsNumber <= TurbulentBoundary)
                return FlowRegime.Transitional;
            else
                return FlowRegime.Turbulent;
        }
        
        /// <summary>
        /// Проверить, является ли режим ламинарным
        /// </summary>
        public static bool IsLaminar(double reynoldsNumber)
        {
            return reynoldsNumber < LaminarBoundary;
        }
        
        /// <summary>
        /// Проверить, является ли режим переходным
        /// </summary>
        public static bool IsTransitional(double reynoldsNumber)
        {
            return reynoldsNumber >= LaminarBoundary && reynoldsNumber <= TurbulentBoundary;
        }
        
        /// <summary>
        /// Проверить, является ли режим турбулентным
        /// </summary>
        public static bool IsTurbulent(double reynoldsNumber)
        {
            return reynoldsNumber > TurbulentBoundary;
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для ламинарного режима
        /// Формула Пуазейля: λ = 64 / Re
        /// </summary>
        public static double CalculateLaminarFrictionFactor(double reynoldsNumber)
        {
            if (reynoldsNumber <= 0)
                throw new ArgumentException("Число Рейнольдса должно быть положительным", nameof(reynoldsNumber));
            
            return 64.0 / reynoldsNumber;
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для переходного режима
        /// Линейная интерполяция между ламинарным и турбулентным
        /// </summary>
        public static double CalculateTransitionalFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            if (reynoldsNumber < LaminarBoundary || reynoldsNumber > TurbulentBoundary)
                throw new ArgumentException(
                    $"Число Рейнольдса должно быть в диапазоне [{LaminarBoundary}, {TurbulentBoundary}]",
                    nameof(reynoldsNumber));
            
            // Коэффициент трения на границе ламинарного режима
            double lambda_lam = CalculateLaminarFrictionFactor(LaminarBoundary);
            
            // Коэффициент трения на границе турбулентного режима
            double lambda_turb = CalculateTurbulentFrictionFactor(TurbulentBoundary, innerDiameter_mm, roughness_mm);
            
            // Линейная интерполяция
            double ratio = (reynoldsNumber - LaminarBoundary) / (TurbulentBoundary - LaminarBoundary);
            return lambda_lam + ratio * (lambda_turb - lambda_lam);
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для турбулентного режима
        /// Формула Колбрука-Уайта (итерационное решение)
        /// </summary>
        public static double CalculateTurbulentFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            if (reynoldsNumber <= TurbulentBoundary)
                throw new ArgumentException(
                    $"Число Рейнольдса должно быть больше {TurbulentBoundary}",
                    nameof(reynoldsNumber));
            
            // Начальное приближение (формула Блазиуса)
            double lambda = 0.316 / Math.Pow(reynoldsNumber, 0.25);
            
            // Итерационное решение формулы Колбрука-Уайта
            // 1 / √λ = -2 × lg(ε / (3.7 × di) + 2.51 / (Re × √λ))
            
            for (int i = 0; i < 20; i++)
            {
                double sqrtLambda = Math.Sqrt(lambda);
                double term1 = roughness_mm / (3.7 * innerDiameter_mm);
                double term2 = 2.51 / (reynoldsNumber * sqrtLambda);
                
                double newLambda = Math.Pow(-2 * Math.Log10(term1 + term2), -2);
                
                if (Math.Abs(newLambda - lambda) < 1e-10)
                    break;
                
                lambda = newLambda;
            }
            
            return lambda;
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для любого режима
        /// </summary>
        public static double CalculateFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm = PEXaRoughness)
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
        /// Получить описание режима течения
        /// </summary>
        public static string GetFlowRegimeDescription(FlowRegime regime)
        {
            return regime switch
            {
                FlowRegime.Laminar => "Ламинарный режим (Re < 2300). Плавное, упорядоченное движение жидкости слоями.",
                FlowRegime.Transitional => "Переходный режим (2300 ≤ Re ≤ 4000). Неустойчивый режим между ламинарным и турбулентным.",
                FlowRegime.Turbulent => "Турбулентный режим (Re > 4000). Хаотичное движение жидкости с вихрями.",
                _ => "Неизвестный режим"
            };
        }
        
        /// <summary>
        /// Получить рекомендации по режиму течения
        /// </summary>
        public static string GetFlowRegimeRecommendation(FlowRegime regime)
        {
            return regime switch
            {
                FlowRegime.Laminar => "Рекомендуется увеличить расход или уменьшить диаметр трубы для перехода в турбулентный режим.",
                FlowRegime.Transitional => "ВНИМАНИЕ: Переходный режим нестабилен. Рекомендуется изменить параметры для обеспечения стабильного течения.",
                FlowRegime.Turbulent => "Оптимальный режим для теплообмена. Рекомендуется поддерживать Re > 4000.",
                _ => ""
            };
        }
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/FlowRegimeCalculatorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class FlowRegimeCalculatorTests
    {
        [Test]
        public void DetermineFlowRegime_ReturnsLaminarForLowRe()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(1000), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(2000), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(2299), Is.EqualTo(FlowRegime.Laminar));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsTransitionalForMediumRe()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(2300), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(3000), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(4000), Is.EqualTo(FlowRegime.Transitional));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsTurbulentForHighRe()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(4001), Is.EqualTo(FlowRegime.Turbulent));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(5000), Is.EqualTo(FlowRegime.Turbulent));
            Assert.That(FlowRegimeCalculator.DetermineFlowRegime(10000), Is.EqualTo(FlowRegime.Turbulent));
        }
        
        [Test]
        public void IsLaminar_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.IsLaminar(1000), Is.True);
            Assert.That(FlowRegimeCalculator.IsLaminar(3000), Is.False);
        }
        
        [Test]
        public void IsTransitional_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.IsTransitional(1000), Is.False);
            Assert.That(FlowRegimeCalculator.IsTransitional(3000), Is.True);
            Assert.That(FlowRegimeCalculator.IsTransitional(5000), Is.False);
        }
        
        [Test]
        public void IsTurbulent_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.IsTurbulent(1000), Is.False);
            Assert.That(FlowRegimeCalculator.IsTurbulent(5000), Is.True);
        }
        
        [Test]
        public void CalculateLaminarFrictionFactor_ReturnsCorrectValue()
        {
            // Arrange
            double re = 2000;
            
            // Act
            double lambda = FlowRegimeCalculator.CalculateLaminarFrictionFactor(re);
            
            // Assert
            // λ = 64 / Re = 64 / 2000 = 0.032
            Assert.That(lambda, Is.EqualTo(0.032).Within(0.0001));
        }
        
        [Test]
        public void CalculateLaminarFrictionFactor_ThrowsForInvalidRe()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                FlowRegimeCalculator.CalculateLaminarFrictionFactor(0));
            Assert.Throws<ArgumentException>(() => 
                FlowRegimeCalculator.CalculateLaminarFrictionFactor(-100));
        }
        
        [Test]
        public void CalculateTransitionalFrictionFactor_ReturnsInterpolatedValue()
        {
            // Arrange
            double re = 3000; // Середина переходного диапазона
            double diameter = 16;
            double roughness = 0.007;
            
            // Act
            double lambda = FlowRegimeCalculator.CalculateTransitionalFrictionFactor(re, diameter, roughness);
            
            // Assert
            // Должно быть между λ_lam ≈ 0.0278 и λ_turb ≈ 0.04
            Assert.That(lambda, Is.GreaterThan(0.0278));
            Assert.That(lambda, Is.LessThan(0.04));
        }
        
        [Test]
        public void CalculateTurbulentFrictionFactor_ReturnsCorrectValue()
        {
            // Arrange
            double re = 10000;
            double diameter = 16;
            double roughness = 0.007;
            
            // Act
            double lambda = FlowRegimeCalculator.CalculateTurbulentFrictionFactor(re, diameter, roughness);
            
            // Assert
            // Для Re=10000, di=16mm, ε=0.007mm: λ ≈ 0.03-0.04
            Assert.That(lambda, Is.GreaterThan(0.02));
            Assert.That(lambda, Is.LessThan(0.05));
        }
        
        [Test]
        public void CalculateFrictionFactor_WorksForAllRegimes()
        {
            // Arrange
            double diameter = 16;
            double roughness = 0.007;
            
            // Act & Assert
            double lambdaLam = FlowRegimeCalculator.CalculateFrictionFactor(2000, diameter, roughness);
            Assert.That(lambdaLam, Is.EqualTo(0.032).Within(0.001));
            
            double lambdaTrans = FlowRegimeCalculator.CalculateFrictionFactor(3000, diameter, roughness);
            Assert.That(lambdaTrans, Is.GreaterThan(0.027));
            Assert.That(lambdaTrans, Is.LessThan(0.04));
            
            double lambdaTurb = FlowRegimeCalculator.CalculateFrictionFactor(10000, diameter, roughness);
            Assert.That(lambdaTurb, Is.GreaterThan(0.02));
            Assert.That(lambdaTurb, Is.LessThan(0.05));
        }
        
        [Test]
        public void GetFlowRegimeDescription_ReturnsCorrectDescription()
        {
            // Act & Assert
            Assert.That(FlowRegimeCalculator.GetFlowRegimeDescription(FlowRegime.Laminar), Does.Contain("Ламинарный"));
            Assert.That(FlowRegimeCalculator.GetFlowRegimeDescription(FlowRegime.Transitional), Does.Contain("Переходный"));
            Assert.That(FlowRegimeCalculator.GetFlowRegimeDescription(FlowRegime.Turbulent), Does.Contain("Турбулентный"));
        }
        
        [Test]
        public void GetFlowRegimeRecommendation_ReturnsWarningForTransitional()
        {
            // Act
            string recommendation = FlowRegimeCalculator.GetFlowRegimeRecommendation(FlowRegime.Transitional);
            
            // Assert
            Assert.That(recommendation, Does.Contain("ВНИМАНИЕ"));
            Assert.That(recommendation, Does.Contain("нестабилен"));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `FlowRegimeCalculator.cs` создан
- [ ] Реализованы все методы для определения режима течения
- [ ] Формула Колбрука-Уайта сходится за 20 итераций
- [ ] Граничные значения Re = 2300 и Re = 4000 корректны
- [ ] Unit-тесты для всех методов проходят успешно
- [ ] XML-документация для всех методов
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Класс статический, не требует DI
- Константы `LaminarBoundary` и `TurbulentBoundary` могут использоваться в других классах
- Формула Колбрука-Уайта решается итерационно с точностью 1e-10