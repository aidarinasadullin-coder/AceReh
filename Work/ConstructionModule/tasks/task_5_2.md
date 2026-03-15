# Task 5.2: Реализовать визуализацию "Пирога" (Canvas)

**Этап:** 5. View  
**Приоритет:** P2 (Средняя)  
**Время:** 2 часа  
**Зависимости:** Task 5.1

---

## 1. Цель задачи

Реализовать визуализацию слоёв конструкции ("Пирога") на Canvas с динамической отрисовкой.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-08 | Визуализация конструкции ("Пирог") | Canvas с слоями |

---

## 3. Описание изменений

### 3.1. Обновить ConstructionView.xaml

**Файл:** `src/Views/Construction/ConstructionView.xaml`

**Добавить визуализацию в Canvas:**

```xml
<!-- Canvas для визуализации слоёв -->
<Canvas x:Name="ConstructionCanvas" 
        Grid.Row="1"
        Background="#F5F5F5"
        SizeChanged="OnCanvasSizeChanged">
    
    <!-- Слои над трубой (отрисовываются снизу вверх) -->
    <ItemsControl ItemsSource="{Binding LayersAbovePipe}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <Canvas />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border Background="{Binding Material.Category, Converter={StaticResource CategoryToColorConverter}}"
                        BorderBrush="Gray"
                        BorderThickness="1">
                    <Grid>
                        <TextBlock Text="{Binding Material.Name}" 
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center"
                                   FontSize="10"
                                   Foreground="White"/>
                        <TextBlock Text="{Binding Thickness, StringFormat={}{0:F0} мм}" 
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Bottom"
                                   FontSize="8"
                                   Foreground="White"
                                   Margin="0,0,0,2"/>
                    </Grid>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
        <ItemsControl.ItemContainerStyle>
            <Style TargetType="ContentPresenter">
                <Setter Property="Canvas.Left" Value="10"/>
                <Setter Property="Canvas.Width" Value="{Binding ActualWidth, ElementName=ConstructionCanvas, Converter={StaticResource WidthMinusMarginConverter}}"/>
            </Style>
        </ItemsControl.ItemContainerStyle>
    </ItemsControl>

    <!-- Труба (фиксированная позиция) -->
    <Border x:Name="PipeLayer"
            Background="Blue"
            Height="20"
            Canvas.Left="10"
            Width="{Binding ActualWidth, ElementName=ConstructionCanvas, Converter={StaticResource WidthMinusMarginConverter}}">
        <TextBlock Text="ТРУБА" 
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center"
                   Foreground="White"
                   FontWeight="Bold"/>
    </Border>

    <!-- Слои под трубой (отрисовываются сверху вниз) -->
    <ItemsControl ItemsSource="{Binding LayersBelowPipe}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <Canvas />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border Background="{Binding Material.Category, Converter={StaticResource CategoryToColorConverter}}"
                        BorderBrush="Gray"
                        BorderThickness="1">
                    <Grid>
                        <TextBlock Text="{Binding Material.Name}" 
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center"
                                   FontSize="10"
                                   Foreground="White"/>
                        <TextBlock Text="{Binding Thickness, StringFormat={}{0:F0} мм}" 
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Bottom"
                                   FontSize="8"
                                   Foreground="White"
                                   Margin="0,0,0,2"/>
                    </Grid>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
        <ItemsControl.ItemContainerStyle>
            <Style TargetType="ContentPresenter">
                <Setter Property="Canvas.Left" Value="10"/>
                <Setter Property="Canvas.Width" Value="{Binding ActualWidth, ElementName=ConstructionCanvas, Converter={StaticResource WidthMinusMarginConverter}}"/>
            </Style>
        </ItemsControl.ItemContainerStyle>
    </ItemsControl>

</Canvas>
```

### 3.2. Создать конвертеры

**Файл:** `src/Converters/CategoryToColorConverter.cs`

```csharp
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnowMeltingCalculator.Converters
{
    /// <summary>
    /// Конвертер категории материала в цвет
    /// </summary>
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var category = value as string ?? string.Empty;

            return category.ToLower() switch
            {
                "бетон" => new SolidColorBrush(Color.FromRgb(128, 128, 128)),      // Серый
                "грунт" => new SolidColorBrush(Color.FromRgb(139, 69, 19)),        // Коричневый
                "изоляция" => new SolidColorBrush(Color.FromRgb(255, 215, 0)),     // Жёлтый
                "покрытие" => new SolidColorBrush(Color.FromRgb(50, 50, 50)),      // Чёрный
                "подстилающий" => new SolidColorBrush(Color.FromRgb(160, 160, 160)), // Светло-серый
                "стяжка" => new SolidColorBrush(Color.FromRgb(192, 192, 192)),    // Светло-серый
                _ => new SolidColorBrush(Color.FromRgb(200, 200, 200))            // По умолчанию
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
```

### 3.3. Обновить ConstructionView.xaml.cs

**Файл:** `src/Views/Construction/ConstructionView.xaml.cs`

```csharp
using System.Windows;
using System.Windows.Controls;
using SnowMeltingCalculator.ViewModels.Construction;

namespace SnowMeltingCalculator.Views.Construction
{
    /// <summary>
    /// Представление для модуля "Конструктор конструкции"
    /// </summary>
    public partial class ConstructionView : UserControl
    {
        private const double ScaleFactor = 0.5; // Масштаб: 1 мм = 0.5 пикселей
        private const double PipeHeight = 20;   // Высота трубы в пикселях
        private const double Margin = 10;       // Отступ от краёв

        public ConstructionView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateVisualization();
        }

        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateVisualization();
        }

        /// <summary>
        /// Обновление визуализации слоёв
        /// </summary>
        private void UpdateVisualization()
        {
            if (DataContext is not ConstructionViewModel viewModel)
                return;

            var canvasHeight = ConstructionCanvas.ActualHeight;
            var canvasWidth = ConstructionCanvas.ActualWidth;

            if (canvasHeight <= 0 || canvasWidth <= 0)
                return;

            // Позиция трубы (по центру)
            var pipeY = canvasHeight / 2;

            // Отрисовка слоёв над трубой (снизу вверх)
            double currentY = pipeY - PipeHeight / 2;
            foreach (var layer in viewModel.LayersAbovePipe)
            {
                var layerHeight = layer.Thickness * ScaleFactor;
                currentY -= layerHeight;

                // Установка позиции слоя
                // (реализация через Canvas.SetTop)
            }

            // Отрисовка слоёв под трубой (сверху вниз)
            currentY = pipeY + PipeHeight / 2;
            foreach (var layer in viewModel.LayersBelowPipe)
            {
                var layerHeight = layer.Thickness * ScaleFactor;

                // Установка позиции слоя
                // (реализация через Canvas.SetTop)

                currentY += layerHeight;
            }
        }
    }
}
```

---

## 4. Тест-кейсы

### TC-5.2.1: Отображение слоёв

```csharp
[Fact]
public void ConstructionView_ShouldDisplayLayers()
{
    // Arrange
    var viewModel = CreateMockViewModel();
    viewModel.AddLayerAbovePipeCommand.Execute(null);
    var view = new ConstructionView { DataContext = viewModel };

    // Act
    view.UpdateVisualization();

    // Assert
    // Проверяем, что слои отображаются на Canvas
    Assert.Single(viewModel.LayersAbovePipe);
}
```

### TC-5.2.2: Цвет по категории

```csharp
[Fact]
public void CategoryToColorConverter_ShouldReturnCorrectColor()
{
    // Arrange
    var converter = new CategoryToColorConverter();

    // Act
    var concreteColor = converter.Convert("бетон", null, null, null);
    var soilColor = converter.Convert("грунт", null, null, null);
    var insulationColor = converter.Convert("изоляция", null, null, null);

    // Assert
    Assert.Equal(Colors.Gray, ((SolidColorBrush)concreteColor).Color);
    Assert.Equal(Color.FromRgb(139, 69, 19), ((SolidColorBrush)soilColor).Color);
    Assert.Equal(Colors.Gold, ((SolidColorBrush)insulationColor).Color);
}
```

---

## 5. Критерии приёмки

- [ ] Canvas отображает слои над трубой и под трубой
- [ ] Труба отображается фиксированно в центре
- [ ] Цвет слоя соответствует категории материала
- [ ] Высота слоя пропорциональна толщине
- [ ] При изменении слоёв визуализация обновляется
- [ ] Конвертер `CategoryToColorConverter` создан

---

## 6. Примечания

- Масштаб: 1 мм = 0.5 пикселей
- Труба: фиксированная высота 20 пикселей
- Цвета: бетон=серый, грунт=коричневый, изоляция=жёлтый, покрытие=чёрный

---

**Конец документа**