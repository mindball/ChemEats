using Shared.Common.Enums;
using Shared.DTOs.Menus;

namespace Shared.DTOs.Suppliers;

public record SupplierDto
{
    public Guid Id { get; init; } 
    public string Name { get; init; } = string.Empty;
    public string VatNumber { get; init; } = string.Empty;

    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? StreetAddress { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    public string? SupervisorId { get; init; }
    public string? SupervisorName { get; init; }

    public PaymentTermsUI PaymentTerms { get; init; }

    public List<MenuDto> Menus { get; init; } = [];
}