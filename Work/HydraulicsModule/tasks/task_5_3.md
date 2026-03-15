# Task 5.3: ResultsView.xaml (Отображение результатов)

**Этап:** 5 - Views  
**Приоритет:** Средний  
**Статус:** Не начато  
**Зависимости:** Task 4.1, Task 5.1

---

## 1. Цель задачи

Создать представление для отображения результатов расчёта.

---

## 2. Создаваемые файлы

### 5.1. ResultsView.xaml

**Путь:** `src/Views/Hydraulics/ResultsView.xaml`

```xml
<UserControl x:Class="SnowMeltingCalculator.Views.Hydraulics.ResultsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:vm="clr-namespace:SnowMeltingCalculator.ViewModels.Hydraulics"
             xmlns:models="clr-namespace:SnowMeltingCalculator.Models.Hydraulics"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="400"
             d:DataContext="{d:DesignInstance Type=vm:HydraulicsViewModel}">

    <UserControl.Resources>
        <!-- Стили -->
        <Style x:Key="ResultGroupStyle" TargetType="Border">
            <Setter Property="Background" Value="#FAFAFA"/>
            <Setter Property="BorderBrush" Value="#E0E0E0"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Padding" Value="15"/>
            <Setter Property="Margin" Value="0,0,0,10"/>
            <Setter Property="CornerRadius" Value="5"/>
        </Style>

        <Style x:Key="GroupHeaderStyle" TargetType="TextBlock">
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="Margin" Value="0,0,0,10"/>
            <Setter Property="Foreground" Value="#1976D2"/>
        </Style>

        <Style x:Key="ResultRowStyle" TargetType="Grid">
            <Setter Property="Margin" Value="0,3"/>
        </Style>

        <Style x:Key="ResultLabelStyle" TargetType="TextBlock">
            <Setter Property="VerticalAlignment" Value="Center"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Foreground" Value="#666"/>
        </Style>

        <Style x:Key="ResultValueStyle" TargetType="TextBlock">
            <Setter Property="VerticalAlignment" Value="Center"/>
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="13"/>
        </Style>

        <Style x:Key="ResultUnitStyle" TargetType="TextBlock">
            <Setter Property="VerticalAlignment" Value="Center"/>
            <Setter Property="Margin" Value="5,0,0,0"/>
            <Setter Property="FontSize" Value="11"/>
            <Setter Property="Foreground" Value="#999"/>
        </Style>

        <Style x:Key="HighlightValueStyle" TargetType="TextBlock">
            <Setter Property="VerticalAlignment" Value="Center"/>
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="16"/>
            <Setter Property="Foreground" Value="#1976D2"/>
        </Style>

        <Style x:Key="WarningTextStyle" TargetType="TextBlock">
            <Setter Property="Foreground" Value="#FF9800"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Margin" Value="0,2"/>
            <Setter Property="TextWrapping" Value="Wrap"/>
        </Style>

        <Style x:Key="ErrorTextStyle" TargetType="TextBlock">
            <Setter Property="Foreground" Value="#F44336"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Margin" Value="0,2"/>
            <Setter Property="TextWrapping" Value="Wrap"/>
        </Style>

        <Style x:Key="SuccessTextStyle" TargetType="TextBlock">
            <Setter Property="Foreground" Value="#4CAF50"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Margin" Value="0,2"/>
        </Style>

        <!-- Конвертеры -->
        <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
        
        <!-- Конвертер режима течения в цвет -->
        <local:FlowRegimeToColorConverter x:Key="FlowRegimeToColorConverter"/>
    </UserControl.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="10">
            
            <!-- Заголовок -->
            <TextBlock Text="Результаты расчёта"
                       FontSize="18"
                       FontWeight="Bold"
                       Margin="0,0,0,15"
                       Foreground="#1976D2"/>

            <!-- Основные параметры потока -->
            <Border Style="{StaticResource ResultGroupStyle}">
                <StackPanel>
                    <TextBlock Text="Параметры потока" Style="{StaticResource GroupHeaderStyle}"/>

                    <!-- Скорость потока -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Скорость потока (w):" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.Velocity, StringFormat={}{0:F3}}"
                                   Style="{StaticResource ResultValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="м/с" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>

                    <!-- Число Рейнольдса -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Число Рейнольдса (Re):" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.ReynoldsNumber, StringFormat={}{0:F0}}"
                                   Style="{StaticResource ResultValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>

                    <!-- Режим течения -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Режим течения:" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.FlowRegime}"
                                   Style="{StaticResource ResultValueStyle}"
                                   Foreground="{Binding Result.FlowRegime, Converter={StaticResource FlowRegimeToColorConverter}}"/>
                    </Grid>

                    <!-- Коэффициент трения -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Коэффициент трения (λ):" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.FrictionFactor, StringFormat={}{0:F5}}"
                                   Style="{StaticResource ResultValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- Потери давления -->
            <Border Style="{StaticResource ResultGroupStyle}">
                <StackPanel>
                    <TextBlock Text="Потери давления" Style="{StaticResource GroupHeaderStyle}"/>

                    <!-- Удельные потери -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Удельные потери (R):" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.PressureLossPerMeter, StringFormat={}{0:F1}}"
                                   Style="{StaticResource ResultValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="Па/м" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>

                    <!-- Потери в контуре -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Потери в контуре:" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.CircuitPressureLoss, StringFormat={}{0:F0}}"
                                   Style="{StaticResource ResultValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="Па" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>

                    <!-- Потери в подводке -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Потери в подводке:" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.SupplyPressureLoss, StringFormat={}{0:F0}}"
                                   Style="{StaticResource ResultValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="Па" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>

                    <!-- Потери в вентиле -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="Потери в вентиле:" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding Result.ValvePressureLoss, StringFormat={}{0:F0}}"
                                   Style="{StaticResource ResultValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="Па" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>

                    <Separator Margin="0,10"/>

                    <!-- Общие потери -->
                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="ОБЩИЕ ПОТЕРИ:" 
                                   Style="{StaticResource ResultLabelStyle}"
                                   FontWeight="Bold"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding TotalPressureLossKPa, StringFormat={}{0:F2}}"
                                   Style="{StaticResource HighlightValueStyle}"/>
                        <TextBlock Grid.Column="2" Text="кПа" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>

                    <Grid Style="{StaticResource ResultRowStyle}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBlock Grid.Column="0" Text="" Style="{StaticResource ResultLabelStyle}"/>
                        <TextBlock Grid.Column="1" 
                                   Text="{Binding TotalPressureLossMbar, StringFormat={}{0:F1}}"
                                   Style="{StaticResource ResultValueStyle}"
                                   Foreground="#666"/>
                        <TextBlock Grid.Column="2" Text="мбар" Style="{StaticResource ResultUnitStyle}"/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- Предупреждения -->
            <Border Background="#FFF3E0" 
                    Padding="10" 
                    Margin="0,0,0,10"
                    CornerRadius="5"
                    Visibility="{Binding HasErrors, Converter={StaticResource BooleanToVisibilityConverter}, ConverterParameter=Inverse}">
                <StackPanel>
                    <TextBlock Text="Предупреждения" 
                               FontWeight="Bold"
                               Foreground="#E65100"
                               Margin="0,0,0,5"/>
                    
                    <ItemsControl ItemsSource="{Binding Warnings}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding}" Style="{StaticResource WarningTextStyle}">
                                    <TextBlock.Margin>
                                        <Thickness Left="10" Top="2" Right="0" Bottom="2"/>
                                    </TextBlock.Margin>
                                </TextBlock>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </Border>

            <!-- Ошибки -->
            <Border Background="#FFEBEE" 
                    Padding="10" 
                    Margin="0,0,0,10"
                    CornerRadius="5"
                    Visibility="{Binding HasErrors, Converter={StaticResource BooleanToVisibilityConverter}}">
                <StackPanel>
                    <TextBlock Text="Ошибки" 
                               FontWeight="Bold"
                               Foreground="#C62828"
                               Margin="0,0,0,5"/>
                    
                    <TextBlock Text="{Binding ErrorMessage}" 
                               Style="{StaticResource ErrorTextStyle}"
                               TextWrapping="Wrap"/>
                </StackPanel>
            </Border>

            <!-- Статус расчёта -->
            <Border Background="#E8F5E9" 
                    Padding="10" 
                    CornerRadius="5"
                    Visibility="{Binding Result.IsValid, Converter={StaticResource BooleanToVisibilityConverter}}">
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                    <TextBlock Text="✓ " 
                               Foreground="#2E7D32"
                               FontWeight="Bold"
                               FontSize="14"/>
                    <TextBlock Text="Расчёт выполнен успешно" 
                               Style="{StaticResource SuccessTextStyle}"
                               FontWeight="Bold"/>
                </StackPanel>
            </Border>

            <!-- Информация о коллекторе -->
            <Border Background="#E3F2FD" 
                    Padding="10" 
                    Margin="0,10,0,0"
                    CornerRadius="5"
                    Visibility="{Binding SelectedCollector, Converter={StaticResource BooleanToVisibilityConverter}}">
                <StackPanel>
                    <TextBlock Text="Рекомендуемый коллектор" 
                               FontWeight="Bold"
                               Foreground="#1565C0"
                               Margin="0,0,0,5"/>
                    
                    <TextBlock Text="{Binding SelectedCollector.Name}" 
                               FontWeight="Bold"
                               FontSize="14"/>
                    
                    <TextBlock Text="{Binding SelectedCollector.Description}" 
                               Margin="0,5,0,0"
                               TextWrapping="Wrap"/>
                    
                    <Grid Margin="0,10,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        
                        <StackPanel Grid.Column="0">
                            <TextBlock Text="Контур:" Foreground="#666" FontSize="11"/>
                            <TextBlock Text="{Binding SelectedCollector.CircuitCount, StringFormat={}{0} шт.}" 
                                       FontWeight="Bold"/>
                        </StackPanel>
                        
                        <StackPanel Grid.Column="1">
                            <TextBlock Text="Макс. расход:" Foreground="#666" FontSize="11"/>
                            <TextBlock Text="{Binding SelectedCollector.MaxFlowRate, StringFormat={}{0} л/ч}" 
                                       FontWeight="Bold"/>
                        </StackPanel>
                        
                        <StackPanel Grid.Column="2">
                            <TextBlock Text="Макс. давление:" Foreground="#666" FontSize="11"/>
                            <TextBlock Text="{Binding SelectedCollector.MaxPressure, StringFormat={}{0} кПа}" 
                                       FontWeight="Bold"/>
                        </StackPanel>
                    </Grid>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

### 5.2. ResultsView.xaml.cs

**Путь:** `src/Views/Hydraulics/ResultsView.xaml.cs`

```csharp
using System.Windows.Controls;
using System.Windows.Media;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Views.Hydraulics
{
    /// <summary>
    /// Представление для отображения результатов расчёта
    /// </summary>
    public partial class ResultsView : UserControl
    {
        public ResultsView()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Конвертер режима течения в цвет
    /// </summary>
    public class FlowRegimeToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is FlowRegime regime)
            {
                return regime switch
                {
                    FlowRegime.Laminar => new SolidColorBrush(Color.FromRgb(46, 125, 50)),    // Зелёный
                    FlowRegime.Transitional => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Оранжевый
                    FlowRegime.Turbulent => new SolidColorBrush(Color.FromRgb(33, 150, 243)),  // Синий
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
```

---

## 3. Интеграция в HydraulicsView

### 3.1. Добавить в HydraulicsView.xaml

```xml
<!-- В раздел Resources добавить -->
<views:ResultsView x:Key="ResultsView"/>

<!-- Заменить секцию результатов на -->
<views:ResultsView DataContext="{Binding}"/>
```

---

## 4. Критерии приёмки

- [ ] Файлы `ResultsView.xaml` и `.xaml.cs` созданы
- [ ] DataBinding к HydraulicsViewModel работает
- [ ] Результаты отображаются корректно
- [ ] Предупреждения отображаются жёлтым цветом
- [ ] Ошибки отображаются красным цветом
- [ ] Успешный статус отображается зелёным цветом
- [ ] Информация о коллекторе отображается
- [ ] Стили применены корректно
- [ ] ScrollViewer работает
- [ ] Конвертер FlowRegimeToColorConverter работает

---

## 5. Примечания

- Используются стили для единообразия UI
- Результаты сгруппированы по категориям
- Основные потери выделены крупным шрифтом
- Цвет режима течения зависит от типа (ламинарный/переходный/турбулентный)
- Предупреждения и ошибки отображаются в отдельных блоках
- Информация о коллекторе отображается в синем блоке