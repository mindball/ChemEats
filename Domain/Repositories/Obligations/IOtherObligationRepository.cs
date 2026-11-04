using Domain.Entities;
using Domain.Models.Obligations;

namespace Domain.Repositories.Obligations;

public interface IOtherObligationRepository
{
    Task<OtherObligationItem?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OtherObligationItem>> GetForUserAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        PaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(OtherObligation obligation, CancellationToken cancellationToken = default);

    // Returns false when the obligation does not exist (or is deleted).
    Task<bool> UpdateAsync(
        Guid id,
        ObligationType obligationType,
        decimal amount,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(int PaidCount, decimal TotalPaid)> MarkAsPaidAsync(
        string userId,
        IReadOnlyList<Guid> obligationIds,
        DateTime paidOn,
        string paidByUserId,
        CancellationToken cancellationToken = default);

    // A null upToDate pays every unpaid obligation of the user, regardless of its date.
    Task<(int PaidCount, decimal TotalPaid)> MarkAllUnpaidAsPaidAsync(
        string userId,
        DateTime? upToDate,
        DateTime paidOn,
        string paidByUserId,
        CancellationToken cancellationToken = default);

    // Unpaid obligations per employee with a date up to upToDate (no lower bound); a null upToDate means all.
    Task<IReadOnlyList<EmployeeObligationSummary>> GetOutstandingSummariesAsync(
        DateTime? upToDate,
        string? userId = null,
        CancellationToken cancellationToken = default);
}
