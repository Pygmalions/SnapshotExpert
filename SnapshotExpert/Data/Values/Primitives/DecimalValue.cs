namespace SnapshotExpert.Data.Values.Primitives;

public class DecimalValue(decimal value = 0) : PrimitiveValue, INumberValue
{
    public override string DebuggerString => $"{Value}";

    public decimal Value { get; set; } = value;

    int IInteger32Value.Value
    {
        get => decimal.ToInt32(Value);
        set => Value = value;
    }

    long IInteger64Value.Value
    {
        get => decimal.ToInt64(Value);
        set => Value = value;
    }

    double IFloat64Value.Value
    {
        get => decimal.ToDouble(Value);
        set => Value = (decimal)value;
    }

    public override bool ContentEquals(SnapshotValue? value)
        => value is IDecimalValue other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator decimal(DecimalValue value) => value.Value;

    public static implicit operator DecimalValue(decimal value) => new(value);
}