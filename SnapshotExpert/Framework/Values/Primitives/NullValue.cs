namespace SnapshotExpert.Framework.Values.Primitives;

public class NullValue : PrimitiveValue
{
    private static int ContentHashCode { get; } = typeof(NullValue).GetHashCode();
    
    internal override string DebuggerString => "(Null)";

    public override bool ContentEquals(SnapshotValue? value)
        => value is NullValue;
    
    public override int GetContentHashCode() => ContentHashCode;
}