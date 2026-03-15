using Domain.Entities;
using Domain.Models.Orders;
using QuestPDF.Infrastructure;
using WebApi.Infrastructure.Reports;

namespace ChemEats.Tests.WebApi.Infrastructure.Reports;

public class MenuReportDocumentTests
{
    [Fact]
    public void Generate_ShouldReturnPdfBytes_WhenInputIsValid()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Guid supplierId = Guid.NewGuid();
        Meal meal = Meal.Create(Guid.NewGuid(), "Soup", new Price(10m));
        Menu menu = Menu.Create(
            supplierId,
            DateTime.Today.AddDays(2),
            DateTime.Today.AddDays(1).AddHours(12),
            [meal]);

        UserOrderItem order = new(
            Guid.NewGuid(),
            "user-1",
            "Employee One",
            meal.Id,
            meal.Name,
            supplierId,
            "Supplier",
            DateTime.Today,
            menu.Date,
            10m,
            "Pending",
            false,
            false,
            0m,
            10m);

        byte[] pdfBytes = MenuReportDocument.Generate(menu, [order]);

        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 100);
    }
}
