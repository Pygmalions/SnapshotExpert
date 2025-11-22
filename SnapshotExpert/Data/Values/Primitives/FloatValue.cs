namespace SnapshotExpert.Data.Values.Primitives;

public class Float64Value(double value = 0) : PrimitiveValue, INumberValue
{
    public override string DebuggerString => $"{Value}";

    public double Value { get; set; } = value;

    int IInteger32Value.Value
    {
        get => (int)Value;
        set => Value = value;
    }

    long IInteger64Value.Value
    {
        get => (long)Value;
        set => Value = value;
    }

    decimal IDecimalValue.Value
    {
        get => (decimal)Value;
        set => Value = decimal.ToDouble(value);
    }

    /// <summary>
    /// Compare the content of this value with another value.
    /// Two floating point numbers are considered equal if their difference is less than 1e-12.
    /// </summary>
    public override bool ContentEquals(SnapshotValue? value)
        => value is IFloat64Value other && Math.Abs(Value - other.Value) < 1e-12;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator double(Float64Value value) => value.Value;

    public static implicit operator Float64Value(double value) => new(value);
}