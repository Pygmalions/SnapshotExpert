namespace SnapshotExpert.Data;

/// <summary>
/// This comparer compares two <see cref="SnapshotNode"/>
/// instances by comparing their <see cref="SnapshotNode.Value"/>.
/// It uses <see cref="SnapshotValue.ContentEquals(SnapshotValue?, SnapshotValue?)"/> for comparison.
/// and <see cref="SnapshotValue.GetContentHashCode"/> for hash code generation.
/// </summary>
public class SnapshotNodeContentEqualityComparer: IEqualityComparer<SnapshotNode?>
{
    public static SnapshotNodeContentEqualityComparer Instance { get; } = new();
    
    public bool Equals(SnapshotNode? x, SnapshotNode? y)
        => ReferenceEquals(x, y) || SnapshotValue.ContentEquals(x?.Value, y?.Value);

    public int GetHashCode(SnapshotNode value)
        => value.Value?.GetContentHashCode() ?? typeof(SnapshotNode).GetHashCode();
}