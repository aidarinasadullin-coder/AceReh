using System;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Валидатор для гидравлических расчётов
    /// </summary>
    /// <remarks>
    /// Проверяет входные параметры и результаты расчёта:
    /// - Диапазоны значений
    /// - Логические связи между параметрами
    /// - Граничные условия
    /// 
    /// Ограничения взяты из docs/Formulas_Snegotayanie.md, раздел 13
    /// </remarks>
    public class HydraulicValidator
    {
        #region Константы валидации

        // Пределы длины контура
        private const double MIN_CIRCUIT_LENGTH = 10;
        private const double MAX_CIRCUIT_LENGTH = 500;

        // Пределы длины подводки
        private const double MIN_SUPPLY_LENGTH = 1;
        private const double MAX_SUPPLY_LENGTH = 100;

        // Пределы доли гликоля
        private const double MIN_GLYCOL_CONCENTRATION = 10;
        private const double MAX_GLYCOL_CONCENTRATION = 90;

        // Пределы температуры подачи
        private const double MIN_SUPPLY_TEMPERATURE = 20;
        private const double MAX_SUPPLY_TEMPERATURE = 90;

        // Пределы температуры обратки
        private const double MIN_RETURN_TEMPERATURE = 15;
        private const double MAX_RETURN_TEMPERATURE = 80;

        // Пределы скорости потока (рекомендации)
        private const double MIN_VELOCITY = 0.2;
        private const double MAX_VELOCITY = 1.5;

        // Пределы числа Рейнольдса
        private const double LAMINAR_UPPER_LIMIT = 2300;
        private const double TURBULENT_LOWER_LIMIT = 4000;

        // Пределы потерь давления
        private const double MAX_PRESSURE_LOSS_PER_METER = 300; // Па/м
        private const double MAX_TOTAL_PRESSURE_LOSS = 32000; // Па (320 мбар)

        #endregion

        /// <summary>
        /// Валидация входных параметров гидравлического расчёта
        /// </summary>
        /// <param name="parameters">Параметры расчёта</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(HydraulicParameters parameters)
        {
            var result = new ValidationResult { IsValid = true };

            if (parameters == null)
            {
                result.AddError("Параметры расчёта не указаны");
                return result;
            }

            // Валидация длины контура
            ValidateCircuitLength(parameters.CircuitLength, result);

            // Валидация длины подводки
            ValidateSupplyLength(parameters.SupplyLength, result);

            // Валидация доли гликоля
            ValidateGlycolConcentration(parameters.GlycolConcentration, result);

            // Валидация температур
            ValidateTemperatures(parameters.SupplyTemperature, parameters.ReturnTemperature, result);

            // Валидация трубы
            ValidatePipe(parameters.Pipe, result);

            // Валидация расхода
            ValidateFlowRate(parameters.VolumeFlowRate, result);

            // Валидация площади
            ValidateArea(parameters.CircuitArea, result);

            // Валидация плотности
            ValidateDensity(parameters.Density, result);

            // Валидация вязкости
            ValidateViscosity(parameters.KinematicViscosity, result);

            return result;
        }

        /// <summary>
        /// Валидация результата гидравлического расчёта
        /// </summary>
        /// <param name="result">Результат расчёта</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult ValidateResult(HydraulicResult result)
        {
            var validationResult = new ValidationResult { IsValid = true };

            if (result == null)
            {
                validationResult.AddError("Результат расчёта не указан");
                return validationResult;
            }

            // Проверка скорости потока
            ValidateVelocity(result.Velocity, validationResult);

            // Проверка режима течения
            ValidateFlowRegime(result.ReynoldsNumber, result.FlowRegime, validationResult);

            // Проверка числа Рейнольдса
            ValidateReynoldsNumber(result.ReynoldsNumber, validationResult);

            // Проверка потерь давления
            ValidatePressureLoss(result.PressureLossPerMeter, result.TotalPressureLoss, validationResult);

            return validationResult;
        }

        #region Private Validation Methods

        private void ValidateCircuitLength(double length, ValidationResult result)
        {
            if (double.IsNaN(length) || double.IsInfinity(length))
            {
                result.AddError("Длина контура имеет недопустимое значение");
                return;
            }

            if (length < MIN_CIRCUIT_LENGTH)
            {
                result.AddError($"Длина контура должна быть не менее {MIN_CIRCUIT_LENGTH} м (текущее значение: {length:F2} м)");
            }
            else if (length > MAX_CIRCUIT_LENGTH)
            {
                result.AddError($"Длина контура должна быть не более {MAX_CIRCUIT_LENGTH} м (текущее значение: {length:F2} м)");
            }
        }

        private void ValidateSupplyLength(double length, ValidationResult result)
        {
            if (double.IsNaN(length) || double.IsInfinity(length))
            {
                result.AddError("Длина подводки имеет недопустимое значение");
                return;
            }

            if (length < MIN_SUPPLY_LENGTH)
            {
                result.AddError($"Длина подводки должна быть не менее {MIN_SUPPLY_LENGTH} м (текущее значение: {length:F2} м)");
            }
            else if (length > MAX_SUPPLY_LENGTH)
            {
                result.AddError($"Длина подводки должна быть не более {MAX_SUPPLY_LENGTH} м (текущее значение: {length:F2} м)");
            }
        }

        private void ValidateGlycolConcentration(double concentration, ValidationResult result)
        {
            if (double.IsNaN(concentration) || double.IsInfinity(concentration))
            {
                result.AddError("Доля гликоля имеет недопустимое значение");
                return;
            }

            if (concentration < MIN_GLYCOL_CONCENTRATION)
            {
                result.AddError($"Доля гликоля должна быть не менее {MIN_GLYCOL_CONCENTRATION}% (текущее значение: {concentration:F1}%)");
            }
            else if (concentration > MAX_GLYCOL_CONCENTRATION)
            {
                result.AddError($"Доля гликоля должна быть не более {MAX_GLYCOL_CONCENTRATION}% (текущее значение: {concentration:F1}%)");
            }
        }

        private void ValidateTemperatures(double supplyTemp, double returnTemp, ValidationResult result)
        {
            if (double.IsNaN(supplyTemp) || double.IsInfinity(supplyTemp))
            {
                result.AddError("Температура подачи имеет недопустимое значение");
                return;
            }

            if (double.IsNaN(returnTemp) || double.IsInfinity(returnTemp))
            {
                result.AddError("Температура обратки имеет недопустимое значение");
                return;
            }

            if (supplyTemp < MIN_SUPPLY_TEMPERATURE)
            {
                result.AddError($"Температура подачи должна быть не менее {MIN_SUPPLY_TEMPERATURE}°C (текущее значение: {supplyTemp:F1}°C)");
            }
            else if (supplyTemp > MAX_SUPPLY_TEMPERATURE)
            {
                result.AddError($"Температура подачи должна быть не более {MAX_SUPPLY_TEMPERATURE}°C (текущее значение: {supplyTemp:F1}°C)");
            }

            if (returnTemp < MIN_RETURN_TEMPERATURE)
            {
                result.AddError($"Температура обратки должна быть не менее {MIN_RETURN_TEMPERATURE}°C (текущее значение: {returnTemp:F1}°C)");
            }
            else if (returnTemp > MAX_RETURN_TEMPERATURE)
            {
                result.AddError($"Температура обратки должна быть не более {MAX_RETURN_TEMPERATURE}°C (текущее значение: {returnTemp:F1}°C)");
            }

            // Проверка логической связи температур
            if (supplyTemp <= returnTemp)
            {
                result.AddError($"Температура подачи ({supplyTemp:F1}°C) должна быть выше температуры обратки ({returnTemp:F1}°C)");
            }

            // Проверка перепада температур
            double deltaT = supplyTemp - returnTemp;
            if (deltaT < 2)
            {
                result.AddWarning($"Перепад температур очень мал ({deltaT:F1}°C). Рекомендуемый перепад: 5-15°C");
            }
            else if (deltaT > 25)
            {
                result.AddWarning($"Перепад температур очень велик ({deltaT:F1}°C). Рекомендуемый перепад: 5-15°C");
            }
        }

        private void ValidatePipe(PipeType? pipe, ValidationResult result)
        {
            if (pipe == null)
            {
                result.AddError("Тип трубы не указан");
                return;
            }

            if (pipe.OuterDiameter <= 0)
            {
                result.AddError("Наружный диаметр трубы должен быть положительным числом");
            }

            if (pipe.WallThickness <= 0)
            {
                result.AddError("Толщина стенки трубы должна быть положительным числом");
            }

            if (pipe.WallThickness * 2 >= pipe.OuterDiameter)
            {
                result.AddError("Толщина стенки слишком велика для данного наружного диаметра");
            }
        }

        private void ValidateFlowRate(double flowRate, ValidationResult result)
        {
            if (double.IsNaN(flowRate) || double.IsInfinity(flowRate))
            {
                result.AddError("Расход имеет недопустимое значение");
                return;
            }

            if (flowRate <= 0)
            {
                result.AddError("Расход должен быть положительным числом");
            }
        }

        private void ValidateArea(double area, ValidationResult result)
        {
            if (double.IsNaN(area) || double.IsInfinity(area))
            {
                result.AddError("Площадь контура имеет недопустимое значение");
                return;
            }

            if (area <= 0)
            {
                result.AddError("Площадь контура должна быть положительным числом");
            }
        }

        private void ValidateDensity(double density, ValidationResult result)
        {
            if (double.IsNaN(density) || double.IsInfinity(density))
            {
                result.AddError("Плотность теплоносителя имеет недопустимое значение");
                return;
            }

            if (density <= 0)
            {
                result.AddError("Плотность теплоносителя должна быть положительным числом");
            }
        }

        private void ValidateViscosity(double viscosity, ValidationResult result)
        {
            if (double.IsNaN(viscosity) || double.IsInfinity(viscosity))
            {
                result.AddError("Кинематическая вязкость имеет недопустимое значение");
                return;
            }

            if (viscosity <= 0)
            {
                result.AddError("Кинематическая вязкость должна быть положительным числом");
            }
        }

        private void ValidateVelocity(double velocity, ValidationResult result)
        {
            if (double.IsNaN(velocity) || double.IsInfinity(velocity))
            {
                result.AddError("Скорость потока имеет недопустимое значение");
                return;
            }

            if (velocity < MIN_VELOCITY)
            {
                result.AddWarning($"Скорость потока ({velocity:F3} м/с) ниже рекомендуемого минимума ({MIN_VELOCITY} м/с). " +
                    "Низкая скорость может привести к неравномерному распределению тепла.");
            }
            else if (velocity > MAX_VELOCITY)
            {
                result.AddWarning($"Скорость потока ({velocity:F3} м/с) выше рекомендуемого максимума ({MAX_VELOCITY} м/с). " +
                    "Высокая скорость увеличивает потери давления и шум.");
            }
        }

        private void ValidateFlowRegime(double reynoldsNumber, FlowRegime regime, ValidationResult result)
        {
            if (regime == FlowRegime.Transitional)
            {
                result.AddWarning($"Режим течения переходный (Re = {reynoldsNumber:F0}). " +
                    $"Рекомендуется ламинарный (Re < {LAMINAR_UPPER_LIMIT}) или турбулентный (Re > {TURBULENT_LOWER_LIMIT}) режим.");
            }
        }

        private void ValidateReynoldsNumber(double reynoldsNumber, ValidationResult result)
        {
            if (double.IsNaN(reynoldsNumber) || double.IsInfinity(reynoldsNumber))
            {
                result.AddError("Число Рейнольдса имеет недопустимое значение");
                return;
            }

            if (reynoldsNumber <= 0)
            {
                result.AddError("Число Рейнольдса должно быть положительным");
            }
        }

        private void ValidatePressureLoss(double pressureLossPerMeter, double totalPressureLoss, ValidationResult result)
        {
            if (double.IsNaN(pressureLossPerMeter) || double.IsInfinity(pressureLossPerMeter))
            {
                result.AddError("Удельные потери давления имеют недопустимое значение");
                return;
            }

            if (double.IsNaN(totalPressureLoss) || double.IsInfinity(totalPressureLoss))
            {
                result.AddError("Общие потери давления имеют недопустимое значение");
                return;
            }

            if (pressureLossPerMeter < 0)
            {
                result.AddError("Удельные потери давления не могут быть отрицательными");
            }

            if (totalPressureLoss < 0)
            {
                result.AddError("Общие потери давления не могут быть отрицательными");
            }

            // Предупреждение о высоких потерях
            if (pressureLossPerMeter > MAX_PRESSURE_LOSS_PER_METER)
            {
                result.AddWarning($"Удельные потери давления высоки ({pressureLossPerMeter:F1} Па/м > {MAX_PRESSURE_LOSS_PER_METER} Па/м). " +
                    "Рекомендуется увеличить диаметр трубы или уменьшить расход.");
            }

            if (totalPressureLoss > MAX_TOTAL_PRESSURE_LOSS)
            {
                result.AddWarning($"Общие потери давления высоки ({totalPressureLoss / 1000:F1} кПа > {MAX_TOTAL_PRESSURE_LOSS / 1000} кПа). " +
                    "Проверьте длину контура и диаметр трубы.");
            }
        }

        #endregion

        #region Static Validation Helpers

        /// <summary>
        /// Быстрая проверка валидности параметров
        /// </summary>
        /// <param name="parameters">Параметры расчёта</param>
        /// <returns>true, если параметры валидны</returns>
        public static bool IsValidParameters(HydraulicParameters parameters)
        {
            var validator = new HydraulicValidator();
            return validator.Validate(parameters).IsValid;
        }

        /// <summary>
        /// Быстрая проверка валидности результата
        /// </summary>
        /// <param name="result">Результат расчёта</param>
        /// <returns>true, если результат валиден</returns>
        public static bool IsValidResult(HydraulicResult result)
        {
            var validator = new HydraulicValidator();
            return validator.ValidateResult(result).IsValid;
        }

        #endregion
    }
}