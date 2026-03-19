# Task 4.1: Создать CircuitsViewModel.cs

**Этап:** 4 - ViewModels  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 1.2 (HydraulicInputData), Task 3.2 (CircuitsCalculator)

---

## 1. Цель задачи

Создать `CircuitsViewModel` для управления таблицей контуров.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Ввод параметров контуров | Свойства и команды |
| UC-08 | Управление контурами и коллекторами | AddCollectorCommand, RemoveCollectorCommand |

---

## 3. Создаваемые файлы

### 3.1. CircuitsViewModel.cs

**Путь:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Содержимое:**

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// ViewModel для управления таблицей контуров
    /// </summary>
    public partial class CircuitsViewModel : ObservableObject
    {
        #region Services

        private readonly ICircuitsCalculator _circuitsCalculator;
        private readonly IGlycolDataService _glycolService;

        #endregion

        #region Observable Properties

        /// <summary>
        /// Список коллекторов
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CollectorViewModel> _collectors = new();

        /// <summary>
        /// Выбранный коллектор
        /// </summary>
        [ObservableProperty]
        private int _selectedCollectorIndex = 0;

        /// <summary>
        /// Режим отображения
        /// </summary>
        public HydraulicMode CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }
        private HydraulicMode _currentMode = HydraulicMode.OperatingTemperature;

        /// <summary>
        /// Тип гликоля
        /// </summary>
        [ObservableProperty]
        private GlycolType _glycolType = GlycolType.Ethylene;

        /// <summary>
        /// Концентрация гликоля (%)
        /// </summary>
        [ObservableProperty]
        private double _glycolConcentration = 50.0;

        /// <summary>
        /// Можно добавить коллектор
        /// </summary>
        public bool CanAddCollector => Collectors.Count < 4;

        /// <summary>
        /// Можно добавить контур
        /// </summary>
        public bool CanAddCircuit => SelectedCollector?.Circuits.Count < 12;

        #endregion

        #region Вычисляемые свойства

        /// <summary>
        /// Выбранный коллектор
        /// </summary>
        public CollectorViewModel? SelectedCollector =>
            SelectedCollectorIndex >= 0 && SelectedCollectorIndex < Collectors.Count
                ? Collectors[SelectedCollectorIndex]
                : null;

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanAddCollector))]
        private void AddCollector()
        {
            var collectorNumber = Collectors.Count + 1;
            var collector = new CollectorViewModel
            {
                CollectorNumber = collectorNumber,
                ValveType = ValveType.HKV_D
            };

            // Добавляем 4 контура по умолчанию
            for (int i = 0; i < 4; i++)
            {
                collector.Circuits.Add(new CircuitRow
                {
                    CircuitNumber = i + 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    PipeSpacing_cm = 20,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                });
            }

            Collectors.Add(collector);
            AddCollectorCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RemoveCollector(CollectorViewModel collector)
        {
            if (collector != null && Collectors.Contains(collector))
            {
                Collectors.Remove(collector);
                AddCollectorCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddCircuit))]
        private void AddCircuit()
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            var circuitNumber = collector.Circuits.Count + 1;
            collector.Circuits.Add(new CircuitRow
            {
                CircuitNumber = circuitNumber
            });
        }

        [RelayCommand]
        private void RemoveCircuit(CircuitRow circuit)
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            if (circuit != null && collector.Circuits.Contains(circuit))
            {
                collector.Circuits.Remove(circuit);
            }
        }

        [RelayCommand]
        private void Calculate()
        {
            // TODO: Реализовать расчёт
        }

        [RelayCommand]
        private void SwitchMode()
        {
            CurrentMode = CurrentMode == HydraulicMode.OperatingTemperature
                ? HydraulicMode.DesignTemperature
                : HydraulicMode.OperatingTemperature;
        }

        #endregion

        #region Constructor

        public CircuitsViewModel(ICircuitsCalculator circuitsCalculator, IGlycolDataService glycolService)
        {
            _circuitsCalculator = circuitsCalculator ?? throw new ArgumentNullException(nameof(circuitsCalculator));
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));

            // Создаём первый коллектор с 4 контурами по умолчанию
            AddCollector();
        }

        #endregion
    }
}
```

---

## 4. Критерии приёмки

- [ ] Файл `CircuitsViewModel.cs` создан в `src/ViewModels/Hydraulics/`
- [ ] Все свойства и команды реализованы
- [ ] INotifyPropertyChanged для всех свойств (CommunityToolkit.Mvvm)
- [ ] Начальное состояние: 1 коллектор с 4 контурами
- [ ] Максимум 4 коллектора, 12 контуров на коллектор
- [ ] XML-документация для всех методов

---

## 5. Примечания

- Используется CommunityToolkit.Mvvm для MVVM
- Коллекторы хранятся в ObservableCollection
- Контролы хранятся в коллекторе (CollectorViewModel.Circuits)

---

## 6. Связанные задачи

- Task 1.2: HydraulicInputData — используется для ввода данных
- Task 3.2: CircuitsCalculator — используется для расчёта
- Task 4.2: CollectorViewModel — связан с CircuitsViewModel
- Task 5.1: CircuitsView.xaml — привязка к CircuitsViewModel

---

*Дата создания: 2026-03-17*