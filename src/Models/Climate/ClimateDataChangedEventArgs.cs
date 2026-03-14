namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Аргументы события изменения климатических данных
    /// </summary>
    public class ClimateDataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Имя изменённого свойства
        /// </summary>
        public string ChangedProperty { get; set; } = string.Empty;

        /// <summary>
        /// Старое значение
        /// </summary>
        public object? OldValue { get; set; }

        /// <summary>
        /// Новое значение
        /// </summary>
        public object? NewValue { get; set; }

        /// <summary>
        /// Признак валидности данных
        /// </summary>
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Аргументы события валидации
    /// </summary>
    public class ValidationEventArgs : EventArgs
    {
        /// <summary>
        /// Признак валидности
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Имя свойства с ошибкой
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;
    }
}