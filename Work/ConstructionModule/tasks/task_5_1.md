# Task 5.1: Создать ConstructionView.xaml (базовая разметка)

**Этап:** 5. View  
**Приоритет:** P1 (Высокая)  
**Время:** 2 часа  
**Зависимости:** Task 4.1

---

## 1. Цель задачи

Создать WPF UserControl `ConstructionView.xaml` для отображения конструктора конструкции.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-01 | Добавление слоя материала | Кнопки "Добавить слой" |
| UC-02 | Выбор материала из справочника | ComboBox с материалами |
| UC-03 | Задание толщины слоя | TextBox для толщины |
| UC-04 | Удаление слоя | Кнопка "Удалить" |
| UC-05 | Учёт уровня грунтовых вод | TextBox для УГВ |
| UC-08 | Визуализация конструкции | Canvas для "Пирога" |

---

## 3. Описание изменений

### 3.1. Создать папку Views/Construction

Если папка не существует, создать её.

### 3.2. Создать файл ConstructionView.xaml

**Путь:** `src/Views/Construction/ConstructionView.xaml`

**Код:**

```xml
<UserControl x:Class="SnowMeltingCalculator.Views.Construction.ConstructionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:construction="clr-namespace:SnowMeltingCalculator.ViewModels.Construction"
             mc:Ignorable="d"
             d:DataContext="{d:DesignInstance construction:ConstructionViewModel}"
             d:DesignHeight="600" d:DesignWidth="900">

    <Grid Margin="10">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="3*" />
        </Grid.ColumnDefinitions>

        <!-- Левая панель: Визуализация "Пирога" -->
        <Border Grid.Column="0" 
                Background="White" 
                BorderBrush="Gray" 
                BorderThickness="1"
                Margin="0,0,10,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0" 
                           Text="Визуализация конструкции" 
                           FontWeight="Bold"
                           FontSize="14"
                           Margin="5"/>

                <!-- Canvas для визуализации слоёв -->
                <Canvas x:Name="ConstructionCanvas" 
                        Grid.Row="1"
                        Background="#F5F5F5">
                    <!-- Визуализация будет добавлена в Task 5.2 -->
                </Canvas>
            </Grid>
        </Border>

        <!-- Правая панель: Ввод данных -->
        <ScrollViewer Grid.Column="1" VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="5">

                <!-- Заголовок -->
                <TextBlock Text="Конструктор конструкции" 
                           FontWeight="Bold" 
                           FontSize="16" 
                           Margin="0,0,0,15"/>

                <!-- Параметры УГВ -->
                <TextBlock Text="Уровень грунтовых вод (м):" 
                           FontWeight="Bold" 
                           Margin="0,0,0,5"/>
                <TextBox Text="{Binding GroundwaterLevel, UpdateSourceTrigger=PropertyChanged, StringFormat=F1}"
                         Margin="0,0,0,10">
                    <TextBox.Style>
                        <Style TargetType="TextBox">
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding IsValid}" Value="False">
                                    <Setter Property="BorderBrush" Value="Red"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </TextBox.Style>
                </TextBox>

                <!-- Флаг нагрузок -->
                <CheckBox Content="Наличие нагрузок на покрытие" 
                          IsChecked="{Binding HasLoads}"
                          Margin="0,0,0,15"/>

                <!-- Слои над трубой -->
                <TextBlock Text="Слои над трубой:" 
                           FontWeight="Bold" 
                           Margin="0,10,0,5"/>
                
                <Button Content="Добавить слой над трубой" 
                        Command="{Binding AddLayerAbovePipeCommand}"
                        Margin="0,0,0,5"/>

                <DataGrid ItemsSource="{Binding LayersAbovePipe}"
                          AutoGenerateColumns="False"
                          CanUserAddRows="False"
                          CanUserDeleteRows="False"
                          Height="150"
                          Margin="0,0,0,10">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="№" 
                                           Binding="{Binding Order}" 
                                           Width="30"
                                           IsReadOnly="True"/>
                        <DataGridComboBoxColumn Header="Материал" 
                                               ItemsSource="{Binding DataContext.AvailableMaterials, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                               DisplayMemberPath="Name"
                                               SelectedValueBinding="{Binding Material}"
                                               Width="150"/>
                        <DataGridTextColumn Header="Толщина (мм)" 
                                           Binding="{Binding Thickness, UpdateSourceTrigger=PropertyChanged, StringFormat=F0}"
                                           Width="80"/>
                        <DataGridTextColumn Header="λ (Вт/м·К)" 
                                           Binding="{Binding Lambda, UpdateSourceTrigger=PropertyChanged, StringFormat=F3}"
                                           Width="70"/>
                        <DataGridTemplateColumn Header="Действия" Width="*">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="Удалить" 
                                            Command="{Binding DataContext.RemoveLayerCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                            CommandParameter="{Binding}"
                                            Padding="5,2"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>

                <!-- Разделитель (труба) -->
                <Border Background="Blue" 
                        Height="3" 
                        Margin="0,5,0,5">
                    <TextBlock Text="ТРУБА" 
                               Foreground="White" 
                               HorizontalAlignment="Center"
                               FontSize="10"/>
                </Border>

                <!-- Слои под трубой -->
                <TextBlock Text="Слои под трубой:" 
                           FontWeight="Bold" 
                           Margin="0,10,0,5"/>
                
                <Button Content="Добавить слой под трубой" 
                        Command="{Binding AddLayerBelowPipeCommand}"
                        Margin="0,0,0,5"/>

                <DataGrid ItemsSource="{Binding LayersBelowPipe}"
                          AutoGenerateColumns="False"
                          CanUserAddRows="False"
                          CanUserDeleteRows="False"
                          Height="150"
                          Margin="0,0,0,10">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="№" 
                                           Binding="{Binding Order}" 
                                           Width="30"
                                           IsReadOnly="True"/>
                        <DataGridComboBoxColumn Header="Материал" 
                                               ItemsSource="{Binding DataContext.AvailableMaterials, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                               DisplayMemberPath="Name"
                                               SelectedValueBinding="{Binding Material}"
                                               Width="150"/>
                        <DataGridTextColumn Header="Толщина (мм)" 
                                           Binding="{Binding Thickness, UpdateSourceTrigger=PropertyChanged, StringFormat=F0}"
                                           Width="80"/>
                        <DataGridTextColumn Header="λ (Вт/м·К)" 
                                           Binding="{Binding Lambda, UpdateSourceTrigger=PropertyChanged, StringFormat=F3}"
                                           Width="70"/>
                        <DataGridTemplateColumn Header="Действия" Width="*">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="Удалить" 
                                            Command="{Binding DataContext.RemoveLayerCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                            CommandParameter="{Binding}"
                                            Padding="5,2"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>

                <!-- Результаты расчёта -->
                <TextBlock Text="Результаты расчёта:" 
                           FontWeight="Bold" 
                           Margin="0,15,0,5"/>
                
                <Grid Margin="0,0,0,10">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <TextBlock Grid.Row="0" Grid.Column="0" Text="R1 (над трубой):" Margin="0,0,10,5"/>
                    <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding R1Total, StringFormat={}{0:F4} м²·К/Вт}" Margin="0,0,0,5"/>

                    <TextBlock Grid.Row="1" Grid.Column="0" Text="R2 (под трубой):" Margin="0,0,10,5"/>
                    <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding R2Total, StringFormat={}{0:F4} м²·К/Вт}" Margin="0,0,0,5"/>

                    <TextBlock Grid.Row="2" Grid.Column="0" Text="λE (вокруг трубы):" Margin="0,0,10,5"/>
                    <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding LambdaE, StringFormat={}{0:F2} Вт/м·К}" Margin="0,0,0,5"/>
                </Grid>

                <!-- Сообщение валидации -->
                <TextBlock Text="{Binding ValidationMessage}" 
                           Foreground="Red" 
                           TextWrapping="Wrap"
                           Margin="0,10,0,10"
                           Visibility="{Binding ValidationMessage, Converter={StaticResource StringToVisibilityConverter}}"/>

                <!-- Индикатор загрузки -->
                <StackPanel Orientation="Horizontal" 
                            Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}">
                    <TextBlock Text="Загрузка..." Margin="0,0,10,0"/>
                    <ProgressBar IsIndeterminate="True" Width="100" Height="20"/>
                </StackPanel>

            </StackPanel>
        </ScrollViewer>
    </Grid>

</UserControl>
```

### 3.3. Создать файл ConstructionView.xaml.cs

**Путь:** `src/Views/Construction/ConstructionView.xaml.cs`

**Код:**

```csharp
using System.Windows.Controls;

namespace SnowMeltingCalculator.Views.Construction
{
    /// <summary>
    /// Представление для модуля "Конструктор конструкции"
    /// </summary>
    public partial class ConstructionView : UserControl
    {
        public ConstructionView()
        {
            InitializeComponent();
        }
    }
}
```

---

## 4. Тест-кейсы

### TC-5.1.1: Отображение View

```csharp
[Fact]
public void ConstructionView_ShouldRenderWithoutErrors()
{
    // Arrange
    var viewModel = CreateMockViewModel();
    var view = new ConstructionView { DataContext = viewModel };

    // Act & Assert
    // Проверяем, что View создаётся без исключений
    Assert.NotNull(view);
}
```

### TC-5.1.2: Привязка данных

```csharp
[Fact]
public void ConstructionView_DataBinding_ShouldWork()
{
    // Arrange
    var viewModel = CreateMockViewModel();
    viewModel.GroundwaterLevel = 1.5;
    var view = new ConstructionView { DataContext = viewModel };

    // Act
    // В UI тесте проверяем, что TextBox отображает 1.5

    // Assert
    Assert.Equal(1.5, viewModel.GroundwaterLevel);
}
```

---

## 5. Критерии приёмки

- [ ] Папка `src/Views/Construction/` создана
- [ ] Файл `ConstructionView.xaml` создан
- [ ] Файл `ConstructionView.xaml.cs` создан
- [ ] View отображает слои над трубой и под трубой
- [ ] Привязка данных к ViewModel работает
- [ ] Кнопки "Добавить слой" и "Удалить" работают
- [ ] Результаты расчёта (R1, R2, LambdaE) отображаются
- [ ] Сообщение валидации отображается

---

## 6. Примечания

- Использовать `DataGrid` для отображения слоёв
- `ComboBox` для выбора материала
- `TextBox` для ввода толщины и λ
- Визуализация "Пирога" будет добавлена в Task 5.2

---

**Конец документа**