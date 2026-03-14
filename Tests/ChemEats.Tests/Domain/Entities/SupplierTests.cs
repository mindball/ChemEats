using Domain.Common.Enums;
using Domain.Entities;
using Domain.Infrastructure.Exceptions;

namespace ChemEats.Tests.Domain.Entities;

public class SupplierTests
{
    [Fact]
    public void Constructor_WhenNameIsMissing_ShouldThrow()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            new Supplier(Guid.NewGuid(), " ", "BG123", PaymentTerms.Net10));

        Assert.Equal("Supplier name is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WhenVatNumberIsMissing_ShouldThrow()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            new Supplier(Guid.NewGuid(), "Valid Supplier", " ", PaymentTerms.Net10));

        Assert.Equal("VAT number is required.", exception.Message);
    }

    [Fact]
    public void AssignSupervisor_WhenUserIdIsMissing_ShouldThrow()
    {
        Supplier supplier = CreateSupplier();

        DomainException exception = Assert.Throws<DomainException>(() => supplier.AssignSupervisor(" "));

        Assert.Equal("Supervisor user ID is required.", exception.Message);
    }

    [Fact]
    public void AssignSupervisor_ShouldSetSupervisorAndIsSupervisorShouldReturnTrue()
    {
        Supplier supplier = CreateSupplier();
        string supervisorId = "SUP-1";

        supplier.AssignSupervisor(supervisorId);

        Assert.Equal(supervisorId, supplier.SupervisorId);
        Assert.True(supplier.IsSupervisor(supervisorId));
    }

    [Fact]
    public void RemoveSupervisor_ShouldClearSupervisorReference()
    {
        Supplier supplier = CreateSupplier();
        supplier.AssignSupervisor("SUP-1");

        supplier.RemoveSupervisor();

        Assert.Null(supplier.SupervisorId);
        Assert.False(supplier.IsSupervisor("SUP-1"));
    }

    [Fact]
    public void IsSupervisor_ShouldReturnFalse_WhenSupervisorIsDifferent()
    {
        Supplier supplier = CreateSupplier();
        supplier.AssignSupervisor("SUP-1");

        bool result = supplier.IsSupervisor("SUP-2");

        Assert.False(result);
    }

    [Fact]
    public void AddMenu_WhenSecondMenuHasSameDate_ShouldThrow()
    {
        Supplier supplier = CreateSupplier();
        Menu firstMenu = CreateMenuForSupplier(supplier.Id, DateTime.Today.AddDays(3));
        Menu duplicateDateMenu = CreateMenuForSupplier(supplier.Id, DateTime.Today.AddDays(3));

        supplier.AddMenu(firstMenu);

        DomainException exception = Assert.Throws<DomainException>(() => supplier.AddMenu(duplicateDateMenu));

        Assert.StartsWith("Menu for", exception.Message);
    }

    [Fact]
    public void Create_WithMenus_ShouldAttachMenus()
    {
        Guid supplierId = Guid.NewGuid();
        Menu firstMenu = CreateMenuForSupplier(supplierId, DateTime.Today.AddDays(3));
        Menu secondMenu = CreateMenuForSupplier(supplierId, DateTime.Today.AddDays(4));

        Supplier supplier = Supplier.Create("Supplier", "BG123", PaymentTerms.Net10, [firstMenu, secondMenu]);

        Assert.Equal(2, supplier.Menus.Count);
    }

    private static Supplier CreateSupplier()
    {
        return new Supplier(Guid.NewGuid(), "Supplier", "BG123", PaymentTerms.Net10);
    }

    private static Menu CreateMenuForSupplier(Guid supplierId, DateTime date)
    {
        Meal meal = Meal.Create(Guid.NewGuid(), "Soup", new Price(8m));
        DateTime activeUntil = DateTime.Today.AddDays(1).AddHours(12);

        return Menu.Create(supplierId, date, activeUntil, [meal]);
    }
}
