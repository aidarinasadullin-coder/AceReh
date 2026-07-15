namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Ошибка валидации с указанием свойства и сообщения
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Имя свойства, к которому относится ошибка
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Создать ошибку валидации
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public ValidationError(string message)
        {
            PropertyName = string.Empty;
            Message = message;
        }

        /// <summary>
        /// Создать ошибку валидации
        /// </summary>
        /// <param name="propertyName">Имя свойства</param>
        /// <param name="message">Сообщение об ошибке</param>
        public ValidationError(string propertyName, string message)
        {
            PropertyName = propertyName ?? string.Empty;
            Message = message;
        }

        /// <summary>
        /// Строковое представление ошибки
        /// </summary>
        public override string ToString() => Message;
    }
}
