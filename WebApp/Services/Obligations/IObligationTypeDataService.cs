using Shared.DTOs.Obligations;

namespace WebApp.Services.Obligations;

public interface IObligationTypeDataService
{
    Task<List<ObligationTypeDto>?> GetObligationTypesAsync();
    Task<ObligationTypeDto?> CreateObligationTypeAsync(string name);
    Task<ObligationTypeDto?> RenameObligationTypeAsync(Guid obligationTypeId, string name);
    Task<bool> DeleteObligationTypeAsync(Guid obligationTypeId);
}
