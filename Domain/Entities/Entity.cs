namespace Domain.Entities;


public class Entity<TId>
{
    public TId Id { get; protected set; }

    public override bool Equals(object? obj) => 
        obj is Entity<TId> entity && EqualityComparer<TId>.Default.Equals(Id, entity.Id);

    protected bool Equals(Entity<TId> other)
    {
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id ?? throw new InvalidOperationException());
    }
}