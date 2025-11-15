namespace SnapshotExpert.Data.Values;

public abstract class PrimitiveValue : SnapshotValue
{
    internal override SnapshotNode? this[string name] => null;
}