namespace ProductManagement.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
    public bool IsDeleted { get; protected set; } = false;

    public void MarkAsDeleted() => IsDeleted = true;
    public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;
}
