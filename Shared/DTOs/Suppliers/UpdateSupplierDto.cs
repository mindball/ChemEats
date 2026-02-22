using Shared.Common.Enums;
using Shared.DTOs.Menus;

namespace Shared.DTOs.Suppliers;

public class UpdateSupplierDto
{
    public Guid Id { get; set; } // non-nullable
    public string Name { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public PaymentTermsUI PaymentTerms { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? SupervisorId { get; set; }
    public List<MenuDto> Menus { get; set; } = [];
}