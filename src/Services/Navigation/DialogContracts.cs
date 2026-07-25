namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// UI-нейтральный результат диалога (аналог MessageBoxResult без зависимости от WPF).
    /// </summary>
    public enum DialogResult
    {
        /// <summary>Диалог закрыт без выбора.</summary>
        None,

        /// <summary>Пользователь нажал OK.</summary>
        OK,

        /// <summary>Пользователь нажал «Отмена».</summary>
        Cancel,

        /// <summary>Пользователь нажал «Да».</summary>
        Yes,

        /// <summary>Пользователь нажал «Нет».</summary>
        No
    }

    /// <summary>
    /// UI-нейтральный набор кнопок диалога (аналог MessageBoxButton без зависимости от WPF).
    /// </summary>
    public enum DialogButtons
    {
        /// <summary>Только кнопка OK.</summary>
        OK,

        /// <summary>Кнопки OK и «Отмена».</summary>
        OKCancel,

        /// <summary>Кнопки «Да» и «Нет».</summary>
        YesNo,

        /// <summary>Кнопки «Да», «Нет» и «Отмена».</summary>
        YesNoCancel
    }

    /// <summary>
    /// UI-нейтральная иконка диалога (аналог MessageBoxImage без зависимости от WPF).
    /// </summary>
    public enum DialogIcon
    {
        /// <summary>Без иконки.</summary>
        None,

        /// <summary>Информация.</summary>
        Information,

        /// <summary>Вопрос.</summary>
        Question,

        /// <summary>Предупреждение.</summary>
        Warning,

        /// <summary>Ошибка.</summary>
        Error
    }
}
