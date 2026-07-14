using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Интерфейс калькулятора теплового расчёта систем снеготаяния
    /// </summary>
    public interface IThermalCalculator
    {
        /// <summary>
        /// Рассчитать коэффициент теплоотдачи на поверхности
        /// </summary>
        /// <param name="surfaceTemp">Температура поверхности, °C</param>
        /// <param name="airTemp">Температура наружного воздуха, °C</param>
        /// <param name="windSpeed">Скорость ветра, м/с</param>
        /// <returns>Коэффициент теплоотдачи α, Вт/м²·К</returns>
        /// <remarks>
        /// Формула: α = 2.26 × (t_П - t_H)^0.33 + 2.6 × v_H
        /// </remarks>
        double CalculateHeatTransferCoefficient(double surfaceTemp, double airTemp, double windSpeed);

        /// <summary>
        /// Рассчитать требуемую мощность вверх (на поверхность)
        /// </summary>
        /// <param name="snowfallIntensity">Интенсивность снегопада, см/ч</param>
        /// <param name="surfaceTemp">Температура поверхности, °C</param>
        /// <param name="airTemp">Температура наружного воздуха, °C</param>
        /// <param name="alpha">Коэффициент теплоотдачи, Вт/м²·К</param>
        /// <returns>Требуемая мощность q_FB, Вт/м²</returns>
        /// <remarks>
        /// Состоит из трёх составляющих:
        /// - Q_таяние: теплота плавления снега
        /// - Q_изл: лучистый теплообмен
        /// - Q_конв: конвективный теплообмен
        /// </remarks>
        double CalculatePowerUp(double snowfallIntensity, double surfaceTemp, double airTemp, double alpha);

        /// <summary>
        /// Рассчитать тепловые сопротивления конструкции
        /// </summary>
        /// <param name="r1Total">Суммарное сопротивление слоёв над трубой, м²·К/Вт</param>
        /// <param name="r2Total">Суммарное сопротивление слоёв под трубой, м²·К/Вт</param>
        /// <param name="alpha">Коэффициент теплоотдачи на поверхности, Вт/м²·К</param>
        /// <returns>Кортеж (RFb, RD) - сопротивления вверх и вниз</returns>
        /// <remarks>
        /// RFb = R1 + 1/α
        /// RD = R2 + 1/α_низ (адиабатические условия)
        /// </remarks>
        (double RFb, double RD) CalculateThermalResistance(double r1Total, double r2Total, double alpha);

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
        /// </remarks>
        (double ParameterM, double EfficiencyEtaR) CalculateRodTheory(double rFb, double rD, double lambdaE, double dE, double spacing);

        /// <summary>
        /// Рассчитать избыточную температуру теплоносителя
        /// </summary>
        /// <param name="parameters">Параметры расчёта</param>
        /// <param name="powerUp">Мощность вверх, Вт/м²</param>
        /// <param name="rFb">Сопротивление вверх, м²·К/Вт</param>
        /// <param name="rD">Сопротивление вниз, м²·К/Вт</param>
        /// <param name="etaR">КПД ребра</param>
        /// <param name="climate">Климатические данные</param>
        /// <param name="construction">Данные конструкции</param>
        /// <returns>Избыточная температура JHmü, °C</returns>
        double CalculateExcessTemperature(
            ThermalInputs parameters,
            double powerUp,
            double rFb,
            double rD,
            double etaR,
            IClimateData climate,
            IConstructionData construction);

        /// <summary>
        /// Выполнить полный тепловой расчёт
        /// </summary>
        /// <param name="inputs">Входные параметры теплового расчёта</param>
        /// <param name="climate">Климатические данные из контрактной шины</param>
        /// <param name="construction">Данные конструкции из контрактной шины</param>
        /// <returns>Результат расчёта</returns>
        /// <remarks>
        /// Калькулятор получает климатические и конструктивные данные
        /// через аргументы <paramref name="climate"/> и <paramref name="construction"/>,
        /// а не из полей <paramref name="inputs"/>.
        /// </remarks>
        ThermalCalculationResult Calculate(ThermalInputs inputs, IClimateData climate, IConstructionData construction);

        /// <summary>
        /// Валидация входных параметров
        /// </summary>
        /// <param name="inputs">Входные параметры теплового расчёта</param>
        /// <param name="climate">Климатические данные из контрактной шины</param>
        /// <param name="construction">Данные конструкции из контрактной шины</param>
        /// <param name="errors">Список ошибок валидации</param>
        /// <returns>true если параметры валидны</returns>
        /// <remarks>
        /// Валидация охватывает тепловые параметры, климатические данные
        /// и данные конструкции, полученные через контрактные объекты.
        /// </remarks>
        bool Validate(ThermalInputs inputs, IClimateData climate, IConstructionData construction, out string[] errors);
    }
}