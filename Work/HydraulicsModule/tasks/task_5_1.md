# Task 5.1: Создать CircuitsView.xaml

**Этап:** 5 - Views  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 4.1 (CircuitsViewModel)

---

## 1. Цель задачи

Создать представление для таблицы контуров с DataGrid.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Ввод параметров контуров | DataGrid для редактирования |
| UC-08 | Управление контурами | Кнопки управления |

---

## 3. Создаваемые файлы

### 3.1. CircuitsView.xaml

**Путь:** `src/Views/Hydraulics/CircuitsView.xaml`

**Структура:**

```xml
<UserControl x:Class="SnowMeltingCalculator.Views.Hydraulics.CircuitsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:SnowMeltingCalculator.ViewModels.Hydraulics"
             xmlns:models="clr-namespace:SnowMeltingCalculator.Models.Hydraulics"
             d:DataContext="{d:DesignInstance Type=vm:CircuitsViewModel}">

    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Заголовок -->
        <TextBlock Grid.Row="0" 
                   Text="Таблица контуров"
                   FontSize="20"
                   FontWeight="Bold"
                   Margin="0,0,0,15"/>

        <!-- Параметры теплоносителя -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="Гликоль:" Margin="0,0,5,0" VerticalAlignment="Center"/>
            <ComboBox SelectedItem="{Binding GlycolType}" Width="150" Margin="0,0,15,0">
                <ComboBoxItem Content="Этиленгликоль" Tag="{x:Static models:GlycolType.Ethylene}"/>
                <ComboBoxItem Content="Пропиленгликоль" Tag="{x:Static models:GlycolType.Propylene}"/>
            </ComboBox>

            <TextBlock Text="Концентрация (%):" Margin="0,0,5,0" VerticalAlignment="Center"/>
            <TextBox Text="{Binding GlycolConcentration, StringFormat=F0}" Width="50" Margin="0,0,15,0"/>

            <TextBlock Text="Режим:" Margin="0,0,5,0" VerticalAlignment="Center"/>
            <Button Content="Рабочая" 
                    Command="{Binding SwitchModeCommand}"
                    Visibility="{Binding CurrentMode, Converter={StaticResource EnumToVisibilityConverter}, ConverterParameter=Design}"/>
            <Button Content="Расчётная" 
                    Command="{Binding SwitchModeCommand}"
                    Visibility="{Binding CurrentMode, Converter={StaticResource EnumToVisibilityConverter}, ConverterParameter=Operating}"/>
        </StackPanel>

        <!-- DataGrid контуров -->
        <DataGrid Grid.Row="2"
                  ItemsSource="{Binding SelectedCollector.Circuits}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="True"
                  CanUserDeleteRows="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="№" Binding="{Binding CircuitNumber}" IsReadOnly="True" Width="40"/>
                <DataGridTextColumn Header="Длина (м)" Binding="{Binding CircuitLength, StringFormat=F1}" Width="80"/>
                <DataGridTextColumn Header="Подводка (м)" Binding="{Binding SupplyLength, StringFormat=F1}" Width="80"/>
                <DataGridTextColumn Header="Площадь (м²)" Binding="{Binding CircuitArea, StringFormat=F1}" Width="80"/>
                <DataGridTextColumn Header="Мощность (Вт)" Binding="{Binding Power, StringFormat=F0}" IsReadOnly="True" Width="100"/>
                <DataGridTextColumn Header="Расход (л/ч)" Binding="{Binding FlowRate, StringFormat=F1}" IsReadOnly="True" Width="80"/>
                <DataGridTextColumn Header="Потери (мбар)" Binding="{Binding OperatingResult.TotalLoss, Converter={StaticResource PaToMbarConverter}, StringFormat=F1}" IsReadOnly="True" Width="100"/>
                <DataGridTextColumn Header="Обороты" Binding="{Binding ValveTurns, StringFormat=F1}" IsReadOnly="True" Width="80"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Кнопки управления -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,15,0,0">
            <Button Content="+ Добавить контур"
                    Command="{Binding AddCircuitCommand}"
                    IsEnabled="{Binding CanAddCircuit}"/>
            <Button Content="Рассчитать"
                    Command="{Binding CalculateCommand}"
                    Margin="10,0,0,0"/>
        </StackPanel>
    </Grid>
</UserControl>
```

---

## 4. Критерии приёмки

- [ ] Файл `CircuitsView.xaml` создан
- [ ] DataGrid для таблицы контуров
- [ ] Карточки коллекторов
- [ ] Переключатель режима (Рабочая/Расчётная)
- [ ] Кнопки управления
- [ ] Валидация ввода
- [ ] DataContext привязан к CircuitsViewModel

---

## 5. Примечания

- DataGrid позволяет редактировать параметры контуров
- Переключатель режима меняет отображаемые потери давления
- Кнопки управления привязаны к командам

---

## 6. Связанные задачи

- Task 4.1: CircuitsViewModel — привязка к View
- Task 5.2: CircuitsView.xaml.cs — code-behind

---

*Дата создания: 2026-03-17*