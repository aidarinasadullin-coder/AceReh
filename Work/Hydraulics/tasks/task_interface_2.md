# Задача: Блок данных укладки и мощности

**ID:** task_interface_2  
**Модуль:** Hydraulics  
**Юзер-кейс:** UC-2 — Просмотр данных укладки и мощности  
**Приоритет:** Высокий  
**Зависимости:** Нет  
**Статус:** Ожидает

---

## 1. Цель задачи

Создать блок "ДАННЫЕ УКЛАДКИ И МОЩНОСТИ" (Verlege- und Leistungsdaten) в центральной части информационной панели вкладки "Контур". Блок должен отображать параметры укладки трубы и удельные мощности.

---

## 2. Связь с юзер-кейсом

**UC-2: Просмотр данных укладки и мощности**

### Предусловия:
- Выполнен тепловой расчёт
- Выбран шаг укладки трубы

### Основной сценарий:
1. Пользователь открывает вкладку "Контур"
2. Система отображает блок "Данные укладки и мощности" в центральной части информационной панели
3. Блок содержит 5 полей с параметрами

---

## 3. Описание изменений

### 3.1. Файлы для создания/изменения

| Файл | Действие | Описание |
|------|----------|----------|
| `src/Views/CircuitsView.xaml` | Изменить | Добавить блок "ДАННЫЕ УКЛАДКИ" |
| `src/ViewModels/CircuitsViewModel.cs` | Изменить | Добавить свойства для привязки |

### 3.2. Новые свойства в CircuitsViewModel

```csharp
// === Данные укладки и мощности ===

/// <summary>
/// Удельная мощность вверх, Вт/м²
/// Берётся из ThermalViewModel.Result.PowerUp
/// </summary>
public double PowerUp => _thermalViewModel.Result?.PowerUp ?? 0;

/// <summary>
/// Удельная мощность вниз, Вт/м²
/// Берётся из ThermalViewModel.Result.PowerDown
/// </summary>
public double PowerDown => _thermalViewModel.Result?.PowerDown ?? 0;

/// <summary>
/// Шаг укладки, см
/// Берётся из ThermalViewModel.PipeSpacing (перевод из мм в см)
/// </summary>
public double PipeSpacing_cm => _thermalViewModel.PipeSpacing / 10.0;

/// <summary>
/// Шаг подводки, см
/// Берётся из CircuitsViewModel.InputData.SupplySpacing_cm
/// </summary>
public double SupplySpacing_cm => InputData.SupplySpacing_cm;

/// <summary>
/// Доля потерь в подводке, %
/// Берётся из CircuitsViewModel.InputData.SupplyHeatPercent
/// </summary>
public double SupplyHeatPercent => InputData.SupplyHeatPercent;
```

### 3.3. XAML-структура блока

**Расположение:** Центральный блок в Grid с 3 колонками (Grid.Column="1")

```xml
<!-- Блок ДАННЫЕ УКЛАДКИ И МОЩНОСТИ (центральный блок) -->
<Border Grid.Column="1"
        Background="#E8F5E9"
        BorderBrush="#4CAF50"
        BorderThickness="1"
        Padding="10"
        Margin="5,0,5,10"
        CornerRadius="5">
    <StackPanel>
        <TextBlock Text="ДАННЫЕ УКЛАДКИ"
                   FontWeight="Bold"
                   FontSize="12"
                   Foreground="#2E7D32"
                   Margin="0,0,0,8"/>
        <TextBlock Text="Verlege- und Leistungsdaten"
                   FontSize="10"
                   Foreground="#757575"
                   Margin="0,0,0,8"/>
        
        <!-- Уд. мощность вверх -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="q_вверх:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PowerUp, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" Вт/м²" FontSize="11"/>
        </StackPanel>
        
        <!-- Уд. мощность вниз -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="q_вниз:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PowerDown, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" Вт/м²" FontSize="11"/>
        </StackPanel>
        
        <!-- Шаг укладки -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Шаг:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding PipeSpacing_cm, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" см" FontSize="11"/>
        </StackPanel>
        
        <!-- Шаг подводки -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Подводка:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding SupplySpacing_cm, StringFormat=F1}" FontSize="11"/>
            <TextBlock Text=" см" FontSize="11"/>
        </StackPanel>
        
        <!-- Доля потерь -->
        <StackPanel Orientation="Horizontal" Margin="0,2">
            <TextBlock Text="Потери:" Width="100" FontSize="11"/>
            <TextBlock Text="{Binding SupplyHeatPercent, StringFormat=F0}" FontSize="11"/>
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
        OnPropertyChanged(nameof(PowerUp));
        OnPropertyChanged(nameof(PowerDown));
    }
    else if (e.PropertyName == nameof(ThermalViewModel.PipeSpacing))
    {
        UpdatePipeSpacingInCircuits();
        OnPropertyChanged(nameof(PipeSpacing_cm));
    }
}
```

---

## 4. Источники данных

| Поле | Источник | Формат | Примечание |
|------|----------|--------|------------|
| q_вверх | ThermalViewModel.Result.PowerUp | F1 Вт/м² | Удельная мощность вверх |
| q_вниз | ThermalViewModel.Result.PowerDown | F1 Вт/м² | Удельная мощность вниз |
| Шаг | ThermalViewModel.PipeSpacing / 10 | F1 см | Шаг укладки (мм → см) |
| Подводка | CircuitsViewModel.InputData.SupplySpacing_cm | F1 см | Шаг подводки |
| Потери | CircuitsViewModel.InputData.SupplyHeatPercent | F0 % | Доля потерь в подводке |

---

## 5. Тест-кейсы

### 5.1. TC-2.1: Отображение блока при наличии данных

**Предусловия:**
- Тепловой расчёт выполнен
- Шаг укладки: 200 мм (20 см)
- Шаг подводки: 50 мм (5 см)
- Доля потерь: 10%

**Шаги:**
1. Открыть вкладку "Контур"

**Ожидаемый результат:**
- Блок "ДАННЫЕ УКЛАДКИ" отображается в центре
- q_вверх: 256.6 Вт/м²
- q_вниз: 4.6 Вт/м²
- Шаг: 20.0 см
- Подводка: 5.0 см
- Потери: 10 %

### 5.2. TC-2.2: Отображение при отсутствии данных

**Предусловия:**
- Тепловой расчёт не выполнен

**Шаги:**
1. Открыть вкладку "Контур"

**Ожидаемый результат:**
- q_вверх: 0.0 Вт/м²
- q_вниз: 0.0 Вт/м²
- Шаг: 0.0 см
- Подводка: 5.0 см (значение по умолчанию)
- Потери: 10 % (значение по умолчанию)

### 5.3. TC-2.3: Обновление при изменении шага укладки

**Предусловия:**
- Открыта вкладка "Контур"
- Шаг укладки: 200 мм

**Шаги:**
1. Перейти на вкладку "Конструкция"
2. Изменить шаг укладки на 150 мм
3. Вернуться на вкладку "Контур"

**Ожидаемый результат:**
- Шаг: 15.0 см

---

## 6. Критерии приёмки

- ✅ Блок отображается в центральной части информационной панели
- ✅ Мощность вверх отображается с точностью 1 знак после запятой
- ✅ Мощность вниз отображается с точностью 1 знак после запятой
- ✅ Шаг укладки отображается в см с точностью 1 знак после запятой
- ✅ Шаг подводки отображается в см с точностью 1 знак после запятой
- ✅ Доля потерь отображается в % с точностью 0 знаков
- ✅ При отсутствии данных отображаются нулевые значения
- ✅ Блок обновляется при изменении шага укладки

---

## 7. Оценка трудозатрат

| Этап | Время |
|------|-------|
| Добавление свойств в ViewModel | 20 мин |
| Создание XAML-разметки | 40 мин |
| Тестирование | 20 мин |
| **Итого** | **1-1.5 часа** |

---

## 8. Статус

**Статус:** Ожидает разработки  
**Дата создания:** 2026-03-20  
**Дата обновления:** 2026-03-20