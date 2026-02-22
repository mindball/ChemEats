using Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApi.Infrastructure.Reports;

public sealed class MenuInfoComponent : IComponent
{
    private readonly Menu _menu;

    public MenuInfoComponent(Menu menu)
    {
        _menu = menu;
    }

    public void Compose(IContainer container)
    {
        container.Background(Colors.Grey.Lighten3)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(5);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Supplier:").Bold();
                    row.RelativeItem().Text(_menu.Supplier?.Name ?? string.Empty);
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Date:").Bold();
                    row.RelativeItem().Text(_menu.Date.ToString("dd.MM.yyyy"));
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Active Until:").Bold();
                    row.RelativeItem().Text(_menu.ActiveUntil.ToString("dd.MM.yyyy HH:mm"));
                });
            });
    }
}