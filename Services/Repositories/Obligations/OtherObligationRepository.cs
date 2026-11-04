using Domain;
using Domain.Entities;
using Domain.Models.Obligations;
using Domain.Repositories.Obligations;
using Microsoft.EntityFrameworkCore;

namespace Services.Repositories.Obligations;

public class OtherObligationRepository : IOtherObligationRepository
{
    private readonly AppDbContext _dbContext;

    public OtherObligationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    #region Base Query

    // A null userId returns rows for all users (admin-wide queries).
    private IQueryable<ObligationJoinRow> BuildBaseQuery(string? userId)
    {
        IQueryable<OtherObligation> obligations = _dbContext.OtherObligations.AsNoTracking();

        if (userId is not null)
            obligations = obligations.Where(o => o.UserId == userId);

        return
            from o in obligations
            join user in _dbContext.Users.AsNoTracking()
                on o.UserId equals user.Id
            join createdBy in _dbContext.Users.AsNoTracking()
                on o.CreatedByUserId equals createdBy.Id into createdByUsers
            from createdBy in createdByUsers.DefaultIfEmpty()
            join paidBy in _dbContext.Users.AsNoTracking()
                on o.PaidByUserId equals paidBy.Id into paidByUsers
            from paidBy in paidByUsers.DefaultIfEmpty()
            select new ObligationJoinRow
            {
                Id = o.Id,
                UserId = o.UserId,
                EmployeeName = user.FullName,
                Description = o.Description,
                Amount = o.Amount,
                Date = o.Date,
                CreatedAt = o.CreatedAt,
                CreatedByName = createdBy == null ? null : createdBy.FullName,
                PaymentStatus = o.PaymentStatus,
                PaidOn = o.PaidOn,
                PaidByName = paidBy == null ? null : paidBy.FullName
            };
    }

    private static IQueryable<ObligationJoinRow> ApplyFilters(
        IQueryable<ObligationJoinRow> query,
        DateTime? startDate,
        DateTime? endDate,
        PaymentStatus? paymentStatus)
    {
        if (startDate.HasValue)
            query = query.Where(x => x.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.Date < endDate.Value.Date.AddDays(1));

        if (paymentStatus.HasValue)
            query = query.Where(x => x.PaymentStatus == paymentStatus.Value);

        return query;
    }

    private static IQueryable<OtherObligationItem> ToItems(IQueryable<ObligationJoinRow> query) =>
        query.Select(x => new OtherObligationItem(
            x.Id,
            x.UserId,
            x.EmployeeName,
            x.Description,
            x.Amount,
            x.Date,
            x.CreatedAt,
            x.CreatedByName,
            x.PaymentStatus,
            x.PaidOn,
            x.PaidByName));

    #endregion

    #region CRUD

    public async Task<OtherObligationItem?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ToItems(BuildBaseQuery(null).Where(x => x.Id == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(OtherObligation obligation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(obligation);

        bool userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == obligation.UserId, cancellationToken);

        if (!userExists)
            throw new ArgumentException($"User with id '{obligation.UserId}' does not exist.");

        await _dbContext.OtherObligations.AddAsync(obligation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        string description,
        decimal amount,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        OtherObligation? obligation = await _dbContext.OtherObligations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (obligation is null)
            return false;

        obligation.Update(description, amount, date);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        OtherObligation? obligation = await _dbContext.OtherObligations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (obligation is null)
            return false;

        obligation.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region Queries

    public async Task<IReadOnlyList<OtherObligationItem>> GetForUserAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        PaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        IQueryable<ObligationJoinRow> query = ApplyFilters(BuildBaseQuery(userId), startDate, endDate, paymentStatus)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.CreatedAt);

        return await ToItems(query).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeObligationSummary>> GetOutstandingSummariesAsync(
        DateTime? upToDate,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        List<EmployeeObligationSummary> summaries = await ApplyFilters(BuildBaseQuery(userId), null, upToDate, PaymentStatus.Unpaid)
            .GroupBy(x => new { x.UserId, x.EmployeeName })
            .Select(g => new EmployeeObligationSummary(
                g.Key.UserId,
                g.Key.EmployeeName,
                g.Count(),
                g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);

        return summaries
            .OrderBy(s => s.EmployeeName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.UserId, StringComparer.Ordinal)
            .ToList();
    }

    #endregion

    #region Payments

    public async Task<(int PaidCount, decimal TotalPaid)> MarkAsPaidAsync(
        string userId,
        IReadOnlyList<Guid> obligationIds,
        DateTime paidOn,
        string paidByUserId,
        CancellationToken cancellationToken = default)
    {
        // Already paid or unknown IDs are skipped instead of failing the whole batch.
        List<OtherObligation> obligations = await _dbContext.OtherObligations
            .Where(o => o.UserId == userId
                        && obligationIds.Contains(o.Id)
                        && o.PaymentStatus == PaymentStatus.Unpaid)
            .ToListAsync(cancellationToken);

        return await PayAsync(obligations, paidOn, paidByUserId, cancellationToken);
    }

    public async Task<(int PaidCount, decimal TotalPaid)> MarkAllUnpaidAsPaidAsync(
        string userId,
        DateTime? upToDate,
        DateTime paidOn,
        string paidByUserId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OtherObligation> query = _dbContext.OtherObligations
            .Where(o => o.UserId == userId && o.PaymentStatus == PaymentStatus.Unpaid);

        if (upToDate.HasValue)
        {
            DateTime upToExclusive = upToDate.Value.Date.AddDays(1);
            query = query.Where(o => o.Date < upToExclusive);
        }

        List<OtherObligation> obligations = await query.ToListAsync(cancellationToken);

        return await PayAsync(obligations, paidOn, paidByUserId, cancellationToken);
    }

    private async Task<(int PaidCount, decimal TotalPaid)> PayAsync(
        List<OtherObligation> obligations,
        DateTime paidOn,
        string paidByUserId,
        CancellationToken cancellationToken)
    {
        if (obligations.Count == 0)
            return (0, 0m);

        decimal totalPaid = 0m;

        foreach (OtherObligation obligation in obligations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            obligation.MarkAsPaid(paidOn, paidByUserId);
            totalPaid += obligation.Amount;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (obligations.Count, totalPaid);
    }

    #endregion

    private sealed class ObligationJoinRow
    {
        public Guid Id { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string EmployeeName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTime Date { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByName { get; init; }
        public PaymentStatus PaymentStatus { get; init; }
        public DateTime? PaidOn { get; init; }
        public string? PaidByName { get; init; }
    }
}
