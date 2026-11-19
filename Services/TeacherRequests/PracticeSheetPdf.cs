using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

/// <summary>Datos de la "Ficha de práctica" imprimible (para llevar al Economato).</summary>
public sealed record PracticeSheet(
    string Code,
    string Status,
    string Practice,
    string Subject,
    string Group,
    string Teacher,
    int? Students,
    DateTime Date,
    string Time,
    string Periods,
    string Laboratory,
    string? Observations,
    IReadOnlyList<RequestLineView> Lines,
    string? ApprovedBy,
    DateTime? ApprovedAt);

/// <summary>Genera la ficha de práctica en PDF (QuestPDF, mismo estilo institucional que el acta L-6).</summary>
public static class PracticeSheetPdf
{
    public static byte[] Generate(PracticeSheet sheet)
    {
        var es = new System.Globalization.CultureInfo("es-BO");
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("UNIVERSIDAD DEL VALLE").FontSize(16).SemiBold().FontColor(Colors.Red.Medium);
                        col.Item().Text("DIRECCIÓN DE LABORATORIOS").FontSize(10).SemiBold();
                        col.Item().Text("Ficha de práctica de laboratorio").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Text($"FICHA {sheet.Code}").FontSize(14).Bold();
                        col.Item().AlignRight().Text($"Estado: {sheet.Status}").FontSize(10);
                        col.Item().AlignRight().Text($"Emitida: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text(sheet.Practice).FontSize(13).Bold();
                        c.Item().Text($"{sheet.Subject} · Grupo {sheet.Group}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    });

                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                    {
                        void Field(IContainer container, string label, string value) => container.Column(c =>
                        {
                            c.Item().Text(label).FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                            c.Item().Text(value).FontSize(10).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            Field(c.Item(), "FECHA", sheet.Date.ToString("dddd d 'de' MMMM 'de' yyyy", es));
                            Field(c.Item().PaddingTop(6), "HORARIO", $"{sheet.Time} ({sheet.Periods})");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            Field(c.Item(), "LABORATORIO", sheet.Laboratory);
                            Field(c.Item().PaddingTop(6), "DOCENTE", sheet.Teacher);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            Field(c.Item(), "ESTUDIANTES", sheet.Students?.ToString() ?? "—");
                            Field(c.Item().PaddingTop(6), "APROBADA POR", sheet.ApprovedBy == null ? "—"
                                : $"{sheet.ApprovedBy}{(sheet.ApprovedAt is DateTime at ? $" · {at:dd/MM/yyyy}" : "")}");
                        });
                    });

                    col.Item().PaddingTop(6).Text("MATERIAL APROBADO").FontSize(12).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(28);
                            c.ConstantColumn(80);
                            c.RelativeColumn();
                            c.ConstantColumn(70);
                            c.ConstantColumn(60);
                            c.ConstantColumn(60);
                        });
                        table.Header(h =>
                        {
                            foreach (var title in new[] { "#", "CÓDIGO", "ARTÍCULO", "CANTIDAD", "ENTREGADO", "DEVUELTO" })
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(title).FontSize(8).SemiBold();
                        });
                        var i = 1;
                        foreach (var line in sheet.Lines)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text((i++).ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(line.Code).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(line.Name).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                                .Text($"{line.Quantity:0.##} {line.Unit}").FontSize(9).Bold();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text("");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text("");
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(sheet.Observations))
                    {
                        col.Item().PaddingTop(6).Text("INDICACIONES DEL DOCENTE").FontSize(9).SemiBold().FontColor(Colors.Grey.Medium);
                        col.Item().Text(sheet.Observations).FontSize(9);
                    }

                    col.Item().PaddingTop(40).Row(row =>
                    {
                        foreach (var who in new[] { "Docente", "Entregó (Economato)", "Recibió devolución" })
                            row.RelativeItem().PaddingHorizontal(10).Column(c =>
                            {
                                c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(4).AlignCenter().Text(who).FontSize(8);
                            });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Laboratorios Univalle · Ficha generada por el sistema · Página ").FontSize(7).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(7);
                });
            });
        }).GeneratePdf();
    }
}
