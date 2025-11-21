namespace SnapshotExpert.Data.Values.Primitives;

public class Integer32Value(int value = 0) : PrimitiveValue,
    INumberValue
{
    public override string DebuggerString => $"(Integer32) {Value}";

    public int Value { get; set; } = value;

    public override bool ContentEquals(SnapshotValue? value)
        => value is IInteger32Value other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator int(Integer32Value value) => value.Value;

    public static implicit operator Integer32Value(int value) => new(value);

    long IInteger64Value.Value
    {
        get => Value;
        set => Value = (int)value;
    }

    double IFloat64Value.Value
    {
        get => Value;
        set => Value = (int)value;
    }

    decimal IDecimalValue.Value
    {
        get => Value;
        set => Value = decimal.ToInt32(value);
    }
}

public class Integer64Value(long value = 0) : PrimitiveValue, INumberValue
{
    public override string DebuggerString => $"(Integer64) {Value}";

    public long Value { get; set; } = value;

    public override bool ContentEquals(SnapshotValue? value)
        => value is IInteger64Value other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator long(Integer64Value value) => value.Value;

    public static implicit operator Integer64Value(long value) => new(value);
    
    int IInteger32Value.Value
    {
        get => (int)Value;
        set => Value = value;
    }

    double IFloat64Value.Value
    {
        get => Value;
        set => Value = (long)value;
    }

    decimal IDecimalValue.Value
    {
        get => Value;
        set => Value = decimal.ToInt64(value);
    }
}