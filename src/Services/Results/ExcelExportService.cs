// ================================================================================
// REHAU Снеготаяние - Экспорт спецификации закупки в Excel (.xlsx)
// ================================================================================
//
// Назначение: запись ResultsSpecificationData в книгу Excel через ClosedXML
// (план docs/plans/2026-09-13-excel-specification-plan.md, фаза Ф2).
//
// Соответствует:
// - Паттерн PdfExportService: путь снаружи, наружу bool, ошибки не глотаются
//   молча (StatusMessage в VM)
// - Числовые значения — числовые ячейки (суммируются в Excel), разделитель
//   рисует Excel по своей локали; форматы столбцов — «0.00»/«#,##0»
// - Бренд: плашки Brand.Red #E50040 / Brand.Teal.Deep #2F776D, линии
//   #E4E4E4, зебра Brand.Gray.100 / Brand.Teal.Pale (токены Tokens.Colors.xaml)
//
// ================================================================================

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;

using SnowMeltingCalculator.Services.Logging;
namespace SnowMeltingCalculator.Services.Results
{
    public class ExcelExportService : IResultsExcelExportService
    {
        private const string RedHex = "#E50040";        // Brand.Red
        private const string RedTintHex = "#FCC2B8";    // Brand.Red.Tint
        private const string TealDeepHex = "#2F776D";   // Brand.Teal.Deep
        private const string TealPaleHex = "#95DDD3";   // Brand.Teal.Pale
        private const string BorderHex = "#E4E4E4";     // Brand.Gray.100
        private const string ZebraHex = "#FAFAFA";      // нейтральный хром таблиц

        /// <inheritdoc />
        public Task<bool> ExportSpecificationToXlsxAsync(
            string filePath,
            ResultsSpecificationData data,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                try
                {
                    WriteWorkbook(filePath, data);
                    return true;
                }
                catch (Exception ex) {
                    AppLog.Warn(ex, "ExcelExportService.ExportSpecificationToXlsxAsync");
                    Debug.WriteLine($"Excel export failed: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        private static void WriteWorkbook(string filePath, ResultsSpecificationData data)
        {
            using var workbook = new XLWorkbook();
            WriteSpecificationSheet(workbook, data);
            WriteCircuitsSheet(workbook, data);
            workbook.SaveAs(filePath);
        }

        private static void WriteSpecificationSheet(XLWorkbook workbook, ResultsSpecificationData data)
        {
            var sheet = workbook.AddWorksheet("Спецификация");

            // Шапка-плашка
            var title = sheet.Range("A1:D1");
            title.Merge().Value = "РЕХАУ Калькулятор снеготаяния — спецификация закупки";
            StyleTitle(title, RedHex);

            sheet.Cell("A2").Value = "Проект №";
            sheet.Cell("B2").Value = string.IsNullOrEmpty(data.ProjectNumber) ? "—" : data.ProjectNumber;
            sheet.Cell("A3").Value = "Объект";
            sheet.Cell("B3").Value = string.IsNullOrEmpty(data.ProjectObject) ? "—" : data.ProjectObject;
            sheet.Range("A2:A3").Style.Font.SetBold();

            // Таблица позиций
            int row = 5;
            var specHeaders = new[] { "Наименование", "Артикул", "Ед.", "Кол-во" };
            for (int i = 0; i < specHeaders.Length; i++)
            {
                sheet.Cell(row, i + 1).Value = specHeaders[i];
            }

            var header = sheet.Range(row, 1, row, 4);
            StyleTableHeader(header);
            row++;

            var sections = data.Rows.Select(r => r.Section).Distinct().ToList();
            foreach (var section in sections)
            {
                var sectionRows = data.Rows.Where(r => r.Section == section).ToList();

                var sectionCell = sheet.Cell(row, 1);
                sectionCell.Value = section;
                sheet.Range(row, 1, row, 4).Merge();
                sectionCell.Style.Font.SetBold();
                sectionCell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(RedTintHex));
                row++;

                foreach (var item in sectionRows)
                {
                    sheet.Cell(row, 1).Value = item.Name;
                    sheet.Cell(row, 2).Value = string.IsNullOrEmpty(item.Article) ? "—" : item.Article;
                    sheet.Cell(row, 3).Value = item.Unit;
                    var quantityCell = sheet.Cell(row, 4);
                    quantityCell.Value = item.Quantity;
                    quantityCell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    quantityCell.Style.NumberFormat.SetFormat(QuantityFormat(item.Unit));
                    if (row % 2 == 0)
                    {
                        sheet.Range(row, 1, row, 4).Style.Fill.SetBackgroundColor(XLColor.FromHtml(ZebraHex));
                    }
                    row++;
                }

                // Примечания секции (например «артикул уточняется» для РЗС 25×2,3)
                foreach (var note in sectionRows.Where(r => !string.IsNullOrWhiteSpace(r.Notes)).Select(r => r.Notes!))
                {
                    var noteCell = sheet.Cell(row, 1);
                    noteCell.Value = $"Примечание: {note}";
                    noteCell.Style.Font.SetItalic();
                    noteCell.Style.Font.SetFontSize(9);
                    sheet.Range(row, 1, row, 4).Merge();
                    row++;
                }
            }

            StyleTableBorders(sheet, 5, row - 1, 4);
            sheet.Column(1).AdjustToContents();
            sheet.Column(2).AdjustToContents();
            sheet.Column(3).Width = 6;
            sheet.Column(4).Width = 12;
        }

        private static void WriteCircuitsSheet(XLWorkbook workbook, ResultsSpecificationData data)
        {
            var sheet = workbook.AddWorksheet("Контуры");

            var title = sheet.Range("A1:J1");
            title.Merge().Value = "Наладка по контурам";
            StyleTitle(title, TealDeepHex);

            sheet.Cell("A2").Value = "Контуров:";
            sheet.Cell("B2").Value = data.TotalCircuits;
            sheet.Cell("C2").Value = "Труба, м:";
            sheet.Cell("D2").Value = data.TotalPipeLength_m;
            sheet.Cell("D2").Style.NumberFormat.SetFormat("#,##0.0");
            sheet.Cell("E2").Value = "Мощность, кВт:";
            sheet.Cell("F2").Value = data.TotalPower_kW;
            sheet.Cell("F2").Style.NumberFormat.SetFormat("0.00");
            sheet.Range("A2:A2").Style.Font.SetBold();
            sheet.Cell("C2").Style.Font.SetBold();
            sheet.Cell("E2").Style.Font.SetBold();

            int row = 4;
            var circuitHeaders = new[]
            {
                "Коллектор", "№ контура", "Петля, м", "Подача, м", "Площадь, м²",
                "Мощность, Вт", "Расход, л/ч", "Δp, кПа", "Обороты клапана", "Преднастройка, кПа"
            };
            for (int i = 0; i < circuitHeaders.Length; i++)
            {
                sheet.Cell(row, i + 1).Value = circuitHeaders[i];
            }

            var header = sheet.Range(row, 1, row, 10);
            StyleTableHeader(header);
            row++;

            foreach (var group in data.CircuitRows.GroupBy(r => new { r.CollectorNumber, r.CollectorType }))
            {
                var sectionCell = sheet.Cell(row, 1);
                sectionCell.Value = $"Коллектор №{group.Key.CollectorNumber} — {group.Key.CollectorType}";
                sheet.Range(row, 1, row, 10).Merge();
                sectionCell.Style.Font.SetBold();
                sectionCell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(TealPaleHex));
                row++;

                foreach (var circuit in group)
                {
                    sheet.Cell(row, 1).Value = circuit.CollectorType;
                    sheet.Cell(row, 2).Value = circuit.CircuitNumber;
                    SetNumeric(sheet.Cell(row, 3), circuit.LoopLength_m, "0.00");
                    SetNumeric(sheet.Cell(row, 4), circuit.SupplyLength_m, "0.00");
                    SetNumeric(sheet.Cell(row, 5), circuit.Area_m2, "0.00");
                    SetNumeric(sheet.Cell(row, 6), circuit.Power_W, "#,##0");
                    SetNumeric(sheet.Cell(row, 7), circuit.FlowRate_lh, "#,##0");
                    SetNumeric(sheet.Cell(row, 8), circuit.PressureLoss_Pa / 1000.0, "0.00");
                    SetNumeric(sheet.Cell(row, 9), circuit.ValveTurns, "0.0");
                    SetNumeric(sheet.Cell(row, 10), circuit.ZuDrosseln_Pa / 1000.0, "0.00");
                    if (row % 2 == 0)
                    {
                        sheet.Range(row, 1, row, 10).Style.Fill.SetBackgroundColor(XLColor.FromHtml(ZebraHex));
                    }
                    row++;
                }
            }

            StyleTableBorders(sheet, 4, row - 1, 10);
            sheet.Column(1).AdjustToContents();
            for (int c = 2; c <= 10; c++)
            {
                sheet.Column(c).Width = 13;
            }
        }

        private static string QuantityFormat(string unit) => unit switch
        {
            "м" => "#,##0.0",
            "кВт" => "0.00",
            _ => "#,##0"
        };

        private static void SetNumeric(IXLCell cell, double value, string format)
        {
            cell.Value = value;
            cell.Style.NumberFormat.SetFormat(format);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        }

        private static void StyleTitle(IXLRange range, string backgroundHex)
        {
            range.Style.Font.SetBold();
            range.Style.Font.SetFontSize(13);
            range.Style.Font.SetFontColor(XLColor.White);
            range.Style.Fill.SetBackgroundColor(XLColor.FromHtml(backgroundHex));
            range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            range.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        private static void StyleTableHeader(IXLRange range)
        {
            range.Style.Font.SetBold();
            range.Style.Fill.SetBackgroundColor(XLColor.FromHtml(BorderHex));
            range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        private static void StyleTableBorders(IXLWorksheet sheet, int firstRow, int lastRow, int columnCount)
        {
            if (lastRow < firstRow)
            {
                return;
            }

            var range = sheet.Range(firstRow, 1, lastRow, columnCount);
            range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            range.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
            range.Style.Border.SetOutsideBorderColor(XLColor.FromHtml(BorderHex));
            range.Style.Border.SetInsideBorderColor(XLColor.FromHtml(BorderHex));
        }
    }
}
