namespace SnapshotExpert.Framework;

/// <summary>
/// This comparer compares two <see cref="SnapshotValue"/> instances by their content.
/// It uses <see cref="SnapshotValue.ContentEquals(SnapshotValue?, SnapshotValue?)"/> for comparison,
/// and <see cref="SnapshotValue.GetContentHashCode"/> for hash code generation.
/// </summary>
public class SnapshotValueContentEqualityComparer : IEqualityComparer<SnapshotValue?>
{
    public static SnapshotValueContentEqualityComparer Instance { get; } = new();
    
    public bool Equals(SnapshotValue? x, SnapshotValue? y)
        => SnapshotValue.ContentEquals(x, y);

    public int GetHashCode(SnapshotValue value)
        => value.GetContentHashCode();
}