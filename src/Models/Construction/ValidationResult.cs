using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Результат валидации конструкции
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Признак валидности данных
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Список ошибок
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Список предупреждений
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Создать пустой валидный результат
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };
        }

        /// <summary>
        /// Создать невалидный результат с ошибкой
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        public static ValidationResult Failure(string error)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { error },
                Warnings = new List<string>()
            };
        }

        /// <summary>
        /// Создать невалидный результат с несколькими ошибками
        /// </summary>
        /// <param name="errors">Список ошибок</param>
        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string>(errors),
                Warnings = new List<string>()
            };
        }

        /// <summary>
        /// Добавить ошибку
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Добавить предупреждение
        /// </summary>
        /// <param name="warning">Сообщение с предупреждением</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Объединить результаты валидации
        /// </summary>
        /// <param name="other">Другой результат валидации</param>
        public void Merge(ValidationResult other)
        {
            if (!other.IsValid)
            {
                IsValid = false;
            }
            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
        }

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

        public override string ToString()
        {
            if (IsValid && Warnings.Count == 0)
                return "Валидация пройдена успешно";

            var result = IsValid ? "Валидация пройдена с предупреждениями" : "Валидация не пройдена";

            if (Errors.Count > 0)
            {
                result += $"\nОшибки ({Errors.Count}):";
                foreach (var error in Errors)
                {
                    result += $"\n  - {error}";
                }
            }

            if (Warnings.Count > 0)
            {
                result += $"\nПредупреждения ({Warnings.Count}):";
                foreach (var warning in Warnings)
                {
                    result += $"\n  - {warning}";
                }
            }

            return result;
        }
    }
}