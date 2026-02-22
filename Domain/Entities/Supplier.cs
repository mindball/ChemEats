using Domain.Common.Enums;
using Domain.Infrastructure.Exceptions;
using Domain.Infrastructure.Identity;

namespace Domain.Entities;

public class Supplier
{
    private readonly List<Menu> _menus = new();

    private Supplier() { }

    public Supplier(Guid id, string name, string vatNumber, PaymentTerms paymentTerms)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Supplier name is required");

        if (string.IsNullOrWhiteSpace(vatNumber))
            throw new DomainException("VAT number is required");

        Id = id;
        Name = name;
        VatNumber = vatNumber;
        PaymentTerms = paymentTerms;
    }

    public static Supplier Create(string name, string vatNumber, PaymentTerms paymentTerms, IEnumerable<Menu>? menus = null)
    {
        Supplier supplier = new(Guid.NewGuid(), name, vatNumber, paymentTerms);

        if (menus != null)
        {
            foreach (Menu menu in menus)
                supplier.AddMenu(menu);
        }

        return supplier;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string VatNumber { get; private set; }
    public PaymentTerms PaymentTerms { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? StreetAddress { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }

    public string? SupervisorId { get; private set; }
    public ApplicationUser? Supervisor { get; private set; }

    public IReadOnlyCollection<Menu> Menus => _menus.AsReadOnly();

    public void AssignSupervisor(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("Supervisor user ID is required");

        SupervisorId = userId;
    }

    public void RemoveSupervisor()
    {
        SupervisorId = null;
        Supervisor = null;
    }

    public bool IsSupervisor(string userId) =>
        !string.IsNullOrEmpty(SupervisorId) && SupervisorId == userId;

    public void AddMenu(Menu menu)
    {
        if (_menus.Any(m => m.Date == menu.Date))
            throw new DomainException(
                $"Menu for {menu.Date:d} already exists for this supplier");

        _menus.Add(menu);
    }
}

