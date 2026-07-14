using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Данные коллектора
    /// </summary>
    /// <remarks>
    /// Содержит информацию о коллекторе и его контурах.
    /// Используется для отображения в UI и расчётов.
    /// </remarks>
    public partial class CollectorData : ObservableObject
    {
        private int _collectorNumber;
        public int CollectorNumber
        {
            get => _collectorNumber;
            set => SetProperty(ref _collectorNumber, value);
        }

        [ObservableProperty]
        private ObservableCollection<CircuitRow> _circuits = new();

        [ObservableProperty]
        private CollectorSummary _summary = new();

        private string _collectorType = "HKV-D (2-12 контуров)";
        public string CollectorType
        {
            get => _collectorType;
            set
            {
                if (SetProperty(ref _collectorType, value))
                {
                    // Автоматически обновляем тип клапана при изменении типа коллектора
                    ValveType = value switch
                    {
                        "HKV-D (2-12 контуров)" => ValveType.HKV_D,
                        "IV 1¼\" (2-12 контуров)" => ValveType.IV_1_25,
                        "IV 1½\" (2-12 контуров)" => ValveType.IV_1_5,
                        _ => ValveType.HKV_D
                    };
                    // Уведомить об изменении отображаемого типа с количеством контуров
                    OnPropertyChanged(nameof(CollectorTypeDisplayWithCount));
                }
            }
        }

        [ObservableProperty]
        private ValveType _valveType = ValveType.HKV_D;

        /// <summary>
        /// Отображаемое название типа коллектора с фактическим количеством контуров
        /// </summary>
        /// <remarks>
        /// Формат: "HKV-D (3 контура)", "IV 1¼\" (5 контуров)", "IV 1½\" (8 контуров)"
        /// </remarks>
        public string CollectorTypeDisplayWithCount
        {
            get
            {
                string typeName = ValveType switch
                {
                    ValveType.HKV_D => "HKV-D",
                    ValveType.IV_1_25 => "IV 1¼\"",
                    ValveType.IV_1_5 => "IV 1½\"",
                    _ => "Unknown"
                };

                int count = Circuits.Count;
                string countText = count switch
                {
                    1 => "1 контур",
                    2 or 3 or 4 => $"{count} контура",
                    _ => $"{count} контуров"
                };

                return $"{typeName} ({countText})";
            }
        }

        public CollectorData(int collectorNumber)
        {
            CollectorNumber = collectorNumber;
            // Подписка на изменение коллекции контуров для обновления отображаемого типа
            Circuits.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CollectorTypeDisplayWithCount));
        }

        /// <summary>
        /// Обработчик изменения типа клапана
        /// </summary>
        partial void OnValveTypeChanged(ValveType value)
        {
            OnPropertyChanged(nameof(CollectorTypeDisplayWithCount));
        }
    }
}