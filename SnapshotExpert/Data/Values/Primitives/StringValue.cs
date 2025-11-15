namespace SnapshotExpert.Data.Values.Primitives;

public class StringValue(string value = "") : PrimitiveValue
{
    internal override string DebuggerString => $"(String) \"{Value}\"";

    public string Value { get; set; } = value;
    
    public static implicit operator string(StringValue value) => value.Value;
    
    public static implicit operator StringValue(string value) => new(value);

    public override bool ContentEquals(SnapshotValue? value)
    {
        return value is StringValue other && string.Equals(Value, other.Value);
    }

    public override int GetContentHashCode() => Value.GetHashCode();
}