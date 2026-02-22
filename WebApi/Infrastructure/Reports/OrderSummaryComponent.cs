using Domain.Models.Orders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace WebApi.Infrastructure.Reports;

public sealed class OrderSummaryComponent : IComponent
{
    private readonly IReadOnlyList<UserOrderItem> _orders;

    public OrderSummaryComponent(IReadOnlyList<UserOrderItem> orders)
    {
        _orders = orders;
    }

    public void Compose(IContainer container)
    {
        decimal totalAmount = _orders.Sum(o => o.Price);
        CultureInfo bgCulture = CultureInfo.GetCultureInfo("bg-BG");

        container.Background(Colors.Blue.Lighten4)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(5);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total Orders:").Bold();
                    row.RelativeItem().AlignRight().Text(_orders.Count.ToString());
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total Amount:").Bold().FontSize(14);
                    row.RelativeItem().AlignRight()
                        .Text(totalAmount.ToString("C", bgCulture))
                        .Bold()
                        .FontSize(14)
                        .FontColor(Colors.Red.Darken1);
                });
            });
    }
}