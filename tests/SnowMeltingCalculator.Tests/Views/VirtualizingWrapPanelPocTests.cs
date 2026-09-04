// ================================================================================
// Фаза 2 редизайна — PoC VirtualizingWrapPanel (план Ф2.4).
// ================================================================================
//
// PoC-критерии приёмки встроены как тесты: чипы 2:1 перестраиваются
// 6 → 3×2 при сужении, resize без артефактов (конечные размеры, без
// исключений, детерминированный рефлоу). Пакет — dev-only в тестах;
// PackageReference в src добавляется фазой первого внедрения (Ф3/Ф6),
// решение зафиксировано в плане Ф2 и ADR-006 (docs/architecture/README.md).
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NUnit.Framework;
using WpfToolkit.Controls;

namespace SnowMeltingCalculator.Tests.Views;

/// <summary>
/// STA-PoC панели карточных сеток: VirtualizingWrapPanel 2.5.4 (MIT).
/// Чипы моделируются Border'ами 180×90 (2:1) с полем 5px (зазор 10px),
/// как в эталоне renders/02 (tokens.css .chips, aspect-ratio 2/1).
/// </summary>
[Apartment(System.Threading.ApartmentState.STA)]
[TestFixture]
public class VirtualizingWrapPanelPocTests
{
    private const double ChipWidth = 180;
    private const double ChipHeight = 90;
    private const double ChipMargin = 5;

    [Test]
    public void WideWidth_SixChips_ArrangeInSingleRow()
    {
        var rows = MeasureChipRows(1150);

        Assert.That(rows, Is.EqualTo(new[] { 6 }),
            "При ширине 1150 все 6 чипов должны лечь в один ряд.");
    }

    [Test]
    public void NarrowWidth_SixChips_ReflowToThreeByTwo()
    {
        var rows = MeasureChipRows(600);

        Assert.That(rows, Is.EqualTo(new[] { 3, 3 }),
            "При ширине 600 чипы должны перестроиться в 3×2.");
    }

    [Test]
    public void ResizeCycle_ReflowsDeterministically_WithoutArtifacts()
    {
        // Сужение и обратное расширение: количество рядов детерминировано,
        // артефактов (NaN/Infinity в аранжировке, исключений измерения) нет.
        var widths = new[] { 1150d, 1000, 800, 600, 900, 1150 };
        var expectedRowCounts = new[] { 1, 2, 2, 2, 2, 1 };

        var host = CreateChipStrip(out var chips);
        var actualRowCounts = new List<int>();

        foreach (var width in widths)
        {
            host.Measure(new Size(width, 400));
            host.Arrange(new Rect(0, 0, width, 400));
            PumpDispatcherIdle();

            actualRowCounts.Add(CountRows(chips));
            AssertLayoutIsFinite(chips);
        }

        Assert.That(actualRowCounts, Is.EqualTo(expectedRowCounts),
            "Ряды после каждого шага resize-цикла должны совпадать с расчётным рефлоу.");
    }

    #region Helpers

    /// <summary>
    /// Прокачка очереди диспатчера: VirtualizingWrapPanel достраивает
    /// реализацию контейнеров отложенно (на idle-кадре), поэтому после
    /// Measure/Arrange нужен pump до подсчёта чипов.
    /// </summary>
    private static void PumpDispatcherIdle()
    {
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
            () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
            () => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    /// <summary>Карта «Y-ряд → число чипов» после измерения на заданной ширине.</summary>
    private static IReadOnlyList<int> MeasureChipRows(double hostWidth)
    {
        var host = CreateChipStrip(out var chips);
        host.Measure(new Size(hostWidth, 400));
        host.Arrange(new Rect(0, 0, hostWidth, 400));
        PumpDispatcherIdle();

        AssertLayoutIsFinite(chips);
        return CountRowsByY(chips).Values.ToList();
    }

    private static Grid CreateChipStrip(out List<Border> chips)
    {
        var panelFactory = new FrameworkElementFactory(typeof(VirtualizingWrapPanel));

        var chipFactory = new FrameworkElementFactory(typeof(Border));
        chipFactory.SetValue(FrameworkElement.WidthProperty, ChipWidth);
        chipFactory.SetValue(FrameworkElement.HeightProperty, ChipHeight);
        chipFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(ChipMargin));
        chipFactory.SetValue(Panel.BackgroundProperty, Brushes.Transparent);

        var itemsControl = new ItemsControl
        {
            ItemsPanel = new ItemsPanelTemplate(panelFactory),
            ItemTemplate = new DataTemplate { VisualTree = chipFactory },
            ItemsSource = Enumerable.Range(0, 6).Select(_ => new object()).ToList()
        };
        // Требование VirtualizingWrapPanel (VirtualizingPanelBase.VerifyItemsControl):
        // виртуализация работает только при пиксельном/постраничном скролле.
        ScrollViewer.SetCanContentScroll(itemsControl, true);
        var host = new Grid { Children = { itemsControl } };

        // Генерация контейнеров происходит при первом Measure; отложенная
        // реализация — после прокачки диспатчера (см. PumpDispatcherIdle).
        host.Measure(new Size(1150, 400));
        host.Arrange(new Rect(0, 0, 1150, 400));
        PumpDispatcherIdle();

        chips = FindDescendants<Border>(host)
            .Where(border => Math.Abs(border.Width - ChipWidth) < 0.1)
            .ToList();
        Assert.That(chips, Has.Count.EqualTo(6), "Контейнеры чипов не сгенерированы панелью.");
        return host;
    }

    private static SortedDictionary<double, int> CountRowsByY(List<Border> chips)
    {
        var rows = new SortedDictionary<double, int>();
        foreach (var chip in chips)
        {
            var root = FindVisualRoot(chip);
            var origin = chip.TransformToAncestor(root).Transform(new Point(0, 0));
            var rowKey = FindExistingRowKey(rows, origin.Y);
            rows[rowKey] = rows.GetValueOrDefault(rowKey) + 1;
        }

        return rows;
    }

    private static double FindExistingRowKey(SortedDictionary<double, int> rows, double y)
    {
        foreach (var existing in rows.Keys)
        {
            if (Math.Abs(existing - y) < 1.0)
            {
                return existing;
            }
        }

        return y;
    }

    private static int CountRows(List<Border> chips) => CountRowsByY(chips).Count;

    private static void AssertLayoutIsFinite(List<Border> chips)
    {
        foreach (var chip in chips)
        {
            var root = FindVisualRoot(chip);
            var bounds = chip.TransformToAncestor(root).TransformBounds(new Rect(chip.RenderSize));
            Assert.That(
                double.IsFinite(bounds.X) && double.IsFinite(bounds.Y)
                && double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height),
                Is.True, $"Аранжировка чипа содержит нечисловые значения: {bounds}.");
        }
    }

    private static Visual FindVisualRoot(Visual element)
    {
        var current = element;
        while (VisualTreeHelper.GetParent(current) is Visual parent)
        {
            current = parent;
        }

        return current;
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : class
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var deeper in FindDescendants<T>(child))
            {
                yield return deeper;
            }
        }
    }

    #endregion
}
