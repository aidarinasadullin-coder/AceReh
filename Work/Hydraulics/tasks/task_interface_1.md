# Задача: Шапка с входными данными

**ID:** task_interface_1  
**Модуль:** Hydraulics  
**Юзер-кейс:** UC-1 — Просмотр входных данных гидравлики  
**Приоритет:** Высокий  
**Зависимости:** Нет  
**Статус:** Ожидает

---

## 1. Цель задачи

Создать блок "ВХОДНЫЕ ДАННЫЕ" (EINGABEN - Allgemein) в левой части информационной панели вкладки "Контур". Блок должен отображать параметры трубы, температуры и гликоля.

---

## 2. Связь с юзер-кейсом

**UC-1: Просмотр входных данных гидравлики**

### Предусловия:
- Выполнен тепловой расчёт
- Выбран тип трубы в ThermalViewModel
- Выбран тип гликоля и концентрация

### Основной сценарий:
1. Пользователь открывает вкладку "Контур"
2. Система отображает блок "Входные данные" в левой части информационной панели
3. Блок содержит 8 полей с параметрами

---

## 3. Описание изменений

### 3.1. Файлы для создания/изменения

| Файл | Действие | Описание |
|------|----------|----------|
| `src/Views/CircuitsView.xaml` | Изменить | Добавить блок "ВХОДНЫЕ ДАННЫЕ" |
| `src/ViewModels/CircuitsViewModel.cs` | Изменить | Добавить свойства для привязки |

### 3.2. Новые свойства в CircuitsViewModel

```csharp
// === Входные данные для отображения ===

/// <summary>
/// Температура подачи, °C
/// Берётся из ThermalViewModel.Result.SupplyTemperature
/// </summary>
public double SupplyTemperature => _thermalViewModel.Result?.SupplyTemperature ?? 0;

/// <summary>
/// Температура обратки, °C
/// Берётся из ThermalViewModel.Result.ReturnTemperature
/// </summary>
public double ReturnTemperature => _thermalViewModel.Result?.ReturnTemperature ?? 0;

/// <summary>
/// Тип трубы (наименование)
/// Берётся из ThermalViewModel.SelectedPipe.Name
/// </summary>
public string PipeType => _thermalViewModel.SelectedPipe?.Name ?? "Труба не выбрана";

/// <summary>
/// Наружный диаметр трубы, мм
/// Берётся из ThermalViewModel.SelectedPipe.OuterDiameter
/// </summary>
public double OuterDiameter => _thermalViewModel.SelectedPipe?.OuterDiameter ?? 0;

/// <summary>
/// Толщина стенки трубы, мм
/// Берётся из ThermalViewModel.SelectedPipe.WallThickness
/// </summary>
public double WallThickness => _thermalViewModel.SelectedPipe?.WallThickness ?? 0;

/// <summary>
/// Внутренний диаметр трубы, мм
/// Вычисляется: D_ext - 2 × s
/// </summary>
public double InnerDiameter => InputData.InnerDiameter;

/// <summary>
/// Шероховатость трубы, мм (константа для PE-Xa)
/// </summary>
public double PipeRoughness => 0.007;

/// <summary>
/// Тип гликоля (на русском)
/// </summary>
public string GlycolTypeName => GlycolType switch
{
    GlycolType.Ethylene => "Этиленгликоль",
    GlycolType.Propylene => "Пропиленгликоль",
    _ => "Не указан"
};
```

### 3.3. XAML-структура блока

**Расположение:** Левый блок в Grid с 3 колонками

```xml
<!-- Блок ВХОДНЫЕ ДАННЫЕ (левый блок) -->
<Border Grid.Column="0"
        Background="#FFF3E0"
        BorderBrush="#FF9800"
        BorderThickness="1"
        Padding="10"
        Margin="0,0,5,10"
        CornerRadius="5">
    <StackPanel>
        <TextBlock Text="ВХОДНЫЕ ДАННЫЕ"
                   FontWeight="Bold"
                   FontSize="12"
                   Foreground="#E65100"
                   Margin="0,0,0,8"/>
        <TextBlock Text="EINGABEN - Allgemein"
                   FontSize="10"
                   Foreground="#757575"
                   Margin="0,0,0,8"/>
        
        <!-- Температура подачи -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="T_подачи:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding SupplyTemperature, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" °C" FontSize="11"/>
        </StackPanel>
        
        <!-- Температура обратки -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="T_обратки:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding ReturnTemperature, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" °C" FontSize="11"/>
        </StackPanel>
        
        <!-- Тип трубы -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Труба:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PipeType}" FontSize="11"/>
        </StackPanel>
        
        <!-- Наружный диаметр -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="D_нар:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding OuterDiameter, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text="×" Margin="2,0,2,0" FontSize="11"/>
            <TextBlock Text="{Binding WallThickness, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" мм" FontSize="11"/>
        </StackPanel>
        
        <!-- Внутренний диаметр -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="D_вн:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding InnerDiameter, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" мм" FontSize="11"/>
        </StackPanel>
        
        <!-- Шероховатость -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="ε:" Width="100" FontSize="11"/>
            <TextBlock Text="0.007" FontSize="11"/>
            <TextBlock Text=" мм" FontSize="11"/>
        </StackPanel>
        
        <!-- Тип гликоля -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Гликоль:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding GlycolTypeName}" FontSize="11"/>
        </StackPanel>
        
        <!-- Концентрация -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Конц.:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding GlycolConcentration, StringFormat=F0}" FontSize="11"/>
            <TextBlock Text=" %" FontSize="11"/>
        </StackPanel>
    </StackPanel>
</Border>
```

### 3.4. Обновление при изменении ThermalViewModel

Добавить уведомления в метод `OnThermalViewModelPropertyChanged`:

```csharp
private void OnThermalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(ThermalViewModel.Result))
    {
        UpdateFromThermalModule();
        
        // Уведомить об изменении свойств для отображения
        OnPropertyChanged(nameof(SupplyTemperature));
        OnPropertyChanged(nameof(ReturnTemperature));
    }
    else if (e.PropertyName == nameof(ThermalViewModel.SelectedPipe))
    {
        // Обновить диаметры при смене трубы
        UpdateInnerDiameter();
        OnPropertyChanged(nameof(PipeType));
        OnPropertyChanged(nameof(OuterDiameter));
        OnPropertyChanged(nameof(WallThickness));
        OnPropertyChanged(nameof(InnerDiameter));
    }
}
```

---

## 4. Источники данных

| Поле | Источник | Формат | Примечание |
|------|----------|--------|------------|
| T_подачи | ThermalViewModel.Result.SupplyTemperature | F1 °C | Температура подачи |
| T_обратки | ThermalViewModel.Result.ReturnTemperature | F1 °C | Температура обратки |
| Труба | ThermalViewModel.SelectedPipe.Name | Строка | Например, "RAUTHERM S 20×2.0" |
| D_нар | ThermalViewModel.SelectedPipe.OuterDiameter | F1 мм | Наружный диаметр |
| Толщина | ThermalViewModel.SelectedPipe.WallThickness | F1 мм | Толщина стенки |
| D_вн | HydraulicInputData.InnerDiameter | F1 мм | Вычисляется: D_ext - 2×s |
| ε | Константа | 0.007 мм | Шероховатость PE-Xa |
| Гликоль | CircuitsViewModel.GlycolType | Строка | Этилен/Пропилен |
| Конц. | CircuitsViewModel.GlycolConcentration | F0 % | Концентрация |

---

## 5. Тест-кейсы

### 5.1. TC-1.1: Отображение блока при наличии данных

**Предусловия:**
- Тепловой расчёт выполнен
- Труба выбрана (RAUTHERM S 20×2.0)
- Гликоль: Этилен, 50%

**Шаги:**
1. Открыть вкладку "Контур"

**Ожидаемый результат:**
- Блок "ВХОДНЫЕ ДАННЫЕ" отображается слева
- T_подачи: 47.0 °C
- T_обратки: 35.0 °C
- Труба: RAUTHERM S 20×2.0
- D_нар: 20.0×2.0 мм
- D_вн: 16.0 мм
- ε: 0.007 мм
- Гликоль: Этиленгликоль
- Конц.: 50 %

### 5.2. TC-1.2: Отображение при отсутствии трубы

**Предусловия:**
- Тепловой расчёт не выполнен
- Труба не выбрана

**Шаги:**
1. Открыть вкладку "Контур"

**Ожидаемый результат:**
- T_подачи: 0.0 °C
- T_обратки: 0.0 °C
- Труба: "Труба не выбрана"
- D_нар: 0.0×0.0 мм
- D_вн: 0.0 мм

### 5.3. TC-1.3: Обновление при смене трубы

**Предусловия:**
- Открыта вкладка "Контур"
- Выбрана труба RAUTHERM S 20×2.0

**Шаги:**
1. Перейти на вкладку "Конструкция"
2. Выбрать трубу RAUTHERM S 17×2.0
3. Вернуться на вкладку "Контур"

**Ожидаемый результат:**
- Труба: RAUTHERM S 17×2.0
- D_нар: 17.0×2.0 мм
- D_вн: 13.0 мм

---

## 6. Критерии приёмки

- ✅ Блок отображается в левой части информационной панели
- ✅ Температура подачи отображается с точностью 1 знак после запятой
- ✅ Температура обратки отображается с точностью 1 знак после запятой
- ✅ Тип трубы отображается в формате "RAUTHERM S 20×2.0"
- ✅ Диаметры отображаются в формате "20.0 × 2.0 мм"
- ✅ Шероховатость отображается как "0.007 мм"
- ✅ Тип гликоля отображается на русском языке
- ✅ Данные трубы берутся из ThermalViewModel.SelectedPipe
- ✅ При отсутствии данных отображаются нулевые значения или "Труба не выбрана"
- ✅ Блок обновляется при смене трубы

---

## 7. Оценка трудозатрат

| Этап | Время |
|------|-------|
| Добавление свойств в ViewModel | 30 мин |
| Создание XAML-разметки | 45 мин |
| Тестирование | 30 мин |
| **Итого** | **1.5-2 часа** |

---

## 8. Статус

**Статус:** Ожидает разработки  
**Дата создания:** 2026-03-20  
**Дата обновления:** 2026-03-20