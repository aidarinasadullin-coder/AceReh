using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Сервис экспорта результатов расчёта в PDF с корпоративным дизайном REHAU
    /// </summary>
    public class PdfExportService : IPdfExportService
    {
        // REHAU Corporate Colors
        private static readonly string RehauRed = "#E50040";
        private static readonly string RehauTeal = "#4FC7B5";
        private static readonly string RehauBlack = "#1D1D1B";
        private static readonly string RehauWhite = "#FFFFFF";
        private static readonly string Gray50 = "#FAFAFA";
        private static readonly string Gray100 = "#F5F5F5";
        private static readonly string Gray300 = "#E0E0E0";
        private static readonly string Gray600 = "#757575";
        private static readonly string Gray900 = "#212121";

        // Путь к логотипу REHAU
        private readonly string _logoPath;

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
                    document.GeneratePdf(filePath);
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

        private IDocument CreateDocument(ResultsPdfData data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    // Header на каждой странице
                    page.Header().Element(e => BuildPageHeader(e, data));

                    // Основной контент
                    page.Content().Column(column =>
                    {
                        // Project Info (под шапкой)
                        column.Item().Element(e => BuildProjectInfoSection(e, data));

                        column.Item().PaddingVertical(8);

                        // KPI блок
                        column.Item().Element(e => BuildKpiSection(e, data));

                        column.Item().PaddingVertical(8);

                        // Исходные данные + Конструкция (две колонки)
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(e => BuildClimateSection(e, data));
                            row.ConstantItem(15);
                            row.RelativeItem().Element(e => BuildConstructionSection(e, data));
                        });

                        column.Item().PaddingVertical(8);

                        // Гидравлический расчёт
                        column.Item().Element(e => BuildHydraulicsSection(e, data));

                        column.Item().PaddingVertical(8);

                        // Оборудование
                        column.Item().Element(e => BuildEquipmentSection(e, data));
                    });

                    // Footer
                    page.Footer().Element(e => BuildFooter(e));
                });
            });
        }

        #region Page Header

        private void BuildPageHeader(IContainer container, ResultsPdfData data)
        {
            container.Background(HexColor(RehauRed)).Height(60, Unit.Point).PaddingHorizontal(15).Row(row =>
            {
                // Логотип слева
                row.ConstantItem(200).AlignMiddle().AlignLeft().Element(e =>
                {
                    if (File.Exists(_logoPath))
                    {
                        e.Image(_logoPath).FitHeight();
                    }
                    else
                    {
                        e.Text("РЕХАУ").FontSize(18).Bold().FontColor(HexColor(RehauWhite));
                    }
                });

                // Центр - название
                row.RelativeItem().AlignMiddle().AlignCenter().Text("Калькулятор снеготаяния")
                    .FontSize(14).Bold().FontColor(HexColor(RehauWhite));

                // Справа - номер страницы
                row.ConstantItem(60).AlignMiddle().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber().FontSize(10).FontColor(HexColor(RehauWhite));
                    text.Span(" / ").FontSize(10).FontColor(HexColor(RehauWhite));
                    text.TotalPages().FontSize(10).FontColor(HexColor(RehauWhite));
                });
            });
        }

        #endregion

        #region Project Info Section

        private void BuildProjectInfoSection(IContainer container, ResultsPdfData data)
        {
            container.Background(HexColor(Gray50)).Padding(8).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text(
                    $"Проект: {data.ProjectNumber} | Объект: {data.ProjectObject} | Дата: {data.ReportDate:dd.MM.yyyy}")
                    .FontSize(9).FontColor(HexColor(Gray900));
            });
        }

        #endregion

        #region KPI Section

        private void BuildKpiSection(IContainer container, ResultsPdfData data)
        {
            container.Column(col =>
            {
                // Заголовок
                col.Item().PaddingBottom(5).Text("КПИ ПОКАЗАТЕЛИ")
                    .FontSize(12).Bold().FontColor(HexColor(RehauBlack));

                // Сетка 2×4 карточек
                col.Item().Row(row =>
                {
                    // Первая строка KPI
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.TotalThermalPower_kW:F2}", "Мощность, кВт"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.SystemVolume_L:F1}", "Объём, л"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.PumpFlowRate_m3h:F2}", "Расход насоса, м³/ч"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.PumpHead_kPa:F1}", "Напор, кПа"));
                });

                col.Item().PaddingVertical(5);

                col.Item().Row(row =>
                {
                    // Вторая строка KPI
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.SupplyTemperature:F1}", "Температура подачи, °C"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.ReturnTemperature:F1}", "Обратки, °C"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.OperatingTemperature:F1}", "Рабочая, °C"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => BuildKpiCard(e, $"{data.ExpansionTankVolume_L:F1}", "Расширительный бак, л"));
                });
            });
        }

        private void BuildKpiCard(IContainer container, string value, string label)
        {
            container.Border(1).BorderColor(HexColor(Gray300)).Background(HexColor(RehauWhite))
                .Padding(8).AlignCenter().Column(col =>
            {
                col.Item().Text(value).FontSize(16).Bold().FontColor(HexColor(RehauTeal));
                col.Item().Text(label).FontSize(8).FontColor(HexColor(Gray600));
            });
        }

        #endregion

        #region Climate Section

        private void BuildClimateSection(IContainer container, ResultsPdfData data)
        {
            container.Border(1).BorderColor(HexColor(Gray300)).Background(HexColor(RehauWhite))
                .Padding(8).Column(col =>
            {
                col.Item().Text("Исходные данные").FontSize(11).Bold().FontColor(HexColor(RehauBlack));
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().Element(CellStyleLeft).Text("Город:").SemiBold().FontSize(9);
                    table.Cell().Element(CellStyleRight).Text(data.City).FontSize(9);

                    table.Cell().Element(CellStyleLeft).Text("Расчётная температура:").SemiBold().FontSize(9);
                    table.Cell().Element(CellStyleRight).Text($"{data.DesignTemperature:F1} °C").FontSize(9);

                    table.Cell().Element(CellStyleLeft).Text("Скорость ветра:").SemiBold().FontSize(9);
                    table.Cell().Element(CellStyleRight).Text($"{data.WindSpeed:F1} м/с").FontSize(9);

                    table.Cell().Element(CellStyleLeft).Text("Интенсивность снегопада:").SemiBold().FontSize(9);
                    table.Cell().Element(CellStyleRight).Text($"{data.SnowfallIntensity:F1} мм/ч").FontSize(9);

                    table.Cell().Element(CellStyleLeft).Text("Тип трубы:").SemiBold().FontSize(9);
                    table.Cell().Element(CellStyleRight).Text(data.PipeType).FontSize(9);

                    table.Cell().Element(CellStyleLeft).Text("Шаг укладки:").SemiBold().FontSize(9);
                    table.Cell().Element(CellStyleRight).Text($"{data.PipeSpacing} мм").FontSize(9);
                });
            });
        }

        #endregion

        #region Construction Section

        private void BuildConstructionSection(IContainer container, ResultsPdfData data)
        {
            container.Border(1).BorderColor(HexColor(Gray300)).Background(HexColor(RehauWhite))
                .Padding(8).Column(col =>
            {
                col.Item().Text("Конструкция").FontSize(11).Bold().FontColor(HexColor(RehauBlack));
                col.Item().PaddingVertical(5);

                col.Item().Row(row =>
                {
                    // Левая колонка: параметры и таблица слоёв
                    row.RelativeItem().Column(leftCol =>
                    {
                        leftCol.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(CellStyleLeft).Text("R1 (над трубой):").SemiBold().FontSize(9);
                            table.Cell().Element(CellStyleRight).Text($"{data.R1:F4} м²·К/Вт").FontSize(9);

                            table.Cell().Element(CellStyleLeft).Text("R2 (под трубой):").SemiBold().FontSize(9);
                            table.Cell().Element(CellStyleRight).Text($"{data.R2:F4} м²·К/Вт").FontSize(9);

                            table.Cell().Element(CellStyleLeft).Text("LambdaE:").SemiBold().FontSize(9);
                            table.Cell().Element(CellStyleRight).Text($"{data.LambdaE:F3} Вт/м·К").FontSize(9);

                            table.Cell().Element(CellStyleLeft).Text("Мощность вверх:").SemiBold().FontSize(9);
                            table.Cell().Element(CellStyleRight).Text($"{data.PowerUp:F1} Вт/м²").FontSize(9);

                            table.Cell().Element(CellStyleLeft).Text("Мощность вниз:").SemiBold().FontSize(9);
                            table.Cell().Element(CellStyleRight).Text($"{data.PowerDown:F1} Вт/м²").FontSize(9);
                        });

                        if (data.Layers.Count > 0)
                        {
                            leftCol.Item().PaddingVertical(5);
                            leftCol.Item().Text("Слои конструкции").FontSize(10).SemiBold();
                            leftCol.Item().PaddingVertical(3);

                            leftCol.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                // Заголовок
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyleSmall).Text("Материал").FontSize(8).Bold();
                                    header.Cell().Element(HeaderCellStyleSmall).Text("Толщина").FontSize(8).Bold();
                                    header.Cell().Element(HeaderCellStyleSmall).Text("λ").FontSize(8).Bold();
                                    header.Cell().Element(HeaderCellStyleSmall).Text("R").FontSize(8).Bold();
                                });

                                foreach (var layer in data.Layers)
                                {
                                    table.Cell().Element(CellStyleSmall).Text(layer.MaterialName).FontSize(8);
                                    table.Cell().Element(CellStyleSmall).Text($"{layer.Thickness:F0}").FontSize(8);
                                    table.Cell().Element(CellStyleSmall).Text($"{layer.Lambda:F3}").FontSize(8);
                                    table.Cell().Element(CellStyleSmall).Text($"{layer.R:F4}").FontSize(8);
                                }
                            });
                        }
                    });

                    // Правая колонка: схема конструкции
                    if (data.ConstructionImageBytes != null)
                    {
                        row.ConstantItem(220).PaddingLeft(10).AlignCenter().AlignMiddle()
                            .Image(data.ConstructionImageBytes)
                            .FitArea();
                    }
                });
            });
        }

        #endregion

        #region Hydraulics Section

        private void BuildHydraulicsSection(IContainer container, ResultsPdfData data)
        {
            container.Column(col =>
            {
                col.Item().Text("ГИДРАВЛИЧЕСКИЙ РАСЧЁТ")
                    .FontSize(12).Bold().FontColor(HexColor(RehauBlack));

                foreach (var collector in data.Collectors)
                {
                    col.Item().PaddingVertical(5);
                    col.Item().Element(e => BuildCollectorTable(e, collector));
                }
            });
        }

        private void BuildCollectorTable(IContainer container, CollectorPdfData collector)
        {
            container.Border(1).BorderColor(HexColor(Gray300)).Column(col =>
            {
                // Заголовок коллектора
                col.Item().Background(HexColor(RehauRed)).Padding(5).Row(row =>
                {
                    row.RelativeItem().Text($"КОЛЛЕКТОР {collector.Number} ({collector.Type})")
                        .FontSize(10).Bold().FontColor(HexColor(RehauWhite));
                });

                // Таблица контуров - 11 столбцов
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);   // №
                        columns.ConstantColumn(55);   // Длина трубы, м
                        columns.ConstantColumn(55);   // Расход, л/ч
                        columns.ConstantColumn(55);   // Скорость, м/с
                        columns.ConstantColumn(65);   // Удельные потери, Па/м
                        columns.ConstantColumn(55);   // DpRohr, кПа
                        columns.ConstantColumn(65);   // DpVerteiler, кПа
                        columns.ConstantColumn(55);   // DpVent, кПа
                        columns.ConstantColumn(60);   // DpGesamt, кПа
                        columns.ConstantColumn(75);   // Дросселирование, кПа
                        columns.ConstantColumn(55);   // Обороты клапана
                    });

                    // Заголовок таблицы
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyleSmall).Text("№").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("Длина, м").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("Расход, л/ч").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("Скорость, м/с").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("Потери, Па/м").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("ΔP тр, кПа").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("ΔP р-л, кПа").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("ΔP вент, кПа").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("ΣΔP, кПа").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("Дросс., кПа").FontSize(8).Bold();
                        header.Cell().Element(HeaderCellStyleSmall).Text("Обороты").FontSize(8).Bold();
                    });

                    // Данные контуров
                    int rowIndex = 0;
                    foreach (var circuit in collector.Circuits)
                    {
                        var bgColor = rowIndex % 2 == 0 ? HexColor(RehauWhite) : HexColor(Gray50);

                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.CircuitNumber.ToString()).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.Length.ToString("F1")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.FlowRate.ToString("F1")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.Velocity.ToString("F2")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.PressureLossPerMeter.ToString("F1")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.DpRohr.ToString("F2")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.DpVerteiler.ToString("F2")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.DpVent.ToString("F2")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.DpGesamt.ToString("F2")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.ZuDrosseln.ToString("F2")).FontSize(8);
                        table.Cell().Element(c => CellStyleWithBg(c, bgColor)).Text(circuit.ValveTurns.ToString("F1")).FontSize(8);

                        rowIndex++;
                    }
                });

                // Итоги по коллектору
                col.Item().Background(HexColor(Gray100)).Padding(5).AlignRight().Text(
                    $"Итого: {collector.Summary.CircuitCount} контуров | " +
                    $"Длина {collector.Summary.TotalPipeLength:F1} м | " +
                    $"Мощность {collector.Summary.TotalPower / 1000:F2} кВт | " +
                    $"Расход {collector.Summary.TotalFlowRate / 1000:F2} м³/ч | " +
                    $"Max ΔP раб. = {collector.Summary.PressureLoss_Operating_kPa:F2} кПа | " +
                    $"Max ΔP хол. = {collector.Summary.PressureLoss_Cold_kPa:F2} кПа")
                    .FontSize(8).FontColor(HexColor(Gray900));
            });
        }

        #endregion

        #region Equipment Section

        private void BuildEquipmentSection(IContainer container, ResultsPdfData data)
        {
            container.Border(1).BorderColor(HexColor(Gray300)).Background(HexColor(RehauWhite))
                .Padding(8).Column(col =>
            {
                col.Item().Text("ОБОРУДОВАНИЕ")
                    .FontSize(12).Bold().FontColor(HexColor(RehauBlack));

                col.Item().PaddingVertical(5);

                // Таблица коллекторов
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(60);
                    });

                    // Заголовок
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("№").FontSize(9).Bold();
                        header.Cell().Element(HeaderCellStyle).Text("Тип коллектора").FontSize(9).Bold();
                        header.Cell().Element(HeaderCellStyle).Text("Контуров").FontSize(9).Bold();
                        header.Cell().Element(HeaderCellStyle).Text("Мощность, кВт").FontSize(9).Bold();
                        header.Cell().Element(HeaderCellStyle).Text("Расход, м³/ч").FontSize(9).Bold();
                        header.Cell().Element(HeaderCellStyle).Text("ΔP, мбар").FontSize(9).Bold();
                        header.Cell().Element(HeaderCellStyle).Text("Kv").FontSize(9).Bold();
                    });

                    foreach (var spec in data.CollectorSpecifications)
                    {
                        table.Cell().Element(CellStyle).Text(spec.Number.ToString()).FontSize(9);
                        table.Cell().Element(CellStyle).Text(spec.Type).FontSize(9);
                        table.Cell().Element(CellStyle).Text(spec.CircuitCount.ToString()).FontSize(9);
                        table.Cell().Element(CellStyle).Text(spec.TotalPower_kW.ToString("F2")).FontSize(9);
                        table.Cell().Element(CellStyle).Text(spec.TotalFlowRate_m3h.ToString("F2")).FontSize(9);
                        table.Cell().Element(CellStyle).Text(spec.PressureLoss_mbar.ToString("F1")).FontSize(9);
                        table.Cell().Element(CellStyle).Text(spec.Kv.ToString("F2")).FontSize(9);
                    }
                });

                col.Item().PaddingVertical(5);

                // Общие итоги
                col.Item().Background(HexColor(Gray100)).Padding(8).Column(c =>
                {
                    c.Item().Text($"Общая длина труб: {data.TotalPipeLength:F1} м").SemiBold().FontSize(9);
                    c.Item().Text($"Количество РЗС (коллекторов): {data.RzsCount}").SemiBold().FontSize(9);
                    c.Item().Text($"Расход насоса: {data.PumpFlowRate_m3h:F2} м³/ч").SemiBold().FontSize(9);
                    c.Item().Text($"Напор насоса: {data.PumpHead_kPa:F1} кПа").SemiBold().FontSize(9);
                    c.Item().Text($"Расширительный бак: {data.ExpansionTankVolume_L:F1} л").SemiBold().FontSize(9);
                });
            });
        }

        #endregion

        #region Footer

        private void BuildFooter(IContainer container)
        {
            container.Background(HexColor(Gray100)).Height(30, Unit.Point)
                .PaddingHorizontal(15).AlignMiddle().Row(row =>
            {
                var year = DateTime.Now.Year;
                row.RelativeItem().AlignMiddle().AlignCenter()
                    .Text($"© {year} РЕХАУ | Расчёт выполнен в РЕХАУ Калькуляторе снеготаяния")
                    .FontSize(8).FontColor(HexColor(Gray600));
            });
        }

        #endregion

        #region Helpers

        private static string HexColor(string hex)
        {
            return hex;
        }

        #endregion

        #region Стили ячеек

        private static IContainer CellStyle(IContainer container)
        {
            return container.Padding(4).BorderBottom(0.5f).BorderColor(HexColor(Gray300));
        }

        private static IContainer CellStyleSmall(IContainer container)
        {
            return container.Padding(2).BorderBottom(0.5f).BorderColor(HexColor(Gray300));
        }

        private static IContainer CellStyleWithBg(IContainer container, string bgColor)
        {
            return container.Background(bgColor).Padding(2).BorderBottom(0.5f).BorderColor(HexColor(Gray300));
        }

        private static IContainer CellStyleLeft(IContainer container)
        {
            return container.Padding(3).AlignLeft();
        }

        private static IContainer CellStyleRight(IContainer container)
        {
            return container.Padding(3).AlignRight();
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container.Padding(4).Background(HexColor(Gray100));
        }

        private static IContainer HeaderCellStyleSmall(IContainer container)
        {
            return container.Padding(2).Background(HexColor(Gray100));
        }

        #endregion
    }
}
