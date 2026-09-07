using System.IO;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Сервис экспорта результатов расчёта в PDF с корпоративным дизайном REHAU.
    /// Рендер — MigraDoc/PDFsharp 6.2 (MIT); блоки отчёта соответствуют
    /// прежнему QuestPDF-рендеру, заголовки гидравлической таблицы — по
    /// глоссарию Ф3 (docs/design/glossary-hydraulics.md), числа — по канону
    /// ru-RU (десятичная запятая, пробел-тысячи).
    /// </summary>
    public class PdfExportService : IPdfExportService
    {
        // REHAU Corporate Colors
        private const string RehauRed = "#E50040";
        private const string RehauTeal = "#4FC7B5";
        private const string RehauBlack = "#1D1D1B";
        private const string RehauWhite = "#FFFFFF";
        private const string Gray50 = "#FAFAFA";
        private const string Gray100 = "#F5F5F5";
        private const string Gray300 = "#E0E0E0";
        private const string Gray600 = "#757575";
        private const string Gray900 = "#212121";

        private const string FontName = "Arial";

        /// <summary>Ширина контентной области, pt: A4 landscape 842 − поля 60.</summary>
        private const double ContentWidthPoints = 842 - 60;

        // Ширины колонок таблицы контуров приложения, pt (как в прежнем отчёте)
        private static readonly double[] CircuitColumnWidths =
        {
            25, 55, 55, 55, 65, 55, 65, 55, 60, 75, 55
        };

        // Путь к логотипу REHAU
        private readonly string _logoPath;

        static PdfExportService()
        {
            // Ревью Ф8 (P0): в Core-сборке PDFsharp 6.2 нет рабочего резолвера
            // шрифтов «из коробки» — флаг читает системные шрифты Windows
            // (Arial содержит кириллицу, эмбеддинг подмножеством штатный).
            // Устанавливается один раз за процесс, до первого рендера.
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }

        public PdfExportService()
        {
            // Путь к логотипу относительно рабочей директории
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _logoPath = Path.Combine(baseDir, "Resources", "Images", "rehau_logo.png");
        }

        public Task<bool> ExportResultsToPdfAsync(
            string filePath,
            ResultsPdfData data,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                try
                {
                    var document = CreateDocument(data);
                    // true = эмбеддинг шрифтов (обязателен для кириллицы)
                    var renderer = new PdfDocumentRenderer(true)
                    {
                        Document = document
                    };
                    // FlateEncodeMode.BestSpeed: Flate-потоки режима по умолчанию
                    // Adobe Acrobat отвергает («Недостаточно данных для изображения»);
                    // BestSpeed — санкционированный обход мейнтейнера PDFsharp
                    // (empira/PDFsharp#258). Содержимое документа не меняется.
                    renderer.PdfDocument = new PdfSharp.Pdf.PdfDocument();
                    renderer.PdfDocument.Options.FlateEncodeMode = PdfSharp.Pdf.PdfFlateEncodeMode.BestSpeed;
                    renderer.RenderDocument();
                    renderer.PdfDocument.Save(filePath);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при экспорте PDF: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Форматирование числа по канону ru-RU (десятичная запятая,
        /// пробел-тысячи — решение владельца 2026-09-04, журнал п.9), без
        /// зависимости от CurrentCulture машины прогона. Пин — тестом
        /// NumberFormat_UsesCanonicalRussianCulture в PdfExportServiceTests.
        /// </summary>
        internal static string Num(double value, string format)
        {
            return value.ToString(format, AppCulture.Culture);
        }

        private Document CreateDocument(ResultsPdfData data)
        {
            var document = new Document();

            var pageSetup = document.DefaultPageSetup.Clone();
            // A4 landscape задаётся явными размерами: связка PageFormat+
            // Orientation в PDFsharp 6.2 после Clone() не даёт альбомной
            // страницы (подводный камень из ревью Ф8, проверено рендером).
            pageSetup.PageWidth = Unit.FromPoint(842);
            pageSetup.PageHeight = Unit.FromPoint(595);
            pageSetup.LeftMargin = Unit.FromPoint(30);
            pageSetup.RightMargin = Unit.FromPoint(30);
            // Полосы шапки/подвала живут в полях страницы: 30pt отступ + полоса
            pageSetup.TopMargin = Unit.FromPoint(92);
            pageSetup.HeaderDistance = Unit.FromPoint(30);
            pageSetup.BottomMargin = Unit.FromPoint(62);
            pageSetup.FooterDistance = Unit.FromPoint(30);

            var section = document.AddSection();
            section.PageSetup = pageSetup;

            BuildPageHeader(section.Headers.Primary, data);
            BuildFooter(section.Footers.Primary);
            BuildDashboardPage(section, data);
            BuildHydraulicAppendixPage(section, data);

            return document;
        }

        private void BuildDashboardPage(Section section, ResultsPdfData data)
        {
            AddSpacer(section, 2);
            BuildProjectInfoSection(section, data);
            AddSpacer(section, 6);
            BuildKpiSection(section, data);
            AddSpacer(section, 6);

            // Гидравлическая сводка (2/3) + Оборудование (1/3), зазор 10pt
            var summaryWidth = ContentWidthPoints * 2 / 3 - 5;
            var equipmentWidth = ContentWidthPoints / 3 - 5;
            var row2 = section.AddTable();
            row2.AddColumn(Unit.FromPoint(summaryWidth));
            row2.AddColumn(Unit.FromPoint(10));
            row2.AddColumn(Unit.FromPoint(equipmentWidth));
            var row2Row = row2.AddRow();
            BuildHydraulicSummarySection(row2Row.Cells[0], summaryWidth, data);
            BuildEquipmentSection(row2Row.Cells[2], equipmentWidth, data);
            AddSpacer(section, 6);

            // Конструкция (3/5) + Исходные данные (2/5)
            var constructionWidth = ContentWidthPoints * 3 / 5 - 6;
            var climateWidth = ContentWidthPoints * 2 / 5 - 4;
            var row3 = section.AddTable();
            row3.AddColumn(Unit.FromPoint(constructionWidth));
            row3.AddColumn(Unit.FromPoint(10));
            row3.AddColumn(Unit.FromPoint(climateWidth));
            var row3Row = row3.AddRow();
            BuildConstructionSection(row3Row.Cells[0], constructionWidth, data);
            BuildClimateSection(row3Row.Cells[2], climateWidth, data);
        }

        private void BuildHydraulicAppendixPage(Section section, ResultsPdfData data)
        {
            section.AddPageBreak();
            AddSpacer(section, 2);
            // Оба заголовка стр. 2 — как в прежнем отчёте (ревью Ф8: поблочное
            // соответствие; выправление дубли — отдельное решение владельца).
            AddSectionText(section, "Приложение: подробный гидравлический расчёт", 12, bold: true, colorHex: RehauBlack);
            AddSectionText(section, "ГИДРАВЛИЧЕСКИЙ РАСЧЁТ", 12, bold: true, colorHex: RehauBlack);

            foreach (var collector in data.Collectors)
            {
                AddSpacer(section, 5);
                BuildCollectorTable(section, collector);
            }
        }

        #region Page Header

        private void BuildPageHeader(HeaderFooter header, ResultsPdfData data)
        {
            var table = header.AddTable();
            table.AddColumn(Unit.FromPoint(200));
            table.AddColumn(Unit.FromPoint(ContentWidthPoints - 260));
            table.AddColumn(Unit.FromPoint(60));
            var row = table.AddRow();
            row.Height = Unit.FromPoint(60);
            row.VerticalAlignment = VerticalAlignment.Center;

            // Логотип слева (если не деплоен — текстовый lockup, как раньше)
            var logoCell = row.Cells[0];
            logoCell.Shading.Color = Color.Parse(RehauRed);
            logoCell.Borders.DistanceFromLeft = Unit.FromPoint(15);
            if (File.Exists(_logoPath))
            {
                var image = logoCell.AddParagraph().AddImage(_logoPath);
                image.LockAspectRatio = true;
                // Аналог FitHeight старого рендера: логотип на всю высоту полосы 60pt
                image.Height = Unit.FromPoint(60);
            }
            else
            {
                AddCellText(logoCell, "РЕХАУ", 18, bold: true, colorHex: RehauWhite);
            }

            // Центр - название
            var titleCell = row.Cells[1];
            titleCell.Shading.Color = Color.Parse(RehauRed);
            AddCellText(titleCell, "Калькулятор снеготаяния", 14, bold: true, colorHex: RehauWhite,
                alignment: ParagraphAlignment.Center);

            // Справа - номер страницы
            var pageCell = row.Cells[2];
            pageCell.Shading.Color = Color.Parse(RehauRed);
            pageCell.Borders.DistanceFromRight = Unit.FromPoint(15);
            var pageParagraph = pageCell.AddParagraph();
            pageParagraph.Format.Alignment = ParagraphAlignment.Right;
            pageParagraph.Format.Font.Name = FontName;
            pageParagraph.Format.Font.Size = 10;
            pageParagraph.Format.Font.Color = Color.Parse(RehauWhite);
            pageParagraph.AddPageField();
            pageParagraph.AddText(" / ");
            pageParagraph.AddNumPagesField();
        }

        #endregion

        #region Project Info Section

        private void BuildProjectInfoSection(Section section, ResultsPdfData data)
        {
            var table = section.AddTable();
            table.AddColumn(Unit.FromPoint(ContentWidthPoints));
            var cell = table.AddRow().Cells[0];
            cell.Shading.Color = Color.Parse(Gray50);
            SetPadding(cell, 8);
            AddCellText(cell,
                $"Проект: {data.ProjectNumber} | Объект: {data.ProjectObject} | Дата: {data.ReportDate.ToString("dd.MM.yyyy", AppCulture.Culture)}",
                9, colorHex: Gray900);
        }

        #endregion

        #region KPI Section

        private void BuildKpiSection(Section section, ResultsPdfData data)
        {
            var card = AddCardCell(section, ContentWidthPoints, innerPadding: 6);

            AddCellText(card, "КПИ ПОКАЗАТЕЛИ", 10, bold: true, colorHex: RehauBlack);

            var kpiTable = card.Elements.AddTable();
            var cardInnerWidth = ContentWidthPoints - 14; // минус паддинги и рамка карточки
            var kpiWidth = (cardInnerWidth - 7 * 5) / 8;
            for (var i = 0; i < 8; i++)
            {
                kpiTable.AddColumn(Unit.FromPoint(kpiWidth));
                if (i < 7)
                {
                    kpiTable.AddColumn(Unit.FromPoint(5));
                }
            }

            var kpiRow = kpiTable.AddRow();
            var kpis = new (string Value, string Label)[]
            {
                (Num(data.TotalThermalPower_kW, "N2"), "Мощность, кВт"),
                (Num(data.SystemVolume_L, "N1"), "Объём, л"),
                (Num(data.PumpFlowRate_m3h, "N2"), "Расход насоса, м³/ч"),
                (Num(data.PumpHead_kPa, "N1"), "Напор, кПа"),
                (Num(data.SupplyTemperature, "N1"), "Подача, °C"),
                (Num(data.ReturnTemperature, "N1"), "Обратки, °C"),
                (Num(data.OperatingTemperature, "N1"), "Рабочая, °C"),
                (Num(data.ExpansionTankVolume_L, "N1"), "Бак, л")
            };

            for (var i = 0; i < kpis.Length; i++)
            {
                BuildKpiCard(kpiRow.Cells[i * 2], kpiWidth, kpis[i].Value, kpis[i].Label);
            }
        }

        private void BuildKpiCard(Cell container, double width, string value, string label)
        {
            var table = container.Elements.AddTable();
            table.AddColumn(Unit.FromPoint(width));
            var cell = table.AddRow().Cells[0];
            cell.Borders.Width = Unit.FromPoint(1);
            cell.Borders.Color = Color.Parse(Gray300);
            cell.Shading.Color = Color.Parse(RehauWhite);
            SetPadding(cell, 5);
            cell.VerticalAlignment = VerticalAlignment.Center;

            AddCellText(cell, value, 12, bold: true, colorHex: RehauTeal, alignment: ParagraphAlignment.Center);
            AddCellText(cell, label, 6, colorHex: Gray600, alignment: ParagraphAlignment.Center);
        }

        #endregion

        #region Climate Section

        private void BuildClimateSection(Cell container, double width, ResultsPdfData data)
        {
            StyleCard(container);

            AddCellText(container, "Исходные данные", 10, bold: true, colorHex: RehauBlack);
            AddSubsection(container, width, "Климат", new[]
            {
                ("Город", data.City),
                ("Расчётная t", $"{Num(data.DesignTemperature, "N1")} °C"),
                ("Ветер", $"{Num(data.WindSpeed, "N1")} м/с"),
                ("Снегопад", $"{Num(data.SnowfallIntensity, "N1")} мм/ч"),
                ("Зона", data.ClimateZone.ToString()),
                ("Холодный период", $"{data.ColdPeriodDays} дн.")
            });
            AddSubsection(container, width, "Труба и раскладка", new[]
            {
                ("Тип трубы", data.PipeType),
                ("Шаг укладки", $"{data.PipeSpacing} мм"),
                ("Темп. грунта", $"{Num(data.GroundTemperature, "N1")} °C")
            });
            AddSubsection(container, width, "Режим работы", new[]
            {
                ("Режим", data.OperatingMode.ToString()),
                ("Поверхность", $"{data.SurfaceTemperature} °C")
            });
            AddSubsection(container, width, "Теплоноситель", new[]
            {
                ("Тип", data.GlycolTypeDisplayName),
                ("Концентрация", $"{Num(data.GlycolConcentration, "N0")} %")
            });
        }

        private void AddSubsection(Cell container, double width, string title,
            IReadOnlyList<(string Label, string Value)> rows)
        {
            AddSpacer(container, 3);
            var outer = container.Elements.AddTable();
            outer.AddColumn(Unit.FromPoint(width - 14));
            var cell = outer.AddRow().Cells[0];
            cell.Shading.Color = Color.Parse(Gray50);
            SetPadding(cell, 4);
            AddCellText(cell, title, 8, bold: true, colorHex: RehauBlack);

            var inner = cell.Elements.AddTable();
            inner.AddColumn(Unit.FromPoint((width - 14) * 0.55 - 8));
            inner.AddColumn(Unit.FromPoint((width - 14) * 0.45 - 8));
            foreach (var row in rows)
            {
                var innerRow = inner.AddRow();
                AddCellText(innerRow.Cells[0], $"{row.Label}:", 7, bold: true, colorHex: Gray900);
                AddCellText(innerRow.Cells[1], row.Value, 7, colorHex: Gray900, alignment: ParagraphAlignment.Right);
            }
        }

        #endregion

        #region Construction Section

        private void BuildConstructionSection(Cell container, double width, ResultsPdfData data)
        {
            StyleCard(container);

            AddCellText(container, "Конструкция", 10, bold: true, colorHex: RehauBlack);
            AddSpacer(container, 3);

            var imageWidth = 180.0;
            var innerWidth = width - 14; // минус паддинги и рамка карточки
            var columns = container.Elements.AddTable();
            columns.AddColumn(Unit.FromPoint(innerWidth - imageWidth));
            columns.AddColumn(Unit.FromPoint(imageWidth));
            var row = columns.AddRow();

            var leftCell = row.Cells[0];
            var leftWidth = innerWidth - imageWidth;

            var values = leftCell.Elements.AddTable();
            values.AddColumn(Unit.FromPoint(leftWidth * 0.45));
            values.AddColumn(Unit.FromPoint(leftWidth * 0.55));
            AddKeyValue(values, "R1 над трубой:", $"{Num(data.R1, "N4")} м²·К/Вт", RehauBlack);
            AddKeyValue(values, "R2 под трубой:", $"{Num(data.R2, "N4")} м²·К/Вт", RehauBlack);
            AddKeyValue(values, "LambdaE:", $"{Num(data.LambdaE, "N3")} Вт/м·К", RehauBlack);
            AddKeyValue(values, "q↑ вверх:", $"{Num(data.PowerUp, "N1")} Вт/м²", RehauRed);
            AddKeyValue(values, "q↓ вниз:", $"{Num(data.PowerDown, "N1")} Вт/м²", RehauTeal);
            AddKeyValue(values, "q суммарная:", $"{Num(data.TotalPowerDensity, "N1")} Вт/м²", RehauRed);

            if (data.Layers.Count > 0)
            {
                AddSpacer(leftCell, 3);
                AddCellText(leftCell, "Слои конструкции", 8, bold: true, colorHex: RehauBlack);

                var layers = leftCell.Elements.AddTable();
                layers.AddColumn(Unit.FromPoint(leftWidth * 3 / 6 - 2));
                layers.AddColumn(Unit.FromPoint(leftWidth * 1 / 6));
                layers.AddColumn(Unit.FromPoint(leftWidth * 1 / 6));
                layers.AddColumn(Unit.FromPoint(leftWidth * 1 / 6));
                var header = layers.AddRow();
                header.HeadingFormat = true;
                StyleHeaderCell(header.Cells[0], "Материал");
                StyleHeaderCell(header.Cells[1], "Толщ.");
                StyleHeaderCell(header.Cells[2], "λ");
                StyleHeaderCell(header.Cells[3], "R");

                foreach (var layer in data.Layers)
                {
                    var layerRow = layers.AddRow();
                    AddCellText(layerRow.Cells[0], layer.MaterialName, 6, colorHex: Gray900);
                    AddCellText(layerRow.Cells[1], Num(layer.Thickness, "N0"), 6, colorHex: Gray900,
                        alignment: ParagraphAlignment.Right);
                    AddCellText(layerRow.Cells[2], Num(layer.Lambda, "N3"), 6, colorHex: Gray900,
                        alignment: ParagraphAlignment.Right);
                    AddCellText(layerRow.Cells[3], Num(layer.R, "N4"), 6, colorHex: Gray900,
                        alignment: ParagraphAlignment.Right);
                    foreach (Cell cell in layerRow.Cells)
                    {
                        StyleDataCell(cell, RehauWhite);
                    }
                }
            }

            if (data.ConstructionImageBytes != null)
            {
                var imageCell = row.Cells[1];
                imageCell.VerticalAlignment = VerticalAlignment.Center;
                // Ревью Ф8 (P0): ImageSource.FromBinary в официальном PDFsharp 6.x
                // не существует — штатный путь MigraDoc: fileless base64-протокол.
                var image = imageCell.AddParagraph()
                    .AddImage("base64:" + Convert.ToBase64String(data.ConstructionImageBytes));
                image.LockAspectRatio = true;
                image.Width = Unit.FromPoint(imageWidth - 8);
            }
        }

        #endregion

        #region Hydraulics Section

        private void BuildHydraulicSummarySection(Cell container, double width, ResultsPdfData data)
        {
            StyleCard(container);

            AddCellText(container, "Гидравлический расчёт", 10, bold: true, colorHex: RehauBlack);

            foreach (var collector in data.Collectors)
            {
                BuildCollectorSummaryCard(container, width, collector);
            }
        }

        private void BuildCollectorSummaryCard(Cell container, double width, CollectorPdfData collector)
        {
            var summary = collector.Summary;
            AddSpacer(container, 4);
            var table = container.Elements.AddTable();
            table.AddColumn(Unit.FromPoint(width - 14));
            var cell = table.AddRow().Cells[0];
            cell.Borders.Width = Unit.FromPoint(1);
            cell.Borders.Color = Color.Parse(Gray300);
            cell.Shading.Color = Color.Parse(Gray50);
            SetPadding(cell, 5);

            AddCellText(cell, $"Коллектор {collector.Number}: {collector.Type}", 8, bold: true, colorHex: RehauBlack);

            var stats = cell.Elements.AddTable();
            var statsWidth = width - 14 - 10 - 2;
            stats.AddColumn(Unit.FromPoint(statsWidth / 2));
            stats.AddColumn(Unit.FromPoint(statsWidth / 2));
            var statsRow = stats.AddRow();
            var left = statsRow.Cells[0];
            var right = statsRow.Cells[1];

            AddCellText(left, $"Контуров: {summary.CircuitCount}", 7, colorHex: Gray900);
            AddCellText(left, $"Длина: {Num(summary.TotalPipeLength, "N1")} м", 7, colorHex: Gray900);
            AddCellText(left, $"Мощность: {Num(summary.TotalPower / 1000, "N2")} кВт", 7, colorHex: Gray900);
            AddCellText(left, $"Kv: {Num(summary.Kv, "N2")}", 7, colorHex: Gray900);

            AddCellText(right, $"Расход: {Num(summary.TotalFlowRate / 1000, "N2")} м³/ч", 7, colorHex: Gray900);
            AddCellText(right, $"ΔP рабочая: {Num(summary.PressureLoss_Operating_kPa, "N2")} кПа", 7, colorHex: Gray900);
            AddCellText(right, $"ΔP холодная: {Num(summary.PressureLoss_Cold_kPa, "N2")} кПа", 7, colorHex: Gray900);
            AddCellText(right, $"Тип: {summary.CollectorType}", 7, colorHex: Gray900);
        }

        private void BuildCollectorTable(Section section, CollectorPdfData collector)
        {
            // Внешняя рамка карточки коллектора
            var card = AddCardCell(section, ContentWidthPoints, innerPadding: 0);

            // Заголовок коллектора
            var cardInnerWidth = ContentWidthPoints - 2;
            var band = card.Elements.AddTable();
            band.AddColumn(Unit.FromPoint(cardInnerWidth));
            var bandCell = band.AddRow().Cells[0];
            bandCell.Shading.Color = Color.Parse(RehauRed);
            SetPadding(bandCell, 5);
            AddCellText(bandCell, $"КОЛЛЕКТОР {collector.Number} ({collector.Type})", 10, bold: true, colorHex: RehauWhite);

            // Таблица контуров - 11 столбцов (заголовки по глоссарию Ф3)
            var table = card.Elements.AddTable();
            foreach (var width in CircuitColumnWidths)
            {
                table.AddColumn(Unit.FromPoint(width));
            }
            var header = table.AddRow();
            header.HeadingFormat = true;
            var headers = new[]
            {
                "№", "Длина, м", "Расход, л/ч", "Скорость, м/с",
                "R, Па/м", "Δp трубы, кПа", "Δp коллект., кПа", "Δp клап., кПа",
                "Δp всего, кПа", "Дросс., кПа", "Обороты"
            };
            for (var i = 0; i < headers.Length; i++)
            {
                StyleHeaderCell(header.Cells[i], headers[i], fontSize: 8);
            }

            // Данные контуров (зебра белый/Gray50)
            var rowIndex = 0;
            foreach (var circuit in collector.Circuits)
            {
                var background = rowIndex % 2 == 0 ? RehauWhite : Gray50;
                var circuitRow = table.AddRow();
                var cells = circuitRow.Cells;
                AddCellText(cells[0], circuit.CircuitNumber.ToString(), 8, colorHex: Gray900);
                AddCellText(cells[1], Num(circuit.Length, "N1"), 8, colorHex: Gray900);
                AddCellText(cells[2], Num(circuit.FlowRate, "N1"), 8, colorHex: Gray900);
                AddCellText(cells[3], Num(circuit.Velocity, "N2"), 8, colorHex: Gray900);
                AddCellText(cells[4], Num(circuit.PressureLossPerMeter, "N1"), 8, colorHex: Gray900);
                AddCellText(cells[5], Num(circuit.DpRohr, "N2"), 8, colorHex: Gray900);
                AddCellText(cells[6], Num(circuit.DpVerteiler, "N2"), 8, colorHex: Gray900);
                AddCellText(cells[7], Num(circuit.DpVent, "N2"), 8, colorHex: Gray900);
                AddCellText(cells[8], Num(circuit.DpGesamt, "N2"), 8, colorHex: Gray900);
                AddCellText(cells[9], Num(circuit.ZuDrosseln, "N2"), 8, colorHex: Gray900);
                AddCellText(cells[10], Num(circuit.ValveTurns, "N1"), 8, colorHex: Gray900);
                foreach (Cell cell in cells)
                {
                    StyleDataCell(cell, background);
                }

                rowIndex++;
            }

            // Итоги по коллектору
            AddSpacer(card, 1);
            var totals = card.Elements.AddTable();
            totals.AddColumn(Unit.FromPoint(cardInnerWidth));
            var totalsCell = totals.AddRow().Cells[0];
            totalsCell.Shading.Color = Color.Parse(Gray100);
            SetPadding(totalsCell, 5);
            AddCellText(totalsCell,
                $"Итого: {collector.Summary.CircuitCount} контуров | " +
                $"Длина {Num(collector.Summary.TotalPipeLength, "N1")} м | " +
                $"Мощность {Num(collector.Summary.TotalPower / 1000, "N2")} кВт | " +
                $"Расход {Num(collector.Summary.TotalFlowRate / 1000, "N2")} м³/ч | " +
                $"Max ΔP раб. = {Num(collector.Summary.PressureLoss_Operating_kPa, "N2")} кПа | " +
                $"Max ΔP хол. = {Num(collector.Summary.PressureLoss_Cold_kPa, "N2")} кПа",
                8, colorHex: Gray900, alignment: ParagraphAlignment.Right);
        }

        #endregion

        #region Equipment Section

        private void BuildEquipmentSection(Cell container, double width, ResultsPdfData data)
        {
            StyleCard(container);

            AddCellText(container, "ОБОРУДОВАНИЕ", 10, bold: true, colorHex: RehauBlack);
            AddSpacer(container, 4);

            var innerWidth = width - 14;
            var table = container.Elements.AddTable();
            table.AddColumn(Unit.FromPoint(24));
            table.AddColumn(Unit.FromPoint(innerWidth - 24 - 34 - 42 - 42));
            table.AddColumn(Unit.FromPoint(34));
            table.AddColumn(Unit.FromPoint(42));
            table.AddColumn(Unit.FromPoint(42));
            var header = table.AddRow();
            header.HeadingFormat = true;
            StyleHeaderCell(header.Cells[0], "№");
            StyleHeaderCell(header.Cells[1], "Тип");
            StyleHeaderCell(header.Cells[2], "Конт.");
            StyleHeaderCell(header.Cells[3], "кВт");
            StyleHeaderCell(header.Cells[4], "м³/ч");

            foreach (var spec in data.CollectorSpecifications)
            {
                var specRow = table.AddRow();
                AddCellText(specRow.Cells[0], spec.Number.ToString(), 6, colorHex: Gray900);
                AddCellText(specRow.Cells[1], spec.Type, 6, colorHex: Gray900);
                AddCellText(specRow.Cells[2], spec.CircuitCount.ToString(), 6, colorHex: Gray900);
                AddCellText(specRow.Cells[3], Num(spec.TotalPower_kW, "N2"), 6, colorHex: Gray900,
                    alignment: ParagraphAlignment.Right);
                AddCellText(specRow.Cells[4], Num(spec.TotalFlowRate_m3h, "N2"), 6, colorHex: Gray900,
                    alignment: ParagraphAlignment.Right);
                foreach (Cell cell in specRow.Cells)
                {
                    StyleDataCell(cell, RehauWhite);
                }
            }

            AddSpacer(container, 4);
            var totals = container.Elements.AddTable();
            totals.AddColumn(Unit.FromPoint(innerWidth));
            var totalsCell = totals.AddRow().Cells[0];
            totalsCell.Shading.Color = Color.Parse(Gray100);
            SetPadding(totalsCell, 5);
            AddCellText(totalsCell, $"Коллекторы РЗС: {data.RzsCount}", 7, bold: true, colorHex: Gray900);
            AddCellText(totalsCell, $"Труба: {data.PipeType}", 7, bold: true, colorHex: Gray900);
            AddCellText(totalsCell, $"Общая длина: {Num(data.TotalPipeLength, "N1")} м", 7, bold: true, colorHex: Gray900);
            AddCellText(totalsCell, $"Расширительный бак: {Num(data.ExpansionTankVolume_L, "N1")} л", 7, bold: true, colorHex: Gray900);
            AddCellText(totalsCell, $"Насос: Q={Num(data.PumpFlowRate_m3h, "N2")} м³/ч, H={Num(data.PumpHead_kPa, "N1")} кПа", 7, bold: true, colorHex: Gray900);
        }

        #endregion

        #region Footer

        private void BuildFooter(HeaderFooter footer)
        {
            var table = footer.AddTable();
            table.AddColumn(Unit.FromPoint(ContentWidthPoints));
            var row = table.AddRow();
            row.Height = Unit.FromPoint(30);
            row.VerticalAlignment = VerticalAlignment.Center;
            var cell = row.Cells[0];
            cell.Shading.Color = Color.Parse(Gray100);
            AddCellText(cell,
                $"© {DateTime.Now.Year} РЕХАУ | Расчёт выполнен в РЕХАУ Калькуляторе снеготаяния",
                8, colorHex: Gray600, alignment: ParagraphAlignment.Center);
        }

        #endregion

        #region Helpers

        /// <summary>Рамочная карточка-контейнер секции во всю ширину контента.</summary>
        private static Cell AddCardCell(Section host, double width, double innerPadding)
        {
            var table = host.AddTable();
            return FinishCardCell(table, width, innerPadding);
        }

        private static Cell FinishCardCell(Table table, double width, double innerPadding)
        {
            table.AddColumn(Unit.FromPoint(width));
            var cell = table.AddRow().Cells[0];
            cell.Borders.Width = Unit.FromPoint(1);
            cell.Borders.Color = Color.Parse(Gray300);
            cell.Shading.Color = Color.Parse(RehauWhite);
            SetPadding(cell, innerPadding);
            return cell;
        }

        /// <summary>Оформление ячейки-контейнера как карточки секции.</summary>
        private static void StyleCard(Cell cell)
        {
            cell.Borders.Width = Unit.FromPoint(1);
            cell.Borders.Color = Color.Parse(Gray300);
            cell.Shading.Color = Color.Parse(RehauWhite);
            SetPadding(cell, 6);
        }

        private static void SetPadding(Cell cell, double points)
        {
            cell.Borders.DistanceFromTop = Unit.FromPoint(points);
            cell.Borders.DistanceFromBottom = Unit.FromPoint(points);
            cell.Borders.DistanceFromLeft = Unit.FromPoint(points);
            cell.Borders.DistanceFromRight = Unit.FromPoint(points);
        }

        private static void StyleDataCell(Cell cell, string background)
        {
            cell.Shading.Color = Color.Parse(background);
            cell.Borders.Bottom.Width = Unit.FromPoint(0.5);
            cell.Borders.Bottom.Color = Color.Parse(Gray300);
            SetPadding(cell, 2);
        }

        private static void StyleHeaderCell(Cell cell, string text, double fontSize = 6)
        {
            cell.Shading.Color = Color.Parse(Gray100);
            SetPadding(cell, 2);
            AddCellText(cell, text, fontSize, bold: true, colorHex: RehauBlack);
        }

        private static void AddKeyValue(Table table, string label, string value, string valueColor)
        {
            var row = table.AddRow();
            AddCellText(row.Cells[0], label, 7, bold: true, colorHex: RehauBlack);
            AddCellText(row.Cells[1], value, 7, colorHex: valueColor, alignment: ParagraphAlignment.Right);
        }

        private static Paragraph AddCellText(Cell cell, string text, double sizePt, bool bold = false,
            string? colorHex = null, ParagraphAlignment alignment = ParagraphAlignment.Left)
        {
            var paragraph = cell.AddParagraph(text);
            paragraph.Format.Alignment = alignment;
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = sizePt;
            paragraph.Format.Font.Bold = bold;
            if (colorHex != null)
            {
                paragraph.Format.Font.Color = Color.Parse(colorHex);
            }

            return paragraph;
        }

        private static void AddSectionText(Section section, string text, double sizePt, bool bold = false,
            string? colorHex = null)
        {
            var paragraph = section.AddParagraph(text);
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = sizePt;
            paragraph.Format.Font.Bold = bold;
            if (colorHex != null)
            {
                paragraph.Format.Font.Color = Color.Parse(colorHex);
            }
        }

        private static void AddSpacer(Section host, double points)
        {
            AddSpacer(host.Elements, points);
        }

        private static void AddSpacer(Cell host, double points)
        {
            AddSpacer(host.Elements, points);
        }

        private static void AddSpacer(DocumentElements host, double points)
        {
            var paragraph = host.AddParagraph();
            paragraph.Format.SpaceBefore = Unit.FromPoint(points);
            paragraph.Format.SpaceAfter = Unit.FromPoint(0);
            paragraph.Format.Font.Size = 1;
        }

        #endregion
    }
}
