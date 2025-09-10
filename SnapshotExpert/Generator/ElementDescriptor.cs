namespace SnapshotExpert.Generator;

public abstract class ElementDescriptor
{
    /// <summary>
    /// Description for this item.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Type of the target.
    /// </summary>
    public abstract Type TargetType { get; }

    /// <summary>
    /// Type of this member.
    /// </summary>
    public abstract Type MemberType { get; }

    /// <summary>
    /// Get the value of this member from the specified instance.
    /// </summary>
    /// <param name="target">Target instance to get the value of this member from.</param>
    /// <returns>Member value.</returns>
    public abstract object? GetValue(object target);

    /// <summary>
    /// Set the value of this member on the specified instance.
    /// </summary>
    /// <param name="target">Target instance to set the value of this member on.</param>
    /// <param name="value">Member value.</param>
    public abstract void SetValue(object target, object? value);
}