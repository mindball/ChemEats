using Shared.DTOs.Obligations;

namespace WebApp.Services.Obligations;

public interface IObligationDataService
{
    Task<List<OtherObligationDto>?> GetMyObligationsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<OtherObligationDto>?> GetUserObligationsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<OtherObligationDto?> CreateObligationAsync(CreateOtherObligationDto requestDto);
    Task<OtherObligationDto?> UpdateObligationAsync(Guid obligationId, UpdateOtherObligationDto requestDto);
    Task<bool> DeleteObligationAsync(Guid obligationId);
    Task<ObligationPayResponseDto?> PayObligationsAsync(ObligationPayRequestDto requestDto);
    Task<ObligationPayResponseDto?> PayAllUnpaidAsync(ObligationPayAllRequestDto requestDto);
}
