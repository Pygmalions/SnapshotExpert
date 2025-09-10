namespace SnapshotExpert.Framework.Values.Primitives;

public class Integer32Value(int value = 0) : PrimitiveValue,
    INumberValue
{
    internal override string DebuggerString => $"(Integer32) {Value}";

    public int Value { get; set; } = value;

    public override bool ContentEquals(SnapshotValue? value)
        => value is IInteger32Number other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator int(Integer32Value value) => value.Value;

    public static implicit operator Integer32Value(int value) => new(value);

    long IInteger64Number.Value
    {
        get => Value;
        set => Value = (int)value;
    }

    double IFloat64Number.Value
    {
        get => Value;
        set => Value = (int)value;
    }

    decimal IDecimalNumber.Value
    {
        get => Value;
        set => Value = decimal.ToInt32(value);
    }
}

public class Integer64Value(long value = 0) : PrimitiveValue, INumberValue
{
    internal override string DebuggerString => $"(Integer64) {Value}";

    public long Value { get; set; } = value;

    public override bool ContentEquals(SnapshotValue? value)
        => value is IInteger64Number other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator long(Integer64Value value) => value.Value;

    public static implicit operator Integer64Value(long value) => new(value);
    
    int IInteger32Number.Value
    {
        get => (int)Value;
        set => Value = value;
    }

    double IFloat64Number.Value
    {
        get => Value;
        set => Value = (long)value;
    }

    decimal IDecimalNumber.Value
    {
        get => Value;
        set => Value = decimal.ToInt64(value);
    }
}