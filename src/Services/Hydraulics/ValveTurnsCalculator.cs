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
        /// Максимальное количество оборотов клапана (для IV)
        /// </summary>
        /// <remarks>
        /// Устарело. Использовать GetMaxTurns(ValveType) для получения максимальных оборотов по типу клапана.
        /// HKV-D имеет максимальные обороты 2.5, а не 8.0.
        /// </remarks>
        [Obsolete("Использовать GetMaxTurns(ValveType) для получения максимальных оборотов по типу клапана")]
        public const double MaxTurns = 8.0;

        #endregion

        #region GetMaxTurns

        /// <summary>
        /// Получить максимальные обороты для типа клапана
        /// </summary>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Максимальные обороты</returns>
        /// <remarks>
        /// HKV-D: 2.5 оборота (максимум для бытового коллектора)
        /// IV 1¼": 8.0 оборотов
        /// IV 1½": 8.0 оборотов
        /// 
        /// Важно: HKV-D имеет ограничение в 2.5 оборота из-за конструкции клапана.
        /// Промышленные коллекторы IV имеют больший ход клапана (8 оборотов).
        /// </remarks>
        /// <exception cref="ArgumentException">Неподдерживаемый тип клапана</exception>
        public static double GetMaxTurns(ValveType valveType)
        {
            return valveType switch
            {
                ValveType.HKV_D => 2.5,
                ValveType.IV_1_25 => 8.0,
                ValveType.IV_1_5 => 8.0,
                _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}", nameof(valveType))
            };
        }

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

            // ИЗМЕНЕНИЕ: Использовать GetMaxTurns вместо константы MaxTurns
            double maxTurns = GetMaxTurns(valveType);

            // Проверка ограничения оборотов
            if (turns > maxTurns)
            {
                warning = $"Расчётные обороты ({turns:F2}) превышают максимум ({maxTurns}). Установлено {maxTurns} оборотов.";
                turns = maxTurns;
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

        /// <summary>
        /// Рассчитать Kv по оборотам балансировочного клапана (обратная функция)
        /// </summary>
        /// <param name="turns">Количество оборотов</param>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Kv (м³/ч)</returns>
        /// <remarks>
        /// Обратная функция для расчёта Kv по оборотам.
        /// 
        /// Для IV 1½" и IV 1¼" — линейная формула, Kv рассчитывается напрямую.
        /// Для HKV-D — кубическое уравнение, решается численным методом (Ньютона).
        /// 
        /// Формулы:
        /// - IV 1½": Kv = (Обороты + 0.2106) / 5.122
        /// - IV 1¼": Kv = (Обороты + 0.23) / 5.1818
        /// - HKV-D: Решение кубического уравнения 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712 - Обороты = 0
        /// </remarks>
        /// <exception cref="ArgumentException">Неподдерживаемый тип клапана</exception>
        public static double CalculateKvFromTurns(double turns, ValveType valveType)
        {
            if (turns < 0)
                throw new ArgumentException("Количество оборотов не может быть отрицательным", nameof(turns));

            return valveType switch
            {
                ValveType.IV_1_5 => CalculateKvFromTurnsIV_1_5(turns),
                ValveType.IV_1_25 => CalculateKvFromTurnsIV_1_25(turns),
                ValveType.HKV_D => CalculateKvFromTurnsHKV_D(turns),
                _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}", nameof(valveType))
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

        /// <summary>
        /// Расчёт Kv по оборотам для IV 1½" (обратная функция)
        /// Формула: Kv = (Обороты + 0.2106) / 5.122
        /// </summary>
        private static double CalculateKvFromTurnsIV_1_5(double turns)
        {
            return (turns + 0.2106) / 5.122;
        }

        /// <summary>
        /// Расчёт Kv по оборотам для IV 1¼" (обратная функция)
        /// Формула: Kv = (Обороты + 0.23) / 5.1818
        /// </summary>
        private static double CalculateKvFromTurnsIV_1_25(double turns)
        {
            return (turns + 0.23) / 5.1818;
        }

        /// <summary>
        /// Расчёт Kv по оборотам для HKV-D (обратная функция)
        /// Решение кубического уравнения: 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712 - Обороты = 0
        /// Используется метод Ньютона для численного решения.
        /// </summary>
        private static double CalculateKvFromTurnsHKV_D(double turns)
        {
            // Целевое значение: f(Kv) = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712 - turns = 0
            double target = turns + 0.712; // Переносим константу

            // Начальное приближение (Kv ≈ 1.0 для оборотов ≈ 0.5)
            double kv = 1.0;

            // Метод Ньютона (до 20 итераций)
            for (int i = 0; i < 20; i++)
            {
                // f(Kv) = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - target
                double f = 4.2111 * Math.Pow(kv, 3) - 6.7436 * Math.Pow(kv, 2) + 4.6613 * kv - target;

                // f'(Kv) = 12.6333×Kv² - 13.4872×Kv + 4.6613
                double fPrime = 12.6333 * Math.Pow(kv, 2) - 13.4872 * kv + 4.6613;

                if (Math.Abs(fPrime) < 1e-10)
                    break;

                double newKv = kv - f / fPrime;

                if (Math.Abs(newKv - kv) < 1e-6)
                {
                    kv = newKv;
                    break;
                }

                kv = newKv;
            }

            // Ограничение Kv в допустимом диапазоне для HKV-D
            if (kv < 0.8) kv = 0.8;
            if (kv > 4.0) kv = 4.0;

            return kv;
        }

        #endregion
    }
}