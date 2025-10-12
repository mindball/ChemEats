using Shared.Common.Enums;
using Shared.DTOs.Menus;

namespace Shared.DTOs.Suppliers;

public record CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public PaymentTermsUI PaymentTerms { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? StreetAddress { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public List<MenuDto> Menus { get; init; } = [];
}