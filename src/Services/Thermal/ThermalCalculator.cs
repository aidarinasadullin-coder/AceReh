using System;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Калькулятор теплового расчёта систем снеготаяния
    /// </summary>
    /// <remarks>
    /// Реализует расчёт по методике РЕХАУ для систем снеготаяния
    /// </remarks>
    public class ThermalCalculator : IThermalCalculator
    {
        #region Константы

        /// <summary>
        /// Плотность снега, кг/м³
        /// </summary>
        private const double SnowDensity = 900.0;

        /// <summary>
        /// Удельная теплоёмкость льда, Дж/кг·К
        /// </summary>
        private const double IceHeatCapacity = 2100.0;

        /// <summary>
        /// Удельная теплота плавления льда, Дж/кг
        /// </summary>
        private const double IceMeltingHeat = 330000.0;

        /// <summary>
        /// Удельная теплоёмкость воды, Дж/кг·К
        /// </summary>
        private const double WaterHeatCapacity = 4200.0;

        /// <summary>
        /// Постоянная Стефана-Больцмана, Вт/м²·К⁴
        /// </summary>
        private const double StefanBoltzmann = 5.77e-8;

        /// <summary>
        /// Коэффициент излучения поверхности
        /// </summary>
        private const double EmissionCoefficient = 0.055;

        /// <summary>
        /// Коэффициент теплоотдачи снизу (адиабатические условия)
        /// </summary>
        private const double AlphaBottom = 999999999.0;

        /// <summary>
        /// Коэффициент для расчёта параметра m
        /// </summary>
        private const double RodCoefficient = 0.6;

        #endregion

        #region Основные методы расчёта

        /// <summary>
        /// Рассчитать коэффициент теплоотдачи на поверхности
        /// </summary>
        /// <param name="surfaceTemp">Температура поверхности, °C</param>
        /// <param name="airTemp">Температура наружного воздуха, °C</param>
        /// <param name="windSpeed">Скорость ветра, м/с</param>
        /// <returns>Коэффициент теплоотдачи α, Вт/м²·К</returns>
        /// <remarks>
        /// Формула: α = 2.26 × (t_П - t_H)^0.33 + 2.6 × v_H
        /// где:
        /// - t_П - температура поверхности
        /// - t_H - температура наружного воздуха
        /// - v_H - скорость ветра
        /// </remarks>
        public double CalculateHeatTransferCoefficient(double surfaceTemp, double airTemp, double windSpeed)
        {
            // Валидация
            if (windSpeed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windSpeed), "Скорость ветра не может быть отрицательной");
            }

            // Разность температур (поверхность всегда теплее воздуха при снеготаянии)
            var deltaTemp = surfaceTemp - airTemp;

            // Защита от отрицательной разности температур
            if (deltaTemp <= 0)
            {
                // Если поверхность не теплее воздуха, используем минимальное значение
                deltaTemp = 0.1;
            }

            // Формула: α = 2.26 × (t_П - t_H)^0.33 + 2.6 × v_H
            var alpha = 2.26 * Math.Pow(deltaTemp, 0.33) + 2.6 * windSpeed;

            return alpha;
        }

        /// <summary>
        /// Рассчитать требуемую мощность вверх (на поверхность)
        /// </summary>
        /// <param name="snowfallIntensity">Интенсивность снегопада, мм/ч (водяной эквивалент)</param>
        /// <param name="surfaceTemp">Температура поверхности, °C</param>
        /// <param name="airTemp">Температура наружного воздуха, °C</param>
        /// <param name="alpha">Коэффициент теплоотдачи, Вт/м²·К</param>
        /// <returns>Требуемая мощность q_FB, Вт/м²</returns>
        /// <remarks>
        /// Состоит из двух составляющих:
        /// 1. Q_таяние = (h/3600) × ρ × [c_льда × (0 - t_H) + L_плавл + c_воды × (t_П - 0)]
        /// 2. Q_конв = α × (t_П - t_H)
        /// 
        /// Примечание: Q_изл (лучистый тепловой поток) исключён из основного расчёта,
        /// но вычисляется отдельно для справки (RadiationHeat).
        /// </remarks>
        public double CalculatePowerUp(double snowfallIntensity, double surfaceTemp, double airTemp, double alpha)
        {
            // Валидация
            if (snowfallIntensity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(snowfallIntensity), "Интенсивность снегопада не может быть отрицательной");
            }

            if (alpha <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(alpha), "Коэффициент теплоотдачи должен быть положительным");
            }

            // Конвертация интенсивности снегопада из мм/ч в м/с
            // h [м/с] = h [мм/ч] / 1000 / 3600
            var h = snowfallIntensity / 1000.0 / 3600.0;

            // 1. Теплота плавления снега
            // Q_таяние = (h/3600) × ρ × [c_льда × (0 - t_H) + L_плавл + c_воды × (t_П - 0)]
            // Примечание: в формуле (h/3600) уже учтено в конвертации выше
            var qMelting = h * SnowDensity * (
                IceHeatCapacity * (0 - airTemp) +    // нагрев льда до 0°C
                IceMeltingHeat +                     // плавление льда
                WaterHeatCapacity * surfaceTemp      // нагрев воды до t_П
            );

            // 2. Конвективный теплообмен
            // Q_конв = α × (t_П - t_H)
            var qConvection = alpha * (surfaceTemp - airTemp);

            // Суммарная мощность вверх (без лучистого теплообмена)
            // q_FB = Q_таяние + Q_конв
            var powerUp = qMelting + qConvection;

            return powerUp;
        }

        /// <summary>
        /// Рассчитать тепловые сопротивления конструкции
        /// </summary>
        /// <param name="r1Total">Суммарное сопротивление слоёв над трубой, м²·К/Вт</param>
        /// <param name="r2Total">Суммарное сопротивление слоёв под трубой, м²·К/Вт</param>
        /// <param name="alpha">Коэффициент теплоотдачи на поверхности, Вт/м²·К</param>
        /// <returns>Кортеж (RFb, RD) - сопротивления вверх и вниз</returns>
        /// <remarks>
        /// RFb = R1 + 1/α (сопротивление вверх, к поверхности)
        /// RD = R2 + 1/α_низ (сопротивление вниз, адиабата)
        /// </remarks>
        public (double RFb, double RD) CalculateThermalResistance(double r1Total, double r2Total, double alpha)
        {
            // Валидация
            if (r1Total < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(r1Total), "Сопротивление R1 не может быть отрицательным");
            }

            if (r2Total < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(r2Total), "Сопротивление R2 не может быть отрицательным");
            }

            if (alpha <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(alpha), "Коэффициент теплоотдачи должен быть положительным");
            }

            // Сопротивление вверх (к поверхности)
            var rFb = r1Total + 1.0 / alpha;

            // Сопротивление вниз (адиабатические условия)
            var rD = r2Total + 1.0 / AlphaBottom;

            return (rFb, rD);
        }

        /// <summary>
        /// Рассчитать параметры теории стержня (эффективность ребра)
        /// </summary>
        /// <param name="rFb">Полное сопротивление вверх, м²·К/Вт</param>
        /// <param name="rD">Полное сопротивление вниз, м²·К/Вт</param>
        /// <param name="lambdaE">Теплопроводность стяжки, Вт/м·К</param>
        /// <param name="dE">Эквивалентный диаметр трубы, м</param>
        /// <param name="spacing">Шаг укладки трубы, м</param>
        /// <returns>Кортеж (m, ηR) - параметр m и КПД ребра</returns>
        /// <remarks>
        /// m = 0.6 × √[(1/RFb + 1/RD) / (λE × dE)]
        /// ηR = tanh(m × s/2) / (m × s/2)
        /// где s - шаг укладки трубы
        /// </remarks>
        public (double ParameterM, double EfficiencyEtaR) CalculateRodTheory(
            double rFb, double rD, double lambdaE, double dE, double spacing)
        {
            // Валидация
            if (rFb <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rFb), "Сопротивление RFb должно быть положительным");
            }

            if (rD <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rD), "Сопротивление RD должно быть положительным");
            }

            if (lambdaE <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lambdaE), "Теплопроводность должна быть положительной");
            }

            if (dE <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dE), "Диаметр трубы должен быть положительным");
            }

            if (spacing <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spacing), "Шаг укладки должен быть положительным");
            }

            // Параметр m
            // m = 0.6 × √[(1/RFb + 1/RD) / (λE × dE)]
            var sumReciprocal = 1.0 / rFb + 1.0 / rD;
            var denominator = lambdaE * dE;
            var m = RodCoefficient * Math.Sqrt(sumReciprocal / denominator);

            // Аргумент для tanh
            var x = m * spacing / 2.0;

            // КПД ребра
            // ηR = tanh(x) / x
            // tanh(x) = 1 - 2 / (e^(2x) + 1)
            double etaR;

            if (Math.Abs(x) < 0.001)
            {
                // При x → 0, tanh(x)/x → 1
                etaR = 1.0;
            }
            else
            {
                var tanhX = 1.0 - 2.0 / (Math.Exp(2.0 * x) + 1.0);
                etaR = tanhX / x;
            }

            return (m, etaR);
        }

        /// <summary>
        /// Рассчитать избыточную температуру теплоносителя
        /// </summary>
        /// <param name="parameters">Параметры расчёта</param>
        /// <param name="powerUp">Мощность вверх, Вт/м²</param>
        /// <param name="rFb">Сопротивление вверх, м²·К/Вт</param>
        /// <param name="rD">Сопротивление вниз, м²·К/Вт</param>
        /// <param name="etaR">КПД ребра</param>
        /// <returns>Избыточная температура JHmü, °C</returns>
        /// <remarks>
        /// Формула:
        /// JHmü = [A + (B - C/(q_FB × RFb × RD)) × D × E] × q_FB × RFb
        /// где:
        /// A = 1/ηR
        /// B = 1/RFb + 1/RD
        /// C = |t_H - t_G|
        /// D = lR / (π × λR) - lR = шаг труб, λR = теплопроводность трубы
        /// E = s / (d - s) - s = толщина стенки трубы, d = наружный диаметр
        /// </remarks>
        public double CalculateExcessTemperature(
            ThermalParameters parameters,
            double powerUp,
            double rFb,
            double rD,
            double etaR)
        {
            // Валидация
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (powerUp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(powerUp), "Мощность должна быть положительной");
            }

            if (rFb <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rFb), "Сопротивление RFb должно быть положительным");
            }

            if (rD <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rD), "Сопротивление RD должно быть положительным");
            }

            if (etaR <= 0 || etaR > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(etaR), "КПД ребра должен быть в диапазоне (0, 1]");
            }

            var pipe = parameters.Pipe;

            // Коэффициенты формулы
            // A = 1/ηR
            var a = 1.0 / etaR;

            // B = 1/RFb + 1/RD
            var b = 1.0 / rFb + 1.0 / rD;

            // C = |t_H - t_G|
            var c = Math.Abs(parameters.AirTemperature - parameters.GroundTemperature);

            // D = lR / (π × λR)
            // lR = шаг труб (spacing)
            // λR = теплопроводность материала трубы
            var spacingM = parameters.PipeSpacing / 1000.0;      // мм → м
            var lambdaR = pipe.ThermalConductivity;              // Вт/(м·К)
            var dCoefficient = spacingM / (Math.PI * lambdaR);

            // E = s / (d - s)
            // s = толщина стенки трубы
            // d = наружный диаметр трубы
            var wallThicknessM = pipe.WallThickness / 1000.0;    // мм → м
            var outerDiameterM = pipe.OuterDiameter / 1000.0;    // мм → м
            var eCoefficient = wallThicknessM / (outerDiameterM - wallThicknessM);

            // Избыточная температура
            // JHmü = [A + (B - C/(q_FB × RFb × RD)) × D × E] × q_FB × RFb
            var denominator = powerUp * rFb * rD;
            var excessTemp = (a + (b - c / denominator) * dCoefficient * eCoefficient) * powerUp * rFb;

            return excessTemp;
        }

        /// <summary>
        /// Рассчитать мощность вниз (потери) по сложной формуле
        /// </summary>
        /// <param name="meanTemperature">Средняя температура теплоносителя, °C</param>
        /// <param name="groundTemperature">Температура грунта, °C</param>
        /// <param name="airTemperature">Температура наружного воздуха, °C</param>
        /// <param name="rFb">Сопротивление вверх, м²·К/Вт</param>
        /// <param name="rD">Сопротивление вниз, м²·К/Вт</param>
        /// <param name="etaR">КПД ребра</param>
        /// <param name="pipeSpacing">Шаг укладки трубы, мм</param>
        /// <param name="pipeOuterDiameter">Наружный диаметр трубы, мм</param>
        /// <param name="pipeWallThickness">Толщина стенки трубы, мм</param>
        /// <param name="pipeThermalConductivity">Теплопроводность трубы, Вт/(м·К)</param>
        /// <returns>Мощность вниз (потери), Вт/м²</returns>
        /// <remarks>
        /// Формула:
        /// q_D = (JHmü_low × RFb + C × D × E) / (RFb × RD × (A + B × D × E))
        /// 
        /// Где:
        /// - JHmü_low = T_mean - t_G (избыточная температура вниз)
        /// - A = 1/ηR
        /// - B = 1/RFb + 1/RD
        /// - C = |t_H - t_G|
        /// - D = lR / (π × λR)
        /// - E = s / (d - s)
        /// </remarks>
        private double CalculatePowerDown(
            double meanTemperature,
            double groundTemperature,
            double airTemperature,
            double rFb,
            double rD,
            double etaR,
            double pipeSpacing,
            double pipeOuterDiameter,
            double pipeWallThickness,
            double pipeThermalConductivity)
        {
            // JHmü_low = T_mean - t_G (избыточная температура вниз)
            var jhmuLow = meanTemperature - groundTemperature;
            
            // A = 1/ηR
            var a = 1.0 / etaR;
            
            // B = 1/RFb + 1/RD
            var b = 1.0 / rFb + 1.0 / rD;
            
            // C = |t_H - t_G|
            var c = Math.Abs(airTemperature - groundTemperature);
            
            // D = lR / (π × λR)
            var spacingM = pipeSpacing / 1000.0;  // мм → м
            var dCoefficient = spacingM / (Math.PI * pipeThermalConductivity);
            
            // E = s / (d - s)
            var wallThicknessM = pipeWallThickness / 1000.0;  // мм → м
            var outerDiameterM = pipeOuterDiameter / 1000.0;    // мм → м
            var eCoefficient = wallThicknessM / (outerDiameterM - wallThicknessM);
            
            // q_D = (JHmü_low × RFb + C × D × E) / (RFb × RD × (A + B × D × E))
            var numerator = jhmuLow * rFb + c * dCoefficient * eCoefficient;
            var denominator = rFb * rD * (a + b * dCoefficient * eCoefficient);
            
            return numerator / denominator;
        }

        /// <summary>
        /// Выполнить полный тепловой расчёт
        /// </summary>
        /// <param name="parameters">Входные параметры</param>
        /// <returns>Результат расчёта</returns>
        public ThermalCalculationResult Calculate(ThermalParameters parameters)
        {
            // Валидация входных параметров
            var isValid = Validate(parameters, out var errors);

            var result = new ThermalCalculationResult
            {
                IsValid = isValid,
                ValidationErrors = errors
            };

            if (!isValid)
            {
                return result;
            }

            try
            {
                // Определение температуры поверхности по режиму
                var surfaceTemp = (int)parameters.Mode;  // OperatingMode содержит температуру поверхности

                // 1. Расчёт коэффициента теплоотдачи
                var alpha = CalculateHeatTransferCoefficient(
                    surfaceTemp,
                    parameters.AirTemperature,
                    parameters.WindSpeed);
                result.Alpha = alpha;

                // 2. Расчёт мощности вверх
                var powerUp = CalculatePowerUp(
                    parameters.SnowfallIntensity,
                    surfaceTemp,
                    parameters.AirTemperature,
                    alpha);
                result.PowerUp = powerUp;

                // 3. Расчёт тепловых сопротивлений
                var (rFb, rD) = CalculateThermalResistance(
                    parameters.R1Total,
                    parameters.R2Total,
                    alpha);
                result.RFb = rFb;
                result.RD = rD;
                result.R1Total = parameters.R1Total;
                result.R2Total = parameters.R2Total;

                // 4. Расчёт параметров теории стержня
                var dE = parameters.Pipe.OuterDiameter / 1000.0;  // мм → м
                var spacingM = parameters.PipeSpacing / 1000.0;   // мм → м

                var (m, etaR) = CalculateRodTheory(
                    rFb, rD,
                    parameters.LambdaE,
                    dE,
                    spacingM);
                result.ParameterM = m;
                result.EfficiencyEtaR = etaR;

                // 5. Расчёт избыточной температуры
                var excessTemp = CalculateExcessTemperature(
                    parameters,
                    powerUp,
                    rFb,
                    rD,
                    etaR);
                result.ExcessTemperature = excessTemp;

                // 6. Расчёт температур теплоносителя
                // Средняя температура = избыточная + температура наружного воздуха (формула 7)
                result.MeanTemperature = excessTemp + parameters.AirTemperature;
                
                // Температура подачи - входной параметр (задаётся пользователем, формула 8.1)
                result.SupplyTemperature = parameters.SupplyTemperature;
                
                // Проверка: температура подачи должна быть больше средней температуры (формула 8.2)
                // Округляем минимальную температуру вверх до десятых для корректного отображения в сообщении
                var minSupplyTemp = Math.Ceiling(result.MeanTemperature * 10) / 10;
                if (result.SupplyTemperature <= result.MeanTemperature)
                {
                    result.IsValid = false;
                    result.ValidationErrors = new[] { 
                        $"При текущих параметрах системы не обеспечивается требуемая мощность. " +
                        $"Температура подачи ({result.SupplyTemperature:F1}°C) должна быть не менее {minSupplyTemp:F1}°C. " +
                        $"Увеличьте температуру подачи, уменьшите интенсивность снегопада или измените режим работы."
                    };
                    return result;
                }
                
                // Температура обратки (арифметическая формула 8.2)
                result.ReturnTemperature = 2 * result.MeanTemperature - result.SupplyTemperature;
                
                // Температурный перепад (формула 8.3)
                result.DeltaT = result.SupplyTemperature - result.ReturnTemperature;

                // 7. Расчёт составляющих мощности (для справки)
                var h = parameters.SnowfallIntensity / 1000.0 / 3600.0;
                result.MeltingHeat = h * SnowDensity * (
                    IceHeatCapacity * (0 - parameters.AirTemperature) +
                    IceMeltingHeat +
                    WaterHeatCapacity * surfaceTemp);
                // Лучистый теплообмен: Q = ε × σ × T⁴
                // где T - абсолютная температура поверхности в Кельвинах
                result.RadiationHeat = EmissionCoefficient * StefanBoltzmann *
                    Math.Pow(273.0 + surfaceTemp, 4);
                result.ConvectionHeat = alpha * (surfaceTemp - parameters.AirTemperature);
                
                // Убеждаемся, что PowerUp точно равен сумме составляющих (для корректного отображения)
                result.PowerUp = result.MeltingHeat + result.ConvectionHeat;

                // 8. Расчёт мощности вниз (потери)
                // q_D = (JHmü_low × RFb + C × D × E) / (RFb × RD × (A + B × D × E))
                var powerDown = CalculatePowerDown(
                    result.MeanTemperature,
                    parameters.GroundTemperature,
                    parameters.AirTemperature,
                    rFb,
                    rD,
                    etaR,
                    parameters.PipeSpacing,
                    parameters.Pipe.OuterDiameter,
                    parameters.Pipe.WallThickness,
                    parameters.Pipe.ThermalConductivity);
                result.PowerDown = powerDown;

                // 9. Суммарная мощность
                result.PowerTotal = powerUp + powerDown;

                // 10. Расчёт расхода теплоносителя
                // ṁ = q_total / (c_p / 3.6) / ΔT
                // V_dot = ṁ / ρ × 1000
                var cp = parameters.CoolantHeatCapacity;  // кДж/кг·К
                var rho = parameters.CoolantDensity;      // кг/м³

                // Массовый расход: кг/(ч·м²)
                result.MassFlowRate = result.PowerTotal / (cp / 3.6) / result.DeltaT;

                // Объёмный расход: л/(ч·м²)
                result.VolumeFlowRate = result.MassFlowRate / rho * 1000.0;

                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationErrors = new[] { $"Ошибка расчёта: {ex.Message}" };
            }

            return result;
        }

        /// <summary>
        /// Валидация входных параметров
        /// </summary>
        /// <param name="parameters">Параметры для проверки</param>
        /// <param name="errors">Список ошибок валидации</param>
        /// <returns>true если параметры валидны</returns>
        public bool Validate(ThermalParameters parameters, out string[] errors)
        {
            var errorList = new List<string>();

            if (parameters == null)
            {
                errors = new[] { "Параметры не заданы" };
                return false;
            }

            // Проверка трубы
            if (parameters.Pipe == null)
            {
                errorList.Add("Тип трубы не задан");
            }
            else
            {
                if (parameters.Pipe.OuterDiameter <= 0)
                {
                    errorList.Add("Наружный диаметр трубы должен быть положительным");
                }

                if (parameters.Pipe.WallThickness <= 0)
                {
                    errorList.Add("Толщина стенки трубы должна быть положительной");
                }

                if (parameters.Pipe.ThermalConductivity <= 0)
                {
                    errorList.Add("Теплопроводность материала трубы должна быть положительной");
                }
            }

            // Проверка температур
            if (parameters.AirTemperature > 10)
            {
                errorList.Add("Температура наружного воздуха не должна превышать +10°C");
            }

            if (parameters.AirTemperature < -60)
            {
                errorList.Add("Температура наружного воздуха не должна быть ниже -60°C");
            }

            if (parameters.GroundTemperature < -10 || parameters.GroundTemperature > 30)
            {
                errorList.Add("Температура грунта должна быть в диапазоне от -10°C до +30°C");
            }

            // Проверка скорости ветра
            if (parameters.WindSpeed < 0)
            {
                errorList.Add("Скорость ветра не может быть отрицательной");
            }

            if (parameters.WindSpeed > 50)
            {
                errorList.Add("Скорость ветра не должна превышать 50 м/с");
            }

            // Проверка интенсивности снегопада
            if (parameters.SnowfallIntensity < 0)
            {
                errorList.Add("Интенсивность снегопада не может быть отрицательной");
            }

            if (parameters.SnowfallIntensity > 20)
            {
                errorList.Add("Интенсивность снегопада не должна превышать 20 мм/ч");
            }

            // Проверка шага укладки
            if (parameters.PipeSpacing < 50 || parameters.PipeSpacing > 500)
            {
                errorList.Add("Шаг укладки трубы должен быть в диапазоне от 50 до 500 мм");
            }

            // Проверка тепловых сопротивлений
            if (parameters.R1Total < 0)
            {
                errorList.Add("Сопротивление слоёв над трубой не может быть отрицательным");
            }

            if (parameters.R2Total < 0)
            {
                errorList.Add("Сопротивление слоёв под трубой не может быть отрицательным");
            }

            // Проверка теплопроводности стяжки
            if (parameters.LambdaE <= 0)
            {
                errorList.Add("Теплопроводность стяжки должна быть положительной");
            }

            // Проверка температуры подачи
            // Согласно документации:
            // - Для PE-Xa: макс. 65°C
            // - Для бетона: макс. 50°C
            // Общее ограничение: 20-90°C
            if (parameters.SupplyTemperature < 20 || parameters.SupplyTemperature > 90)
            {
                errorList.Add("Температура подачи должна быть в диапазоне от 20°C до 90°C");
            }

            // Примечание: температурный перепад (DeltaT) теперь рассчитывается автоматически,
            // но параметр остаётся для совместимости с гидравлическим расчётом
            if (parameters.DeltaT <= 0 || parameters.DeltaT > 30)
            {
                errorList.Add("Температурный перепад должен быть в диапазоне от 1 до 30 К");
            }

            // Проверка теплоносителя
            if (parameters.CoolantDensity <= 0)
            {
                errorList.Add("Плотность теплоносителя должна быть положительной");
            }

            if (parameters.CoolantHeatCapacity <= 0)
            {
                errorList.Add("Теплоёмкость теплоносителя должна быть положительной");
            }

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        #endregion
    }
}