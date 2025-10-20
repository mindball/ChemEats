using Domain.Common.Enums;
using Domain.Entities;
using StronglyTypedIds;

namespace Domain.Entities;

public class Supplier
{
    private readonly List<Menu> _menus = new();

    private Supplier() { }

    public Supplier(Guid id, string name, string vatNumber, PaymentTerms paymentTerms)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        VatNumber = vatNumber ?? throw new ArgumentNullException(nameof(vatNumber));
        PaymentTerms = paymentTerms;
    }

    public static Supplier Create(string name, string vatNumber, PaymentTerms paymentTerms, IEnumerable<Menu>? menus = null)
    {
        var supplier = new Supplier(Guid.NewGuid(), name, vatNumber, paymentTerms);
        if (menus != null)
        {
            foreach (var menu in menus)
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

    public IReadOnlyCollection<Menu> Menus => _menus.AsReadOnly();

    public void AddMenu(Menu menu)
    {
        if (menu is null) throw new ArgumentNullException(nameof(menu));
        _menus.Add(menu);
    }
}

