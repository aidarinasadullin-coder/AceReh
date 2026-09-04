namespace SnowMeltingCalculator.Models.Enums
{
    /// <summary>
    /// Семантика статус-бара каркаса (Фаза 1 редизайна): цвет скошенной
    /// плашки модуля. Success — данные валидны (бирюза), Warning — требуется
    /// внимание (янтарный, документированное исключение), Error — валидация
    /// не проходит (бренд-красный), Info — нейтральное сообщение.
    /// </summary>
    public enum ShellStatusKind
    {
        Info,
        Success,
        Warning,
        Error
    }
}
