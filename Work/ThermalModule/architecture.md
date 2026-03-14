# Архитектура модуля теплового расчёта

## Калькулятор снеготаяния РЕХАУ

**Версия:** 1.0  
**Дата:** 15.03.2026  
**Статус:** Утверждено  
**Автор:** Архитектор

---

## 1. Обзор архитектуры

### 1.1. Назначение
Модуль теплового расчёта выполняет расчёт требуемой мощности системы снеготаяния по методике Chapman-Katunich с поправками EN 1264-2.

### 1.2. Диаграмма компонентов

```
┌─────────────────────────────────────────────────────────────────┐
│                         View Layer                               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ThermalView.xaml                      │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │    │
│  │  │ ModeSelect  │  │ TempInputs  │  │ ResultsGrid │     │    │
│  │  │ (ComboBox)  │  │ (TextBoxes) │  │ (DataGrid)  │     │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘     │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │ Data Binding
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       ViewModel Layer                            │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  ThermalViewModel                         │    │
│  │  - Mode: OperatingMode                                    │    │
│  │  - SupplyTemperature: double                              │    │
│  │  - DeltaT: double                                         │    │
│  │  - GroundTemperature: double                              │    │
│  │  - SelectedPipe: PipeType                                 │    │
│  │  - PipeSpacing: double                                    │    │
│  │  - Result: IThermalCalculationResult                      │    │
│  │  + CalculateCommand                                       │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │ IThermalCalculator
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Service Layer                             │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  ThermalCalculator                        │    │
│  │  - CalculateHeatTransferCoefficient()                    │    │
│  │  - CalculatePowerUp()                                     │    │
│  │  - CalculateThermalResistance()                          │    │
│  │  - CalculateRodTheory()                                   │    │
│  │  - CalculateExcessTemperature()                           │    │
│  │  - CalculateFlowRate()                                    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│  ┌───────────────────────┐  ┌───────────────────────────────┐  │
│  │  IClimateData         │  │  IConstructionData            │  │
│  │  (from ClimateModule) │  │  (from ConstructionModule)    │  │
│  └───────────────────────┘  └───────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3. Поток данных

```
1. Пользователь выбирает режим работы (Антиобледенение/Таяние/Интенсивное)
   ↓
2. Вводит температуру подачи и ΔT
   ↓
3. ThermalViewModel передаёт данные в ThermalCalculator
   ↓
4. ThermalCalculator получает климатические данные (IClimateData)
   ↓
5. ThermalCalculator получает данные конструкции (IConstructionData)
   ↓
6. Выполняется расчёт:
   a) α = 2.26 × (t_П - t_H)^0.33 + 2.6 × v_H
   b) q_FB = Q_таяние + Q_изл + Q_конв
   c) RFb, RD = сопротивления
   d) m, ηR = теория стержня
   e) JHmü = избыточная температура
   f) T_mean, T_return = температуры
   g) ṁ, V_dot = расходы
   ↓
7. Результат возвращается в ThermalViewModel
   ↓
8. Отображается в UI и передаётся в модуль гидравлики
```

---

## 2. Слои приложения

### 2.1. Model Layer (Модели данных)

#### Расположение
`src/Models/Thermal/`

#### Классы

##### OperatingMode.cs
```csharp
namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Режим работы системы снеготаяния
    /// </summary>
    public enum OperatingMode
    {
        /// <summary>
        /// Антиобледенение (t_П = +3°C)
        /// </summary>
        AntiIcing = 3,
        
        /// <summary>
        /// Таяние (t_П = +5°C)
        /// </summary>
        Melting = 5,
        
        /// <summary>
        /// Интенсивное (t_П = +7°C)
        /// </summary>
        Intensive = 7
    }
}
```

##### PipeType.cs
```csharp
namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Тип трубы РЕХАУ
    /// </summary>
    public class PipeType
    {
        public string Name { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public double OuterDiameter { get; set; }  // мм
        public double InnerDiameter { get; set; }   // мм
        public double WallThickness { get; set; }   // мм
        public double ThermalConductivity { get; set; } // Вт/м·К
        
        /// <summary>
        /// Стандартные трубы РЕХАУ
        /// </summary>
        public static PipeType[] StandardPipes => new[]
        {
            new PipeType
            {
                Name = "RAUTHERM S 17x2,0",
                Article = "12180501001",
                OuterDiameter = 17,
                InnerDiameter = 13,
                WallThickness = 2.0,
                ThermalConductivity = 0.35
            },
            new PipeType
            {
                Name = "RAUTHERM S 20x2,0",
                Article = "12180502001",
                OuterDiameter = 20,
                InnerDiameter = 16,
                WallThickness = 2.0,
                ThermalConductivity = 0.35
            },
            new PipeType
            {
                Name = "RAUTHERM S 25x2,3",
                Article = "12180503001",
                OuterDiameter = 25,
                InnerDiameter = 20.4,
                WallThickness = 2.3,
                ThermalConductivity = 0.35
            }
        };
    }
}
```

##### ThermalCalculationResult.cs
```csharp
namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Результат теплового расчёта
    /// </summary>
    public class ThermalCalculationResult : IThermalCalculationResult
    {
        // Коэффициенты
        public double Alpha { get; set; }           // Вт/м²·К
        
        // Мощности
        public double PowerUp { get; set; }         // Вт/м² (q_FB)
        public double PowerDown { get; set; }        // Вт/м² (q_D)
        public double PowerTotal { get; set; }      // Вт/м² (q_total)
        
        // Составляющие мощности
        public double MeltingHeat { get; set; }      // Вт/м² (Q_таяние)
        public double RadiationHeat { get; set; }    // Вт/м² (Q_изл)
        public double ConvectionHeat { get; set; }   // Вт/м² (Q_конв)
        
        // Температуры
        public double ExcessTemperature { get; set; } // °C (JHmü)
        public double MeanTemperature { get; set; }   // °C (T_mean)
        public double SupplyTemperature { get; set; } // °C (T_supply)
        public double ReturnTemperature { get; set; } // °C (T_return)
        public double DeltaT { get; set; }           // К
        
        // Сопротивления
        public double R1Total { get; set; }          // м²·К/Вт
        public double R2Total { get; set; }          // м²·К/Вт
        public double RFb { get; set; }              // м²·К/Вт
        public double RD { get; set; }                // м²·К/Вт
        
        // Теория стержня
        public double ParameterM { get; set; }       // 1/м
        public double EfficiencyEtaR { get; set; }   // безразмерный
        
        // Расходы
        public double MassFlowRate { get; set; }     // кг/(ч·м²)
        public double VolumeFlowRate { get; set; }   // л/(ч·м²)
        
        // Валидация
        public bool IsValid { get; set; }
        public string[] ValidationErrors { get; set; } = Array.Empty<string>();
        
        // Событие
        public event EventHandler<ThermalResultChangedEventArgs>? ResultChanged;
        
        public void RaiseResultChanged()
        {
            ResultChanged?.Invoke(this, new ThermalResultChangedEventArgs { Result = this });
        }
    }
    
    public class ThermalResultChangedEventArgs : EventArgs
    {
        public ThermalCalculationResult? Result { get; set; }
    }
}
```

##### ThermalParameters.cs
```csharp
namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Параметры для теплового расчёта
    /// </summary>
    public class ThermalParameters
    {
        // Режим работы
        public OperatingMode Mode { get; set; } = OperatingMode.Melting;
        
        // Температуры
        public double SupplyTemperature { get; set; } = 50.0;  // °C
        public double DeltaT { get; set; } = 15.0;              // К
        public double GroundTemperature { get; set; } = 10.0;  // °C
        
        // Труба
        public PipeType Pipe { get; set; } = PipeType.StandardPipes[1]; // 20x2,0 по умолчанию
        public double PipeSpacing { get; set; } = 200.0;       // мм
        
        // Конструкция (от IConstructionData)
        public double R1Total { get; set; }  // м²·К/Вт
        public double R2Total { get; set; }  // м²·К/Вт
        public double LambdaE { get; set; }  // Вт/м·К (теплопроводность стяжки)
        
        // Климат (от IClimateData)
        public double AirTemperature { get; set; }    // °C
        public double WindSpeed { get; set; }         // м/с
        public double SnowfallIntensity { get; set; } // см/ч
        
        // Теплоноситель
        public double CoolantDensity { get; set; }      // кг/м³
        public double CoolantHeatCapacity { get; set; } // кДж/кг·К
    }
}
```

---

### 2.2. Service Layer

#### Расположение
`src/Services/Thermal/`

#### IThermalCalculator.cs
```csharp
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Интерфейс калькулятора теплового расчёта
    /// </summary>
    public interface IThermalCalculator
    {
        /// <summary>
        /// Рассчитать коэффициент теплоотдачи
        /// </summary>
        double CalculateHeatTransferCoefficient(double surfaceTemp, double airTemp, double windSpeed);
        
        /// <summary>
        /// Рассчитать мощность вверх (q_FB)
        /// </summary>
        double CalculatePowerUp(double snowfallIntensity, double surfaceTemp, 
                                 double airTemp, double alpha);
        
        /// <summary>
        /// Рассчитать тепловое сопротивление
        /// </summary>
        (double RFb, double RD) CalculateThermalResistance(double r1Total, double r2Total, double alpha);
        
        /// <summary>
        /// Рассчитать параметры теории стержня
        /// </summary>
        (double m, double etaR) CalculateRodTheory(double rFb, double rD, 
                                                    double lambdaE, double dE, double spacing);
        
        /// <summary>
        /// Рассчитать избыточную температуру
        /// </summary>
        double CalculateExcessTemperature(double etaR, double rFb, double rD,
                                          double q_FB, double airTemp, double groundTemp,
                                          double spacing, double lambdaR, double d, double s);
        
        /// <summary>
        /// Выполнить полный расчёт
        /// </summary>
        ThermalCalculationResult Calculate(ThermalParameters parameters);
        
        /// <summary>
        /// Валидация параметров
        /// </summary>
        bool Validate(ThermalParameters parameters, out string[] errors);
    }
}
```

#### ThermalCalculator.cs
```csharp
using System;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Калькулятор теплового расчёта по методике Chapman-Katunich
    /// </summary>
    public class ThermalCalculator : IThermalCalculator
    {
        // Константы
        private const double SnowDensity = 900;           // кг/м³
        private const double IceHeatCapacity = 2100;      // Дж/кг·К
        private const double IceMeltingHeat = 330000;     // Дж/кг
        private const double WaterHeatCapacity = 4200;    // Дж/кг·К
        private const double StefanBoltzmann = 5.77e-8;   // Вт/м²·К⁴
        private const double EmissionCoefficient = 0.055;
        private const double AlphaBottom = 999999999;    // Адиабата
        
        /// <summary>
        /// Рассчитать коэффициент теплоотдачи
        /// Формула: α = 2.26 × (t_П - t_H)^0.33 + 2.6 × v_H
        /// </summary>
        public double CalculateHeatTransferCoefficient(double surfaceTemp, double airTemp, double windSpeed)
        {
            double deltaT = surfaceTemp - airTemp;
            double alpha = 2.26 * Math.Pow(deltaT, 0.33) + 2.6 * windSpeed;
            return alpha;
        }
        
        /// <summary>
        /// Рассчитать мощность вверх (q_FB)
        /// q_FB = Q_таяние + Q_изл + Q_конв
        /// </summary>
        public double CalculatePowerUp(double snowfallIntensity, double surfaceTemp, 
                                         double airTemp, double alpha)
        {
            // Q_таяние = (h / 3600) × 900 × [2100 × (0 - t_H) + 330000 + 4200 × (t_П - 0)]
            double h = snowfallIntensity / 100.0; // см/ч → м/ч
            double meltingHeat = (h / 3600.0) * SnowDensity * 
                                 (IceHeatCapacity * (0 - airTemp) + IceMeltingHeat + 
                                  WaterHeatCapacity * (surfaceTemp - 0));
            
            // Q_изл = 0.055 × 5.77 × [(273 + t_П) / 100]^4
            double radiationHeat = EmissionCoefficient * StefanBoltzmann * 
                                   Math.Pow((273.0 + surfaceTemp) / 100.0, 4) * 1e8;
            
            // Q_конв = α × (t_П - t_H)
            double convectionHeat = alpha * (surfaceTemp - airTemp);
            
            return meltingHeat + radiationHeat + convectionHeat;
        }
        
        /// <summary>
        /// Рассчитать тепловое сопротивление
        /// RFb = R1 + 1/α
        /// RD = R2 + 1/α_низ
        /// </summary>
        public (double RFb, double RD) CalculateThermalResistance(double r1Total, double r2Total, double alpha)
        {
            double rFb = r1Total + 1.0 / alpha;
            double rD = r2Total + 1.0 / AlphaBottom;
            return (rFb, rD);
        }
        
        /// <summary>
        /// Рассчитать параметры теории стержня
        /// m = 0.6 × √[(1/RFb + 1/RD) / (λE × dE)]
        /// ηR = tanh(m × lR / 2) / (m × lR / 2)
        /// </summary>
        public (double m, double etaR) CalculateRodTheory(double rFb, double rD, 
                                                          double lambdaE, double dE, double spacing)
        {
            // Параметр m
            double m = 0.6 * Math.Sqrt((1.0 / rFb + 1.0 / rD) / (lambdaE * dE / 1000.0));
            
            // Аргумент x = m × lR / 2
            double lR = spacing / 1000.0; // мм → м
            double x = m * lR / 2.0;
            
            // tanh(x) = 1 - 2 / (e^(2x) + 1)
            double tanhX = 1.0 - 2.0 / (Math.Exp(2.0 * x) + 1.0);
            
            // ηR = tanh(x) / x
            double etaR = x > 0 ? tanhX / x : 1.0;
            
            return (m, etaR);
        }
        
        /// <summary>
        /// Рассчитать избыточную температуру
        /// JHmü = [A + (B - C/(q_FB × RFb × RD)) × D × E] × q_FB × RFb
        /// </summary>
        public double CalculateExcessTemperature(double etaR, double rFb, double rD,
                                                  double q_FB, double airTemp, double groundTemp,
                                                  double spacing, double lambdaR, double d, double s)
        {
            // Вспомогательные коэффициенты
            double A = 1.0 / etaR;
            double B = 1.0 / rFb + 1.0 / rD;
            double C = Math.Abs(airTemp - groundTemp);
            double lR = spacing / 1000.0; // мм → м
            double D = lR / (Math.PI * lambdaR);
            double E = (s / 1000.0) / ((d - s) / 1000.0);
            
            // Избыточная температура
            double denominator = q_FB * rFb * rD;
            double jhmue = (A + (B - C / denominator) * D * E) * q_FB * rFb;
            
            return jhmue;
        }
        
        /// <summary>
        /// Выполнить полный расчёт
        /// </summary>
        public ThermalCalculationResult Calculate(ThermalParameters parameters)
        {
            var result = new ThermalCalculationResult();
            
            // Валидация
            if (!Validate(parameters, out var errors))
            {
                result.IsValid = false;
                result.ValidationErrors = errors;
                return result;
            }
            
            // 1. Коэффициент теплоотдачи
            double surfaceTemp = (int)parameters.Mode; // +3, +5 или +7°C
            result.Alpha = CalculateHeatTransferCoefficient(
                surfaceTemp, parameters.AirTemperature, parameters.WindSpeed);
            
            // 2. Мощность вверх
            result.PowerUp = CalculatePowerUp(
                parameters.SnowfallIntensity, surfaceTemp, 
                parameters.AirTemperature, result.Alpha);
            
            // 3. Тепловое сопротивление
            (result.RFb, result.RD) = CalculateThermalResistance(
                parameters.R1Total, parameters.R2Total, result.Alpha);
            
            // 4. Теория стержня
            (result.ParameterM, result.EfficiencyEtaR) = CalculateRodTheory(
                result.RFb, result.RD, parameters.LambdaE,
                parameters.Pipe.OuterDiameter, parameters.PipeSpacing);
            
            // 5. Избыточная температура
            result.ExcessTemperature = CalculateExcessTemperature(
                result.EfficiencyEtaR, result.RFb, result.RD,
                result.PowerUp, parameters.AirTemperature, parameters.GroundTemperature,
                parameters.PipeSpacing, parameters.Pipe.ThermalConductivity,
                parameters.Pipe.OuterDiameter, parameters.Pipe.WallThickness);
            
            // 6. Температуры
            result.MeanTemperature = result.ExcessTemperature + parameters.AirTemperature;
            result.SupplyTemperature = parameters.SupplyTemperature;
            result.ReturnTemperature = result.MeanTemperature - 
                                        (parameters.SupplyTemperature - result.MeanTemperature);
            result.DeltaT = parameters.DeltaT;
            
            // 7. Мощность вниз (упрощённо)
            result.PowerDown = result.PowerUp * 0.1; // Приближённо 10% потерь
            result.PowerTotal = result.PowerUp + result.PowerDown;
            
            // 8. Расход
            double cp = parameters.CoolantHeatCapacity;
            result.MassFlowRate = result.PowerTotal / (cp / 3.6) / parameters.DeltaT;
            result.VolumeFlowRate = result.MassFlowRate / parameters.CoolantDensity * 1000;
            
            result.IsValid = true;
            return result;
        }
        
        /// <summary>
        /// Валидация параметров
        /// </summary>
        public bool Validate(ThermalParameters parameters, out string[] errors)
        {
            var errorList = new List<string>();
            
            if (parameters.SupplyTemperature <= parameters.AirTemperature + 10)
                errorList.Add("Температура подачи должна быть значительно выше температуры наружного воздуха");
            
            if (parameters.SupplyTemperature > 65)
                errorList.Add("Максимальная температура подачи 65°C (PE-Xa)");
            
            if (parameters.DeltaT < 10 || parameters.DeltaT > 15)
                errorList.Add("Температурный перепад должен быть 10–15 К");
            
            if (parameters.PipeSpacing < 150)
                errorList.Add("Минимальный шаг укладки 150 мм");
            
            if (parameters.Pipe.OuterDiameter == 20 && parameters.PipeSpacing < 200)
                errorList.Add("Для трубы Ø20 мм минимальный шаг 200 мм");
            
            errors = errorList.ToArray();
            return errorList.Count == 0;
        }
    }
}
```

---

### 2.3. ViewModel Layer

#### Расположение
`src/ViewModels/Thermal/`

#### ThermalViewModel.cs
```csharp
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.ViewModels.Thermal
{
    public partial class ThermalViewModel : ObservableObject
    {
        private readonly IThermalCalculator _calculator;
        
        #region Observable Properties
        
        [ObservableProperty]
        private OperatingMode _selectedMode = OperatingMode.Melting;
        
        [ObservableProperty]
        private double _supplyTemperature = 50.0;
        
        [ObservableProperty]
        private double _deltaT = 15.0;
        
        [ObservableProperty]
        private double _groundTemperature = 10.0;
        
        [ObservableProperty]
        private PipeType _selectedPipe = PipeType.StandardPipes[1];
        
        [ObservableProperty]
        private double _pipeSpacing = 200.0;
        
        [ObservableProperty]
        private ThermalCalculationResult? _result;
        
        [ObservableProperty]
        private bool _isCalculating;
        
        [ObservableProperty]
        private string _validationMessage = string.Empty;
        
        public ObservableCollection<PipeType> AvailablePipes { get; } = 
            new ObservableCollection<PipeType>(PipeType.StandardPipes);
        
        public ObservableCollection<OperatingMode> AvailableModes { get; } = 
            new ObservableCollection<OperatingMode>
            {
                OperatingMode.AntiIcing,
                OperatingMode.Melting,
                OperatingMode.Intensive
            };
        
        #endregion
        
        #region Commands
        
        [RelayCommand]
        private async Task Calculate()
        {
            if (IsCalculating) return;
            
            IsCalculating = true;
            try
            {
                // TODO: Получить параметры из ClimateViewModel и ConstructionViewModel
                var parameters = new ThermalParameters
                {
                    Mode = SelectedMode,
                    SupplyTemperature = SupplyTemperature,
                    DeltaT = DeltaT,
                    GroundTemperature = GroundTemperature,
                    Pipe = SelectedPipe,
                    PipeSpacing = PipeSpacing,
                    // Остальные параметры будут установлены из других модулей
                    AirTemperature = -15.0, // Временно
                    WindSpeed = 5.0,
                    SnowfallIntensity = 0.3,
                    R1Total = 0.05,
                    R2Total = 1.0,
                    LambdaE = 1.6,
                    CoolantDensity = 1053,
                    CoolantHeatCapacity = 3.39
                };
                
                Result = _calculator.Calculate(parameters);
                
                if (!Result.IsValid)
                {
                    ValidationMessage = string.Join("; ", Result.ValidationErrors);
                }
                else
                {
                    ValidationMessage = string.Empty;
                }
            }
            finally
            {
                IsCalculating = false;
            }
        }
        
        #endregion
        
        #region Constructor
        
        public ThermalViewModel(IThermalCalculator calculator)
        {
            _calculator = calculator;
        }
        
        #endregion
    }
}
```

---

## 3. Диаграмма классов (текстовая)

```
┌─────────────────────────────────────────────────────────────────┐
│                         Models.Thermal                           │
├─────────────────────────────────────────────────────────────────┤
│  OperatingMode (enum)                                            │
│  ├── AntiIcing = 3                                               │
│  ├── Melting = 5                                                 │
│  └── Intensive = 7                                               │
│                                                                  │
│  PipeType                                                        │
│  ├── Name, Article                                               │
│  ├── OuterDiameter, InnerDiameter, WallThickness                 │
│  └── ThermalConductivity                                         │
│                                                                  │
│  ThermalParameters                                                │
│  ├── Mode, SupplyTemperature, DeltaT, GroundTemperature         │
│  ├── Pipe, PipeSpacing                                           │
│  ├── R1Total, R2Total, LambdaE                                   │
│  └── AirTemperature, WindSpeed, SnowfallIntensity               │
│                                                                  │
│  ThermalCalculationResult : IThermalCalculationResult            │
│  ├── Alpha, PowerUp, PowerDown, PowerTotal                      │
│  ├── ExcessTemperature, MeanTemperature, SupplyTemperature       │
│  ├── R1Total, R2Total, RFb, RD                                   │
│  ├── ParameterM, EfficiencyEtaR                                   │
│  └── MassFlowRate, VolumeFlowRate                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Services.Thermal                           │
├─────────────────────────────────────────────────────────────────┤
│  IThermalCalculator                                              │
│  ├── CalculateHeatTransferCoefficient()                          │
│  ├── CalculatePowerUp()                                           │
│  ├── CalculateThermalResistance()                                 │
│  ├── CalculateRodTheory()                                         │
│  ├── CalculateExcessTemperature()                                  │
│  ├── Calculate()                                                  │
│  └── Validate()                                                   │
│                                                                  │
│  ThermalCalculator : IThermalCalculator                          │
│  └── (реализация формул Chapman-Katunich)                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       ViewModels.Thermal                          │
├─────────────────────────────────────────────────────────────────┤
│  ThermalViewModel : ObservableObject                             │
│  ├── SelectedMode, SupplyTemperature, DeltaT                     │
│  ├── GroundTemperature, SelectedPipe, PipeSpacing                │
│  ├── Result: ThermalCalculationResult                            │
│  ├── AvailablePipes, AvailableModes                              │
│  │                                                               │
│  └── + CalculateCommand                                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Формулы расчёта

### 4.1. Коэффициент теплоотдачи
```
α = 2.26 × (t_П - t_H)^0.33 + 2.6 × v_H    [Вт/м²·К]
```

### 4.2. Мощность вверх
```
Q_таяние = (h / 3600) × 900 × [2100 × (0 - t_H) + 330000 + 4200 × (t_П - 0)]
Q_изл = 0.055 × 5.77 × [(273 + t_П) / 100]^4
Q_конв = α × (t_П - t_H)
q_FB = Q_таяние + Q_изл + Q_конв
```

### 4.3. Тепловое сопротивление
```
RFb = R1 + 1/α
RD = R2 + 1/α_низ    (α_низ ≈ 999999999)
```

### 4.4. Теория стержня
```
m = 0.6 × √[(1/RFb + 1/RD) / (λE × dE)]
ηR = tanh(m × lR / 2) / (m × lR / 2)
```

### 4.5. Избыточная температура
```
JHmü = [A + (B - C/(q_FB × RFb × RD)) × D × E] × q_FB × RFb
где:
  A = 1/ηR
  B = 1/RFb + 1/RD
  C = |t_H - t_G|
  D = lR / (π × λR)
  E = s / (d - s)
```

### 4.6. Расход
```
q_total = q_FB + q_D
ṁ = q_total / (c_p / 3.6) / ΔT
V_dot = ṁ / ρ × 1000
```

---

## 5. Тестирование

### 5.1. Unit тесты

```csharp
[TestFixture]
public class ThermalCalculatorTests
{
    private ThermalCalculator _calculator = null!;
    
    [SetUp]
    public void Setup()
    {
        _calculator = new ThermalCalculator();
    }
    
    [Test]
    public void CalculateHeatTransferCoefficient_ValidInput_ReturnsCorrectValue()
    {
        // Arrange
        double surfaceTemp = 5.0;  // °C
        double airTemp = -28.0;    // °C
        double windSpeed = 4.5;    // м/с
        
        // Act
        double alpha = _calculator.CalculateHeatTransferCoefficient(surfaceTemp, airTemp, windSpeed);
        
        // Assert
        Assert.That(alpha, Is.GreaterThan(0));
        Assert.That(alpha, Is.InRange(10, 30)); // Типичный диапазон
    }
    
    [Test]
    public void CalculatePowerUp_MoscowWinter_ReturnsExpectedRange()
    {
        // Arrange
        double snowfall = 0.3;  // см/ч
        double surfaceTemp = 5.0;
        double airTemp = -28.0;
        double alpha = 15.0;
        
        // Act
        double power = _calculator.CalculatePowerUp(snowfall, surfaceTemp, airTemp, alpha);
        
        // Assert
        Assert.That(power, Is.InRange(100, 300)); // Вт/м²
    }
    
    [Test]
    public void CalculateRodTheory_ValidInput_ReturnsEfficiencyInRange()
    {
        // Arrange
        double rFb = 0.05;
        double rD = 1.0;
        double lambdaE = 1.6;
        double dE = 20.0;  // мм
        double spacing = 200.0;  // мм
        
        // Act
        var (m, etaR) = _calculator.CalculateRodTheory(rFb, rD, lambdaE, dE, spacing);
        
        // Assert
        Assert.That(m, Is.GreaterThan(0));
        Assert.That(etaR, Is.InRange(0, 1)); // КПД ребра
    }
}
```

---

## 6. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.0 | 15.03.2026 | Архитектор | Начальная версия |