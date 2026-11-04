using Domain.Infrastructure.Exceptions;

namespace Domain.Entities;

// A predefined kind of personal expense; other obligations are created only from these.
public class ObligationType
{
    public const int NameMaxLength = 150;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsDeleted { get; private set; }

    private ObligationType() { }

    private ObligationType(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static ObligationType Create(string name)
    {
        return new ObligationType(Guid.NewGuid(), ValidateName(name));
    }

    public void Rename(string name)
    {
        if (IsDeleted)
            throw new DomainException("Deleted expense types cannot be renamed.");

        Name = ValidateName(name);
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            throw new DomainException("Expense type is already deleted.");

        IsDeleted = true;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Expense type name is required.");

        string trimmed = name.Trim();

        if (trimmed.Length > NameMaxLength)
            throw new DomainException($"Expense type name cannot be longer than {NameMaxLength} characters.");

        return trimmed;
    }
}
