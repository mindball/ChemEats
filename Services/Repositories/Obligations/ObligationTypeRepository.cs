using Domain;
using Domain.Entities;
using Domain.Repositories.Obligations;
using Microsoft.EntityFrameworkCore;

namespace Services.Repositories.Obligations;

public class ObligationTypeRepository : IObligationTypeRepository
{
    private readonly AppDbContext _dbContext;

    public ObligationTypeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<ObligationType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<ObligationType> types = await _dbContext.ObligationTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Sorted in memory: Firebird and SQLite collations differ.
        return types
            .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<ObligationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ObligationTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        string trimmed = name.Trim();

        // The table is small; comparing in memory keeps the check case-insensitive on every provider.
        List<ObligationType> types = await _dbContext.ObligationTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return types.Any(t => t.Id != excludeId && string.Equals(t.Name, trimmed, StringComparison.CurrentCultureIgnoreCase));
    }

    public async Task AddAsync(ObligationType obligationType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(obligationType);

        await _dbContext.ObligationTypes.AddAsync(obligationType, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        ObligationType? obligationType = await _dbContext.ObligationTypes
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (obligationType is null)
            return false;

        obligationType.Rename(name);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ObligationType? obligationType = await _dbContext.ObligationTypes
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (obligationType is null)
            return false;

        obligationType.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
