using Microsoft.AspNetCore.Components;
using Shared.Common.Enums;
using Shared.DTOs.Obligations;
using WebApp.Services.Obligations;

namespace WebApp.Pages.Orders;

public class MyObligationsBase : ComponentBase
{
    [Inject] protected IObligationDataService ObligationDataService { get; init; } = null!;

    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }

    protected List<OtherObligationDto>? Obligations { get; private set; }
    protected bool IsLoading { get; private set; }
    protected string? ErrorMessage { get; private set; }

    protected static bool IsUnpaid(OtherObligationDto obligation) =>
        obligation.PaymentStatus == PaymentStatusDto.Unpaid;

    protected int UnpaidCount => Obligations?.Count(IsUnpaid) ?? 0;
    protected decimal UnpaidTotal => Obligations?.Where(IsUnpaid).Sum(o => o.Amount) ?? 0m;
    protected decimal PaidTotal => Obligations?.Where(o => !IsUnpaid(o)).Sum(o => o.Amount) ?? 0m;

    protected override async Task OnInitializedAsync()
    {
        // Default: current month, the same period the payments are made for
        StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = StartDate.Value.AddMonths(1).AddDays(-1);

        await LoadObligationsAsync();
    }

    protected async Task LoadObligationsAsync()
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
        {
            ErrorMessage = "End date cannot be before start date.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        Obligations = null;

        try
        {
            Obligations = await ObligationDataService.GetMyObligationsAsync(StartDate, EndDate);

            if (Obligations is null)
                ErrorMessage = "Failed to load your obligations. Please try again.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load your obligations: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
