using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Калькулятор оборотов балансировочного клапана
    /// </summary>
    /// <remarks>
    /// Рассчитывает количество оборотов балансировочного клапана
    /// в зависимости от коэффициента пропускной способности (Kv).
    /// 
    /// Поддерживаемые типы клапанов:
    /// - HKV-D: бытовой коллектор, Kv = 1.2 м³/ч
    /// - IV 1¼": промышленный коллектор, Kv = 1.45 м³/ч
    /// - IV 1½": промышленный коллектор, Kv = 1.5 м³/ч
    /// </remarks>
    public static class ValveTurnsCalculator
    {
        #region Константы

        /// <summary>
        /// Kv для HKV-D (бытовой коллектор)
        /// </summary>
        public const double KV_HKV_D = 1.2;

        /// <summary>
        /// Kv для IV 1¼" (промышленный коллектор)
        /// </summary>
        public const double KV_IV_1_25 = 1.45;

        /// <summary>
        /// Kv для IV 1½" (промышленный коллектор)
        /// </summary>
        public const double KV_IV_1_5 = 1.5;

        /// <summary>
        /// Максимальное количество оборотов клапана
        /// </summary>
        public const double MaxTurns = 8.0;

        #endregion

        #region Основные методы

        /// <summary>
        /// Рассчитать обороты балансировочного клапана
        /// </summary>
        /// <param name="kv">Коэффициент пропускной способности (м³/ч)</param>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Количество оборотов (округлено до 0.25, максимум 8)</returns>
        /// <remarks>
        /// Формулы расчёта:
        /// - IV 1½": Обороты = 5.122 × Kv - 0.2106
        /// - IV 1¼": Обороты = 5.1818 × Kv - 0.23
        /// - HKV-D: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
        /// 
        /// Ограничения:
        /// - Максимальное количество оборотов: 8
        /// - Округление: до 0.25 оборота
        /// </remarks>
        /// <exception cref="ArgumentException">Неподдерживаемый тип клапана</exception>
        public static double CalculateTurns(double kv, ValveType valveType)
        {
            var (turns, _) = CalculateTurnsWithWarning(kv, valveType);
            return turns;
        }

        /// <summary>
        /// Рассчитать обороты балансировочного клапана с предупреждением
        /// </summary>
        /// <param name="kv">Коэффициент пропускной способности (м³/ч)</param>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Кортеж: (обороты, предупреждение или null)</returns>
        /// <remarks>
        /// Формулы расчёта:
        /// - IV 1½": Обороты = 5.122 × Kv - 0.2106
        /// - IV 1¼": Обороты = 5.1818 × Kv - 0.23
        /// - HKV-D: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
        /// 
        /// Ограничения:
        /// - Максимальное количество оборотов: 8
        /// - Округление: до 0.25 оборота
        /// 
        /// Если расчётные обороты превышают 8, возвращается 8 и предупреждение.
        /// </remarks>
        /// <exception cref="ArgumentException">Неподдерживаемый тип клапана</exception>
        public static (double Turns, string? Warning) CalculateTurnsWithWarning(double kv, ValveType valveType)
        {
            double turns = valveType switch
            {
                ValveType.IV_1_5 => CalculateTurnsIV_1_5(kv),
                ValveType.IV_1_25 => CalculateTurnsIV_1_25(kv),
                ValveType.HKV_D => CalculateTurnsHKV_D(kv),
                _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}", nameof(valveType))
            };

            string? warning = null;

            // Проверка ограничения оборотов
            if (turns > MaxTurns)
            {
                warning = $"Расчётные обороты ({turns:F2}) превышают максимум ({MaxTurns}). Установлено {MaxTurns} оборотов.";
                turns = MaxTurns;
            }

            // Округление до 0.25 оборота
            turns = Math.Round(turns * 4) / 4;

            return (turns, warning);
        }

        /// <summary>
        /// Получить Kv по типу клапана
        /// </summary>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Kv (м³/ч)</returns>
        /// <exception cref="ArgumentException">Неподдерживаемый тип клапана</exception>
        public static double GetDefaultKv(ValveType valveType)
        {
            return valveType switch
            {
                ValveType.HKV_D => KV_HKV_D,
                ValveType.IV_1_25 => KV_IV_1_25,
                ValveType.IV_1_5 => KV_IV_1_5,
                _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}", nameof(valveType))
            };
        }

        /// <summary>
        /// Получить название клапана
        /// </summary>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Название клапана</returns>
        public static string GetValveTypeName(ValveType valveType)
        {
            return valveType switch
            {
                ValveType.HKV_D => "HKV-D (бытовой коллектор)",
                ValveType.IV_1_25 => "IV 1¼\" (промышленный коллектор)",
                ValveType.IV_1_5 => "IV 1½\" (промышленный коллектор)",
                _ => "Неизвестный тип"
            };
        }

        /// <summary>
        /// Проверить валидность Kv для типа клапана
        /// </summary>
        /// <param name="kv">Коэффициент пропускной способности</param>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>True, если Kv в допустимом диапазоне</returns>
        public static bool IsValidKv(double kv, ValveType valveType)
        {
            return valveType switch
            {
                ValveType.HKV_D => kv >= 0.8 && kv <= 4.0,
                ValveType.IV_1_25 => kv >= 0.5 && kv <= 3.0,
                ValveType.IV_1_5 => kv >= 0.5 && kv <= 3.5,
                _ => false
            };
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Расчёт оборотов для IV 1½"
        /// Формула: Обороты = 5.122 × Kv - 0.2106
        /// </summary>
        private static double CalculateTurnsIV_1_5(double kv)
        {
            return 5.122 * kv - 0.2106;
        }

        /// <summary>
        /// Расчёт оборотов для IV 1¼"
        /// Формула: Обороты = 5.1818 × Kv - 0.23
        /// </summary>
        private static double CalculateTurnsIV_1_25(double kv)
        {
            return 5.1818 * kv - 0.23;
        }

        /// <summary>
        /// Расчёт оборотов для HKV-D
        /// Формула: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
        /// </summary>
        private static double CalculateTurnsHKV_D(double kv)
        {
            return 4.2111 * Math.Pow(kv, 3)
                   - 6.7436 * Math.Pow(kv, 2)
                   + 4.6613 * kv
                   - 0.712;
        }

        #endregion
    }
}