# РУКОВОДСТВО ПО ДИЗАЙНУ РЕХАУ ДЛЯ WPF

**Версия:** 1.0  
**Дата:** 2026-01-21  
**Назначение:** Практическое руководство для разработчиков WPF/XAML

---

## СОДЕРЖАНИЕ

1. [Цветовая палитра](#1-цветовая-палитра)
2. [Дизайн-токены](#2-дизайн-токены)
3. [Типографика](#3-типографика)
4. [Компоновка и отступы](#4-компоновка-и-отступы)
5. [Компоненты UI](#5-компоненты-ui)
6. [Состояния компонентов](#6-состояния-компонентов)
7. [Иконки и изображения](#7-иконки-и-изображения)
8. [Запреты и ограничения](#8-запреты-и-ограничения)
9. [Примеры XAML](#9-примеры-xaml)

---

## 1. ЦВЕТОВАЯ ПАЛИТРА

### 1.1 Корпоративные цвета РЕХАУ

| Название | HEX | RGB | CMYK | RAL | Назначение |
|----------|-----|-----|------|-----|------------|
| **Активный Красный** | `#E50040` | 229/0/64 | 0/100/65/0 | 3028 | Акценты, кнопки, бренд |
| **Умный Зелёный** | `#4FC7B5` | 79/199/181 | 80/0/30/0 | 6033 | Успех, подтверждение |
| **Чёрный** | `#000000` | 0/0/0 | 0/0/0/100 | 8022 | Текст, контраст |
| **Белый** | `#FFFFFF` | 255/255/255 | 0/0/0/0 | 9003 | Фон, поверхности |

### 1.2 Вспомогательные цвета

| Название | HEX | RGB | Назначение |
|----------|-----|-----|------------|
| **Серый светлый** | `#FAFAFA` | 250/250/250 | Фон поверхностей |
| **Серый средний** | `#F5F5F5` | 245/245/245 | Разделители, бордеры |
| **Серый тёмный** | `#757575` | 117/117/117 | Вторичный текст |
| **Ошибка** | `#D32F2F` | 211/47/47 | Ошибки, предупреждения |
| **Предупреждение** | `#FF9800` | 255/152/0 | Внимание |

### 1.3 Цвета для тёмной темы

| Название | HEX | Назначение |
|----------|-----|------------|
| **Фон тёмный** | `#121212` | Основной фон |
| **Поверхность** | `#1E1E1E` | Карточки, панели |
| **Поверхность высокая** | `#2C2C2C` | Всплывающие элементы |
| **Текст основной** | `#FFFFFF` | Заголовки, текст |
| **Текст вторичный** | `#B0B0B0` | Подписи, hints |

### 1.4 Правила баланса цветов

```
┌─────────────────────────────────────────────────────┐
│                 БАЛАНС ЦВЕТОВ (80/10/10)            │
├─────────────────────────────────────────────────────┤
│  ████████████████████████████████████ 80%          │
│  Белый, серые оттенки, чёрный                       │
│                                                     │
│  ████████ 10%                                      │
│  Активный Красный (#E50040)                         │
│                                                     │
│  ████████ 10%                                      │
│  Умный Зелёный (#4FC7B5)                            │
└─────────────────────────────────────────────────────┘
```

**ВАЖНО:** Если используется Активный Красный, **обязательно** должен присутствовать Умный Зелёный!

---

## 2. ДИЗАЙН-ТОКЕНЫ

### 2.1 Трёхслойная модель токенов

```
┌─────────────────────────────────────────────────────────────┐
│                     ИЕРАРХИЯ ТОКЕНОВ                         │
├─────────────────────────────────────────────────────────────┤
│  Layer 1: PRIMITIVE                                          │
│  ─────────────────                                           │
│  Базовые значения: #E50040, #4FC7B5, 16px, 8px...           │
│                     ↓                                        │
│  Layer 2: SEMANTIC                                            │
│  ─────────────────                                           │
│  Роли: color-bg-primary, color-text-brand, spacing-md...    │
│                     ↓                                        │
│  Layer 3: COMPONENT                                           │
│  ─────────────────                                           │
│  Применение: button-bg-primary, card-border-radius...        │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Primitive Tokens (базовые значения)

#### Цвета

```xml
<!-- Файл: Themes/PrimitiveColors.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Корпоративные цвета РЕХАУ -->
    <Color x:Key="RehauRed">#E50040</Color>
    <Color x:Key="RehauTeal">#4FC7B5</Color>
    <Color x:Key="RehauBlack">#000000</Color>
    <Color x:Key="RehauWhite">#FFFFFF</Color>
    
    <!-- Серая палитра -->
    <Color x:Key="Gray50">#FAFAFA</Color>
    <Color x:Key="Gray100">#F5F5F5</Color>
    <Color x:Key="Gray200">#EEEEEE</Color>
    <Color x:Key="Gray300">#E0E0E0</Color>
    <Color x:Key="Gray400">#BDBDBD</Color>
    <Color x:Key="Gray500">#9E9E9E</Color>
    <Color x:Key="Gray600">#757575</Color>
    <Color x:Key="Gray700">#616161</Color>
    <Color x:Key="Gray800">#424242</Color>
    <Color x:Key="Gray900">#212121</Color>
    
    <!-- Семантические цвета -->
    <Color x:Key="ErrorRed">#D32F2F</Color>
    <Color x:Key="WarningOrange">#FF9800</Color>
    <Color x:Key="SuccessGreen">#4FC7B5</Color>
    <Color x:Key="InfoBlue">#1976D2</Color>
    
    <!-- Тёмная тема -->
    <Color x:Key="DarkBackground">#121212</Color>
    <Color x:Key="DarkSurface">#1E1E1E</Color>
    <Color x:Key="DarkSurfaceHigh">#2C2C2C</Color>

</ResourceDictionary>
```

#### Отступы (Spacing)

```xml
<!-- Файл: Themes/PrimitiveSpacing.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:system="clr-namespace:System;assembly=mscorlib">

    <!-- Базовая единица: 4px -->
    <system:Double x:Key="Spacing.Base">4</system:Double>
    
    <!-- Множители -->
    <system:Double x:Key="Spacing.XXS">4</system:Double>    <!-- 4px -->
    <system:Double x:Key="Spacing.XS">8</system:Double>     <!-- 8px -->
    <system:Double x:Key="Spacing.SM">12</system:Double>     <!-- 12px -->
    <system:Double x:Key="Spacing.MD">16</system:Double>     <!-- 16px -->
    <system:Double x:Key="Spacing.LG">24</system:Double>     <!-- 24px -->
    <system:Double x:Key="Spacing.XL">32</system:Double>     <!-- 32px -->
    <system:Double x:Key="Spacing.XXL">48</system:Double>    <!-- 48px -->
    <system:Double x:Key="Spacing.XXXL">64</system:Double>   <!-- 64px -->

</ResourceDictionary>
```

#### Радиусы скругления

```xml
<!-- Файл: Themes/PrimitiveRadius.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <CornerRadius x:Key="Radius.None">0</CornerRadius>
    <CornerRadius x:Key="Radius.SM">4</CornerRadius>
    <CornerRadius x:Key="Radius.MD">8</CornerRadius>
    <CornerRadius x:Key="Radius.LG">12</CornerRadius>
    <CornerRadius x:Key="Radius.XL">16</CornerRadius>
    <CornerRadius x:Key="Radius.Full">999</CornerRadius> <!-- Полностью круглый -->

</ResourceDictionary>
```

### 2.3 Semantic Tokens (роли)

```xml
<!-- Файл: Themes/SemanticTokens.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ============================================ -->
    <!-- ФОН (Background)                              -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Color.Bg.Primary" 
                     Color="{StaticResource RehauWhite}"/>
    <SolidColorBrush x:Key="Color.Bg.Secondary" 
                     Color="{StaticResource Gray50}"/>
    <SolidColorBrush x:Key="Color.Bg.Surface" 
                     Color="{StaticResource Gray100}"/>
    <SolidColorBrush x:Key="Color.Bg.Brand" 
                     Color="{StaticResource RehauRed}"/>
    <SolidColorBrush x:Key="Color.Bg.Success" 
                     Color="{StaticResource RehauTeal}"/>
    
    <!-- ============================================ -->
    <!-- ТЕКСТ (Text)                                  -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Color.Text.Primary" 
                     Color="{StaticResource Gray900}"/>
    <SolidColorBrush x:Key="Color.Text.Secondary" 
                     Color="{StaticResource Gray600}"/>
    <SolidColorBrush x:Key="Color.Text.Disabled" 
                     Color="{StaticResource Gray400}"/>
    <SolidColorBrush x:Key="Color.Text.Brand" 
                     Color="{StaticResource RehauRed}"/>
    <SolidColorBrush x:Key="Color.Text.OnBrand" 
                     Color="{StaticResource RehauWhite}"/>
    <SolidColorBrush x:Key="Color.Text.Success" 
                     Color="{StaticResource RehauTeal}"/>
    <SolidColorBrush x:Key="Color.Text.Error" 
                     Color="{StaticResource ErrorRed}"/>
    
    <!-- ============================================ -->
    <!-- ГРАНИЦЫ (Border)                              -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Color.Border.Default" 
                     Color="{StaticResource Gray300}"/>
    <SolidColorBrush x:Key="Color.Border.Focus" 
                     Color="{StaticResource RehauRed}"/>
    <SolidColorBrush x:Key="Color.Border.Error" 
                     Color="{StaticResource ErrorRed}"/>
    <SolidColorBrush x:Key="Color.Border.Success" 
                     Color="{StaticResource RehauTeal}"/>
    
    <!-- ============================================ -->
    <!-- ИКОНКИ (Icon)                                 -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Color.Icon.Default" 
                     Color="{StaticResource Gray600}"/>
    <SolidColorBrush x:Key="Color.Icon.Brand" 
                     Color="{StaticResource RehauRed}"/>
    <SolidColorBrush x:Key="Color.Icon.Success" 
                     Color="{StaticResource RehauTeal}"/>

</ResourceDictionary>
```

### 2.4 Component Tokens (применение)

```xml
<!-- Файл: Themes/ComponentTokens.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ============================================ -->
    <!-- КНОПКА: Основная (Primary)                    -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Button.Primary.Bg" 
                     Color="{StaticResource RehauRed}"/>
    <SolidColorBrush x:Key="Button.Primary.BgHover" 
                     Color="#C70036"/> <!-- На 10% темнее -->
    <SolidColorBrush x:Key="Button.Primary.BgPressed" 
                     Color="#A3002D"/> <!-- На 20% темнее -->
    <SolidColorBrush x:Key="Button.Primary.BgDisabled" 
                     Color="{StaticResource Gray300}"/>
    <SolidColorBrush x:Key="Button.Primary.Text" 
                     Color="{StaticResource RehauWhite}"/>
    <SolidColorBrush x:Key="Button.Primary.TextDisabled" 
                     Color="{StaticResource Gray500}"/>
    
    <!-- ============================================ -->
    <!-- КНОПКА: Вторичная (Secondary)                -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Button.Secondary.Bg" 
                     Color="{StaticResource RehauWhite}"/>
    <SolidColorBrush x:Key="Button.Secondary.BgHover" 
                     Color="{StaticResource Gray100}"/>
    <SolidColorBrush x:Key="Button.Secondary.BgPressed" 
                     Color="{StaticResource Gray200}"/>
    <SolidColorBrush x:Key="Button.Secondary.BgDisabled" 
                     Color="{StaticResource Gray100}"/>
    <SolidColorBrush x:Key="Button.Secondary.Border" 
                     Color="{StaticResource Gray300}"/>
    <SolidColorBrush x:Key="Button.Secondary.Text" 
                     Color="{StaticResource Gray900}"/>
    
    <!-- ============================================ -->
    <!-- КНОПКА: Успех (Success)                       -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Button.Success.Bg" 
                     Color="{StaticResource RehauTeal}"/>
    <SolidColorBrush x:Key="Button.Success.BgHover" 
                     Color="#3DA89A"/>
    <SolidColorBrush x:Key="Button.Success.Text" 
                     Color="{StaticResource RehauWhite}"/>
    
    <!-- ============================================ -->
    <!-- ПОЛЕ ВВОДА (TextBox)                          -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Input.Bg" 
                     Color="{StaticResource RehauWhite}"/>
    <SolidColorBrush x:Key="Input.BgDisabled" 
                     Color="{StaticResource Gray100}"/>
    <SolidColorBrush x:Key="Input.Border" 
                     Color="{StaticResource Gray300}"/>
    <SolidColorBrush x:Key="Input.BorderFocus" 
                     Color="{StaticResource RehauRed}"/>
    <SolidColorBrush x:Key="Input.BorderError" 
                     Color="{StaticResource ErrorRed}"/>
    <SolidColorBrush x:Key="Input.Text" 
                     Color="{StaticResource Gray900}"/>
    <SolidColorBrush x:Key="Input.TextPlaceholder" 
                     Color="{StaticResource Gray500}"/>
    
    <!-- ============================================ -->
    <!-- КАРТОЧКА (Card)                               -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Card.Bg" 
                     Color="{StaticResource RehauWhite}"/>
    <SolidColorBrush x:Key="Card.Border" 
                     Color="{StaticResource Gray200}"/>
    <SolidColorBrush x:Key="Card.Shadow" 
                     Color="#19000000"/> <!-- 10% чёрного -->
    
    <!-- ============================================ -->
    <!-- НАВИГАЦИЯ (Navigation)                        -->
    <!-- ============================================ -->
    <SolidColorBrush x:Key="Nav.Bg" 
                     Color="{StaticResource RehauWhite}"/>
    <SolidColorBrush x:Key="Nav.Item.BgActive" 
                     Color="#1AE50040"/> <!-- 10% красного -->
    <SolidColorBrush x:Key="Nav.Item.Text" 
                     Color="{StaticResource Gray600}"/>
    <SolidColorBrush x:Key="Nav.Item.TextActive" 
                     Color="{StaticResource RehauRed}"/>

</ResourceDictionary>
```

---

## 3. ТИПОГРАФИКА

### 3.1 Шрифт Inter

**Основной шрифт:** Inter (SIL Open Font License)

| Начертание | Вес | Назначение |
|------------|-----|------------|
| **Inter Light** | 300 | Подзаголовки, маркеры, длинный текст |
| **Inter Medium** | 500 | Ссылки, текст в инфобоксах |
| **Inter Bold** | 700 | Акцентирование, полужирный текст |
| **Inter Extra Bold** | 800 | Крупные заголовки |
| **Inter Black** | 900 | Экстремальные заголовки |

**Расположение файлов:**
```
D:\IA\ace\Work\Bold\
├── Inter-Light.ttf
├── Inter-Medium.ttf
├── Inter-Bold.ttf
├── Inter-ExtraBold.ttf
└── Inter-Black.ttf
```

### 3.2 Резервный шрифт

**Arial** — используется только если Inter недоступен:

| Начертание | Назначение |
|------------|------------|
| Arial Bold | Крупные заголовки |
| Arial Regular (в цвете) | Подзаголовки, маркеры |
| Arial Regular (чёрный) | Основной текст |
| Arial Bold (в цвете) | Ссылки (URL) |

**ЗАПРЕТ:** Arial запрещён в официальных материалах для печати, на корпоративных сайтах и в мобильных приложениях.

### 3.3 Иерархия заголовков

```xml
<!-- Файл: Themes/Typography.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ============================================ -->
    <!-- ШРИФТОВЫЕ СЕМЕЙСТВА                           -->
    <!-- ============================================ -->
    <FontFamily x:Key="Font.Primary">pack://application:,,,/;/Fonts/#Inter</FontFamily>
    <FontFamily x:Key="Font.Fallback">Arial, sans-serif</FontFamily>
    
    <!-- Комбинированное семейство -->
    <FontFamily x:Key="FontFamily.Main">pack://application:,,,/;/Fonts/#Inter, Arial, sans-serif</FontFamily>

    <!-- ============================================ -->
    <!-- СТИЛИ ТЕКСТА                                  -->
    <!-- ============================================ -->
    
    <!-- Заголовок H1: Крупный заголовок страницы -->
    <Style x:Key="Text.H1" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="ExtraBold"/>
        <Setter Property="FontSize" Value="32"/>
        <Setter Property="LineHeight" Value="40"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Primary}"/>
    </Style>
    
    <!-- Заголовок H2: Заголовок секции -->
    <Style x:Key="Text.H2" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="FontSize" Value="24"/>
        <Setter Property="LineHeight" Value="32"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Primary}"/>
    </Style>
    
    <!-- Заголовок H3: Заголовок подраздела -->
    <Style x:Key="Text.H3" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="FontSize" Value="20"/>
        <Setter Property="LineHeight" Value="28"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Primary}"/>
    </Style>
    
    <!-- Заголовок H4: Малый заголовок -->
    <Style x:Key="Text.H4" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="FontSize" Value="16"/>
        <Setter Property="LineHeight" Value="24"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Primary}"/>
    </Style>
    
    <!-- Основной текст -->
    <Style x:Key="Text.Body" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Light"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="LineHeight" Value="22"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Primary}"/>
    </Style>
    
    <!-- Вторичный текст -->
    <Style x:Key="Text.BodySecondary" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Light"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="LineHeight" Value="20"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Secondary}"/>
    </Style>
    
    <!-- Подпись / Caption -->
    <Style x:Key="Text.Caption" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Light"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="LineHeight" Value="16"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Secondary}"/>
    </Style>
    
    <!-- Ссылка -->
    <Style x:Key="Text.Link" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Brand}"/>
        <Setter Property="TextDecorations" Value="Underline"/>
    </Style>
    
    <!-- Текст на цветном фоне -->
    <Style x:Key="Text.OnBrand" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.OnBrand}"/>
    </Style>

</ResourceDictionary>
```

### 3.4 Правила использования

```
┌────────────────────────────────────────────────────────────┐
│                    ПРАВИЛА ТИПОГРАФИКИ                     │
├────────────────────────────────────────────────────────────┤
│  ✅ Используйте строчные и прописные буквы                  │
│  ✅ Inter Extra Bold — только для крупных заголовков        │
│  ✅ Inter Light — для длинного текста и подзаголовков      │
│  ✅ Inter Medium — для ссылок и текста в инфобоксах        │
│  ✅ Inter Bold — для акцентирования в тексте               │
│                                                            │
│  ❌ ЗАПРЕЩЕНО:                                              │
│  • Использовать CAPS (кроме бренда РЕХАУ)                   │
│  • Использовать другие шрифты                              │
│  • Arial в официальных материалах                          │
│  • Начертания Light для заголовков                        │
│  • Начертания Black для основного текста                   │
└────────────────────────────────────────────────────────────┘
```

---

## 4. КОМПОНОВКА И ОТСТУПЫ

### 4.1 Сетка и множитель

**Базовый множитель:** 0.11 от диагонали экрана (в пикселях)

```
Примеры расчёта:
┌────────────┬────────────┬──────────────┐
│ Диагональ  │ Множитель  │ Базовая ед.  │
├────────────┼────────────┼──────────────┤
│ 1366 px    │ × 0.11     │ ~150 px      │
│ 1920 px    │ × 0.11     │ ~211 px      │
│ 2560 px    │ × 0.11     │ ~282 px      │
└────────────┴────────────┴──────────────┘
```

### 4.2 Система отступов

```
┌─────────────────────────────────────────────────────────────┐
│                    СИСТЕМА ОТСТУПОВ                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  XXS (4px)   ── Внутри элемента, между иконками            │
│                                                             │
│  XS (8px)    ── Внутри компонента, padding кнопки           │
│                                                             │
│  SM (12px)   ── Между элементами в группе                    │
│                                                             │
│  MD (16px)   ── Стандартный отступ между компонентами       │
│                                                             │
│  LG (24px)   ── Между секциями внутри карточки              │
│                                                             │
│  XL (32px)   ── Между карточками, разделы                   │
│                                                             │
│  XXL (48px)  ── Между крупными секциями                     │
│                                                             │
│  XXXL (64px) ── Отступы от краёв окна                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.3 Примеры компоновки

```xml
<!-- Стандартный контейнер с отступами -->
<Grid Margin="{StaticResource Spacing.XXL}">
    <!-- Верхняя секция -->
    <StackPanel Margin="{StaticResource Spacing.LG}">
        <!-- Заголовок -->
        <TextBlock Style="{StaticResource Text.H2}"
                   Margin="{StaticResource Spacing.SM}"/>
        
        <!-- Контент -->
        <ContentControl Margin="{StaticResource Spacing.MD}"/>
    </StackPanel>
    
    <!-- Нижняя секция -->
    <StackPanel Margin="{StaticResource Spacing.XL}">
        <!-- Кнопки -->
        <StackPanel Orientation="Horizontal"
                    Margin="{StaticResource Spacing.SM}">
            <Button Margin="{StaticResource Spacing.XS}"/>
            <Button Margin="{StaticResource Spacing.XS}"/>
        </StackPanel>
    </StackPanel>
</Grid>
```

### 4.4 Правила белого пространства

```
┌────────────────────────────────────────────────────────────┐
│                 БЕЛОЕ ПРОСТРАНСТВО                        │
├────────────────────────────────────────────────────────────┤
│  • Белое пространство обеспечивает визуальный баланс       │
│  • Размер элементов соотносится с площадью                 │
│  • Минимум 16px между группами элементов                   │
│  • Минимум 8px внутри группы                               │
│  • Отступы от краёв окна: 24-48px                          │
│                                                            │
│  ВЫРАВНИВАНИЕ:                                             │
│  • Заголовки — по левому краю                              │
│  • Кнопки — по правому краю (действия)                     │
│  • Формы — по левому краю                                  │
│  • Таблицы — по центру или левому краю                    │
└────────────────────────────────────────────────────────────┘
```

---

## 5. КОМПОНЕНТЫ UI

### 5.1 Кнопки

#### Основные стили кнопок

```xml
<!-- Файл: Themes/Buttons.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ============================================ -->
    <!-- КНОПКА: Primary (Основная)                    -->
    <!-- ============================================ -->
    <Style x:Key="Button.Primary" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource Button.Primary.Bg}"/>
        <Setter Property="Foreground" Value="{StaticResource Button.Primary.Text}"/>
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="MinWidth" Value="100"/>
        <Setter Property="Height" Value="40"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            CornerRadius="{StaticResource Radius.MD}"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="{StaticResource Button.Primary.BgHover}"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
                <Setter Property="Background" Value="{StaticResource Button.Primary.BgPressed}"/>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Background" Value="{StaticResource Button.Primary.BgDisabled}"/>
                <Setter Property="Foreground" Value="{StaticResource Button.Primary.TextDisabled}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ============================================ -->
    <!-- КНОПКА: Secondary (Вторичная)                 -->
    <!-- ============================================ -->
    <Style x:Key="Button.Secondary" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource Button.Secondary.Bg}"/>
        <Setter Property="Foreground" Value="{StaticResource Button.Secondary.Text}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Button.Secondary.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="Height" Value="40"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="{StaticResource Radius.MD}"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="{StaticResource Button.Secondary.BgHover}"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
                <Setter Property="Background" Value="{StaticResource Button.Secondary.BgPressed}"/>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Background" Value="{StaticResource Button.Secondary.BgDisabled}"/>
                <Setter Property="Foreground" Value="{StaticResource Color.Text.Disabled}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ============================================ -->
    <!-- КНОПКА: Success (Успех)                       -->
    <!-- ============================================ -->
    <Style x:Key="Button.Success" TargetType="Button"
           BasedOn="{StaticResource Button.Primary}">
        <Setter Property="Background" Value="{StaticResource Button.Success.Bg}"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="{StaticResource Button.Success.BgHover}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ============================================ -->
    <!-- КНОПКА: Icon (Только иконка)                  -->
    <!-- ============================================ -->
    <Style x:Key="Button.Icon" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Icon.Default}"/>
        <Setter Property="Width" Value="40"/>
        <Setter Property="Height" Value="40"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            CornerRadius="{StaticResource Radius.MD}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="{StaticResource Color.Bg.Surface}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

</ResourceDictionary>
```

#### Использование кнопок

```xml
<!-- Примеры использования -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <!-- Основная кнопка -->
    <Button Style="{StaticResource Button.Primary}"
            Content="Сохранить"/>
    
    <!-- Вторичная кнопка -->
    <Button Style="{StaticResource Button.Secondary}"
            Content="Отмена"/>
    
    <!-- Кнопка успеха -->
    <Button Style="{StaticResource Button.Success}"
            Content="Подтвердить"/>
    
    <!-- Иконка-кнопка -->
    <Button Style="{StaticResource Button.Icon}">
        <PathIcon Data="{StaticResource Icon.Settings}"/>
    </Button>
</StackPanel>
```

### 5.2 Поля ввода (TextBox)

```xml
<!-- Файл: Themes/Inputs.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ============================================ -->
    <!-- TEXTBOX: Стандартное поле ввода               -->
    <!-- ============================================ -->
    <Style x:Key="TextBox.Default" TargetType="TextBox">
        <Setter Property="Background" Value="{StaticResource Input.Bg}"/>
        <Setter Property="Foreground" Value="{StaticResource Input.Text}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Input.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Padding" Value="12,8"/>
        <Setter Property="Height" Value="40"/>
        <Setter Property="VerticalContentAlignment" Value="Center"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="TextBox">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="{StaticResource Radius.MD}"
                            Padding="{TemplateBinding Padding}">
                        <ScrollViewer x:Name="PART_ContentHost"
                                      VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsFocused" Value="True">
                <Setter Property="BorderBrush" Value="{StaticResource Input.BorderFocus}"/>
                <Setter Property="BorderThickness" Value="2"/>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Background" Value="{StaticResource Input.BgDisabled}"/>
                <Setter Property="Foreground" Value="{StaticResource Color.Text.Disabled}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ============================================ -->
    <!-- TEXTBOX: Поле с ошибкой                       -->
    <!-- ============================================ -->
    <Style x:Key="TextBox.Error" TargetType="TextBox"
           BasedOn="{StaticResource TextBox.Default}">
        <Setter Property="BorderBrush" Value="{StaticResource Input.BorderError}"/>
        <Setter Property="BorderThickness" Value="2"/>
    </Style>

    <!-- ============================================ -->
    <!-- TEXTBOX: Placeholder                          -->
    <!-- ============================================ -->
    <!-- Используется через attached property или TextBox с Watermark -->

</ResourceDictionary>
```

### 5.3 Карточки (Cards)

```xml
<!-- Файл: Themes/Cards.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ============================================ -->
    <!-- CARD: Стандартная карточка                    -->
    <!-- ============================================ -->
    <Style x:Key="Card.Default" TargetType="Border">
        <Setter Property="Background" Value="{StaticResource Card.Bg}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Card.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="{StaticResource Radius.LG}"/>
        <Setter Property="Padding" Value="16"/>
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect Color="{StaticResource Card.Shadow}"
                                  BlurRadius="8"
                                  ShadowDepth="2"
                                  Opacity="0.1"/>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ============================================ -->
    <!-- CARD: Карточка с заголовком                   -->
    <!-- ============================================ -->
    <Style x:Key="Card.WithHeader" TargetType="Border"
           BasedOn="{StaticResource Card.Default}">
        <!-- Используется с внутренней структурой -->
    </Style>

    <!-- ============================================ -->
    <!-- CARD: Интерактивная карточка                  -->
    <!-- ============================================ -->
    <Style x:Key="Card.Interactive" TargetType="Border"
           BasedOn="{StaticResource Card.Default}">
        <Setter Property="Cursor" Value="Hand"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="BorderBrush" Value="{StaticResource Color.Border.Focus}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

</ResourceDictionary>
```

#### Пример карточки

```xml
<!-- Стандартная карточка -->
<Border Style="{StaticResource Card.Default}">
    <StackPanel>
        <TextBlock Style="{StaticResource Text.H3}"
                   Text="Заголовок карточки"/>
        
        <TextBlock Style="{StaticResource Text.Body}"
                   Text="Описание содержимого карточки"
                   Margin="0,8,0,0"/>
        
        <StackPanel Orientation="Horizontal"
                    Margin="0,16,0,0">
            <Button Style="{StaticResource Button.Primary}"
                    Content="Действие"/>
        </StackPanel>
    </StackPanel>
</Border>
```

### 5.4 Индикаторы статуса

```xml
<!-- Файл: Themes/StatusIndicators.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ============================================ -->
    <!-- ИНДИКАТОРЫ: Потери давления                   -->
    <!-- ============================================ -->
    
    <!-- Зелёный: Δp ≤ 200 мбар — оптимально -->
    <SolidColorBrush x:Key="Status.Optimal" Color="#4FC7B5"/>
    
    <!-- Жёлтый: 200 < Δp ≤ 320 мбар — допустимо -->
    <SolidColorBrush x:Key="Status.Warning" Color="#FF9800"/>
    
    <!-- Красный: Δp > 320 мбар — превышение -->
    <SolidColorBrush x:Key="Status.Error" Color="#E50040"/>

    <!-- ============================================ -->
    <!-- ИНДИКАТОРЫ: Шаги мастера                      -->
    <!-- ============================================ -->
    
    <!-- Не начат -->
    <SolidColorBrush x:Key="Step.Inactive" Color="{StaticResource Gray400}"/>
    
    <!-- Текущий -->
    <SolidColorBrush x:Key="Step.Active" Color="{StaticResource RehauRed}"/>
    
    <!-- Завершён -->
    <SolidColorBrush x:Key="Step.Completed" Color="{StaticResource RehauTeal}"/>

</ResourceDictionary>
```

### 5.5 Таблицы (DataGrid)

```xml
<!-- Файл: Themes/DataGrid.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Style x:Key="DataGrid.Default" TargetType="DataGrid">
        <Setter Property="Background" Value="{StaticResource Color.Bg.Primary}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Color.Border.Default}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="RowBackground" Value="{StaticResource Color.Bg.Primary}"/>
        <Setter Property="AlternatingRowBackground" Value="{StaticResource Color.Bg.Secondary}"/>
        <Setter Property="GridLinesVisibility" Value="Horizontal"/>
        <Setter Property="HorizontalGridLinesBrush" Value="{StaticResource Color.Border.Default}"/>
        <Setter Property="FontFamily" Value="{StaticResource FontFamily.Main}"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="RowHeight" Value="40"/>
        <Setter Property="HeadersVisibility" Value="Column"/>
    </Style>

    <Style x:Key="DataGrid.Header" TargetType="DataGridColumnHeader">
        <Setter Property="Background" Value="{StaticResource Color.Bg.Secondary}"/>
        <Setter Property="Foreground" Value="{StaticResource Color.Text.Primary}"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="Padding" Value="12,8"/>
        <Setter Property="BorderBrush" Value="{StaticResource Color.Border.Default}"/>
        <Setter Property="BorderThickness" Value="0,0,1,1"/>
    </Style>

    <Style x:Key="DataGrid.Cell" TargetType="DataGridCell">
        <Setter Property="Padding" Value="12,8"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="VerticalAlignment" Value="Center"/>
        <Style.Triggers>
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="Background" Value="#1AE50040"/>
                <Setter Property="BorderBrush" Value="{StaticResource Color.Border.Focus}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

</ResourceDictionary>
```

---

## 6. СОСТОЯНИЯ КОМПОНЕНТОВ

### 6.1 Состояния кнопок

| Состояние | Primary | Secondary | Success |
|-----------|---------|-----------|---------|
| **Default** | `#E50040` | `#FFFFFF` (border `#E0E0E0`) | `#4FC7B5` |
| **Hover** | `#C70036` (-10%) | `#F5F5F5` | `#3DA89A` |
| **Pressed** | `#A3002D` (-20%) | `#EEEEEE` | `#2D8A7E` |
| **Disabled** | `#E0E0E0` | `#F5F5F5` | `#E0E0E0` |
| **Focused** | + контур 2px `#E50040` | + контур 2px `#E50040` | + контур 2px `#4FC7B5` |

### 6.2 Состояния полей ввода

| Состояние | Фон | Граница | Текст |
|-----------|-----|---------|-------|
| **Default** | `#FFFFFF` | `#E0E0E0` (1px) | `#212121` |
| **Focused** | `#FFFFFF` | `#E50040` (2px) | `#212121` |
| **Error** | `#FFFFFF` | `#D32F2F` (2px) | `#212121` |
| **Disabled** | `#F5F5F5` | `#E0E0E0` | `#9E9E9E` |
| **Placeholder** | `#FFFFFF` | `#E0E0E0` | `#9E9E9E` |

### 6.3 Состояния навигации

| Состояние | Фон | Текст | Иконка |
|-----------|-----|-------|--------|
| **Default** | `Transparent` | `#757575` | `#757575` |
| **Hover** | `#1AE50040` | `#212121` | `#212121` |
| **Active** | `#1AE50040` | `#E50040` | `#E50040` |
| **Completed** | `Transparent` | `#4FC7B5` | `#4FC7B5` |

### 6.4 Диаграмма состояний

```
┌─────────────────────────────────────────────────────────────┐
│                 ДИАГРАММА СОСТОЯНИЙ КНОПКИ                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ┌──────────┐                                             │
│   │ DEFAULT  │ ─── hover ──→ ┌──────────┐                  │
│   │ #E50040  │               │  HOVER   │                  │
│   └──────────┘ ←── leave ── │ #C70036  │                  │
│        │                    └──────────┘                  │
│        │                          │                        │
│        │ press                    │ press                  │
│        ↓                          ↓                        │
│   ┌──────────┐               ┌──────────┐                  │
│   │ PRESSED  │               │ PRESSED  │                  │
│   │ #A3002D  │               │ #A3002D  │                  │
│   └──────────┘               └──────────┘                  │
│        │                          │                        │
│        │ release                  │ release                │
│        ↓                          ↓                        │
│   ┌──────────┐               ┌──────────┐                  │
│   │ DEFAULT  │               │  CLICK   │                  │
│   └──────────┘               │  Event   │                  │
│                              └──────────┘                  │
│                                                             │
│   ┌──────────┐                                             │
│   │ DISABLED │ ←── IsEnabled = false                       │
│   │ #E0E0E0  │ ─── IsEnabled = true ──→ DEFAULT             │
│   └──────────┘                                             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 7. ИКОНКИ И ИЗОБРАФЕНИЯ

### 7.1 Логотип РЕХАУ

**Расположение:** `D:\IA\ace\resources\РЕХАУ_logo.svg`

**Правила использования:**
- Минимальный размер: 80×24 px
- Свободное пространство: 20% от высоты логотипа
- Всегда на белом или светлом фоне
- Никогда не растягивать, не вращать, не добавлять эффекты

### 7.2 Стиль изображений

```
┌────────────────────────────────────────────────────────────┐
│                 ТРЕБОВАНИЯ К ИЗОБРАЖЕНИЯМ                  │
├────────────────────────────────────────────────────────────┤
│  ✅ Аутентичные и непринуждённые                            │
│  ✅ Высокий контраст                                        │
│  ✅ Насыщенность слегка завышена                            │
│  ✅ Тёплые оттенки (баланс белого)                         │
│  ✅ Естественный тон кожи людей                             │
│  ✅ Реальные цвета продукции                                │
│                                                            │
│  ❌ Пересвеченные изображения                               │
│  ❌ Слишком тёмные фото                                     │
│  ❌ Искусственные фильтры                                   │
│  ❌ Несоответствующие бренду цвета                          │
└────────────────────────────────────────────────────────────┘
```

### 7.3 Иконки в приложении

Рекомендуется использовать Material Design Icons или создать собственные на основе стиля РЕХАУ.

```xml
<!-- Пример использования PathIcon -->
<PathIcon Data="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"
          Width="24"
          Height="24"
          Foreground="{StaticResource Color.Icon.Default}"/>
```

---

## 8. ЗАПРЕТЫ И ОГРАНИЧЕНИЯ

### 8.1 Цвета

```
┌────────────────────────────────────────────────────────────┐
│                    ЗАПРЕТЫ ПО ЦВЕТАМ                        │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ❌ ЗАПРЕЩЕНО:                                              │
│  • Использовать корпоративные цвета в градиентах           │
│  • Использовать корпоративные цвета с прозрачностью <100%  │
│  • Использовать Активный Красный без Умного Зелёного       │
│  • Менять оттенки корпоративных цветов                      │
│  • Применять эффекты (тени, свечения) к брендовым цветам   │
│                                                            │
│  ✅ РАЗРЕШЕНО:                                              │
│  • Полная прозрачность (opacity: 0)                        │
│  • Белый/чёрный с прозрачностью для теней                   │
│  • Серые оттенки с прозрачностью                            │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 8.2 Типографика

```
┌────────────────────────────────────────────────────────────┐
│                 ЗАПРЕТЫ ПО ТИПОГРАФИКЕ                      │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ❌ ЗАПРЕЩЕНО:                                              │
│  • Использовать CAPS (кроме бренда РЕХАУ)                   │
│  • Использовать другие шрифты кроме Inter                   │
│  • Arial в официальных материалах                           │
│  • Начертания Light для заголовков                          │
│  • Начертания Black для основного текста                    │
│  • Слишком узкий или широкий межбуквенный интервал         │
│  • Изменение пропорций шрифта                               │
│                                                            │
│  ✅ РАЗРЕШЕНО:                                              │
│  • Inter Light/Medium/Bold/ExtraBold/Black                  │
│  • Arial как резервный шрифт                                │
│  • Строчные и прописные буквы                               │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 8.3 Компоновка

```
┌────────────────────────────────────────────────────────────┐
│                ЗАПРЕТЫ ПО КОМПОНОВКЕ                        │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ❌ ЗАПРЕЩЕНО:                                              │
│  • Нарушать систему отступов (произвольные значения)       │
│  • Использовать отрицательные margins для наложения        │
│  • Игнорировать белое пространство                          │
│  • Загромождать интерфейс                                   │
│  • Выходить за минимальное разрешение 1280×720            │
│                                                            │
│  ✅ РАЗРЕШЕНО:                                              │
│  • Стандартные отступы (4, 8, 12, 16, 24, 32, 48, 64)      │
│  • Адаптивная компоновка                                    │
│  • Щедрое белое пространство                                │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## 9. ПРИМЕРЫ XAML

### 9.1 Полная структура ресурсов

```xml
<!-- App.xaml -->
<Application x:Class="REHAU.Snegotayanie.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Layer 1: Primitive Tokens -->
                <ResourceDictionary Source="Themes/PrimitiveColors.xaml"/>
                <ResourceDictionary Source="Themes/PrimitiveSpacing.xaml"/>
                <ResourceDictionary Source="Themes/PrimitiveRadius.xaml"/>
                
                <!-- Layer 2: Semantic Tokens -->
                <ResourceDictionary Source="Themes/SemanticTokens.xaml"/>
                
                <!-- Layer 3: Component Tokens -->
                <ResourceDictionary Source="Themes/ComponentTokens.xaml"/>
                
                <!-- Typography -->
                <ResourceDictionary Source="Themes/Typography.xaml"/>
                
                <!-- Components -->
                <ResourceDictionary Source="Themes/Buttons.xaml"/>
                <ResourceDictionary Source="Themes/Inputs.xaml"/>
                <ResourceDictionary Source="Themes/Cards.xaml"/>
                <ResourceDictionary Source="Themes/DataGrid.xaml"/>
                <ResourceDictionary Source="Themes/StatusIndicators.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 9.2 Пример окна логина

```xml
<Window x:Class="REHAU.Snegotayanie.Views.LoginView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="РЕХАУ — Калькулятор снеготаяния"
        Height="500" Width="400"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        Background="{StaticResource Color.Bg.Primary}">

    <Grid Margin="{StaticResource Spacing.XXL}">
        <StackPanel VerticalAlignment="Center">
            <!-- Логотип -->
            <Image Source="/resources/РЕХАУ_logo.svg"
                   Width="160"
                   Height="48"
                   Margin="0,0,0,{StaticResource Spacing.XXL}"
                   HorizontalAlignment="Center"/>
            
            <!-- Заголовок -->
            <TextBlock Style="{StaticResource Text.H2}"
                       Text="Калькулятор снеготаяния"
                       HorizontalAlignment="Center"
                       Margin="0,0,0,{StaticResource Spacing.LG}"/>
            
            <!-- Форма -->
            <StackPanel Margin="0,0,0,{StaticResource Spacing.XL}">
                <TextBlock Style="{StaticResource Text.Caption"
                           Text="Email"
                           Margin="0,0,0,{StaticResource Spacing.XS}"/>
                <TextBox Style="{StaticResource TextBox.Default}"
                         Text="{Binding Email}"/>
            </StackPanel>
            
            <StackPanel Margin="0,0,0,{StaticResource Spacing.LG}">
                <TextBlock Style="{StaticResource Text.Caption"
                           Text="Пароль"
                           Margin="0,0,0,{StaticResource Spacing.XS}"/>
                <PasswordBox Style="{StaticResource PasswordBox.Default}"
                             Password="{Binding Password}"/>
            </StackPanel>
            
            <!-- Кнопки -->
            <Button Style="{StaticResource Button.Primary}"
                    Content="Войти"
                    Command="{Binding LoginCommand}"
                    Margin="0,0,0,{StaticResource Spacing.MD}"/>
            
            <Button Style="{StaticResource Button.Secondary}"
                    Content="Настройки"
                    Command="{Binding OpenSettingsCommand}"/>
        </StackPanel>
        
        <!-- Версия -->
        <TextBlock Style="{StaticResource Text.Caption"
                   Text="Версия 1.0.0"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Bottom"
                   Margin="0,0,0,{StaticResource Spacing.SM}"
                   Foreground="{StaticResource Color.Text.Secondary}"/>
    </Grid>
</Window>
```

### 9.3 Пример карточки результата

```xml
<!-- Карточка с результатом расчёта -->
<Border Style="{StaticResource Card.Default}"
        Width="200">
    <StackPanel>
        <!-- Значение -->
        <TextBlock Style="{StaticResource Text.H1}"
                   Text="{Binding PowerValue}"
                   Foreground="{StaticResource Color.Text.Primary}"
                   HorizontalAlignment="Center"/>
        
        <!-- Единица измерения -->
        <TextBlock Style="{StaticResource Text.H4"
                   Text="Вт"
                   Foreground="{StaticResource Color.Text.Secondary}"
                   HorizontalAlignment="Center"
                   Margin="0,{StaticResource Spacing.XS},0,0"/>
        
        <!-- Название -->
        <TextBlock Style="{StaticResource Text.Caption"
                   Text="МОЩНОСТЬ"
                   Foreground="{StaticResource Color.Text.Secondary}"
                   HorizontalAlignment="Center"
                   Margin="0,{StaticResource Spacing.SM},0,0"/>
    </StackPanel>
</Border>
```

### 9.4 Пример панели навигации

```xml
<!-- Боковая панель навигации -->
<Border Background="{StaticResource Color.Bg.Primary}"
        Width="280"
        BorderBrush="{StaticResource Color.Border.Default}"
        BorderThickness="0,0,1,0">
    <Grid>
        <!-- Шаги -->
        <ItemsControl ItemsSource="{Binding Steps}"
                      Margin="{StaticResource Spacing.MD}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Border Style="{StaticResource Card.Interactive}"
                            Padding="{StaticResource Spacing.MD}"
                            Margin="0,0,0,{StaticResource Spacing.SM}">
                        <StackPanel Orientation="Horizontal">
                            <!-- Индикатор -->
                            <Ellipse Width="24" Height="24"
                                     Margin="0,0,{StaticResource Spacing.SM},0">
                                <Ellipse.Style>
                                    <Style TargetType="Ellipse">
                                        <Setter Property="Fill" 
                                                Value="{StaticResource Step.Inactive}"/>
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding IsActive}" 
                                                         Value="True">
                                                <Setter Property="Fill" 
                                                        Value="{StaticResource Step.Active}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding IsCompleted}" 
                                                         Value="True">
                                                <Setter Property="Fill" 
                                                        Value="{StaticResource Step.Completed}"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Ellipse.Style>
                            </Ellipse>
                            
                            <!-- Название -->
                            <TextBlock Text="{Binding Title}"
                                       VerticalAlignment="Center">
                                <TextBlock.Style>
                                    <Style TargetType="TextBlock" 
                                           BasedOn="{StaticResource Text.Body}">
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding IsActive}" 
                                                         Value="True">
                                                <Setter Property="Foreground" 
                                                        Value="{StaticResource Color.Text.Brand}"/>
                                                <Setter Property="FontWeight" 
                                                        Value="Medium"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        
        <!-- Кнопки навигации -->
        <StackPanel VerticalAlignment="Bottom"
                    Margin="{StaticResource Spacing.MD}"
                    Orientation="Horizontal"
                    HorizontalAlignment="Center">
            <Button Style="{StaticResource Button.Secondary}"
                    Content="◀ Назад"
                    Command="{Binding BackCommand}"
                    Margin="0,0,{StaticResource Spacing.SM},0"/>
            
            <Button Style="{StaticResource Button.Primary}"
                    Content="Вперёд ▶"
                    Command="{Binding NextCommand}"/>
        </StackPanel>
    </Grid>
</Border>
```

### 9.5 Пример таблицы контуров

```xml
<DataGrid Style="{StaticResource DataGrid.Default}"
          ItemsSource="{Binding Circuits}"
          SelectedItem="{Binding SelectedCircuit}"
          AutoGenerateColumns="False"
          CanUserAddRows="False">
    
    <DataGrid.Columns>
        <DataGridTextColumn Header="№"
                           Binding="{Binding Number}"
                           Width="50"
                           IsReadOnly="True"/>
        
        <DataGridTextColumn Header="Площадь, м²"
                           Binding="{Binding Area, StringFormat=F1}"
                           Width="100"/>
        
        <DataGridTextColumn Header="Подводка, м"
                           Binding="{Binding SupplyLength}"
                           Width="100"/>
        
        <DataGridComboBoxColumn Header="Шаг, мм"
                               SelectedItemBinding="{Binding Step}"
                               Width="100">
            <DataGridComboBoxColumn.ElementStyle>
                <Style TargetType="ComboBox">
                    <Setter Property="ItemsSource" 
                            Value="{Binding Source={StaticResource StepValues}}"/>
                </Style>
            </DataGridComboBoxColumn.ElementStyle>
        </DataGridComboBoxColumn>
        
        <DataGridTemplateColumn Header="Статус"
                               Width="80"
                               IsReadOnly="True">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <Ellipse Width="12" Height="12">
                        <Ellipse.Style>
                            <Style TargetType="Ellipse">
                                <Setter Property="Fill" 
                                        Value="{StaticResource Status.Optimal}"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding PressureStatus}" 
                                                 Value="Warning">
                                        <Setter Property="Fill" 
                                                Value="{StaticResource Status.Warning}"/>
                                    </DataTrigger>
                                    <DataTrigger Binding="{Binding PressureStatus}" 
                                                 Value="Error">
                                        <Setter Property="Fill" 
                                                Value="{StaticResource Status.Error}"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </Ellipse.Style>
                    </Ellipse>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

### 9.6 Загрузка шрифтов

```csharp
// FontLoader.cs
using System;
using System.Windows;
using System.Windows.Media;

namespace REHAU.Snegotayanie.Themes
{
    public static class FontLoader
    {
        public static void LoadInterFonts()
        {
            var fonts = new[]
            {
                "pack://application:,,,/Work/Bold/Inter-Light.ttf#Inter",
                "pack://application:,,,/Work/Bold/Inter-Medium.ttf#Inter",
                "pack://application:,,,/Work/Bold/Inter-Bold.ttf#Inter",
                "pack://application:,,,/Work/Bold/Inter-ExtraBold.ttf#Inter",
                "pack://application:,,,/Work/Bold/Inter-Black.ttf#Inter"
            };
            
            foreach (var fontUri in fonts)
            {
                try
                {
                    var fontFamily = new FontFamily(new Uri(fontUri), "./#Inter");
                    // Шрифт загружен
                }
                catch (Exception ex)
                {
                    // Fallback на Arial
                    System.Diagnostics.Debug.WriteLine($"Font load error: {ex.Message}");
                }
            }
        }
    }
}
```

---

## ПРИЛОЖЕНИЕ А: БЫСТРАЯ СПРАВКА

### Цвета для копирования

```
#E50040  — Активный Красный (РЕХАУ Red)
#4FC7B5  — Умный Зелёный (РЕХАУ Teal)
#000000  — Чёрный
#FFFFFF  — Белый
#FAFAFA  — Серый светлый (фон)
#F5F5F5  — Серый средний (разделители)
#757575  — Серый тёмный (вторичный текст)
#212121  — Почти чёрный (основной текст)
#D32F2F  — Ошибка
#FF9800  — Предупреждение
```

### Отступы для копирования

```
XXS:  4px   — внутри элемента
XS:   8px   — padding кнопки
SM:   12px  — между элементами в группе
MD:   16px  — между компонентами
LG:   24px  — секции внутри карточки
XL:   32px  — между карточками
XXL:  48px  — крупные секции
XXXL: 64px  — отступы от краёв окна
```

### Скругления для копирования

```
SM:  4px   — кнопки, поля ввода
MD:  8px   — карточки, панели
LG:  12px  — модальные окна
XL:  16px  — большие контейнеры
Full: 999px — круглые элементы
```

---

## 10. МИГРАЦИЯ ОТ MATERIAL DESIGN К REHAU

> **Контекст:** Приложение изначально использовало Material Design цвета (синий `#2196F3`, зелёный `#4CAF50`, оранжевый `#FF9800`). Этот раздел описывает переход на корпоративные цвета REHAU.

### 10.1 Таблица замены цветов

| Material Design | HEX | REHAU | HEX | Применение |
|----------------|-----|-------|-----|------------|
| Синий (Primary) | `#2196F3` | **RehauRed** | `#E50040` | Кнопки, акценты |
| Тёмно-синий | `#1976D2` | **RehauRedDark** | `#B3002E` | Заголовки |
| Зелёный (Success) | `#4CAF50` | **RehauTeal** | `#4FC7B5` | Успех, свойства |
| Тёмно-зелёный | `#2E7D32` | **RehauTealDark** | `#2E9B8E` | Заголовки успеха |
| Оранжевый (Warning) | `#FF9800` | **RehauRedLight** | `#FF6B6B` | Предупреждения |
| Светло-синий фон | `#E3F2FD` | **RehauRedLightOpacity** | `#FFE5EC` | Фон результатов |
| Светло-зелёный фон | `#E8F5E9` | **RehauTealLightOpacity** | `#E8F6F4` | Фон свойств |
| Красный (Error) | `#F44336` | **RehauRed** | `#E50040` | Ошибки |

### 10.2 Примеры замены в XAML

```xml
<!-- ❌ ДО: Material Design -->
<Border Background="#E3F2FD" BorderBrush="#2196F3">
    <TextBlock Foreground="#1976D2" Text="Заголовок"/>
</Border>

<!-- ✅ ПОСЛЕ: REHAU -->
<Border Background="{DynamicResource RehauRedLightOpacityBrush}" 
        BorderBrush="{DynamicResource RehauRedBrush}">
    <TextBlock Foreground="{DynamicResource RehauRedDarkBrush}" Text="Заголовок"/>
</Border>
```

### 10.3 Файлы для миграции

| Файл | Статус | Действие |
|------|--------|----------|
| `Views/Hydraulics/CircuitsView.xaml` | ❌ Material Design | Заменить 18 локальных стилей |
| `Views/Hydraulics/CircuitInputView.xaml` | ❌ Material Design | Заменить 6 локальных стилей |
| `Views/Hydraulics/CircuitsResultsView.xaml` | ❌ Material Design | Заменить 11 локальных стилей |
| `Views/Climate/ClimateView.xaml` | ✅ REHAU | Не требует изменений |
| `Views/Construction/ConstructionView.xaml` | ✅ REHAU | Не требует изменений |
| `Views/Thermal/ThermalView.xaml` | ✅ REHAU | Не требует изменений |

### 10.4 Ресурсы миграции

**Техническая документация:**
- 📄 `Work/RehauStyling/design_plan.md` — Детальный план миграции Hydraulics Views
- 📄 `Work/RehauStyling/color_guidelines.md` — Техническая инструкция по работе с цветами XAML

**Целевые ресурсы (App.xaml):**
```xml
<!-- Основные цвета REHAU -->
<Color x:Key="RehauRed">#E50040</Color>
<Color x:Key="RehauRedDark">#B3002E</Color>
<Color x:Key="RehauRedLight">#FF6B6B</Color>
<Color x:Key="RehauTeal">#4FC7B5</Color>
<Color x:Key="RehauTealDark">#2E9B8E</Color>
<Color x:Key="RehauGray">#575756</Color>

<!-- Прозрачные фоны -->
<Color x:Key="RehauRedLightOpacity">#FFE5EC</Color>
<Color x:Key="RehauTealLightOpacity">#E8F6F4</Color>

<!-- Кисти -->
<SolidColorBrush x:Key="RehauRedBrush" Color="{StaticResource RehauRed}"/>
<SolidColorBrush x:Key="RehauTealBrush" Color="{StaticResource RehauTeal}"/>
<SolidColorBrush x:Key="RehauRedLightOpacityBrush" Color="{StaticResource RehauRedLightOpacity}"/>
<SolidColorBrush x:Key="RehauTealLightOpacityBrush" Color="{StaticResource RehauTealLightOpacity}"/>
```

### 10.5 Критические правила

⚠️ **ВАЖНО: Избегайте ошибки с 8-символьным HEX**

```xml
<!-- ❌ ОШИБКА: 8 символов (#AARRGGBB) не работает в Brush-свойствах -->
<Border Background="#FFF5F5F5"/>  <!-- ОШИБКА! -->

<!-- ✅ ПРАВИЛЬНО: 6 символов (#RRGGBB) -->
<Border Background="#F5F5F5"/>     <!-- Работает -->

<!-- ✅ ЕЩЁ ЛУЧШЕ: Через ресурс -->
<Border Background="{DynamicResource RehauBackgroundGrayBrush}"/>
```

### 10.6 Чек-лист миграции

- [ ] Добавить новые цвета в `App.xaml`
- [ ] Добавить новые стили в `Dictionary.xaml`
- [ ] Заменить inline-цвета на DynamicResource
- [ ] Заменить локальные стили на REHAU стили
- [ ] Проверить отсутствие 8-символьных HEX
- [ ] Проверить запуск приложения
- [ ] Проверить отображение в дизайнере VS

---

## 11. СВЯЗАННАЯ ДОКУМЕНТАЦИЯ

| Документ | Назначение | Расположение |
|----------|------------|--------------|
| **Руководство по дизайну** (этот файл) | Общие принципы, цвета, типографика, компоненты | `docs/design_guidelines.md` |
| **План миграции** | Конкретный план изменений Hydraulics Views | `Work/RehauStyling/design_plan.md` |
| **Инструкция по цветам** | Технические детали работы с цветами XAML | `Work/RehauStyling/color_guidelines.md` |

---

**Дата создания:** 2026-01-21  
**Дата обновления:** 2026-03-24  
**Автор:** AI Assistant  
**Версия:** 1.1