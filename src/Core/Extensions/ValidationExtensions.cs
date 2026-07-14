using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Core.Constants;

namespace SnowMeltingCalculator.Core.Extensions
{
    /// <summary>
    /// Расширения для валидации
    /// </summary>
    public static class ValidationExtensions
    {
        #region Диапазон

        /// <summary>
        /// Проверить, что значение в заданном диапазоне
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="min">Минимальное значение (включительно)</param>
        /// <param name="max">Максимальное значение (включительно)</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <exception cref="ArgumentOutOfRangeException">Если значение вне диапазона</exception>
        public static void ValidateRange(this double value, double min, double max, string paramName)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Format(ValidationConstants.RangeErrorMessage, paramName, min, max));
            }
        }

        /// <summary>
        /// Проверить, что значение в заданном диапазоне (для int)
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="min">Минимальное значение (включительно)</param>
        /// <param name="max">Максимальное значение (включительно)</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <exception cref="ArgumentOutOfRangeException">Если значение вне диапазона</exception>
        public static void ValidateRange(this int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Format(ValidationConstants.RangeErrorMessage, paramName, min, max));
            }
        }

        /// <summary>
        /// Проверить, что значение в заданном диапазоне, и вернуть ошибку если нет
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="min">Минимальное значение (включительно)</param>
        /// <param name="max">Максимальное значение (включительно)</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если значение в диапазоне</returns>
        public static bool ValidateRange(this double value, double min, double max, string paramName, List<string> errors)
        {
            if (value < min || value > max)
            {
                errors.Add(string.Format(ValidationConstants.RangeErrorMessage, paramName, min, max));
                return false;
            }
            return true;
        }

        #endregion

        #region Положительные значения

        /// <summary>
        /// Проверить, что значение положительное
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <exception cref="ArgumentOutOfRangeException">Если значение не положительное</exception>
        public static void ValidatePositive(this double value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Format(ValidationConstants.PositiveErrorMessage, paramName));
            }
        }

        /// <summary>
        /// Проверить, что значение положительное (для int)
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <exception cref="ArgumentOutOfRangeException">Если значение не положительное</exception>
        public static void ValidatePositive(this int value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Format(ValidationConstants.PositiveErrorMessage, paramName));
            }
        }

        /// <summary>
        /// Проверить, что значение положительное, и вернуть ошибку если нет
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если значение положительное</returns>
        public static bool ValidatePositive(this double value, string paramName, List<string> errors)
        {
            if (value <= 0)
            {
                errors.Add(string.Format(ValidationConstants.PositiveErrorMessage, paramName));
                return false;
            }
            return true;
        }

        #endregion

        #region Неотрицательные значения

        /// <summary>
        /// Проверить, что значение неотрицательное
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <exception cref="ArgumentOutOfRangeException">Если значение отрицательное</exception>
        public static void ValidateNonNegative(this double value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Format(ValidationConstants.NonNegativeErrorMessage, paramName));
            }
        }

        /// <summary>
        /// Проверить, что значение неотрицательное (для int)
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <exception cref="ArgumentOutOfRangeException">Если значение отрицательное</exception>
        public static void ValidateNonNegative(this int value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Format(ValidationConstants.NonNegativeErrorMessage, paramName));
            }
        }

        /// <summary>
        /// Проверить, что значение неотрицательное, и вернуть ошибку если нет
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если значение неотрицательное</returns>
        public static bool ValidateNonNegative(this double value, string paramName, List<string> errors)
        {
            if (value < 0)
            {
                errors.Add(string.Format(ValidationConstants.NonNegativeErrorMessage, paramName));
                return false;
            }
            return true;
        }

        #endregion

        #region Не null

        /// <summary>
        /// Проверить, что объект не null
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="value">Объект для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <exception cref="ArgumentNullException">Если объект null</exception>
        public static void ValidateNotNull<T>(this T? value, string paramName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(
                    paramName,
                    string.Format(ValidationConstants.NotSetErrorMessage, paramName));
            }
        }

        /// <summary>
        /// Проверить, что объект не null, и вернуть ошибку если null
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="value">Объект для проверки</param>
        /// <param name="paramName">Имя параметра для сообщения об ошибке</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если объект не null</returns>
        public static bool ValidateNotNull<T>(this T? value, string paramName, List<string> errors) where T : class
        {
            if (value == null)
            {
                errors.Add(string.Format(ValidationConstants.NotSetErrorMessage, paramName));
                return false;
            }
            return true;
        }

        #endregion

        #region Специализированные валидации

        /// <summary>
        /// Проверить температуру наружного воздуха
        /// </summary>
        /// <param name="value">Температура, °C</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если температура в допустимом диапазоне</returns>
        public static bool ValidateAirTemperature(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinAirTemperature,
                ValidationConstants.MaxAirTemperature,
                "Температура наружного воздуха",
                errors);
        }

        /// <summary>
        /// Проверить скорость ветра
        /// </summary>
        /// <param name="value">Скорость ветра, м/с</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если скорость в допустимом диапазоне</returns>
        public static bool ValidateWindSpeed(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinWindSpeed,
                ValidationConstants.MaxWindSpeed,
                "Скорость ветра",
                errors);
        }

        /// <summary>
        /// Проверить интенсивность снегопада
        /// </summary>
        /// <param name="value">Интенсивность снегопада, мм/ч</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если интенсивность в допустимом диапазоне</returns>
        public static bool ValidateSnowfallIntensity(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinSnowfallIntensity,
                ValidationConstants.MaxSnowfallIntensity,
                "Интенсивность снегопада",
                errors);
        }

        /// <summary>
        /// Проверить температуру подачи
        /// </summary>
        /// <param name="value">Температура подачи, °C</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если температура в допустимом диапазоне</returns>
        public static bool ValidateSupplyTemperature(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinSupplyTemperature,
                ValidationConstants.MaxSupplyTemperature,
                "Температура подачи",
                errors);
        }

        /// <summary>
        /// Проверить шаг укладки трубы
        /// </summary>
        /// <param name="value">Шаг укладки, мм</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если шаг в допустимом диапазоне</returns>
        public static bool ValidatePipeSpacing(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinPipeSpacing,
                ValidationConstants.MaxPipeSpacing,
                "Шаг укладки трубы",
                errors);
        }

        /// <summary>
        /// Проверить температурный перепад
        /// </summary>
        /// <param name="value">Температурный перепад, К</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если перепад в допустимом диапазоне</returns>
        public static bool ValidateDeltaT(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinDeltaT,
                ValidationConstants.MaxDeltaT,
                "Температурный перепад",
                errors);
        }

        /// <summary>
        /// Проверить длину контура
        /// </summary>
        /// <param name="value">Длина контура, м</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если длина в допустимом диапазоне</returns>
        public static bool ValidateCircuitLength(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinCircuitLength,
                ValidationConstants.MaxCircuitLength,
                "Длина контура",
                errors);
        }

        /// <summary>
        /// Проверить скорость потока
        /// </summary>
        /// <param name="value">Скорость потока, м/с</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если скорость в допустимом диапазоне</returns>
        public static bool ValidateVelocity(this double value, List<string> errors)
        {
            return value.ValidateRange(
                ValidationConstants.MinVelocity,
                ValidationConstants.MaxVelocity,
                "Скорость потока",
                errors);
        }

        /// <summary>
        /// Проверить потери давления
        /// </summary>
        /// <param name="value">Потери давления, Па</param>
        /// <param name="errors">Список ошибок для добавления</param>
        /// <returns>true если потери в допустимом диапазоне</returns>
        public static bool ValidatePressureLoss(this double value, List<string> errors)
        {
            if (value > ValidationConstants.MaxPressureLoss)
            {
                errors.Add($"Потери давления ({value / 100.0:F1} мбар) превышают максимально допустимые ({ValidationConstants.MaxPressureLoss / 100.0:F0} мбар)");
                return false;
            }
            return true;
        }

        #endregion
    }
}