using System.Collections.Generic;

namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Унифицированный результат валидации
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Признак успешной валидации
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Список ошибок валидации
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Список предупреждений
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Создать успешный результат
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult
            {
                IsValid = true,
                Errors = new List<ValidationError>(),
                Warnings = new List<string>()
            };
        }

        /// <summary>
        /// Создать результат с ошибкой
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        public static ValidationResult Failure(string error)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError> { new ValidationError(error) },
                Warnings = new List<string>()
            };
        }

        /// <summary>
        /// Создать результат с несколькими ошибками
        /// </summary>
        /// <param name="errors">Список сообщений об ошибках</param>
        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            var result = new ValidationResult
            {
                IsValid = false,
                Warnings = new List<string>()
            };

            foreach (var error in errors)
            {
                result.Errors.Add(new ValidationError(error));
            }

            return result;
        }

        /// <summary>
        /// Создать результат с ошибками валидации
        /// </summary>
        /// <param name="errors">Список ошибок</param>
        public static ValidationResult Failure(IEnumerable<ValidationError> errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>(errors),
                Warnings = new List<string>()
            };
        }

        /// <summary>
        /// Добавить ошибку
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        public void AddError(string error)
        {
            Errors.Add(new ValidationError(error));
            IsValid = false;
        }

        /// <summary>
        /// Добавить ошибку
        /// </summary>
        /// <param name="propertyName">Имя свойства</param>
        /// <param name="message">Сообщение об ошибке</param>
        public void AddError(string propertyName, string message)
        {
            Errors.Add(new ValidationError(propertyName, message));
            IsValid = false;
        }

        /// <summary>
        /// Добавить ошибку
        /// </summary>
        /// <param name="error">Ошибка валидации</param>
        public void AddError(ValidationError error)
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
        /// Проверить, есть ли ошибки
        /// </summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Проверить, есть ли предупреждения
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Объединить результаты валидации
        /// </summary>
        /// <param name="other">Другой результат валидации</param>
        public void Merge(ValidationResult other)
        {
            if (other == null) return;

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
            messages.AddRange(Errors.ConvertAll(e => e.Message));
            messages.AddRange(Warnings);
            return messages;
        }

        /// <summary>
        /// Получить строковое представление результата валидации
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
