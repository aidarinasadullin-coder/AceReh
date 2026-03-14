namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Интерфейс для передачи данных конструкции другим модулям
    /// </summary>
    public interface IConstructionData
    {
        /// <summary>
        /// Суммарное термическое сопротивление слоёв над трубой, м²·К/Вт
        /// </summary>
        double R1Total { get; }

        /// <summary>
        /// Суммарное термическое сопротивление слоёв под трубой, м²·К/Вт
        /// </summary>
        double R2Total { get; }

        /// <summary>
        /// Теплопроводность стяжки (бетона) вокруг трубы, Вт/м·К
        /// </summary>
        double LambdaE { get; }

        /// <summary>
        /// Признак валидности данных конструкции
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Событие изменения данных
        /// </summary>
        event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;
    }

    /// <summary>
    /// Реализация интерфейса данных конструкции (заглушка)
    /// </summary>
    public class ConstructionData : IConstructionData
    {
        public double R1Total { get; set; } = 0.05;
        public double R2Total { get; set; } = 0.1;
        public double LambdaE { get; set; } = 1.6;
        public bool IsValid => R1Total > 0 && R2Total > 0 && LambdaE > 0;

        public event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// Вызвать событие изменения данных
        /// </summary>
        public void RaiseDataChanged(string propertyName, object? oldValue, object? newValue, bool isValid = true)
        {
            DataChanged?.Invoke(this, new ConstructionDataChangedEventArgs
            {
                ChangedProperty = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                IsValid = isValid
            });
        }
    }

    /// <summary>
    /// Аргументы события изменения данных конструкции
    /// </summary>
    public class ConstructionDataChangedEventArgs : EventArgs
    {
        public string? ChangedProperty { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public bool IsValid { get; set; } = true;
    }
}