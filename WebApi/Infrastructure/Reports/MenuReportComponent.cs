using Domain.Entities;
using Domain.Models.Orders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApi.Infrastructure.Reports;

public static class MenuReportDocument
{
    public static byte[] Generate(Menu menu, IReadOnlyList<UserOrderItem> orders)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text($"Menu Report — {menu.Supplier?.Name}")
                    .SemiBold()
                    .FontSize(20)
                    .FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Component(new MenuInfoComponent(menu));
                        column.Item().Component(new OrdersTableComponent(orders));
                        column.Item().Component(new OrderSummaryComponent(orders));
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on ");
                        x.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).SemiBold();
                    });
            });
        }).GeneratePdf();
    }
}