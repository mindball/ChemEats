using Domain.Models.Orders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApi.Infrastructure.Reports;

public sealed class MealsSummaryTableComponent : IComponent
{
    private readonly IReadOnlyList<UserOrderItem> _orders;

    public MealsSummaryTableComponent(IReadOnlyList<UserOrderItem> orders)
    {
        _orders = orders;
    }

    public void Compose(IContainer container)
    {
        var mealSummaries = _orders
            .GroupBy(o => o.MealName)
            .Select(g => new { MealName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);  // №
                columns.RelativeColumn(3);   // Meal
                columns.RelativeColumn(1);   // Count
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("№").Bold();
                header.Cell().Element(HeaderStyle).Text("Meal").Bold();
                header.Cell().Element(HeaderStyle).AlignRight().Text("Count").Bold();
            });

            int index = 1;
            foreach (var meal in mealSummaries)
            {
                table.Cell().Element(CellStyle).Text(index.ToString());
                table.Cell().Element(CellStyle).Text(meal.MealName);
                table.Cell().Element(CellStyle).AlignRight().Text($"{meal.Count} бр.");
                index++;
            }
        });

        static IContainer HeaderStyle(IContainer c) =>
            c.Background(Colors.Blue.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

        static IContainer CellStyle(IContainer c) =>
            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
    }
}
