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
        decimal totalGross = _orders.Sum(o => o.Price);
        decimal totalPortion = _orders.Sum(o => o.PortionApplied ? o.PortionAmount : 0m);
        decimal totalNet = _orders.Sum(o => o.NetAmount);

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
                    row.RelativeItem().Text("Gross Amount:").Bold();
                    row.RelativeItem().AlignRight().Text(totalGross.ToString("C", bgCulture));
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Company Portion:").Bold();
                    row.RelativeItem().AlignRight()
                        .Text(totalPortion.ToString("C", bgCulture))
                        .FontColor(Colors.Green.Darken2);
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Net Payable:").Bold().FontSize(14);
                    row.RelativeItem().AlignRight()
                        .Text(totalNet.ToString("C", bgCulture))
                        .Bold()
                        .FontSize(14)
                        .FontColor(Colors.Red.Darken1);
                });
            });
    }
}