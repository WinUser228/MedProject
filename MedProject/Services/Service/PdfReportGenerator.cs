using HospitalStatistics.Core.Models;
using MedProject.Services.Interfaces;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.Services.Service;

public class PdfReportGenerator : IPdfReportGenerator
{
    public async Task<byte[]> GenerateReportSevenPdfBytesAsync(
        List<ReportSevenDTO> data,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var document = new Document();
            document.Info.Title = "Отчет 7 - Движение больных";
            var section = document.AddSection();

            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.LeftMargin = "1cm";
            section.PageSetup.RightMargin = "0.5cm";
            section.PageSetup.TopMargin = "1.5cm";
            section.PageSetup.BottomMargin = "0.5cm";

            var style = document.Styles["Normal"];
            style.Font.Name = "Arial";
            style.Font.Size = 10;
            style.ParagraphFormat.SpaceAfter = 0;

            Paragraph p;
            p = section.AddParagraph("БРЯНСКАЯ ДЕТСКАЯ ОБЛАСТНАЯ БОЛЬНИЦА");
            p.Format.Font.Bold = true;
            p.Format.Alignment = ParagraphAlignment.Center;
            p.Format.SpaceAfter = 2;

            p = section.AddParagraph("Учет движения больных и коечного фонда стационара");
            p.Format.Font.Bold = true;
            p.Format.Alignment = ParagraphAlignment.Center;

            p = section.AddParagraph($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}");
            p.Format.Font.Bold = true;
            p.Format.Alignment = ParagraphAlignment.Center;
            p.Format.SpaceAfter = "1cm";

            var table = section.AddTable();
            table.Style = "Table";
            table.Borders.Width = 0.5;
            table.Borders.Color = Colors.Black;
            table.Format.Font.Size = 7;
            table.Format.Alignment = ParagraphAlignment.Center;
            table.Rows.LeftIndent = 0;

            // Сумма ширин ≈28.1 см (A4 landscape минус поля). Узкие подписи (ПЕРЕВЕДЕНО, МАТЕРЯМИ/СЕЛЬСКИМИ)
            // вынесены в более широкие колонки за счёт «ОТДЕЛЕНИЯ» и колонок без длинного текста в шапке.
            table.AddColumn("0.5cm");
            table.AddColumn("3.6cm");
            table.AddColumn("1.3cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.55cm");
            table.AddColumn("1.55cm");
            table.AddColumn("1.75cm");
            table.AddColumn("1.75cm");
            table.AddColumn("1.55cm");
            table.AddColumn("1.55cm");
            table.AddColumn("1.3cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.6cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.55cm");
            table.AddColumn("1.55cm");

            var row5 = table.AddRow();
            row5.HeightRule = RowHeightRule.AtLeast;
            row5.Height = "0.8cm";
            row5.Cells[0].AddParagraph("N"); row5.Cells[0].MergeDown = 2;
            row5.Cells[1].AddParagraph("ОТДЕЛЕНИЯ"); row5.Cells[1].MergeDown = 2;
            row5.Cells[2].AddParagraph("КОЛ-ВО КОЕК"); row5.Cells[2].MergeDown = 2;
            row5.Cells[3].AddParagraph("НАЧАЛО СУТОК"); row5.Cells[3].MergeDown = 2;
            row5.Cells[4].AddParagraph("ДВИЖЕНИЕ БОЛЬНЫХ ЗА ИСТЕКШИЕ СУТКИ"); row5.Cells[4].MergeRight = 5;
            row5.Cells[10].AddParagraph("УМЕРЛО"); row5.Cells[10].MergeDown = 2;
            row5.Cells[11].AddParagraph("НА НАЧАЛО ТЕКУЩЕГО ДНЯ"); row5.Cells[11].MergeRight = 2;
            row5.Cells[14].AddParagraph("% ЗА ВЫБРАННЫЙ ПЕРИОД"); row5.Cells[14].MergeDown = 2;
            row5.Cells[15].AddParagraph("ПРОВЕДЕНО КОЙКО-ДНЕЙ"); row5.Cells[15].MergeRight = 2;

            var row6 = table.AddRow();
            row6.HeightRule = RowHeightRule.AtLeast;
            row6.Height = "0.8cm";
            row6.Cells[4].AddParagraph("ПОСТУПИЛО"); row6.Cells[4].MergeRight = 1;
            row6.Cells[6].AddParagraph("ПЕРЕВЕДЕНО"); row6.Cells[6].MergeRight = 1;
            row6.Cells[8].AddParagraph("ВЫПИСАНО"); row6.Cells[8].MergeRight = 1;
            row6.Cells[11].AddParagraph("СОСТОИТ"); row6.Cells[11].MergeRight = 2;
            row6.Cells[15].AddParagraph("ВСЕГО");
            row6.Cells[15].MergeDown = 1;
            row6.Cells[16].AddParagraph("ИЗ НИХ");
            row6.Cells[16].MergeRight = 1;

            var row7 = table.AddRow();
            row7.HeightRule = RowHeightRule.AtLeast;
            row7.Height = "0.8cm";
            row7.Cells[4].AddParagraph("ВСЕГО");
            row7.Cells[5].AddParagraph("ИЗ СЕЛА");
            row7.Cells[6].AddParagraph("ИЗ\nОТДЕЛЕНИЯ");
            row7.Cells[7].AddParagraph("В\nОТДЕЛЕНИЕ");
            row7.Cells[8].AddParagraph("ВСЕГО");
            row7.Cells[9].AddParagraph("ПЕРЕВЕДЕНО");
            row7.Cells[11].AddParagraph("ВСЕГО");
            row7.Cells[12].AddParagraph("ИЗ СЕЛА");
            row7.Cells[13].AddParagraph("МАТЕРЕЙ");
            row7.Cells[15].AddParagraph("");
            row7.Cells[16].AddParagraph("МАТЕРЯМИ");
            row7.Cells[17].AddParagraph("СЕЛЬСКИМИ");

            StyleHeaderRows(table, new[] { row5, row6, row7 });

            int n = 1;
            var grouped = data.GroupBy(d => d.DepartmentName);

            foreach (var departmentGroup in grouped)
            {
                ct.ThrowIfCancellationRequested();

                var total = departmentGroup.FirstOrDefault(x => x.BedType == "ИТОГО");
                if (total != null)
                {
                    var totalRow = table.AddRow();
                    totalRow.Format.Font.Bold = true;
                    totalRow.Shading.Color = Colors.WhiteSmoke;
                    totalRow.VerticalAlignment = VerticalAlignment.Center;

                    bool isGeneralTotal = total.DepartmentName == "ИТОГО";
                    if (isGeneralTotal)
                    {
                        totalRow.Cells[0].MergeRight = 1;
                        var pName = totalRow.Cells[0].AddParagraph("ИТОГО");
                        pName.Format.Alignment = ParagraphAlignment.Center;
                        pName.Format.Font.Bold = true;
                        totalRow.Cells[1].AddParagraph(string.Empty);
                    }
                    else
                    {
                        totalRow.Cells[0].AddParagraph(n++.ToString());
                        totalRow.Cells[1].AddParagraph(total.DepartmentName);
                    }

                    totalRow.Cells[2].AddParagraph(total.BedCount.ToString());
                    totalRow.Cells[3].AddParagraph(total.PatientsAtStartOfDay.ToString());
                    totalRow.Cells[4].AddParagraph(total.TotalAdmitted.ToString());
                    totalRow.Cells[5].AddParagraph(total.FromVillage.ToString());
                    totalRow.Cells[6].AddParagraph(total.TransferredFromOtherDepartments.ToString());
                    totalRow.Cells[7].AddParagraph(total.TransferredToOtherDepartments.ToString());
                    totalRow.Cells[8].AddParagraph(total.TotalDischarged.ToString());
                    totalRow.Cells[9].AddParagraph(total.DischargedByTransfer.ToString());
                    totalRow.Cells[10].AddParagraph(total.Died.ToString());
                    totalRow.Cells[11].AddParagraph(total.TotalPatients.ToString());
                    totalRow.Cells[12].AddParagraph(total.FromVillagePatient.ToString());
                    totalRow.Cells[13].AddParagraph(total.MothersWithPatients.ToString());
                    totalRow.Cells[14].AddParagraph(total.MonthlyPlanPercent.ToString("N2"));
                    totalRow.Cells[15].AddParagraph(total.BedDaysTotal.ToString());
                    totalRow.Cells[16].AddParagraph(total.BedDaysMothers.ToString());
                    totalRow.Cells[17].AddParagraph(total.BedDaysFromVillage.ToString());
                    AlignNumericCells(totalRow, table.Columns.Count, 2);
                }

                foreach (var d in departmentGroup.Where(x => x.BedType != "ИТОГО"))
                {
                    ct.ThrowIfCancellationRequested();

                    var dataRow = table.AddRow();
                    dataRow.VerticalAlignment = VerticalAlignment.Center;
                    dataRow.Cells[1].AddParagraph(d.BedType);
                    dataRow.Cells[1].Format.LeftIndent = "0.2cm";
                    dataRow.Cells[2].AddParagraph(d.BedCount.ToString());
                    dataRow.Cells[3].AddParagraph(d.PatientsAtStartOfDay.ToString());
                    dataRow.Cells[4].AddParagraph(d.TotalAdmitted.ToString());
                    dataRow.Cells[5].AddParagraph(d.FromVillage.ToString());
                    dataRow.Cells[6].AddParagraph(d.TransferredFromOtherDepartments.ToString());
                    dataRow.Cells[7].AddParagraph(d.TransferredToOtherDepartments.ToString());
                    dataRow.Cells[8].AddParagraph(d.TotalDischarged.ToString());
                    dataRow.Cells[9].AddParagraph(d.DischargedByTransfer.ToString());
                    dataRow.Cells[10].AddParagraph(d.Died.ToString());
                    dataRow.Cells[11].AddParagraph(d.TotalPatients.ToString());
                    dataRow.Cells[12].AddParagraph(d.FromVillagePatient.ToString());
                    dataRow.Cells[13].AddParagraph(d.MothersWithPatients.ToString());
                    dataRow.Cells[14].AddParagraph(d.MonthlyPlanPercent.ToString("N2"));
                    dataRow.Cells[15].AddParagraph(d.BedDaysTotal.ToString());
                    dataRow.Cells[16].AddParagraph(d.BedDaysMothers.ToString());
                    dataRow.Cells[17].AddParagraph(d.BedDaysFromVillage.ToString());
                    AlignNumericCells(dataRow, table.Columns.Count, 2);
                }
            }

            return RenderToPdf(document);
        }, ct);
    }

    public async Task<byte[]> GenerateReportSevenEmptyPdfBytesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            var document = CreateNoDataDocument(
                title: "Отчёт №7 — учет движения больных и коечного фонда стационара",
                startDate: startDate,
                endDate: endDate);
            return RenderToPdf(document);
        }, ct);
    }

    public async Task<byte[]> GenerateReportSevenPart2PdfBytesAsync(
        List<ReportSevenPart2DTO> data,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var document = new Document();
            document.Info.Title = "Отчет 7 - Показатели использования коек";
            var section = document.AddSection();

            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.LeftMargin = "1cm";
            section.PageSetup.RightMargin = "1cm";
            section.PageSetup.TopMargin = "1.5cm";
            section.PageSetup.BottomMargin = "1.5cm";

            var style = document.Styles["Normal"];
            style.Font.Name = "Arial";
            style.Font.Size = 7;
            style.ParagraphFormat.SpaceAfter = 0;

            Paragraph p;
            p = section.AddParagraph("БРЯНСКАЯ ДЕТСКАЯ ОБЛАСТНАЯ БОЛЬНИЦА");
            p.Format.Font.Bold = true;
            p.Format.Alignment = ParagraphAlignment.Center;
            p.Format.SpaceAfter = 2;

            p = section.AddParagraph("Отчёт №7 — показатели использования коечного фонда");
            p.Format.Font.Bold = true;
            p.Format.Alignment = ParagraphAlignment.Center;

            p = section.AddParagraph($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}");
            p.Format.Font.Bold = true;
            p.Format.Alignment = ParagraphAlignment.Center;
            p.Format.SpaceAfter = "1cm";

            var table = section.AddTable();
            table.Style = "Table";
            table.Borders.Width = 0.5;
            table.Borders.Color = Colors.Black;
            table.Format.Font.Size = 7;
            table.Format.Alignment = ParagraphAlignment.Center;
            table.Rows.LeftIndent = 0;

            table.AddColumn("0.5cm");
            table.AddColumn("6.2cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.4cm");
            table.AddColumn("1.5cm");
            table.AddColumn("1.5cm");
            table.AddColumn("1.6cm");
            table.AddColumn("1.6cm");
            table.AddColumn("1.6cm");
            table.AddColumn("1.6cm");
            table.AddColumn("1.8cm");
            table.AddColumn("1.6cm");
            table.AddColumn("1.5cm");
            table.AddColumn("1.8cm");

            var rowH1 = table.AddRow();
            rowH1.HeightRule = RowHeightRule.AtLeast;
            rowH1.Height = "0.7cm";
            rowH1.Cells[0].AddParagraph("N"); rowH1.Cells[0].MergeDown = 1;
            rowH1.Cells[1].AddParagraph("ОТДЕЛЕНИЯ"); rowH1.Cells[1].MergeDown = 1;
            rowH1.Cells[2].AddParagraph("ИСПОЛЬЗОВАНИЕ КОЙКО-ДНЕЙ"); rowH1.Cells[2].MergeRight = 2;
            rowH1.Cells[5].AddParagraph("ПРОВЕДЕНО КОЙКО-ДНЕЙ"); rowH1.Cells[5].MergeRight = 1;
            rowH1.Cells[7].AddParagraph("ПОКАЗАТЕЛИ ЭФФЕКТИВНОСТИ"); rowH1.Cells[7].MergeRight = 3;
            rowH1.Cells[11].AddParagraph("ЛЕТАЛЬНОСТЬ %"); rowH1.Cells[11].MergeDown = 1;
            rowH1.Cells[12].AddParagraph("ВЫБЫЛО МАТЕРЕЙ"); rowH1.Cells[12].MergeDown = 1;
            rowH1.Cells[13].AddParagraph("БОЛЬНЫЕ"); rowH1.Cells[13].MergeRight = 1;

            var rowH2 = table.AddRow();
            rowH2.HeightRule = RowHeightRule.AtLeast;
            rowH2.Height = "0.7cm";
            rowH2.Cells[2].AddParagraph("ПЛАН К/Д");
            rowH2.Cells[3].AddParagraph("ФАКТ К/Д");
            rowH2.Cells[4].AddParagraph("% ВЫПОЛ-\nНЕНИЯ ПЛАНА");
            rowH2.Cells[5].AddParagraph("СЕЛЬСКИМИ");
            rowH2.Cells[6].AddParagraph("МАТЕРЯМИ");
            rowH2.Cells[7].AddParagraph("СРЕДНЕЕ ПРЕБЫВАНИЕ (дн.)");
            rowH2.Cells[8].AddParagraph("ЗАНЯТОСТЬ КОЙКИ (дн.)");
            rowH2.Cells[9].AddParagraph("ОБОРОТ КОЙКИ");
            rowH2.Cells[10].AddParagraph("СРЕД. ПРОСТОЙ КОЙКИ (дн.)");
            rowH2.Cells[13].AddParagraph("ВЫБЫЛО");
            rowH2.Cells[14].AddParagraph("ПОЛЬЗОВАННЫЕ");

            StyleHeaderRows(table, new[] { rowH1, rowH2 });

            int n = 1;
            var generalTotal = data.FirstOrDefault(d => d.DepartmentName == "Total hospital");
            var departmentData = data.Where(d => d.DepartmentName != "Total hospital").ToList();
            var grouped = departmentData.GroupBy(d => d.DepartmentName);

            foreach (var departmentGroup in grouped)
            {
                ct.ThrowIfCancellationRequested();

                var total = departmentGroup.FirstOrDefault(x => x.BedType == "Total by department");
                if (total != null)
                {
                    var totalRow = table.AddRow();
                    totalRow.Format.Font.Bold = true;
                    totalRow.Shading.Color = Colors.WhiteSmoke;
                    totalRow.VerticalAlignment = VerticalAlignment.Center;

                    totalRow.Cells[0].AddParagraph(n++.ToString());
                    totalRow.Cells[1].AddParagraph(total.DepartmentName);
                    FillPart2Cells(totalRow, total, 2);
                    AlignNumericCells(totalRow, table.Columns.Count, 2);
                }

                foreach (var d in departmentGroup.Where(x => x.BedType != "Total by department"))
                {
                    ct.ThrowIfCancellationRequested();

                    var dataRow = table.AddRow();
                    dataRow.VerticalAlignment = VerticalAlignment.Center;
                    dataRow.Cells[0].AddParagraph(string.Empty);
                    dataRow.Cells[1].AddParagraph(d.BedType);
                    dataRow.Cells[1].Format.LeftIndent = "0.2cm";
                    FillPart2Cells(dataRow, d, 2);
                    AlignNumericCells(dataRow, table.Columns.Count, 2);
                }
            }

            if (generalTotal != null)
            {
                ct.ThrowIfCancellationRequested();

                var totalRow = table.AddRow();
                totalRow.Format.Font.Bold = true;
                totalRow.Shading.Color = Colors.WhiteSmoke;
                totalRow.VerticalAlignment = VerticalAlignment.Center;
                totalRow.Cells[0].MergeRight = 1;
                var pName = totalRow.Cells[0].AddParagraph("ИТОГО");
                pName.Format.Alignment = ParagraphAlignment.Center;
                pName.Format.Font.Bold = true;
                totalRow.Cells[1].AddParagraph(string.Empty);
                FillPart2Cells(totalRow, generalTotal, 2);
                AlignNumericCells(totalRow, table.Columns.Count, 2);
            }

            return RenderToPdf(document);
        }, ct);
    }

    public async Task<byte[]> GenerateReportSevenPart2EmptyPdfBytesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            var document = CreateNoDataDocument(
                title: "Отчёт №7 (часть 2) — показатели использования коечного фонда",
                startDate: startDate,
                endDate: endDate);
            return RenderToPdf(document);
        }, ct);
    }

    private static void FillPart2Cells(Row row, ReportSevenPart2DTO d, int startCol)
    {
        row.Cells[startCol].AddParagraph(d.PlanForPeriod.ToString("N0"));
        row.Cells[startCol + 1].AddParagraph(d.FactBedDays.ToString("N0"));
        row.Cells[startCol + 2].AddParagraph(d.FulfillmentPercentage.ToString("N2"));
        row.Cells[startCol + 3].AddParagraph(d.FromVillage.ToString("N0"));
        row.Cells[startCol + 4].AddParagraph(d.Mothers.ToString("N0"));
        row.Cells[startCol + 5].AddParagraph(d.AverageStay.ToString("N2"));
        row.Cells[startCol + 6].AddParagraph(d.BedOccupancy.ToString("N2"));
        row.Cells[startCol + 7].AddParagraph(d.BedTurnover.ToString("N2"));
        row.Cells[startCol + 8].AddParagraph(d.AverageBedDowntime.ToString("N2"));
        row.Cells[startCol + 9].AddParagraph(d.MortalityRate.ToString("N2"));
        row.Cells[startCol + 10].AddParagraph(d.DischargedMothers.ToString("N0"));
        row.Cells[startCol + 11].AddParagraph(d.DischargedPatients.ToString("N0"));
        row.Cells[startCol + 12].AddParagraph(d.UsedPatients.ToString("N2"));
    }

    private static void StyleHeaderRows(Table table, Row[] rows)
    {
        const double headerFontPt = 6;
        foreach (var row in rows)
        {
            row.HeightRule = RowHeightRule.AtLeast;
            row.Height = "1.05cm";
            row.Shading.Color = Colors.LightGray;
            row.VerticalAlignment = VerticalAlignment.Center;
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var cell = row.Cells[i];
                cell.Format.Alignment = ParagraphAlignment.Center;
                cell.Format.Font.Bold = true;
                cell.Format.Font.Size = headerFontPt;
                cell.VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }

    private static void AlignNumericCells(Row row, int columnCount, int fromIndex)
    {
        for (int i = fromIndex; i < columnCount; i++)
            row.Cells[i].Format.Alignment = ParagraphAlignment.Center;
    }

    private static Document CreateNoDataDocument(string title, DateOnly startDate, DateOnly endDate)
    {
        var document = new Document();
        document.Info.Title = title;
        var section = document.AddSection();

        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Portrait;
        section.PageSetup.LeftMargin = "2cm";
        section.PageSetup.RightMargin = "2cm";
        section.PageSetup.TopMargin = "2cm";
        section.PageSetup.BottomMargin = "2cm";

        var style = document.Styles["Normal"];
        style.Font.Name = "Arial";
        style.Font.Size = 11;
        style.ParagraphFormat.SpaceAfter = 0;

        var p = section.AddParagraph("БРЯНСКАЯ ДЕТСКАЯ ОБЛАСТНАЯ БОЛЬНИЦА");
        p.Format.Font.Bold = true;
        p.Format.Alignment = ParagraphAlignment.Center;
        p.Format.SpaceAfter = 6;

        p = section.AddParagraph(title);
        p.Format.Font.Bold = true;
        p.Format.Alignment = ParagraphAlignment.Center;
        p.Format.SpaceAfter = 6;

        p = section.AddParagraph($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}");
        p.Format.Font.Bold = true;
        p.Format.Alignment = ParagraphAlignment.Center;
        p.Format.SpaceAfter = "1.2cm";

        p = section.AddParagraph("Данных за выбранный период нет.");
        p.Format.Font.Size = 14;
        p.Format.Font.Bold = true;
        p.Format.Alignment = ParagraphAlignment.Center;

        return document;
    }

    private static byte[] RenderToPdf(Document document)
    {
        using var ms = new MemoryStream();
        var pdfRenderer = new PdfDocumentRenderer();
        pdfRenderer.Document = document;
        pdfRenderer.RenderDocument();
        pdfRenderer.PdfDocument.Save(ms, false);
        return ms.ToArray();
    }
}
