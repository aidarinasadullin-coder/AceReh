# ИНТЕГРАЦИЯ ИНДИКАТОРОВ ПЕРЕСЧЁТА — РУКОВОДСТВО

**Дата:** 2026-03-25  
**Версия:** 1.0  
**Файлы:**
- `src/Themes/RecalcIndicators.xaml` — стили
- `src/Controls/RecalcIndicator.xaml` — UserControl
- `src/Models/Enums/RecalcState.cs` — enum состояний
- `src/ViewModels/Shared/RecalcIndicatorViewModel.cs` — ViewModel

---

## БЫСТРЫЙ СТАРТ

### Шаг 1: Добавить ресурсы в App.xaml

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Существующие ресурсы -->
            <ResourceDictionary Source="Themes/PrimitiveColors.xaml"/>
            <ResourceDictionary Source="Themes/PrimitiveSpacing.xaml"/>
            <ResourceDictionary Source="Themes/PrimitiveRadius.xaml"/>
            <ResourceDictionary Source="Themes/SemanticTokens.xaml"/>
            <ResourceDictionary Source="Themes/ComponentTokens.xaml"/>
            <ResourceDictionary Source="Themes/Typography.xaml"/>
            <ResourceDictionary Source="Themes/Buttons.xaml"/>
            
            <!-- НОВОЕ: Ресурсы индикаторов -->
            <ResourceDictionary Source="Themes/RecalcIndicators.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### Шаг 2: Использовать в View

**Вариант A: Через UserControl (рекомендуется)**

```xml
<Window xmlns:controls="clr-namespace:REHAU.Snegotayanie.Controls">
    
    <!-- В начале содержимого вкладки -->
    <controls:RecalcIndicator 
        State="{Binding RecalcIndicator.State}"
        Message="{Binding RecalcIndicator.Message}"
        ShowButton="{Binding RecalcIndicator.ShowRecalculateButton}"
        RecalculateCommand="{Binding RecalcIndicator.RecalculateCommand}"
        Visibility="{Binding RecalcIndicator.IsVisible, Converter={StaticResource BoolToVisibilityConverter}}"/>
    
    <!-- Остальной контент -->
    
</Window>
```

**Вариант B: Через стили (для кастомизации)**

```xml
<Border Style="{StaticResource RecalcIndicator.Container}">
    <Border.Style>
        <Style TargetType="Border" BasedOn="{StaticResource RecalcIndicator.Container}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding RecalcState}" Value="Warning">
                    <Setter Property="Background" Value="{StaticResource RecalcIndicator.Bg.Warning}"/>
                    <Setter Property="BorderBrush" Value="{StaticResource Color.Border.Error}"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
    <Grid>
        <PathIcon Style="{StaticResource RecalcIndicator.Icon}" 
                  Data="..."/>
        <TextBlock Style="{StaticResource RecalcIndicator.Text}" 
                   Text="..."/>
        <Button Style="{StaticResource Button.Primary}" 
                Content="Пересчитать"/>
    </Grid>
</Border>
```

### Шаг 3: Добавить в ViewModel

```csharp
public class YourViewModel : ObservableObject
{
    // Индикатор пересчёта
    public RecalcIndicatorViewModel RecalcIndicator { get; } = new();
    
    // При изменении параметров
    private void OnParameterChanged()
    {
        RecalcIndicator.MarkAsNeedsRecalculation();
    }
    
    // После пересчёта
    private async Task RecalculateAsync()
    {
        RecalcIndicator.StartProcessing();
        
        // Выполнить расчёт...
        await PerformCalculation();
        
        RecalcIndicator.MarkAsCalculated();
    }
}
```

---

## ДОСТУПНЫЕ СТИЛИ

### Бейджи навигации

| Стиль | Назначение |
|-------|------------|
| `Badge.Navigation` | Базовый стиль бейджа |
| `Badge.Navigation.Warning` | Красный бейдж (требуется пересчёт) |
| `Badge.Navigation.Valid` | Зелёный бейдж (актуально) |
| `Badge.Text` | Текст внутри бейджа |

### Индикаторы пересчёта

| Стиль | Назначение |
|-------|------------|
| `RecalcIndicator.Container` | Контейнер индикатора |
| `RecalcIndicator.Container.Warning` | Красный фон |
| `RecalcIndicator.Container.Success` | Зелёный фон |
| `RecalcIndicator.Text` | Текст сообщения |
| `RecalcIndicator.Text.Warning` | Красный текст |
| `RecalcIndicator.Text.Success` | Зелёный текст |
| `RecalcIndicator.Icon` | Иконка |
| `RecalcIndicator.Icon.Warning` | Красная иконка |
| `RecalcIndicator.Icon.Success` | Зелёная иконка |

---

## ЦВЕТА (Component Tokens)

```xml
<!-- Бейджи -->
Badge.Bg.Valid          → RehauTeal (#4FC7B5)
Badge.Bg.Invalid        → RehauRed (#E50040)
Badge.Foreground        → White (#FFFFFF)

<!-- Индикаторы -->
RecalcIndicator.Bg.Info         → Gray100 (#F5F5F5)
RecalcIndicator.Bg.Warning      → RehauRedLightOpacity (#FFE5EC)
RecalcIndicator.Bg.Success      → RehauTealLightOpacity (#E8F6F4)

RecalcIndicator.Text.Info       → Gray700 (#616161)
RecalcIndicator.Text.Warning    → RehauRed (#E50040)
RecalcIndicator.Text.Success    → RehauTeal (#4FC7B5)
```

---

## ПРИМЕР: Интеграция в HydraulicsViewModel

```csharp
public partial class HydraulicsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CircuitViewModel> _circuits = new();
    
    // Индикатор пересчёта
    public RecalcIndicatorViewModel RecalcIndicator { get; } = new();
    
    partial void OnCircuitsChanged(ObservableCollection<CircuitViewModel> value)
    {
        // При изменении коллекции контуров
        RecalcIndicator.MarkAsNeedsRecalculation();
    }
    
    [RelayCommand]
    private async Task RecalculateHydraulics()
    {
        RecalcIndicator.StartProcessing();
        
        try
        {
            // Выполнить гидравлический расчёт
            await _hydraulicsService.CalculateAsync(Circuits);
            
            RecalcIndicator.MarkAsCalculated();
        }
        catch (Exception ex)
        {
            // Обработка ошибки
            _logger.LogError(ex, "Hydraulics calculation failed");
        }
    }
}
```

---

## ПРИМЕР: Бейдж в навигации

```xml
<!-- Элемент навигации -->
<Border Style="{StaticResource Card.Interactive}" 
        Padding="12,8">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="24"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        
        <PathIcon Grid.Column="0" Data="{StaticResource Icon.Hydraulics}"/>
        
        <TextBlock Grid.Column="1" 
                   Text="Гидравлика"
                   Margin="12,0,0,0"/>
        
        <!-- Бейдж статуса -->
        <Border Grid.Column="2" 
                Style="{StaticResource Badge.Navigation.Warning}"
                Visibility="{Binding HydraulicsNeedsRecalc, Converter={StaticResource BoolToVisibilityConverter}}">
            <TextBlock Style="{StaticResource Badge.Text}" Text="!"/>
        </Border>
    </Grid>
</Border>
```

---

## СОСТОЯНИЯ И ПЕРЕХОДЫ

```
Info → Warning → Processing → Success → Info
       (параметры  (кнопка     (расчёт    (авто через
        изменены)  "Пересчитать") завершён)  3 сек)
```

---

## СООТВЕТСТВИЕ GUIDELINES

| Параметр | Guidelines | Реализация |
|----------|------------|------------|
| Цвета | RehauRed #E50040, RehauTeal #4FC7B5 | ✅ Используются статические ресурсы |
| Типографика | Inter Medium 14px | ✅ `RecalcIndicator.Text` |
| Отступы | 16,12 для padding | ✅ `Padding="16,12"` |
| Скругления | 8px для карточек | ✅ `Radius.MD` |
| Состояния | Default/Warning/Success | ✅ 4 состояния |

---

## ТЕСТИРОВАНИЕ

Проверьте следующие сценарии:

1. **Info** → Отображается серый индикатор с текстом "Данные актуальны"
2. **Warning** → Отображается красный индикатор с кнопкой "Пересчитать"
3. **Processing** → Отображается индикатор с текстом "Выполняется пересчёт..."
4. **Success** → Отображается зелёный индикатор, затем автоматически скрывается
5. **Бейдж** → Меняет цвет с зелёного на красный при изменении данных

---

**Связанные документы:**
- [design_guidelines.md](../docs/design_guidelines.md)
- [recalc_indicators_design.md](../docs/recalc_indicators_design.md)
