using CommunityToolkit.Mvvm.ComponentModel;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// ViewModel для отдельного контура системы снеготаяния
    /// </summary>
    /// <remarks>
    /// Используется для поддержки нескольких контуров в HydraulicsViewModel.
    /// Содержит параметры и результаты расчёта для одного контура.
    /// </remarks>
    public partial class CircuitViewModel : ObservableObject
    {
        #region Observable Properties

        /// <summary>
        /// Номер контура
        /// </summary>
        [ObservableProperty]
        private int _circuitNumber;

        /// <summary>
        /// Название контура
        /// </summary>
        [ObservableProperty]
        private string _circuitName;

        /// <summary>
        /// Длина контура (м)
        /// </summary>
        [ObservableProperty]
        private double _length;

        /// <summary>
        /// Длина подводки (м)
        /// </summary>
        [ObservableProperty]
        private double _supplyLength;

        /// <summary>
        /// Площадь контура (м²)
        /// </summary>
        [ObservableProperty]
        private double _area;

        /// <summary>
        /// Расход контура (л/ч)
        /// </summary>
        [ObservableProperty]
        private double _flowRate;

        /// <summary>
        /// Потери давления (Па)
        /// </summary>
        [ObservableProperty]
        private double _pressureLoss;

        /// <summary>
        /// Дросселирование (Па)
        /// </summary>
        [ObservableProperty]
        private double _throttling;

        /// <summary>
        /// Настройка вентиля (1-8)
        /// </summary>
        [ObservableProperty]
        private int _valveSetting = 1;

        /// <summary>
        /// Признак опорного контура
        /// </summary>
        [ObservableProperty]
        private bool _isReferenceCircuit;

        /// <summary>
        /// Скорость потока (м/с)
        /// </summary>
        [ObservableProperty]
        private double _velocity;

        /// <summary>
        /// Число Рейнольдса
        /// </summary>
        [ObservableProperty]
        private double _reynoldsNumber;

        /// <summary>
        /// Режим течения
        /// </summary>
        [ObservableProperty]
        private string _flowRegime = string.Empty;

        /// <summary>
        /// Признак валидности
        /// </summary>
        [ObservableProperty]
        private bool _isValid = true;

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Потери давления в кПа
        /// </summary>
        public double PressureLossKPa => PressureLoss / 1000;

        /// <summary>
        /// Потери давления в мбар
        /// </summary>
        public double PressureLossMbar => PressureLoss / 100;

        /// <summary>
        /// Дросселирование в кПа
        /// </summary>
        public double ThrottlingKPa => Throttling / 1000;

        /// <summary>
        /// Дросселирование в мбар
        /// </summary>
        public double ThrottlingMbar => Throttling / 100;

        /// <summary>
        /// Удельный расход на м² (л/ч/м²)
        /// </summary>
        public double SpecificFlowRate => Area > 0 ? FlowRate / Area : 0;

        /// <summary>
        /// Статус контура
        /// </summary>
        public string Status
        {
            get
            {
                if (!IsValid)
                    return $"Ошибка: {ErrorMessage}";

                if (IsReferenceCircuit)
                    return "Опорный контур";

                if (Throttling > 0)
                    return $"Дросселирование: {ThrottlingMbar:F1} мбар";

                return "Готов";
            }
        }

        /// <summary>
        /// Цвет статуса для UI
        /// </summary>
        public string StatusColor
        {
            get
            {
                if (!IsValid)
                    return "Red";

                if (IsReferenceCircuit)
                    return "Green";

                if (Throttling > 0)
                    return "Orange";

                return "Gray";
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public CircuitViewModel()
        {
            CircuitName = "Новый контур";
        }

        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        /// <param name="number">Номер контура</param>
        /// <param name="name">Название контура</param>
        /// <param name="length">Длина контура, м</param>
        /// <param name="supplyLength">Длина подводки, м</param>
        /// <param name="area">Площадь контура, м²</param>
        public CircuitViewModel(int number, string name, double length, double supplyLength, double area)
        {
            CircuitNumber = number;
            CircuitName = name;
            Length = length;
            SupplyLength = supplyLength;
            Area = area;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Сбросить значения
        /// </summary>
        public void Reset()
        {
            Length = 0;
            SupplyLength = 0;
            Area = 0;
            FlowRate = 0;
            PressureLoss = 0;
            Throttling = 0;
            ValveSetting = 1;
            IsReferenceCircuit = false;
            Velocity = 0;
            ReynoldsNumber = 0;
            FlowRegime = string.Empty;
            IsValid = true;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Клонировать контур
        /// </summary>
        /// <returns>Копия контура</returns>
        public CircuitViewModel Clone()
        {
            return new CircuitViewModel
            {
                CircuitNumber = CircuitNumber,
                CircuitName = CircuitName,
                Length = Length,
                SupplyLength = SupplyLength,
                Area = Area,
                FlowRate = FlowRate,
                PressureLoss = PressureLoss,
                Throttling = Throttling,
                ValveSetting = ValveSetting,
                IsReferenceCircuit = IsReferenceCircuit,
                Velocity = Velocity,
                ReynoldsNumber = ReynoldsNumber,
                FlowRegime = FlowRegime,
                IsValid = IsValid,
                ErrorMessage = ErrorMessage
            };
        }

        /// <summary>
        /// Строковое представление
        /// </summary>
        /// <returns>Строковое представление контура</returns>
        public override string ToString()
        {
            return $"Контур {CircuitNumber}: {CircuitName} (L={Length}м, Q={FlowRate}л/ч)";
        }

        #endregion
    }
}