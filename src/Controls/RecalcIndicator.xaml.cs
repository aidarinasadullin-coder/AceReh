// ================================================================================
// REHAU Снеготаяние - UserControl: Индикатор пересчёта
// ================================================================================
//
// Назначение: Готовый компонент для отображения статуса пересчёта
//
// Соответствует: design_guidelines.md
// - Цвета: Warning (жёлтый), Processing (синий), Success (зелёный)
// - Типографика: Inter Medium 14px
// - Отступы: 16,12 (padding)
//
// ================================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SnowMeltingCalculator.Models.Enums;

namespace SnowMeltingCalculator.Controls
{
    /// <summary>
    /// Компонент индикатора пересчёта
    /// Отображает статус актуальности данных и кнопку пересчёта
    /// </summary>
    public partial class RecalcIndicator : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Состояние индикатора
        /// </summary>
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register(
                nameof(State),
                typeof(RecalcState),
                typeof(RecalcIndicator),
                new PropertyMetadata(RecalcState.Info));

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(RecalcIndicator),
                new PropertyMetadata("Данные актуальны"));

        /// <summary>
        /// Показывать ли кнопку пересчёта
        /// </summary>
        public static readonly DependencyProperty ShowButtonProperty =
            DependencyProperty.Register(
                nameof(ShowButton),
                typeof(bool),
                typeof(RecalcIndicator),
                new PropertyMetadata(false));

        /// <summary>
        /// Команда пересчёта
        /// </summary>
        public static readonly DependencyProperty RecalculateCommandProperty =
            DependencyProperty.Register(
                nameof(RecalculateCommand),
                typeof(ICommand),
                typeof(RecalcIndicator),
                new PropertyMetadata(null));

        #endregion

        #region Properties

        /// <summary>
        /// Состояние индикатора
        /// </summary>
        public RecalcState State
        {
            get => (RecalcState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>
        /// Показывать ли кнопку пересчёта
        /// </summary>
        public bool ShowButton
        {
            get => (bool)GetValue(ShowButtonProperty);
            set => SetValue(ShowButtonProperty, value);
        }

        /// <summary>
        /// Команда пересчёта
        /// </summary>
        public ICommand RecalculateCommand
        {
            get => (ICommand)GetValue(RecalculateCommandProperty);
            set => SetValue(RecalculateCommandProperty, value);
        }

        #endregion

        #region Constructor

        public RecalcIndicator()
        {
            InitializeComponent();
        }

        #endregion
    }
}
