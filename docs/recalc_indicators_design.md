# СИСТЕМА ИНДИКАТОРОВ ПЕРЕСЧЁТА — ДИЗАЙН-ДОКУМЕНТ

**Версия:** 1.1  
**Дата:** 2026-03-25  
**На основе:** [design_guidelines.md](./design_guidelines.md)  
**Назначение:** Единый дизайн индикаторов состояния пересчёта для всех модулей приложения

---

## СОДЕРЖАНИЕ

1. [Обзор системы](#1-обзор-системы)
2. [Бейджи на навигации](#2-бейджи-на-навигации)
3. [Индикаторы внутри вкладок](#3-индикаторы-внутри-вкладок)
4. [XAML Ресурсы](#4-xaml-ресурсы)
5. [Примеры использования](#5-примеры-использования)
6. [Состояния и анимации](#6-состояния-и-анимации)

---

## 1. ОБЗОР СИСТЕМЫ

### 1.1 Цель

Система индикаторов пересчёта показывает пользователю актуальность данных в разных модулях калькулятора. Когда пользователь меняет параметры, индикатор сигнализирует о необходимости пересчёта результатов.

### 1.2 Компоненты системы

```
┌─────────────────────────────────────────────────────────────────┐
│                    СИСТЕМА ИНДИКАТОРОВ ПЕРЕСЧЁТА                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. БЕЙДЖИ НА НАВИГАЦИИ (Navigation Badges)                     │
│     ├── Расположение: Рядом с названием вкладки                  │
│     ├── Размер: Компактный (16×16 px)                            │
│     └── Состояния: Default → NeedsRecalculation → Calculated    │
│                                                                  │
│  2. ИНДИКАТОРЫ ВНУТРИ ВКЛАДОК (Tab Indicators)                  │
│     ├── Расположение: Внутри контента вкладки                    │
│     ├── Размер: Полноразмерный (высота 40 px)                    │
│     └── Состояния: Info → Warning → Processing → Success        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Соответствие design_guidelines.md

| Параметр | Значение из Guidelines | Применение в индикаторах |
|----------|------------------------|--------------------------|
| **Цвета** | RehauRed `#E50040`, RehauTeal `#4FC7B5` | Бейджи, статусы |
| **Типографика** | Inter Medium 12-14px | Текст индикаторов |
| **Отступы** | XS(8px), SM(12px), MD(16px) | Padding и margin |
| **Скругления** | MD(8px), Full(999px) | Бейджи круглые, индикаторы 8px |
| **Состояния** | Default/Hover/Pressed/Disabled | Все компоненты |

---

## 2. БЕЙДЖИ НА НАВИГАЦИИ

### 2.1 Визуальный дизайн

```
┌─────────────────────────────────────────────────────────────────┐
│  НАВИГАЦИЯ С БЕЙДЖАМИ                                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  📊 Конструкция                                    [●]   │    │
│  │  🌡️ Тепловой расчёт                               [●]   │    │
│  │  🌍 Климат                                        [●]   │    │
│  │  💧 Гидравлика                              [⚠️ 3]       │    │
│  │  🔥 Результаты                                      [●]   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Легенда:                                                        │
│  [●] — данные актуальны (RehauTeal)                              │
│  [⚠️] — требуется пересчёт (RehauRed)                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Спецификация

| Параметр | Значение |
|----------|----------|
| **Размер** | 20×20 px (круглый) |
| **Шрифт** | Inter Medium, 11px |
| **Цвет фона (актуально)** | `RehauTeal` `#4FC7B5` |
| **Цвет фона (требуется пересчёт)** | `RehauRed` `#E50040` |
| **Цвет текста** | `RehauWhite` `#FFFFFF` |
| **Отступ от текста** | 8px (Spacing.XS) |

### 2.3 XAML Стили

```xml
<!-- ============================================ -->
<!-- БЕЙДЖИ НА НАВИГАЦИИ: Статические ресурсы     -->
<!-- ============================================ -->

<!-- Цвета бейджей (используются существующие из guidelines) -->
<SolidColorBrush x:Key="Badge.Bg.Valid" 
                 Color="{StaticResource RehauTeal}"/>
<SolidColorBrush x:Key="Badge.Bg.Invalid" 
                 Color="{StaticResource RehauRed}"/>
<SolidColorBrush x:Key="Badge.Foreground" 
                 Color="{StaticResource RehauWhite}"/>

<!-- ============================================ -->
<!-- БЕЙДЖ: Статус актуальности (Navigation)      -->
<!-- ============================================ -->
<Style x:Key="Badge.Navigation" TargetType="Border">
    <Setter Property="Width" Value="20"/>
    <Setter Property="Height" Value="20"/>
    <Setter Property="CornerRadius" Value="999"/>
    <Setter Property="Background" Value="{StaticResource Badge.Bg.Valid}"/>
    <Setter Property="HorizontalAlignment" Value="Center"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
    <Setter Property="Margin" Value="8,0,0,0"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding NeedsRecalculation}" Value="True">
            <Setter Property="Background" Value="{StaticResource Badge.Bg.Invalid}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

<!-- ============================================ -->
<!-- БЕЙДЖ: Текст счётчика                        -->
<!-- ============================================ -->
<Style x:Key="Badge.Text" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
    <Setter Property="FontWeight" Value="Medium"/>
    <Setter Property="FontSize" Value="11"/>
    <Setter Property="Foreground" Value="{StaticResource Badge.Foreground}"/>
    <Setter Property="HorizontalAlignment" Value="Center"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
</Style>
```

### 2.4 Использование в навигации

```xml
<!-- Элемент навигации с бейджем -->
<Border Style="{StaticResource Nav.Item.Container}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        
        <!-- Иконка -->
        <Path Grid.Column="0"
              Data="{StaticResource Icon.Hydraulics}"
              Width="24" Height="24"/>
        
        <!-- Название -->
        <TextBlock Grid.Column="1"
                   Text="Гидравлика"
                   Style="{StaticResource Text.Body}"
                   Margin="12,0,0,0"/>
        
        <!-- Бейдж статуса -->
        <Border Grid.Column="2"
                Style="{StaticResource Badge.Navigation}">
            <TextBlock Style="{StaticResource Badge.Text}"
                       Text="{Binding InvalidItemsCount, FallbackValue='●'}"/>
        </Border>
    </Grid>
</Border>
```

---

## 3. ИНДИКАТОРЫ ВНУТРИ ВКЛАДОК

### 3.1 Визуальный дизайн

```
┌─────────────────────────────────────────────────────────────────┐
│  ИНДИКАТОРЫ ВНУТРИ ВКЛАДОК                                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  [ℹ️] Данные актуальны                                  │    │
│  └─────────────────────────────────────────────────────────┘    │
│       Серый фон (#F5F5F5), серая рамка (#E0E0E0)                │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  [⚠️] Изменены параметры. Требуется пересчёт.          │    │
│  │       [Пересчитать]                                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│       Жёлтый фон (#FFF8E8), оранжевая рамка (#FFB300)           │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  [⟳] Выполняется пересчёт...                            │    │
│  └─────────────────────────────────────────────────────────┘    │
│       Синий фон (#E3F2FD), синяя рамка (#2196F3)                │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  [✓] Пересчёт завершён                                  │    │
│  └─────────────────────────────────────────────────────────┘    │
│       Зелёный фон (#E8F6F4), зелёная рамка (#4FC7B5)            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Состояния индикаторов

| Состояние | Иконка | Цвет фона | Цвет рамки | Цвет текста | Кнопка действия |
|-----------|--------|-----------|------------|-------------|-----------------|
| **Info** (Актуально) | ℹ️ | `#F5F5F5` | `#E0E0E0` | `#616161` | Нет |
| **Warning** (Требуется пересчёт) | ⚠️ | `#FFF8E8` | `#FFB300` | `#FF9800` | Да (Primary) |
| **Processing** (Идёт пересчёт) | ⟳ | `#E3F2FD` | `#2196F3` | `#1976D2` | Нет (Disabled) |
| **Success** (Завершено) | ✓ | `#E8F6F4` | `#4FC7B5` | `#4FC7B5` | Нет |

### 3.3 XAML Стили

```xml
<!-- ============================================ -->
<!-- ИНДИКАТОРЫ ПЕРЕСЧЁТА: Цвета                  -->
<!-- ============================================ -->

<!-- Дополнительные цвета (добавляются в RecalcIndicators.xaml) -->
<Color x:Key="WarningBackground">#FFF8E8</Color>
<Color x:Key="WarningBorder">#FFB300</Color>
<Color x:Key="ProcessingBackground">#E3F2FD</Color>
<Color x:Key="ProcessingBorder">#2196F3</Color>

<!-- Brushes для границ -->
<SolidColorBrush x:Key="Color.Border.Default" Color="{StaticResource Gray300}"/>
<SolidColorBrush x:Key="Color.Border.Warning" Color="{StaticResource WarningBorder}"/>
<SolidColorBrush x:Key="Color.Border.Processing" Color="{StaticResource ProcessingBorder}"/>
<SolidColorBrush x:Key="Color.Border.Success" Color="{StaticResource RehauTeal}"/>

<!-- Фоны индикаторов -->
<SolidColorBrush x:Key="RecalcIndicator.Bg.Info" 
                 Color="{StaticResource Gray100}"/>
<SolidColorBrush x:Key="RecalcIndicator.Bg.Warning" 
                 Color="{StaticResource WarningBackground}"/>
<SolidColorBrush x:Key="RecalcIndicator.Bg.Processing" 
                 Color="{StaticResource ProcessingBackground}"/>
<SolidColorBrush x:Key="RecalcIndicator.Bg.Success" 
                 Color="{StaticResource RehauTealLightOpacity}"/>

<!-- Текст индикаторов -->
<SolidColorBrush x:Key="RecalcIndicator.Text.Info" 
                 Color="{StaticResource Gray700}"/>
<SolidColorBrush x:Key="RecalcIndicator.Text.Warning" 
                 Color="{StaticResource WarningOrange}"/>
<SolidColorBrush x:Key="RecalcIndicator.Text.Processing" 
                 Color="{StaticResource InfoBlue}"/>
<SolidColorBrush x:Key="RecalcIndicator.Text.Success" 
                 Color="{StaticResource RehauTeal}"/>

<!-- ============================================ -->
<!-- ИНДИКАТОР: Контейнер (Card-стиль)            -->
<!-- ============================================ -->
<Style x:Key="RecalcIndicator.Container" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource RecalcIndicator.Bg.Info}"/>
    <Setter Property="BorderBrush" Value="{StaticResource Color.Border.Default}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="{StaticResource Radius.MD}"/>
    <Setter Property="Padding" Value="16,12"/>
    <Setter Property="Margin" Value="0,0,0,16"/>
</Style>

<!-- ============================================ -->
<!-- ИНДИКАТОР: Текст сообщения                   -->
<!-- ============================================ -->
<Style x:Key="RecalcIndicator.Text" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
    <Setter Property="FontWeight" Value="Medium"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Foreground" Value="{StaticResource RecalcIndicator.Text.Info}"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
</Style>

<!-- ============================================ -->
<!-- ИНДИКАТОР: Иконка (Path вместо PathIcon)     -->
<!-- ============================================ -->
<Style x:Key="RecalcIndicator.Icon" TargetType="Path">
    <Setter Property="Width" Value="20"/>
    <Setter Property="Height" Value="20"/>
    <Setter Property="Stretch" Value="Uniform"/>
    <Setter Property="Margin" Value="0,0,12,0"/>
    <Setter Property="Fill" Value="{StaticResource RecalcIndicator.Text.Info}"/>
</Style>
```

### 3.4 Использование индикатора

```xml
<!-- Индикатор пересчёта внутри вкладки -->
<Border Style="{StaticResource RecalcIndicator.Container}"
        Visibility="{Binding ShowRecalcIndicator, Converter={StaticResource BoolToVisibilityConverter}}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        
        <!-- Иконка состояния (Path вместо PathIcon) -->
        <Path Grid.Column="0"
              Style="{StaticResource RecalcIndicator.Icon}"
              Data="{Binding IndicatorIcon}"/>
        
        <!-- Текст сообщения -->
        <TextBlock Grid.Column="1"
                   Style="{StaticResource RecalcIndicator.Text}"
                   Text="{Binding IndicatorMessage}"/>
        
        <!-- Кнопка действия (только для Warning) -->
        <Button Grid.Column="2"
                Style="{StaticResource Button.Primary}"
                Content="Пересчитать"
                Command="{Binding RecalculateCommand}"
                Visibility="{Binding ShowRecalculateButton, Converter={StaticResource BoolToVisibilityConverter}}"/>
    </Grid>
</Border>
```

---

## 4. XAML РЕСУРСЫ

### 4.1 Полный ResourceDictionary

См. файл `src/Themes/RecalcIndicators.xaml` — полная реализация всех стилей.

### 4.2 Интеграция в App.xaml

```xml
<!-- App.xaml -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Существующие ресурсы -->
            <ResourceDictionary Source="Themes/PrimitiveColors.xaml"/>
            <ResourceDictionary Source="Themes/SemanticTokens.xaml"/>
            <ResourceDictionary Source="Themes/ComponentTokens.xaml"/>
            <ResourceDictionary Source="Themes/Typography.xaml"/>
            <ResourceDictionary Source="Themes/Buttons.xaml"/>
            <ResourceDictionary Source="Themes/Cards.xaml"/>
            
            <!-- Новый ресурс индикаторов -->
            <ResourceDictionary Source="Themes/RecalcIndicators.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

---

## 5. ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ

### 5.1 Полный пример навигации

```xml
<!-- Views/Shared/NavigationView.xaml -->
<ItemsControl ItemsSource="{Binding NavigationItems}"
              Margin="16">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Style="{StaticResource Card.Interactive}"
                    Padding="12,8"
                    Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="24"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    
                    <!-- Иконка (Path вместо PathIcon) -->
                    <Path Grid.Column="0"
                          Data="{Binding Icon}"
                          Width="20" Height="20"/>
                    
                    <!-- Название -->
                    <TextBlock Grid.Column="1"
                               Text="{Binding Title}"
                               Style="{StaticResource Text.Body}"
                               Margin="12,0,0,0"/>
                    
                    <!-- Бейдж статуса -->
                    <Border Grid.Column="2"
                            Style="{StaticResource Badge.Navigation}">
                        <Border.Style>
                            <Style TargetType="Border" BasedOn="{StaticResource Badge.Navigation}">
                                <Setter Property="Background" Value="{StaticResource Badge.Bg.Valid}"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding NeedsRecalculation}" Value="True">
                                        <Setter Property="Background" Value="{StaticResource Badge.Bg.Invalid}"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </Border.Style>
                        <TextBlock Style="{StaticResource Badge.Text}"
                                   Text="{Binding InvalidCount, FallbackValue='●'}"/>
                    </Border>
                </Grid>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 5.2 Готовый компонент RecalcIndicator

```xml
<!-- Использование готового UserControl -->
<controls:RecalcIndicator 
    State="{Binding RecalcState}"
    Message="{Binding RecalcMessage}"
    ShowButton="{Binding ShowRecalculateButton}"
    RecalculateCommand="{Binding RecalculateCommand}"/>
```

### 5.3 ViewModel для индикатора

```csharp
// ViewModels/Shared/RecalcIndicatorViewModel.cs
using SnowMeltingCalculator.Models.Enums;

namespace SnowMeltingCalculator.ViewModels.Shared
{
    public partial class RecalcIndicatorViewModel : ObservableObject
    {
        [ObservableProperty]
        private RecalcState _state = RecalcState.Info;

        public string Message => State switch
        {
            RecalcState.Info => "Данные актуальны",
            RecalcState.Warning => "Изменены параметры. Требуется пересчёт.",
            RecalcState.Processing => "Выполняется пересчёт...",
            RecalcState.Success => "Пересчёт завершён",
            _ => ""
        };

        public string IconPath => State switch
        {
            RecalcState.Info => "M12 2C6.48 2...",
            RecalcState.Warning => "M1 21h22L12 2...",
            RecalcState.Processing => "M12 6v3l4-4...",
            RecalcState.Success => "M9 16.17L4.83...",
            _ => ""
        };
    }
}

// Enum: Models/Enums/RecalcState.cs
namespace SnowMeltingCalculator.Models.Enums
{
    public enum RecalcState
    {
        Info,       // Актуально (серый)
        Warning,    // Требуется пересчёт (жёлтый #FFF8E8)
        Processing, // Идёт пересчёт (синий #E3F2FD)
        Success     // Завершено (зелёный #E8F6F4)
    }
}
```

---

## 6. СОСТОЯНИЯ И АНИМАЦИИ

### 6.1 Диаграмма состояний

```
┌─────────────────────────────────────────────────────────────────┐
│              ДИАГРАММА СОСТОЯНИЙ ИНДИКАТОРА                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│                         ┌──────────┐                            │
│                    ┌───│   Info   │◄────────┐                   │
│                    │    │(серый)  │         │                   │
│                    │    └──────────┘         │                   │
│                    │         │               │                   │
│                    │         │ параметры     │ пересчёт          │
│                    │         │ изменены      │ завершён          │
│                    │         ▼               │                   │
│   отмена ─────────►│    ┌──────────┐         │                   │
│   изменений        │    │ Warning  │─────────┘                   │
│                    └─── │(жёлтый)  │                             │
│                         └──────────┘                             │
│                              │                                   │
│                              │ нажатие                           │
│                              │ "Пересчитать"                     │
│                              ▼                                   │
│                         ┌──────────┐                             │
│                         │Processing│                             │
│                         │(синий)   │                             │
│                         └──────────┘                             │
│                              │                                   │
│                              │ завершено                         │
│                              ▼                                   │
│                         ┌──────────┐                             │
│                         │ Success  │◄────────────────────────┐   │
│                         │(зелёный) │                          │   │
│                         └──────────┘                          │   │
│                              │                                 │   │
│                              │ 3 сек                           │   │
│                              ▼                                 │   │
│                         (скрывается) ──────────────────────────┘   │
│                                                                    │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Анимации

```xml
<!-- Анимация появления индикатора -->
<Storyboard x:Key="IndicatorShowAnimation">
    <DoubleAnimation Storyboard.TargetProperty="Opacity"
                     From="0" To="1"
                     Duration="0:0:0.2"/>
    <ThicknessAnimation Storyboard.TargetProperty="Margin"
                        From="0,-20,0,16" To="0,0,0,16"
                        Duration="0:0:0.3">
        <ThicknessAnimation.EasingFunction>
            <QuadraticEase EasingMode="EaseOut"/>
        </ThicknessAnimation.EasingFunction>
    </ThicknessAnimation>
</Storyboard>

<!-- Анимация вращения для Processing -->
<Storyboard x:Key="ProcessingSpinAnimation" RepeatBehavior="Forever">
    <DoubleAnimation Storyboard.TargetProperty="(Path.RenderTransform).(RotateTransform.Angle)"
                     From="0" To="360"
                     Duration="0:0:1"/>
</Storyboard>
```

### 6.3 Временные параметры

| Действие | Длительность | Easing |
|----------|--------------|--------|
| Появление индикатора | 200ms | Quadratic EaseOut |
| Скрытие индикатора | 300ms | Quadratic EaseIn |
| Смена состояния | 150ms | Linear |
| Вращение иконки (Processing) | 1000ms | Linear (бесконечно) |
| Автоскрытие Success | 3000ms | — |

---

## ПРИЛОЖЕНИЕ: ЧЕК-ЛИСТ ВНЕДРЕНИЯ

### Добавление в проект

- [x] Создать файл `Themes/RecalcIndicators.xaml`
- [x] Добавить в `App.xaml` в MergedDictionaries
- [x] Создать `RecalcIndicatorViewModel`
- [x] Обновить ViewModel модулей (добавить свойства индикатора)
- [x] Обновить XAML Views (добавить индикаторы)
- [x] Протестировать все состояния

### Проверка соответствия guidelines

- [x] Цвета используются из существующих ресурсов
- [x] Типографика — Inter Medium 11-14px
- [x] Отступы — 8px, 12px, 16px
- [x] Скругления — 8px (MD) и 999px (Full)
- [x] Warning: жёлтый фон (#FFF8E8), оранжевая рамка (#FFB300)
- [x] Processing: синий фон (#E3F2FD), синяя рамка (#2196F3)

### Исправленные проблемы (v1.1)

- [x] Добавлены недостающие ресурсы: Gray100, Gray700, Color.Border.*
- [x] Заменён PathIcon на Path (PathIcon не существует в WPF)
- [x] Исправлен namespace: `SnowMeltingCalculator` вместо `REHAU.Snegotayanie`
- [x] Обновлены цвета Warning (жёлтый) и Processing (синий)

---

**Документ создан:** 2026-03-25  
**Обновлён:** 2026-03-25 (v1.1 — исправлены критичные проблемы)  
**На основе:** [design_guidelines.md](./design_guidelines.md)  
**Версия:** 1.1
