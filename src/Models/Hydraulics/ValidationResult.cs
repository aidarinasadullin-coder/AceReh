using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Результат валидации
    /// </summary>
    /// <remarks>
    /// Содержит информацию о результатах проверки параметров:
    /// - Признак успешной валидации
    /// - Список ошибок (критические проблемы)
    /// - Список предупреждений (некритические проблемы)
    /// </remarks>
    public class ValidationResult
    {
        /// <summary>
        /// Признак успешной валидации
        /// </summary>
        /// <remarks>
        /// true, если нет критических ошибок
        /// </remarks>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Список ошибок
        /// </summary>
        /// <remarks>
        /// Критические проблемы, препятствующие расчёту
        /// </remarks>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Список предупреждений
        /// </summary>
        /// <remarks>
        /// Некритические проблемы, на которые следует обратить внимание
        /// </remarks>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Создать успешный результат
        /// </summary>
        /// <returns>Результат без ошибок</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Создать результат с ошибками
        /// </summary>
        /// <param name="errors">Список ошибок</param>
        /// <returns>Результат с ошибками</returns>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string>(errors)
            };
        }

        /// <summary>
        /// Добавить ошибку
        /// </summary>
        /// <param name="error">Текст ошибки</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Добавить предупреждение
        /// </summary>
        /// <param name="warning">Текст предупреждения</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Проверить, есть ли ошибки
        /// </summary>
        /// <returns>true, если есть ошибки</returns>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Проверить, есть ли предупреждения
        /// </summary>
        /// <returns>true, если есть предупреждения</returns>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Получить все сообщения (ошибки и предупреждения)
        /// </summary>
        /// <returns>Список всех сообщений</returns>
        public List<string> GetAllMessages()
        {
            var messages = new List<string>();
            messages.AddRange(Errors);
            messages.AddRange(Warnings);
            return messages;
        }

        /// <summary>
        /// Объединить результаты валидации
        /// </summary>
        /// <param name="other">Другой результат</param>
        public void Merge(ValidationResult other)
        {
            if (other == null) return;
            
            foreach (var error in other.Errors)
            {
                AddError(error);
            }
            
            foreach (var warning in other.Warnings)
            {
                AddWarning(warning);
            }
        }

        /// <summary>
        /// Получить строковое представление
        /// </summary>
        public override string ToString()
        {
            if (IsValid && !HasWarnings)
                return "Валидация пройдена успешно";
            
            if (!IsValid)
                return $"Ошибки: {Errors.Count}, Предупреждения: {Warnings.Count}";
            
            return $"Предупреждения: {Warnings.Count}";
        }
    }
}