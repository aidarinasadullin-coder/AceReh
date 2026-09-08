using System.IO;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Services.Reports.Calculation;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Экспорт результатов расчёта в PDF — дизайн «Корешок + плитки»
    /// (план 2026-09-09, приёмка владельца 2026-09-09).
    /// Композиция — пирамида «Ответ → Спека → Обоснование»: каждое число
    /// встречается ровно один раз; красный — внимание/действие, зелёный —
    /// подтверждение. Корешок — фирменный градиент «Активный красный»
    /// (брендбук стр. 16), типографика: белый на насыщенном верху, тёмный
    /// на светлой нижней зоне. Рендер — MigraDoc/PDFsharp 6.2 (MIT),
    /// шрифт Inter через общий с ПЗ bootstrapper; числа — канон ru-RU
    /// (десятичная запятая, пробел-тысячи).
    /// </summary>
    public class PdfExportService : IPdfExportService
    {
        // Палитра брендбука REHAU 2026
        private const string RehauRed = "#E50040";
        private const string RehauTeal = "#4FC7B5";
        private const string Ink = "#1D1D1B";
        private const string Gray1 = "#4E4E4E";
        private const string Gray2 = "#818181";
        private const string Gray3 = "#B0B0B0";
        private const string Gray4 = "#D9D9D9";
        private const string White = "#FFFFFF";

        private const double PageWidth = 842;
        private const double PageHeight = 595;
        private const double SpineWidth = 120;      // ширина корешка
        private const double ContentX = 180;        // отступ контентной зоны
        private const double ContentWidth = PageWidth - ContentX - 48;

        private string _fontName = "Arial";

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
                    renderer.RenderDocument();
                    // PDFsharp 6.x пишет Flate-потоки без трейлера Adler-32 —
                    // Acrobat показывает «Недостаточно данных для изображения».
                    // Дописываем трейлер image-потокам; содержимое не меняется.
                    PdfFlateStreamRepair.RepairImageStreams(renderer.PdfDocument);
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
            // Шрифты инициализируются до любой шрифтовой операции: если
            // Результаты рендерятся раньше ПЗ-записки, Inter-резолвер всё
            // равно успевает встать (PDFsharp запрещает смену резолвера
            // после первой шрифтовой операции — ревью плана 2026-09-09, P1).
            CalculationReportPdfFontBootstrapper.EnsureInitialized();
            _fontName = CalculationReportPdfFontBootstrapper.InterAvailable
                ? CalculationReportInterFontResolver.FamilyName
                : "Arial";
            _staticFontName = _fontName;

            var document = new Document();
            var pageSetup = document.DefaultPageSetup.Clone();
            // Явные размеры A4 landscape: связка PageFormat+Orientation после
            // Clone() не даёт альбомной страницы (подводный камень из ревью Ф8).
            pageSetup.PageWidth = Unit.FromPoint(PageWidth);
            pageSetup.PageHeight = Unit.FromPoint(PageHeight);
            // Нулевые поля: подложка корешка — картинка во всю страницу в
            // Primary-хедере; сетку контента держат таблицы-обёртки.
            pageSetup.LeftMargin = Unit.FromPoint(0);
            pageSetup.RightMargin = Unit.FromPoint(0);
            pageSetup.TopMargin = Unit.FromPoint(0);
            pageSetup.BottomMargin = Unit.FromPoint(44);
            pageSetup.HeaderDistance = Unit.FromPoint(0);
            pageSetup.FooterDistance = Unit.FromPoint(16);

            // Секция 1 — «Ответ + Спека» (подложка с активными ОТВЕТ/СПЕКА).
            var section1 = document.AddSection();
            section1.PageSetup = pageSetup;
            SetPageBackground(section1, SpinePage.Page1);
            var body1 = AddContentBlock(section1);
            BuildPageOne(body1, data);
            BuildFooter(section1);

            // Секция 2+ — «Обоснование и наладка»; Primary-хедер повторяет
            // подложку на всех продолжениях при переносе таблиц контуров.
            var section2 = document.AddSection();
            section2.PageSetup = pageSetup.Clone();
            SetPageBackground(section2, SpinePage.Page2Plus);
            var body2 = AddContentBlock(section2);
            BuildPageTwo(body2, data);
            BuildFooter(section2);

            return document;
        }

        #region Каркас страницы

        private void SetPageBackground(Section section, SpinePage page)
        {
            var spineBytes = TryLoadSpineBytes(
                page == SpinePage.Page1 ? "rehau_spine_page1.png" : "rehau_spine_page2.png");
            if (spineBytes == null)
            {
                return;
            }

            var header = section.Headers.Primary;
            var bgTable = header.AddTable();
            bgTable.AddColumn(Unit.FromPoint(PageWidth));
            var bgCell = bgTable.AddRow().Cells[0];
            SetZeroPadding(bgCell);
            var img = bgCell.AddParagraph()
                .AddImage("base64:" + Convert.ToBase64String(spineBytes));
            img.Width = Unit.FromPoint(PageWidth);
            img.Height = Unit.FromPoint(PageHeight);
        }

        private Cell AddContentBlock(Section section)
        {
            var wrap = section.AddTable();
            wrap.AddColumn(Unit.FromPoint(ContentX));
            wrap.AddColumn(Unit.FromPoint(ContentWidth));
            var row = wrap.AddRow();
            var content = row.Cells[1];
            SetZeroPadding(content);
            return content;
        }

        private void BuildFooter(Section section)
        {
            var footer = section.Footers.Primary;
            var t = footer.AddTable();
            t.AddColumn(Unit.FromPoint(ContentX));
            t.AddColumn(Unit.FromPoint(ContentWidth));
            var row = t.AddRow();
            SetZeroPadding(row.Cells[0]);
            SetZeroPadding(row.Cells[1]);

            var left = row.Cells[1].AddParagraph();
            left.Format.Font.Name = _fontName;
            left.Format.Font.Size = 7;
            left.Format.Font.Color = Color.Parse(Gray2);
            left.AddFormattedText("© РЕХАУ", TextFormat.Bold);
            left.AddText(" · Калькулятор снеготаяния · Результаты носят рекомендательный характер");

            var right = row.Cells[1].AddParagraph();
            right.Format.Alignment = ParagraphAlignment.Right;
            right.Format.Font.Name = _fontName;
            right.Format.Font.Size = 7;
            right.Format.Font.Color = Color.Parse(Gray2);
            right.AddPageField();
            right.AddText(" / ");
            right.AddNumPagesField();
        }

        #endregion

        #region Страница 1 — Ответ + Спека

        private void BuildPageOne(Cell host, ResultsPdfData data)
        {
            var h1 = host.AddParagraph();
            h1.Format.Font.Name = _fontName;
            h1.Format.Font.Size = 23;
            h1.Format.Font.Bold = true;
            h1.Format.Font.Color = Color.Parse(Ink);
            h1.Format.SpaceBefore = Unit.FromPoint(42);
            h1.Format.SpaceAfter = Unit.FromPoint(0);
            h1.AddText("Результаты расчёта");

            var info = host.AddParagraph();
            info.Format.Font.Name = _fontName;
            info.Format.Font.Size = 8.5;
            info.Format.Font.Color = Color.Parse(Gray2);
            info.Format.SpaceBefore = Unit.FromPoint(8);
            info.Format.SpaceAfter = Unit.FromPoint(0);
            InfoPair(info, "Проект", EmptyAsDash(data.ProjectNumber));
            info.AddText("   ·   ");
            InfoPair(info, "Объект", EmptyAsDash(data.ProjectObject));
            info.AddText("   ·   ");
            InfoPair(info, "Дата", data.ReportDate.ToString("dd.MM.yyyy", AppCulture.Culture));

            // ─── ОТВЕТ: герой ───
            AddGapInCell(host, 34);
            var hero = host.Elements.AddTable();
            hero.AddColumn(Unit.FromPoint(ContentWidth * 0.55));
            hero.AddColumn(Unit.FromPoint(ContentWidth * 0.45));
            var heroRow = hero.AddRow();
            var heroLeft = heroRow.Cells[0];
            SetZeroPadding(heroLeft);
            var heroRight = heroRow.Cells[1];
            SetZeroPadding(heroRight);

            var qPara = heroLeft.AddParagraph();
            qPara.Format.SpaceBefore = Unit.FromPoint(0);
            qPara.Format.SpaceAfter = Unit.FromPoint(0);
            var qNum = qPara.AddFormattedText(Num(data.TotalPowerDensity, "N1"));
            qNum.Font.Name = _fontName;
            qNum.Font.Size = 46;
            qNum.Font.Bold = true;
            qNum.Font.Color = Color.Parse(RehauRed);
            var qUnit = qPara.AddFormattedText("  Вт/м²");
            qUnit.Font.Name = _fontName;
            qUnit.Font.Size = 15;
            qUnit.Font.Bold = true;
            qUnit.Font.Color = Color.Parse(Ink);

            var qLabel = heroLeft.AddParagraph();
            qLabel.Format.Font.Name = _fontName;
            qLabel.Format.Font.Size = 8;
            qLabel.Format.Font.Bold = true;
            qLabel.Format.Font.Color = Color.Parse(Gray1);
            qLabel.Format.SpaceBefore = Unit.FromPoint(6);
            qLabel.AddText("Q СУММАРНАЯ — УДЕЛЬНАЯ МОЩНОСТЬ ПОКРЫТИЯ");

            // Статус-строка: зелёный квадрат + подтверждение (зелёный = ок)
            AddGapInCell(heroLeft, 12);
            var status = heroLeft.Elements.AddTable();
            status.AddColumn(Unit.FromPoint(7));
            status.AddColumn(Unit.FromPoint(280));
            var statusRow = status.AddRow();
            statusRow.Height = Unit.FromPoint(7);
            statusRow.HeightRule = RowHeightRule.Exactly;
            var statusDot = statusRow.Cells[0];
            statusDot.Shading.Color = Color.Parse(RehauTeal);
            var statusPad = statusDot.AddParagraph();
            statusPad.Format.Font.Size = 1;
            var statusText = statusRow.Cells[1];
            statusText.Borders.DistanceFromLeft = Unit.FromPoint(5);
            statusText.VerticalAlignment = VerticalAlignment.Bottom;
            var statusPara = statusText.AddParagraph();
            statusPara.Format.Font.Name = _fontName;
            statusPara.Format.Font.Size = 8;
            statusPara.Format.Font.Bold = true;
            statusPara.Format.Font.Color = Color.Parse(Ink);
            statusPara.Format.SpaceAfter = Unit.FromPoint(0);
            statusPara.AddText(
                $"{OperatingModeText(data.OperatingMode)} · поверхность +{data.SurfaceTemperature} °C — расчёт подтверждён");

            // Свита героя: тепловые величины
            (string Label, string Value)[] suiteItems =
            {
                ("q вверх", $"{Num(data.PowerUp, "N1")} Вт/м²"),
                ("q вниз", $"{Num(data.PowerDown, "N1")} Вт/м²"),
                ("R над трубой", $"{Num(data.R1, "N4")} м²·К/Вт"),
                ("R под трубой", $"{Num(data.R2, "N4")} м²·К/Вт"),
            };
            var suite = heroRight.Elements.AddTable();
            suite.AddColumn(Unit.FromPoint(ContentWidth * 0.45 * 0.42));
            suite.AddColumn(Unit.FromPoint(ContentWidth * 0.45 * 0.58));
            foreach (var item in suiteItems)
            {
                var suiteRow = suite.AddRow();
                suiteRow.Borders.Bottom.Width = Unit.FromPoint(0.25);
                suiteRow.Borders.Bottom.Color = Color.Parse(Gray4);
                var suiteLabel = suiteRow.Cells[0].AddParagraph();
                suiteLabel.Format.Font.Name = _fontName;
                suiteLabel.Format.Font.Size = 7.5;
                suiteLabel.Format.Font.Color = Color.Parse(Gray2);
                suiteLabel.Format.SpaceBefore = Unit.FromPoint(5);
                suiteLabel.Format.SpaceAfter = Unit.FromPoint(2);
                suiteLabel.AddText(item.Label);
                var suiteValue = suiteRow.Cells[1].AddParagraph();
                suiteValue.Format.Alignment = ParagraphAlignment.Right;
                suiteValue.Format.Font.Name = _fontName;
                suiteValue.Format.Font.Size = 7.5;
                suiteValue.Format.Font.Bold = true;
                suiteValue.Format.Font.Color = Color.Parse(Ink);
                suiteValue.Format.SpaceBefore = Unit.FromPoint(5);
                suiteValue.Format.SpaceAfter = Unit.FromPoint(2);
                suiteValue.AddText(item.Value);
            }

            // ─── ОТВЕТ: плитки теплотехники (цвета пиксель-иконки) ───
            AddGapInCell(host, 34);
            var tiles = host.Elements.AddTable();
            var tileWidth = (ContentWidth - 3 * 8) / 4;
            for (var i = 0; i < 4; i++)
            {
                tiles.AddColumn(Unit.FromPoint(tileWidth));
                if (i < 3)
                {
                    tiles.AddColumn(Unit.FromPoint(8));
                }
            }

            var tileRow = tiles.AddRow();
            tileRow.Height = Unit.FromPoint(82);
            tileRow.HeightRule = RowHeightRule.Exactly;
            (string Value, string Label, string Fill, string Text)[] tileData =
            {
                ($"{Num(data.TotalThermalPower_kW, "N2")} кВт", "МОЩНОСТЬ СИСТЕМЫ", RehauRed, White),
                // Белый на #4FC7B5 даёт контраст ~2.1:1 — на зелёной плитке текст тёмный
                ($"{Num(data.PumpFlowRate_m3h, "N2")} м³/ч", "РАСХОД НАСОСА", RehauTeal, Ink),
                ($"{Num(data.PumpHead_kPa, "N1")} кПа", "НАПОР НАСОСА", Gray1, White),
                ($"{Num(data.SupplyTemperature, "N1")} → {Num(data.ReturnTemperature, "N1")}",
                    "ПОДАЧА → ОБРАТКА, °C", Gray2, White),
            };
            for (var i = 0; i < 4; i++)
            {
                var cell = tileRow.Cells[i * 2];
                cell.Shading.Color = Color.Parse(tileData[i].Fill);
                cell.VerticalAlignment = VerticalAlignment.Center;
                cell.Borders.DistanceFromLeft = Unit.FromPoint(12);
                cell.Borders.DistanceFromRight = Unit.FromPoint(6);
                var value = cell.AddParagraph();
                value.Format.Font.Name = _fontName;
                value.Format.Font.Size = 17;
                value.Format.Font.Bold = true;
                value.Format.Font.Color = Color.Parse(tileData[i].Text);
                value.Format.SpaceAfter = Unit.FromPoint(0);
                value.AddText(tileData[i].Value);
                var label = cell.AddParagraph();
                label.Format.Font.Name = _fontName;
                label.Format.Font.Size = 6.5;
                label.Format.Font.Bold = true;
                label.Format.Font.Color = Color.Parse(tileData[i].Text);
                label.Format.SpaceBefore = Unit.FromPoint(2);
                label.AddText(tileData[i].Label);
            }

            // ─── СПЕКА ЗАКУПКИ ───
            AddGapInCell(host, 38);
            SectionTitle(host, ContentWidth, "Спека закупки и монтажа");

            (string Label, string Value)[] spec =
            {
                ("Коллектор", BuildCollectorSpecLine(data)),
                ("Труба", $"{data.PipeType} · шаг укладки {data.PipeSpacing} мм"),
                ("Длина трассы", $"{Num(data.TotalPipeLength, "N1")} м"),
                ("Насос циркуляционный",
                    $"Q {Num(data.PumpFlowRate_m3h, "N2")} м³/ч · H {Num(data.PumpHead_kPa, "N1")} кПа"),
                ("Расширительный бак", $"{Num(data.ExpansionTankVolume_L, "N1")} л"),
                ("Теплоноситель",
                    $"{data.GlycolTypeDisplayName} {Num(data.GlycolConcentration, "N0")} %"),
            };
            var specTable = host.Elements.AddTable();
            specTable.AddColumn(Unit.FromPoint(ContentWidth * 0.34));
            specTable.AddColumn(Unit.FromPoint(ContentWidth * 0.66));
            foreach (var item in spec)
            {
                var specRow = specTable.AddRow();
                specRow.Borders.Bottom.Width = Unit.FromPoint(0.25);
                specRow.Borders.Bottom.Color = Color.Parse(Gray4);
                var specLabel = specRow.Cells[0].AddParagraph();
                specLabel.Format.Font.Name = _fontName;
                specLabel.Format.Font.Size = 7.5;
                specLabel.Format.Font.Color = Color.Parse(Gray2);
                specLabel.Format.SpaceBefore = Unit.FromPoint(9);
                specLabel.Format.SpaceAfter = Unit.FromPoint(2);
                specLabel.AddText(item.Label.ToUpper(AppCulture.Culture));
                var specValue = specRow.Cells[1].AddParagraph();
                specValue.Format.Font.Name = _fontName;
                specValue.Format.Font.Size = 11;
                specValue.Format.Font.Bold = true;
                specValue.Format.Font.Color = Color.Parse(Ink);
                specValue.Format.SpaceBefore = Unit.FromPoint(6.5);
                specValue.Format.SpaceAfter = Unit.FromPoint(2);
                specValue.AddText(item.Value);
            }

            var note = host.Elements.AddParagraph();
            note.Format.Font.Name = _fontName;
            note.Format.Font.Size = 6.5;
            note.Format.Font.Color = Color.Parse(Gray3);
            note.Format.SpaceBefore = Unit.FromPoint(26);
            note.AddText("Обоснование величин, конструкция и гидравлическая наладка контуров — на стр. 2.");
        }

        /// <summary>Строка «Коллектор» в спеке: тип(ы), шт, суммарные контуры.</summary>
        private static string BuildCollectorSpecLine(ResultsPdfData data)
        {
            var specs = data.CollectorSpecifications;
            if (specs.Count == 0)
            {
                return "—";
            }

            var unitCount = data.RzsCount > 0 ? data.RzsCount : specs.Count;
            var circuitCount = specs.Sum(s => s.CircuitCount);
            var types = string.Join(" · ", specs
                .Select(s => s.Type)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct());

            var circuitsSuffix = $" · {circuitCount} {PluralizeCircuits(circuitCount)}";
            // Тип вида «HKV-D (2 контура)» уже несёт счётчик — не дублируем
            var firstType = specs[0].Type;
            var typeHasCounter = firstType.Contains("контур", StringComparison.OrdinalIgnoreCase);

            return specs.Count == 1
                ? typeHasCounter
                    ? $"{firstType} — {unitCount} шт"
                    : $"{firstType} — {unitCount} шт{circuitsSuffix}"
                : $"{unitCount} шт · {types}{circuitsSuffix}";
        }

        private static string PluralizeCircuits(int count)
        {
            var mod100 = count % 100;
            var mod10 = count % 10;
            if (mod100 is >= 11 and <= 14 || mod10 == 0 || mod10 is >= 5 and <= 9)
            {
                return "контуров";
            }

            return mod10 == 1 ? "контур" : "контура";
        }

        #endregion

        #region Страница 2 — Обоснование и наладка

        private void BuildPageTwo(Cell host, ResultsPdfData data)
        {
            var h1 = host.AddParagraph();
            h1.Format.Font.Name = _fontName;
            h1.Format.Font.Size = 23;
            h1.Format.Font.Bold = true;
            h1.Format.Font.Color = Color.Parse(Ink);
            h1.Format.SpaceBefore = Unit.FromPoint(42);
            h1.Format.SpaceAfter = Unit.FromPoint(0);
            h1.AddText("Обоснование и наладка");

            // ─── ОБОСНОВАНИЕ (притенённый справочный блок) ───
            AddGapInCell(host, 30);
            var obs = host.Elements.AddTable();
            obs.AddColumn(Unit.FromPoint(ContentWidth * 0.48));
            obs.AddColumn(Unit.FromPoint(ContentWidth * 0.04));
            obs.AddColumn(Unit.FromPoint(ContentWidth * 0.48));
            var obsRow = obs.AddRow();
            var obsLeft = obsRow.Cells[0];
            var obsRight = obsRow.Cells[2];
            SetZeroPadding(obsLeft);
            SetZeroPadding(obsRight);

            AddReferenceCaption(obsLeft, "КЛИМАТ · РЕЖИМ · ТЕПЛОНОСИТЕЛЬ");
            (string Label, string Value)[] climate =
            {
                ("Город", EmptyAsDash(data.City)),
                ("Расчётная температура",
                    $"{Num(data.DesignTemperature, "N1")} °C · ветер {Num(data.WindSpeed, "N1")} м/с"),
                ("Снегопад / холодный период",
                    $"{Num(data.SnowfallIntensity, "N1")} мм/ч · {data.ColdPeriodDays} дн. · {data.ClimateZone}"),
                ("Режим", $"{OperatingModeText(data.OperatingMode)} · поверхность +{data.SurfaceTemperature} °C"),
                ("Грунт / теплоноситель",
                    $"+{Num(data.GroundTemperature, "N1")} °C · {data.GlycolTypeDisplayName} " +
                    $"{Num(data.GlycolConcentration, "N0")} %"),
            };
            var climateTable = obsLeft.Elements.AddTable();
            climateTable.AddColumn(Unit.FromPoint(ContentWidth * 0.48 * 0.52));
            climateTable.AddColumn(Unit.FromPoint(ContentWidth * 0.48 * 0.48));
            foreach (var item in climate)
            {
                var climateRow = climateTable.AddRow();
                var climateLabel = climateRow.Cells[0].AddParagraph();
                climateLabel.Format.Font.Name = _fontName;
                climateLabel.Format.Font.Size = 7.5;
                climateLabel.Format.Font.Color = Color.Parse(Gray2);
                climateLabel.Format.SpaceBefore = Unit.FromPoint(9);
                climateLabel.Format.SpaceAfter = Unit.FromPoint(1.5);
                climateLabel.AddText(item.Label);
                var climateValue = climateRow.Cells[1].AddParagraph();
                climateValue.Format.Alignment = ParagraphAlignment.Right;
                climateValue.Format.Font.Name = _fontName;
                climateValue.Format.Font.Size = 7.5;
                climateValue.Format.Font.Color = Color.Parse(Gray1);
                climateValue.Format.SpaceBefore = Unit.FromPoint(9);
                climateValue.Format.SpaceAfter = Unit.FromPoint(1.5);
                climateValue.AddText(item.Value);
            }

            AddReferenceCaption(obsRight, "КОНСТРУКЦИЯ · СЛОИ СВЕРХУ ВНИЗ");
            var layersTable = obsRight.Elements.AddTable();
            layersTable.AddColumn(Unit.FromPoint(ContentWidth * 0.48 - 96));
            layersTable.AddColumn(Unit.FromPoint(32));
            layersTable.AddColumn(Unit.FromPoint(32));
            layersTable.AddColumn(Unit.FromPoint(32));
            var layersHeader = layersTable.AddRow();
            layersHeader.Borders.Bottom.Width = Unit.FromPoint(0.5);
            layersHeader.Borders.Bottom.Color = Color.Parse(Gray3);
            LayerHead(layersHeader.Cells[0], "Слой", ParagraphAlignment.Left);
            LayerHead(layersHeader.Cells[1], "мм", ParagraphAlignment.Right);
            LayerHead(layersHeader.Cells[2], "λ", ParagraphAlignment.Right);
            LayerHead(layersHeader.Cells[3], "R", ParagraphAlignment.Right);
            foreach (var layer in data.Layers)
            {
                var layerRow = layersTable.AddRow();
                LayerCell(layerRow.Cells[0], layer.MaterialName, ParagraphAlignment.Left);
                LayerCell(layerRow.Cells[1], Num(layer.Thickness, "N0"), ParagraphAlignment.Right);
                LayerCell(layerRow.Cells[2], Num(layer.Lambda, "N3"), ParagraphAlignment.Right);
                LayerCell(layerRow.Cells[3], Num(layer.R, "N4"), ParagraphAlignment.Right);
            }

            // ─── НАЛАДКА: проверка холодного пуска (по коллектору) ───
            AddGapInCell(host, 44);
            SectionTitle(host, ContentWidth, "Наладка · проверка холодного пуска");
            foreach (var collector in data.Collectors)
            {
                if (data.Collectors.Count > 1)
                {
                    var collectorLabel = host.Elements.AddParagraph();
                    collectorLabel.Format.Font.Name = _fontName;
                    collectorLabel.Format.Font.Size = 8;
                    collectorLabel.Format.Font.Bold = true;
                    collectorLabel.Format.Font.Color = Color.Parse(Gray1);
                    collectorLabel.Format.SpaceBefore = Unit.FromPoint(10);
                    collectorLabel.Format.SpaceAfter = Unit.FromPoint(0);
                    collectorLabel.AddText($"КОЛЛЕКТОР {collector.Number}");
                }

                var check = host.Elements.AddTable();
                check.AddColumn(Unit.FromPoint(ContentWidth * 0.28));
                check.AddColumn(Unit.FromPoint(ContentWidth * 0.06));
                check.AddColumn(Unit.FromPoint(ContentWidth * 0.32));
                check.AddColumn(Unit.FromPoint(ContentWidth * 0.34));
                var checkRow = check.AddRow();
                checkRow.Borders.Bottom.Width = Unit.FromPoint(0.25);
                checkRow.Borders.Bottom.Color = Color.Parse(Gray4);

                CheckValue(checkRow.Cells[0], Ink, $"{Num(collector.Summary.PressureLoss_Operating_kPa, "N2")} кПа",
                    "РАБОЧИЙ РЕЖИМ");
                var arrowCell = checkRow.Cells[1];
                arrowCell.VerticalAlignment = VerticalAlignment.Center;
                var arrow = arrowCell.AddParagraph();
                arrow.Format.Font.Name = _fontName;
                arrow.Format.Font.Size = 12;
                arrow.Format.Font.Color = Color.Parse(Gray3);
                arrow.Format.SpaceAfter = Unit.FromPoint(0);
                arrow.AddText("→");
                CheckValue(checkRow.Cells[2], RehauRed, $"{Num(collector.Summary.PressureLoss_Cold_kPa, "N2")} кПа",
                    "ХОЛОДНЫЙ ПУСК (ОСТЫВШИЕ КОНТУРЫ)");

                var actionCell = checkRow.Cells[3];
                actionCell.VerticalAlignment = VerticalAlignment.Center;
                var action = actionCell.AddParagraph();
                action.Format.Font.Name = _fontName;
                action.Format.Font.Size = 8;
                action.Format.Font.Color = Color.Parse(Gray1);
                action.Format.SpaceAfter = Unit.FromPoint(0);
                var actionLabel = action.AddFormattedText("Действие: ", TextFormat.Bold);
                actionLabel.Font.Color = Color.Parse(Ink);
                action.AddText(BuildThrottlingAction(collector));
            }

            // ─── КОНТУРЫ: полная гидравлика по коллекторам ───
            AddGapInCell(host, 44);
            foreach (var collector in data.Collectors)
            {
                var head = host.AddParagraph();
                head.Format.KeepWithNext = true;
                head.Format.Font.Name = _fontName;
                head.Format.Font.Size = 11.5;
                head.Format.Font.Bold = true;
                head.Format.Font.Color = Color.Parse(Ink);
                head.Format.SpaceBefore = Unit.FromPoint(14);
                head.Format.SpaceAfter = Unit.FromPoint(0);
                var typeHasCounter = collector.Type.Contains("контур", StringComparison.OrdinalIgnoreCase);
                head.AddText(typeHasCounter
                    ? $"Коллектор {collector.Number} · {collector.Type}"
                    : $"Коллектор {collector.Number} · {collector.Type} " +
                      $"({collector.Summary.CircuitCount} {PluralizeCircuits(collector.Summary.CircuitCount)})");

                AppendCircuitTable(host, collector);
            }
        }

        /// <summary>Таблица контуров коллектора (11 колонок, дроссель > 0 — красным).</summary>
        private void AppendCircuitTable(Cell host, CollectorPdfData collector)
        {
            const double layoutWidth = 620;
            var scale = ContentWidth / layoutWidth;
            double[] widths = { 25, 55, 55, 55, 65, 55, 65, 55, 60, 75, 55 };
            var table = host.Elements.AddTable();
            foreach (var width in widths)
            {
                table.AddColumn(Unit.FromPoint(width * scale));
            }

            string[] headers =
            {
                "№", "Длина, м", "Расход, л/ч", "Скорость, м/с", "R, Па/м", "Δp трубы, кПа",
                "Δp коллект., кПа", "Δp клап., кПа", "Δp всего, кПа", "Дроссель, кПа", "Обороты",
            };
            var headerRow = table.AddRow();
            headerRow.HeadingFormat = true;
            headerRow.Format.SpaceBefore = Unit.FromPoint(14);
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.Cells[i];
                cell.Borders.Top.Width = Unit.FromPoint(1.5);
                cell.Borders.Top.Color = Color.Parse(RehauRed);
                cell.Borders.Bottom.Width = Unit.FromPoint(0.75);
                cell.Borders.Bottom.Color = Color.Parse(Ink);
                cell.Borders.DistanceFromTop = Unit.FromPoint(4);
                cell.Borders.DistanceFromBottom = Unit.FromPoint(3);
                cell.Borders.DistanceFromLeft = Unit.FromPoint(3);
                cell.VerticalAlignment = VerticalAlignment.Bottom;
                var para = cell.AddParagraph();
                if (i > 0)
                {
                    para.Format.Alignment = ParagraphAlignment.Right;
                }

                para.Format.Font.Name = _fontName;
                para.Format.Font.Size = 7;
                para.Format.Font.Bold = true;
                para.Format.Font.Color = Color.Parse(Ink);
                para.AddText(headers[i].ToUpper(AppCulture.Culture));
            }

            foreach (var circuit in collector.Circuits)
            {
                var row = table.AddRow();
                string[] values =
                {
                    circuit.CircuitNumber.ToString(AppCulture.Culture),
                    Num(circuit.Length, "N1"),
                    Num(circuit.FlowRate, "N1"),
                    Num(circuit.Velocity, "N2"),
                    Num(circuit.PressureLossPerMeter, "N1"),
                    Num(circuit.DpRohr, "N2"),
                    Num(circuit.DpVerteiler, "N2"),
                    Num(circuit.DpVent, "N2"),
                    Num(circuit.DpGesamt, "N2"),
                    Num(circuit.ZuDrosseln, "N2"),
                    Num(circuit.ValveTurns, "N1"),
                };
                for (var i = 0; i < values.Length; i++)
                {
                    var cell = row.Cells[i];
                    cell.Borders.Bottom.Width = Unit.FromPoint(0.25);
                    cell.Borders.Bottom.Color = Color.Parse(Gray4);
                    cell.Borders.DistanceFromTop = Unit.FromPoint(8);
                    cell.Borders.DistanceFromBottom = Unit.FromPoint(8);
                    cell.Borders.DistanceFromLeft = Unit.FromPoint(3);
                    var para = cell.AddParagraph();
                    if (i > 0)
                    {
                        para.Format.Alignment = ParagraphAlignment.Right;
                    }

                    para.Format.Font.Name = _fontName;
                    para.Format.Font.Size = 9;
                    var isTotal = i == 8;
                    var isThrottling = i == 9 && circuit.ZuDrosseln > 0;
                    para.Format.Font.Bold = isTotal || isThrottling;
                    para.Format.Font.Color = Color.Parse(isThrottling ? RehauRed : Ink);
                    para.AddText(values[i]);
                }
            }

            var summary = collector.Summary;
            var totals = host.Elements.AddParagraph();
            totals.Format.Alignment = ParagraphAlignment.Right;
            totals.Format.Font.Name = _fontName;
            totals.Format.Font.Size = 9.5;
            totals.Format.Font.Bold = true;
            totals.Format.Font.Color = Color.Parse(Ink);
            totals.Format.SpaceBefore = Unit.FromPoint(26);
            totals.AddText(
                $"Итого: {summary.CircuitCount} {PluralizeCircuits(summary.CircuitCount)} · " +
                $"{Num(summary.TotalPipeLength, "N1")} м · " +
                $"{Num(summary.TotalPower / 1000, "N2")} кВт · " +
                $"{Num(summary.TotalFlowRate / 1000, "N2")} м³/ч");
        }

        /// <summary>«Действие» для пуска: максимальное дросселирование контура.</summary>
        private static string BuildThrottlingAction(CollectorPdfData collector)
        {
            if (collector.Circuits.Count == 0)
            {
                return "контуры не заданы";
            }

            var worst = collector.Circuits.OrderByDescending(c => c.ZuDrosseln).First();
            if (worst.ZuDrosseln <= 0)
            {
                return "дросселирование не требуется";
            }

            var turns = collector.Circuits.Select(c => c.ValveTurns).OrderBy(t => t).ToList();
            return $"дросселирование {Num(worst.ZuDrosseln, "N2")} кПа (контур {worst.CircuitNumber}), " +
                   $"обороты {Num(turns[0], "N1")}–{Num(turns[^1], "N1")}";
        }

        #endregion

        #region Примитивы

        private static void AddGapInCell(Cell host, double points)
        {
            var gap = host.Elements.AddParagraph();
            gap.Format.SpaceBefore = Unit.FromPoint(points);
            gap.Format.SpaceAfter = Unit.FromPoint(0);
            gap.Format.Font.Size = 1;
        }

        private static void SetZeroPadding(Cell cell)
        {
            cell.Borders.DistanceFromTop = 0;
            cell.Borders.DistanceFromBottom = 0;
            cell.Borders.DistanceFromLeft = 0;
            cell.Borders.DistanceFromRight = 0;
        }

        private static void SectionTitle(Cell host, double innerWidth, string title)
        {
            var titleTable = host.Elements.AddTable();
            titleTable.AddColumn(Unit.FromPoint(7));
            titleTable.AddColumn(Unit.FromPoint(innerWidth - 12));
            var row = titleTable.AddRow();
            row.Height = Unit.FromPoint(9);
            row.HeightRule = RowHeightRule.Exactly;
            var mark = row.Cells[0];
            mark.Shading.Color = Color.Parse(RehauRed);
            var markPad = mark.AddParagraph();
            markPad.Format.Font.Size = 1;
            var textCell = row.Cells[1];
            textCell.Borders.DistanceFromLeft = Unit.FromPoint(5);
            textCell.VerticalAlignment = VerticalAlignment.Bottom;
            var para = textCell.AddParagraph();
            para.Format.Font.Name = _staticFontName;
            para.Format.Font.Size = 10;
            para.Format.Font.Bold = true;
            para.Format.Font.Color = Color.Parse(Ink);
            para.AddText(title.ToUpper(AppCulture.Culture));
        }

        private static void CheckValue(Cell cell, string valueColor, string value, string label)
        {
            cell.Borders.DistanceFromTop = Unit.FromPoint(16);
            cell.Borders.DistanceFromBottom = Unit.FromPoint(12);
            cell.VerticalAlignment = VerticalAlignment.Center;
            var valuePara = cell.AddParagraph();
            valuePara.Format.Font.Name = _staticFontName;
            valuePara.Format.Font.Size = 16;
            valuePara.Format.Font.Bold = true;
            valuePara.Format.Font.Color = Color.Parse(valueColor);
            valuePara.Format.SpaceAfter = Unit.FromPoint(0);
            valuePara.AddText(value);
            var labelPara = cell.AddParagraph();
            labelPara.Format.Font.Name = _staticFontName;
            labelPara.Format.Font.Size = 6.5;
            labelPara.Format.Font.Bold = true;
            labelPara.Format.Font.Color = Color.Parse(Gray2);
            labelPara.AddText(label);
        }

        private static void AddReferenceCaption(Cell host, string caption)
        {
            var para = host.AddParagraph();
            para.Format.Font.Name = _staticFontName;
            para.Format.Font.Size = 7;
            para.Format.Font.Bold = true;
            para.Format.Font.Color = Color.Parse(Gray3);
            para.Format.SpaceAfter = Unit.FromPoint(4);
            para.AddText(caption);
        }

        private static void LayerHead(Cell cell, string text, ParagraphAlignment align)
        {
            cell.VerticalAlignment = VerticalAlignment.Bottom;
            var para = cell.AddParagraph();
            para.Format.Alignment = align;
            para.Format.Font.Name = _staticFontName;
            para.Format.Font.Size = 6;
            para.Format.Font.Bold = true;
            para.Format.Font.Color = Color.Parse(Gray1);
            para.Format.SpaceBefore = Unit.FromPoint(3);
            para.AddText(text.ToUpper(AppCulture.Culture));
        }

        private static void LayerCell(Cell cell, string text, ParagraphAlignment align)
        {
            cell.Borders.Bottom.Width = Unit.FromPoint(0.25);
            cell.Borders.Bottom.Color = Color.Parse(Gray4);
            var para = cell.AddParagraph();
            para.Format.Alignment = align;
            para.Format.Font.Name = _staticFontName;
            para.Format.Font.Size = 7;
            para.Format.Font.Color = Color.Parse(Gray1);
            para.Format.SpaceBefore = Unit.FromPoint(3.5);
            para.Format.SpaceAfter = Unit.FromPoint(2);
            para.AddText(text);
        }

        private static void InfoPair(Paragraph paragraph, string label, string value)
        {
            paragraph.AddText(label + " ");
            var valueRun = paragraph.AddFormattedText(value, TextFormat.Bold);
            valueRun.Font.Color = Color.Parse(Ink);
        }

        private static string EmptyAsDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        }

        /// <summary>Человекочитаемое имя режима; невалидное значение (0) — прочерк.</summary>
        private static string OperatingModeText(Models.Thermal.OperatingMode mode)
        {
            return Enum.IsDefined(mode) ? mode.ToString() : "—";
        }

        /// <summary>
        /// Подложки корешка — встроенные WPF-ресурсы (deploy-независимо,
        /// fileless). WPF кладёт ресурсы в &lt;Assembly&gt;.g.resources;
        /// ResourceManager сам добавляет суффикс «.resources», поэтому
        /// корень — «.g». Паттерн TryLoadLogoBytes ПЗ-рендерера.
        /// </summary>
        private static byte[]? TryLoadSpineBytes(string resourceName)
        {
            try
            {
                var assembly = typeof(PdfExportService).Assembly;
                var manager = new System.Resources.ResourceManager(assembly.GetName().Name + ".g", assembly);
                using var stream = manager.GetStream("resources/images/" + resourceName) as Stream;
                if (stream == null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.Length > 0 ? ms.ToArray() : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Подложка отчёта не загружена: {ex.Message}");
                return null;
            }
        }

        /// <summary>Шрифт для статических хелперов (заполняется до рендера).</summary>
        private static string _staticFontName = "Arial";

        #endregion

        private enum SpinePage
        {
            Page1,
            Page2Plus,
        }
    }
}
