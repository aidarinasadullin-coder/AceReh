using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.Services.Reports.Calculation;

namespace SnowMeltingCalculator.Tests.Services.Reports.Calculation
{
    /// <summary>
    /// Тесты PDF-рендера детального расчётного отчёта (мини-фаза PDF-PZ):
    /// разделы 1:1 с Markdown, отсутствие путей кодовой базы (решение
    /// владельца 2026-09-07, спека §7.2), успешный рендер с кириллицей.
    /// Контент-пины проверяются по обходу объектной модели MigraDoc —
    /// это текст, который попадёт в PDF.
    /// </summary>
    [TestFixture]
    public class CalculationReportPdfRendererTests
    {
        private static readonly byte[] PdfMagicHeader = { 0x25, 0x50, 0x44, 0x46 };
        private static readonly DateTime FixedReportDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Render_FullReport_TextContainsAllSectionHeadings()
        {
            var text = RenderToText(BuildFullData(CalculationReportMode.Operating));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("Детальный расчётный отчёт"));
                Assert.That(text, Does.Contain("Методика"));
                Assert.That(text, Does.Contain("Краткая сводка"));
                Assert.That(text, Does.Contain("Исходные данные проекта"));
                Assert.That(text, Does.Contain("Климатические данные"));
                Assert.That(text, Does.Contain("Конструкция"));
                Assert.That(text, Does.Contain("Теплотехнический расчёт"));
                Assert.That(text, Does.Contain("Пошаговый расчёт"));
                Assert.That(text, Does.Contain("Константы расчёта (из кода программы)"));
                Assert.That(text, Does.Contain("Гидравлический расчёт"));
                Assert.That(text, Does.Contain("Референсный контур"));
                Assert.That(text, Does.Contain("Оборудование и KPI"));
                Assert.That(text, Does.Contain("Предупреждения и ограничения"));
                Assert.That(text, Does.Contain("Приложение: источники значений"));
                Assert.That(text, Does.Contain("Приложение: формулы и обозначения"));
            });
        }

        [Test]
        public void Render_FullReport_TextDoesNotContainCodeBasePaths()
        {
            // Решение владельца 2026-09-07 (спека §7.2): пути к коду
            // (SourceDetail, констант-таблица, SourcePath) в PDF не выводятся.
            var text = RenderToText(BuildFullData(CalculationReportMode.Operating));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Not.Contain("ThermalConstants"));
                Assert.That(text, Does.Not.Contain("ProjectData."));
                Assert.That(text, Does.Not.Contain("ThermalCalculationResult."));
                Assert.That(text, Does.Not.Contain("ThermalCalculator."));
                Assert.That(text, Does.Not.Contain("FlowRegimeCalculator"));
                Assert.That(text, Does.Not.Contain("SourceDetail"));
                Assert.That(text, Does.Not.Contain(".cs"));
            });
        }

        [Test]
        public void Render_FullReport_TracesByEngineeringCategories()
        {
            var text = RenderToText(BuildFullData(CalculationReportMode.Operating));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("введено пользователем"));
                Assert.That(text, Does.Contain("база программы"));
                Assert.That(text, Does.Contain("рассчитано программой"));
            });
        }

        [Test]
        public void Render_FullReport_RendersPdfFile_WithCyrillicContent()
        {
            var document = new CalculationReportPdfRenderer().Render(BuildFullData(CalculationReportMode.Operating));
            var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"report-pz-{Guid.NewGuid():N}.pdf");

            try
            {
                // Та же настройка, что в CalculationReportPdfExportService:
                // ремонт Flate-потоков против ошибки Acrobat
                // «Недостаточно данных для изображения» (PDFsharp 6.x пишет
                // zlib без трейлера Adler-32).
                var renderer = new PdfDocumentRenderer(true) { Document = document };
                renderer.RenderDocument();
                PdfFlateStreamRepair.RepairImageStreams(renderer.PdfDocument);
                renderer.PdfDocument.Save(filePath);

                var bytes = File.ReadAllBytes(filePath);
                Assert.That(bytes.Length, Is.GreaterThan(0));
                Assert.That(bytes[..4], Is.EqualTo(PdfMagicHeader), "PDF должен начинаться с %PDF-магии");
                var validation = ValidateImageStreamsStrictly(bytes);
                Assert.That(validation.Total, Is.GreaterThan(0), "в документе есть image-потоки");
                Assert.That(
                    validation.Failures,
                    Is.Empty,
                    "все image-потоки — валидные zlib-потоки (трейлер Adler-32 на месте; пин ремонта Acrobat)");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Test]
        public void Render_TwoRuns_ProduceIdenticalDocumentText()
        {
            var data = BuildFullData(CalculationReportMode.Operating);
            var first = RenderToText(data);
            var second = RenderToText(data);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Render_BodyFormulas_NumberedContinuously_AppendixUnnumbered()
        {
            // В11: формулы тела нумеруются сквозно «(N)» справа; приложение
            // формул — справочник без номеров.
            var data = BuildFullData(CalculationReportMode.Operating);
            var expected = CountBodyFormulas(data);
            Assert.That(expected, Is.GreaterThan(0), "в фикстуре есть формулы тела");

            var document = new CalculationReportPdfRenderer().Render(data);
            var paragraphs = CollectParagraphInfos(document);

            var numbers = paragraphs
                .Where(p => !p.HasImage && IsFormulaNumber(p.Text) && p.Alignment == ParagraphAlignment.Right)
                .Select(p => int.Parse(p.Text.Trim('(', ')'), System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            Assert.That(
                numbers,
                Is.EqualTo(Enumerable.Range(1, expected).ToList()),
                "сквозная нумерация формул тела — 1..N без пропусков и повторов");
        }

        [Test]
        public void Render_BodyFormulas_Centered_NumberColumnRight()
        {
            // В11: формула — по центру широкой ячейки; номер — справа.
            var data = BuildFullData(CalculationReportMode.Operating);
            var expected = CountBodyFormulas(data);

            var document = new CalculationReportPdfRenderer().Render(data);
            var paragraphs = CollectParagraphInfos(document);

            Assert.Multiple(() =>
            {
                Assert.That(
                    paragraphs.Count(p => p.HasImage && p.Alignment == ParagraphAlignment.Center),
                    Is.EqualTo(expected),
                    "каждая формула тела центрируется");
                Assert.That(
                    paragraphs.Count(p => p.HasImage && p.Alignment == ParagraphAlignment.Left),
                    Is.GreaterThan(0),
                    "приложение формул остаётся невыровненным справочником (без центрирования)");
            });
        }

        [Test]
        public void Render_NullData_ThrowsArgumentNullException()
        {
            Assert.That(() => new CalculationReportPdfRenderer().Render(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Render_OperatingMode_FullSteps_WithoutModeComparison()
        {
            // T2-09-PDF (Operating): полный пошаговый расчёт, сравнения режимов нет.
            var text = RenderToText(BuildFullData(CalculationReportMode.Operating));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("Режим отчёта"));
                Assert.That(text, Does.Contain("Рабочий режим"));
                Assert.That(text, Does.Contain("Пошаговый расчёт"));
                Assert.That(text, Does.Not.Contain("Краткая тепловая справка"));
                Assert.That(text, Does.Not.Contain("Сравнение режимов"));
            });
        }

        [Test]
        public void Render_DesignColdMode_ShortSummary_Comparison_AndDesignHydraulics()
        {
            // T2-09-PDF (DesignCold, В3): краткая справка + сравнение
            // «рабочий vs пуск», гидравлика — DesignResult (DpGesamt 150 000).
            var text = RenderToText(BuildFullData(CalculationReportMode.DesignCold));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("Расчётный/холодный режим"));
                Assert.That(text, Does.Contain("Краткая тепловая справка"));
                Assert.That(text, Does.Not.Contain("Пошаговый расчёт"));
                Assert.That(text, Does.Contain("Сравнение режимов: рабочий vs холодный пуск"));
                Assert.That(text, Does.Contain("худшего контура"));
                // Гидравлика пуска: DpGesamt контура DesignResult = 150 000
                // (NBSP-тысячи, каноническая культура; Па → 0 знаков, В9).
                Assert.That(text, Does.Contain("150\u00A0000"));
                // Кратность роста потерь 150000/45000.
                Assert.That(text, Does.Contain("×3,3"));
            });
        }

        [Test]
        public void InterResolver_ResolvesEmbeddedBrandFont()
        {
            // Брендбук (спека §7.2): Inter подаётся из встроенных TTF,
            // Inter в системе не установлен.
            var resolver = new CalculationReportInterFontResolver();

            Assert.Multiple(() =>
            {
                Assert.That(CalculationReportInterFontResolver.CanLoadFonts(), Is.True,
                    "встроенный Inter-Regular должен загружаться из g.resources");

                var regular = resolver.ResolveTypeface(CalculationReportInterFontResolver.FamilyName, isBold: false, isItalic: false);
                Assert.That(regular.FaceName, Is.EqualTo("Inter-Regular"));

                var bold = resolver.ResolveTypeface(CalculationReportInterFontResolver.FamilyName, isBold: true, isItalic: false);
                Assert.That(bold.FaceName, Is.EqualTo("Inter-Bold"));

                var bytes = resolver.GetFont("Inter-Regular");
                Assert.That(bytes, Is.Not.Null);
                Assert.That(bytes!.Length, Is.GreaterThan(1000));
                // TrueType-магия 0x00010000.
                Assert.That(bytes[0], Is.EqualTo(0x00));
                Assert.That(bytes[1], Is.EqualTo(0x01));
                Assert.That(bytes[2], Is.EqualTo(0x00));
                Assert.That(bytes[3], Is.EqualTo(0x00));

                // Делегация чужих семейств (краткий PDF — Arial) проверяется
                // смоук-рендером: платформенный резолвер PDFsharp отвечает
                // только вызову из-под установленного глобального резолвера.
            });
        }

        [Test]
        public async Task ExportReportAsync_WritesPdfFileWithMagicHeader()
        {
            // Мини-фаза PDF-PZ (PDF-4): сквозной экспорт builder → рендер →
            // запись файла; результат начинается с %PDF-магии.
            var service = new CalculationReportPdfExportService(
                new CalculationReportDataBuilder(),
                new CalculationReportPdfRenderer());
            var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"report-pz-export-{Guid.NewGuid():N}.pdf");

            try
            {
                var exported = await service.ExportReportAsync(
                    filePath,
                    MakeProject(),
                    CalculationReportMode.Operating,
                    new ThermalReportDetail { Source = ThermalReportDetailSource.Snapshot },
                    FixedReportDate);

                Assert.That(exported, Is.True);
                Assert.That(File.Exists(filePath), Is.True);
                var bytes = await File.ReadAllBytesAsync(filePath);
                Assert.That(bytes.Length, Is.GreaterThan(0));
                Assert.That(bytes[..4], Is.EqualTo(PdfMagicHeader));
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Test]
        public void Render_FullReport_UsesCanonicalRussianNumberFormat()
        {
            // В6: числа таблиц — каноническая культура приложения
            // (десятичная запятая, NBSP-тысячи); Re = 10600 → 0 знаков (В9).
            var text = RenderToText(BuildFullData(CalculationReportMode.Operating));

            Assert.That(text, Does.Contain("10\u00A0600"));
        }

        [Test]
        public void LaTeXConverter_ConvertsPlainNotation()
        {
            // Запрос владельца 2026-09-07: формулы PDF — LaTeX-вёрстка.
            // Греческие буквы остаются юникодом (CSharpMath рендерит фолбэком),
            // кириллические индексы не заворачиваются в \text.
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "α = 2,26·(tП − tH)^0,33 + 2,6·vH");

            Assert.That(latex, Is.EqualTo("α = 2,26 \\cdot (t_{П} - t_{H})^{0,33} + 2,6 \\cdot v_{H}"));
        }

        [Test]
        public void LaTeXConverter_KeepsSubscriptNames()
        {
            // qTotal не разбивается на q_T + otal: за заглавной идут строчные.
            // ṁ → \dot{m} (комбинируемая диакритика в math-фолбэке отсутствует);
            // греческая Δ остаётся юникодом (рендерится фолбэком CSharpMath).
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "ṁ = qTotal/(c_p/3,6)/ΔT");

            Assert.That(latex, Is.EqualTo("\\dot{m} = qTotal/(c_{p}/3,6)/ΔT"));
        }

        [Test]
        public void LaTeXConverter_RejectsProseFormulas()
        {
            // Пояснительная проза остаётся текстом (пин рендера).
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "Re < 2300: λ = 64/Re; Re > 4000: Колбрук–Уайт (итерации, старт по Блазиусу); между — линейная интерполяция");

            Assert.That(latex, Is.Null);
        }

        [Test]
        public void LaTeXRenderer_ProducesPngBytes()
        {
            var image = CalculationReportLaTeXFormulaRenderer.TryRenderPng(
                "Q_таяния = (h/1000/3600)·ρ_снега·[c_льда·(0 − tH) + L_плавл + c_воды·tП] (h в мм/ч)");

            Assert.Multiple(() =>
            {
                Assert.That(image, Is.Not.Null, "формула с кириллическими индексами должна рендериться");
                Assert.That(image!.Bytes.Length, Is.GreaterThan(100));
                // PNG-магия 0x89 0x50 0x4E 0x47.
                Assert.That(image.Bytes[..4], Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
                Assert.That(image.WidthPx, Is.GreaterThan(50));
                Assert.That(image.HeightPx, Is.GreaterThan(10));
            });
        }

        [Test]
        public void LaTeXRenderer_ProseFormula_ReturnsNull()
        {
            Assert.That(CalculationReportLaTeXFormulaRenderer.TryRenderPng(
                "Re < 2300: λ = 64/Re; Re > 4000: Колбрук–Уайт (итерации, старт по Блазиусу); между — линейная интерполяция"),
                Is.Null);
        }

        [Test]
        public void LaTeXConverter_ConvertsRadicalToSqrt()
        {
            // В10: √( … ) → \sqrt{ … } — винкула рисуется CSharpMath, а не
            // голый глиф; подкоренный индекс zu_dr защищён до конверсии.
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "Kv = (V̇/1000)/√(zu_dr/10⁵/ρ)");

            Assert.That(latex, Is.EqualTo("Kv = (\\dot{V}/1000)/\\sqrt{zu_{dr}/10^{5}/ρ}"));
        }

        [Test]
        public void LaTeXConverter_ConvertsRadical_KeepsRadicandSubscripts()
        {
            // План P2: конверсия корня — после защиты подстрочных групп,
            // d_нар под корнем не разъезжается.
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "m = 0,6·√((1/RFb + 1/RD)/(λE·d_нар))");

            Assert.That(latex, Is.EqualTo("m = 0,6 \\cdot \\sqrt{(1/RFb + 1/RD)/(λE \\cdot d_{нар})}"));
        }

        [Test]
        public void LaTeXConverter_ConvertsAsciiSqrt()
        {
            // В10: sqrt( … ) из инженерной нотации приложения формул → \sqrt{ … };
            // вложенные скобки балансируются, sqrt внутри слова не задевается.
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "0.6 * sqrt((1/RFb + 1/RD) / (λE * dE))");

            Assert.That(latex, Is.EqualTo("0,6 \\cdot \\sqrt{(1/RFb + 1/RD) / (λE \\cdot d_{E})}"));
        }

        [Test]
        public void LaTeXConverter_NestedSquareRoots()
        {
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "√(1 + √(a + b))");

            Assert.That(latex, Is.EqualTo("\\sqrt{1 + \\sqrt{a + b}}"));
        }

        [Test]
        public void LaTeXConverter_UnbalancedParen_RadicalLeftUnconverted()
        {
            // Несбалансированная скобка — конверсия не выполняется, рендер
            // откатится к текстовому фолбэку.
            var latex = CalculationReportLaTeXFormulaRenderer.TryConvertToLaTeX(
                "√(a + b");

            Assert.That(latex, Does.StartWith("√(a + b"));
        }

        [Test]
        public void LaTeXRenderer_RendersSqrtFormula_NotClippedByCanvas()
        {
            // В10/AC-2: радикал с винкулой рендерится в PNG; канва 3× не режет
            // контент (периметр обрезанного изображения прозрачен).
            var image = CalculationReportLaTeXFormulaRenderer.TryRenderPng(
                "m = 0,6·√((1/RFb + 1/RD)/(λE·d_нар))");

            Assert.Multiple(() =>
            {
                Assert.That(image, Is.Not.Null, "формула с корнем должна рендериться в PNG");
                Assert.That(image!.Bytes[..4], Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
                Assert.That(image.HeightPx, Is.GreaterThan(40), "радикал должен быть сопоставим с кеглем рендера");
                AssertBorderTransparent(image);
            });
        }

        [Test]
        public void LaTeXRenderer_FractionUnderRadical_FitsCanvas()
        {
            // В10: \sqrt{дробь} — самая высокая конструкция ПЗ; запас канвы
            // над базовой линией (≥ 5em при кегле 30px) не срезает её.
            var image = CalculationReportLaTeXFormulaRenderer.TryRenderPng(
                "√(\\frac{Q_таяния·3,6}{c_воды·ρ_снега·h})");

            Assert.Multiple(() =>
            {
                Assert.That(image, Is.Not.Null, "дробь под радикалом должна рендериться");
                Assert.That(image!.HeightPx, Is.GreaterThan(80), "дробь под корнем — высокая конструкция");
                AssertBorderTransparent(image);
            });
        }

        [Test]
        public void LaTeXRenderer_PngDeterministic()
        {
            const string formula = "m = 0,6·√((1/RFb + 1/RD)/(λE·d_нар))";

            var first = CalculationReportLaTeXFormulaRenderer.TryRenderPng(formula);
            var second = CalculationReportLaTeXFormulaRenderer.TryRenderPng(formula);

            Assert.That(second!.Bytes, Is.EqualTo(first!.Bytes), "повторный рендер той же формулы — байт в байт");
        }

        /// <summary>Периметр PNG прозрачен — обрезка по контенту не упёрлась
        /// в канву, конструкция не срезана (иначе на границе остались
        /// полупрозрачные пиксели антиалиасинга).</summary>
        private static void AssertBorderTransparent(CalculationReportLaTeXFormulaRenderer.FormulaImage image)
        {
            using var bitmap = SkiaSharp.SKBitmap.Decode(image.Bytes);
            Assert.That(bitmap, Is.Not.Null, "PNG декодируется");
            Assert.Multiple(() =>
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    Assert.That(bitmap.GetPixel(x, 0).Alpha, Is.EqualTo(0), $"верхняя кромка, x={x}");
                    Assert.That(bitmap.GetPixel(x, bitmap.Height - 1).Alpha, Is.EqualTo(0), $"нижняя кромка, x={x}");
                }

                for (var y = 0; y < bitmap.Height; y++)
                {
                    Assert.That(bitmap.GetPixel(0, y).Alpha, Is.EqualTo(0), $"левая кромка, y={y}");
                    Assert.That(bitmap.GetPixel(bitmap.Width - 1, y).Alpha, Is.EqualTo(0), $"правая кромка, y={y}");
                }
            });
        }

        #region Подготовка данных

        private static CalculationReportData BuildFullData(CalculationReportMode mode)
        {
            var detail = new ThermalReportDetail
            {
                Source = ThermalReportDetailSource.Snapshot,
                Alpha = 14.13,
                MeltingHeat = 47.8,
                RadiationHeat = 320.0,
                ConvectionHeat = 282.7,
                ExcessTemperature = 60.2,
                RFb = 0.1283,
                RD = 5.6374,
                ParameterM = 9.08,
                EfficiencyEtaR = 0.793,
                MassFlowRate = 22.1,
                VolumeFlowRate = 21.62
            };

            var builder = new CalculationReportDataBuilder();
            return builder.Build(MakeProject(), mode, FixedReportDate, detail);
        }

        private static ProjectData MakeProject()
        {
            return new ProjectData
            {
                ProjectNumber = "9-100000",
                ProjectObject = "Екатеринбург",
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Екатеринбург",
                    AirTemperature = -15.0,
                    WindSpeed = 3.1,
                    Humidity = 72.0,
                    SnowfallIntensity = 0.5
                },
                ConstructionData = new ConstructionProjectData
                {
                    GroundwaterLevel = 2.0,
                    R1 = 0.0575,
                    R2 = 5.6374,
                    LambdaE = 1.74,
                    Layers = new List<LayerProjectData>
                    {
                        new() { Position = LayerPosition.AbovePipe, MaterialName = "Бетон", Thickness = 100.0, CalculatedLambda = 1.74, CalculatedR = 0.0575 },
                        new() { Position = LayerPosition.BelowPipe, MaterialName = "Пенополистирол ЭППС", Thickness = 80.0, CalculatedLambda = 0.035, CalculatedR = 2.2857 }
                    }
                },
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    SupplyTemperature = 53.0,
                    GroundTemperature = 10.0,
                    PipeSpacing = 200,
                    SelectedPipe = new PipeTypeProjectData { Name = "RAUTHERM S 20x2.0", OuterDiameter = 20.0, InnerDiameter = 16.0, WallThickness = 2.0 },
                    Result = new ThermalResultProjectData
                    {
                        PowerUp = 330.5,
                        PowerDown = 4.9,
                        PowerTotal = 335.4,
                        SupplyTemperature = 53.0,
                        ReturnTemperature = 37.4,
                        MeanTemperature = 45.2,
                        DeltaT = 15.6,
                        IsValid = true
                    }
                },
                HydraulicsData = new HydraulicsProjectData
                {
                    GlycolType = GlycolType.Ethylene,
                    GlycolConcentration = 40.0,
                    Collectors = new List<CollectorProjectData>
                    {
                        new()
                        {
                            CollectorNumber = 1,
                            CollectorType = "IV 1¼\"",
                            Summary = new CollectorSummaryProjectData { PressureLoss_Operating_Pa = 45000, PressureLoss_Cold_Pa = 150000 },
                            Circuits = new List<CircuitProjectData>
                            {
                                new()
                                {
                                    CircuitNumber = 1, CircuitLength = 100.0, SupplyLength = 10.0, PipeSpacingCm = 20,
                                    OperatingResult = new CircuitResultProjectData { DpGesamt = 45000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 10600, FrictionFactor = 0.031, PressureLossPerMeter = 204, DpRohr = 40000, DpVerteiler = 3000, DpVent = 2000, Throttling = 0, ValveTurns = 8, Density = 1.053, KinematicViscosity = 0.66, FlowRegime = "Турбулентный" },
                                    DesignResult = new CircuitResultProjectData { DpGesamt = 150000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 450, FrictionFactor = 0.1422, PressureLossPerMeter = 680, DpRohr = 140000, DpVerteiler = 5000, DpVent = 5000, Throttling = 0, ValveTurns = 8, Density = 1.053, KinematicViscosity = 15.64, FlowRegime = "Ламинарный" }
                                }
                            }
                        }
                    }
                }
            };
        }

        #endregion

        #region Обход текста MigraDoc

        private static string RenderToText(CalculationReportData data)
        {
            var document = new CalculationReportPdfRenderer().Render(data);
            var sb = new StringBuilder();
            CollectText(document, sb);
            return sb.ToString();
        }

        /// <summary>Число формул тела документа: шаги, чей FormulaText
        /// рендерится LaTeX-картинкой (те же условия нумерации, что в
        /// RenderStep). Приложение формул не учитывается.</summary>
        private static int CountBodyFormulas(CalculationReportData data)
        {
            var steps = new List<CalculationStep>();
            steps.AddRange(data.ConstructionSection.Steps);
            steps.AddRange(data.ThermalSection.Steps);
            steps.AddRange(data.HydraulicsSection.ReferenceCircuit?.Steps ?? new List<CalculationStep>());
            steps.AddRange(data.HydraulicsSection.ReferenceCircuit?.BalancingSteps ?? new List<CalculationStep>());
            return steps.Count(s => CalculationReportLaTeXFormulaRenderer.TryRenderPng(s.FormulaText) != null);
        }

        /// <summary>Строгая zlib-валидация image-потоков сохранённого PDF:
        /// для каждого «/Subtype/Image … >> stream» — raw-deflate после
        /// 2-байтового zlib-заголовка декодируется полностью, а последние
        /// 4 байта потока равны Adler-32 распакованных данных. Пин ремонта
        /// Acrobat-дефекта PDFsharp 6.x (zlib без трейлера).</summary>
        private static (int Total, List<string> Failures) ValidateImageStreamsStrictly(byte[] pdf)
        {
            // Latin1: байт↔символ 1:1, индексы строк == индексы байтов.
            var text = System.Text.Encoding.Latin1.GetString(pdf);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                text,
                "/Subtype/Image.*?>>\\s*stream\\r?\\n",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            var total = 0;
            var failures = new List<string>();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var lengthMatch = System.Text.RegularExpressions.Regex.Match(
                    match.Value, "/Length (\\d+)");
                if (!lengthMatch.Success)
                {
                    continue;
                }

                total++;
                var dataStart = match.Index + match.Length;
                var length = int.Parse(lengthMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                var data = pdf[dataStart..(dataStart + length)];

                byte[] raw;
                try
                {
                    using var input = new MemoryStream(data, 2, data.Length - 2);
                    using var deflate = new System.IO.Compression.DeflateStream(
                        input, System.IO.Compression.CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    deflate.CopyTo(output);
                    raw = output.ToArray();
                }
                catch (System.IO.InvalidDataException)
                {
                    failures.Add($"obj@{match.Index}: deflate не декодируется");
                    continue;
                }

                var adler = ComputeAdler32(raw);
                var trailerOk = data.Length >= 4
                    && data[^4] == (byte)(adler >> 24)
                    && data[^3] == (byte)(adler >> 16)
                    && data[^2] == (byte)(adler >> 8)
                    && data[^1] == (byte)adler;
                if (!trailerOk)
                {
                    failures.Add($"obj@{match.Index}: трейлер Adler-32 отсутствует или не совпадает");
                }
            }

            return (total, failures);
        }

        /// <summary>Adler-32 (RFC 1950) — эталон для проверки трейлера.</summary>
        private static uint ComputeAdler32(byte[] data)
        {
            const uint modulus = 65521;
            uint a = 1, b = 0;
            foreach (var value in data)
            {
                a = (a + value) % modulus;
                b = (b + a) % modulus;
            }

            return (b << 16) | a;
        }

        private static bool IsFormulaNumber(string text)
        {
            return text.Length > 2 && text[0] == '(' && text[^1] == ')'
                && int.TryParse(text[1..^1], System.Globalization.CultureInfo.InvariantCulture, out _);
        }

        private sealed record ParagraphInfo(string Text, ParagraphAlignment Alignment, bool HasImage);

        private static List<ParagraphInfo> CollectParagraphInfos(Document document)
        {
            var result = new List<ParagraphInfo>();

            void Walk(DocumentObject? obj)
            {
                switch (obj)
                {
                    case Document doc:
                        foreach (Section section in doc.Sections)
                        {
                            Walk(section);
                        }
                        break;
                    case Section section:
                        foreach (DocumentObject element in section.Elements)
                        {
                            Walk(element);
                        }
                        break;
                    case Table table:
                        foreach (Row row in table.Rows)
                        {
                            foreach (Cell cell in row.Cells)
                            {
                                foreach (DocumentObject element in cell.Elements)
                                {
                                    Walk(element);
                                }
                            }
                        }
                        break;
                    case Paragraph paragraph:
                        {
                            var sb = new StringBuilder();
                            var hasImage = false;
                            foreach (DocumentObject element in paragraph.Elements)
                            {
                                if (element is Text text)
                                {
                                    sb.Append(text.Content);
                                }

                                if (element is MigraDoc.DocumentObjectModel.Shapes.Image)
                                {
                                    hasImage = true;
                                }
                            }
                            result.Add(new ParagraphInfo(sb.ToString(), paragraph.Format.Alignment, hasImage));
                            break;
                        }
                }
            }

            Walk(document);
            return result;
        }

        private static void CollectText(DocumentObject? obj, StringBuilder sb)
        {
            switch (obj)
            {
                case Document document:
                    foreach (Section section in document.Sections)
                    {
                        CollectText(section, sb);
                    }

                    break;
                case Section section:
                    foreach (DocumentObject element in section.Elements)
                    {
                        CollectText(element, sb);
                    }

                    // HeadersFooters не enumerable — обход по известным слотам.
                    CollectText(section.Headers.Primary, sb);
                    CollectText(section.Headers.FirstPage, sb);
                    CollectText(section.Headers.EvenPage, sb);
                    CollectText(section.Footers.Primary, sb);
                    CollectText(section.Footers.FirstPage, sb);
                    CollectText(section.Footers.EvenPage, sb);

                    break;
                case HeaderFooter headerFooter:
                    foreach (DocumentObject element in headerFooter.Elements)
                    {
                        CollectText(element, sb);
                    }

                    break;
                case Table table:
                    foreach (Row row in table.Rows)
                    {
                        foreach (Cell cell in row.Cells)
                        {
                            foreach (DocumentObject element in cell.Elements)
                            {
                                CollectText(element, sb);
                            }

                            sb.Append(" | ");
                        }

                        sb.AppendLine();
                    }

                    break;
                case Paragraph paragraph:
                    foreach (DocumentObject element in paragraph.Elements)
                    {
                        if (element is Text text)
                        {
                            sb.Append(text.Content);
                        }
                    }

                    sb.AppendLine();
                    break;
            }
        }

        #endregion
    }
}
