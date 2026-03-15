using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Интерфейс сервиса для получения свойств гликолей
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для получения физических свойств гликолевого раствора:
    /// - Плотность (ρ)
    /// - Кинематическая вязкость (ν)
    /// - Удельная теплоёмкость (c_p)
    /// - Теплопроводность (λ)
    /// 
    /// Данные получаются интерполяцией из data/glycol_data.json
    /// для заданного типа гликоля, концентрации и температуры.
    /// 
    /// Источник данных: ASHRAE Handbook
    /// Диапазон температур: -34.4°C до 98.9°C
    /// Диапазон концентраций: 10% до 90%
    /// </remarks>
    public interface IGlycolDataService
    {
        /// <summary>
        /// Получить плотность гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля (этиленгликоль/пропиленгликоль)</param>
        /// <param name="concentration">Концентрация, % (объёмные)</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Плотность, кг/м³</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~998 кг/м³ при 20°C
        /// - 50% этиленгликоль: ~1053 кг/м³ при 40°C
        /// - 50% пропиленгликоль: ~1040 кг/м³ при 40°C
        /// </remarks>
        double GetDensity(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить удельную теплоёмкость гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Удельная теплоёмкость, кДж/(кг·К)</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~4.18 кДж/(кг·К) при 20°C
        /// - 50% этиленгликоль: ~3.39 кДж/(кг·К) при 40°C
        /// - 50% пропиленгликоль: ~3.50 кДж/(кг·К) при 40°C
        /// </remarks>
        double GetSpecificHeat(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить кинематическую вязкость гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Кинематическая вязкость, мм²/с</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// ВАЖНО: Вязкость значительно возрастает при низких температурах!
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~1.0 мм²/с при 20°C
        /// - 50% этиленгликоль при 40°C: ~2.16 мм²/с
        /// - 50% этиленгликоль при -15°C: ~18.17 мм²/с
        /// </remarks>
        double GetKinematicViscosity(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить теплопроводность гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Теплопроводность, Вт/(м·К)</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~0.60 Вт/(м·К) при 20°C
        /// - 50% этиленгликоль: ~0.42 Вт/(м·К) при 40°C
        /// </remarks>
        double GetThermalConductivity(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить все свойства гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Объект со всеми свойствами гликоля</returns>
        /// <remarks>
        /// Возвращает объект GlycolProperties со всеми свойствами:
        /// - Density
        /// - SpecificHeat
        /// - KinematicViscosity
        /// - ThermalConductivity
        /// 
        /// Это более эффективный способ, чем вызов отдельных методов,
        /// так как интерполяция выполняется один раз.
        /// </remarks>
        GlycolProperties GetProperties(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Проверить, поддерживается ли температура
        /// </summary>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>true, если температура в допустимом диапазоне</returns>
        /// <remarks>
        /// Диапазон температур: -34.4°C до 98.9°C
        /// При температуре вне диапазона используется экстраполяция.
        /// </remarks>
        bool IsTemperatureSupported(double temperature);
        
        /// <summary>
        /// Проверить, поддерживается ли концентрация
        /// </summary>
        /// <param name="concentration">Концентрация, %</param>
        /// <returns>true, если концентрация в допустимом диапазоне</returns>
        /// <remarks>
        /// Диапазон концентраций: 10% до 90%
        /// При концентрации вне диапазона используется экстраполяция.
        /// </remarks>
        bool IsConcentrationSupported(double concentration);
        
        /// <summary>
        /// Получить минимальную поддерживаемую температуру
        /// </summary>
        /// <returns>Минимальная температура, °C</returns>
        double GetMinTemperature();
        
        /// <summary>
        /// Получить максимальную поддерживаемую температуру
        /// </summary>
        /// <returns>Максимальная температура, °C</returns>
        double GetMaxTemperature();
        
        /// <summary>
        /// Получить минимальную поддерживаемую концентрацию
        /// </summary>
        /// <returns>Минимальная концентрация, %</returns>
        double GetMinConcentration();
        
        /// <summary>
        /// Получить максимальную поддерживаемую концентрацию
        /// </summary>
        /// <returns>Максимальная концентрация, %</returns>
        double GetMaxConcentration();
    }
}