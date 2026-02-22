using Domain.Models.Orders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace WebApi.Infrastructure.Reports;

public sealed class OrdersTableComponent : IComponent
{
    private readonly IReadOnlyList<UserOrderItem> _orders;

    public OrdersTableComponent(IReadOnlyList<UserOrderItem> orders)
    {
        _orders = orders;
    }

    public void Compose(IContainer container)
    {
        CultureInfo bgCulture = CultureInfo.GetCultureInfo("bg-BG");

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);  // №
                columns.RelativeColumn(2);   // Employee
                columns.RelativeColumn(3);   // Meal
                columns.RelativeColumn(1);   // Price
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("№").Bold();
                header.Cell().Element(HeaderStyle).Text("Employee").Bold();
                header.Cell().Element(HeaderStyle).Text("Meal").Bold();
                header.Cell().Element(HeaderStyle).AlignRight().Text("Price").Bold();
            });

            int index = 1;
            foreach (UserOrderItem order in _orders)
            {
                table.Cell().Element(CellStyle).Text(index.ToString());
                table.Cell().Element(CellStyle).Text(order.EmployeeName);
                table.Cell().Element(CellStyle).Text(order.MealName);
                table.Cell().Element(CellStyle).AlignRight()
                    .Text(order.Price.ToString("C", bgCulture));
                index++;
            }
        });

        static IContainer HeaderStyle(IContainer c) =>
            c.Background(Colors.Blue.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

        static IContainer CellStyle(IContainer c) =>
            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
    }
}