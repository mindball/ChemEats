using Domain.Entities;

namespace Domain.Repositories.Obligations;

public interface IObligationTypeRepository
{
    Task<IReadOnlyList<ObligationType>> GetAllAsync(CancellationToken cancellationToken = default);

    // Deleted types are not returned.
    Task<ObligationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Case-insensitive match among non-deleted types, optionally ignoring one type (the one being renamed).
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(ObligationType obligationType, CancellationToken cancellationToken = default);

    // Returns false when the type does not exist (or is deleted).
    Task<bool> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
