namespace SnapshotExpert.Data.Values.Primitives;

public class Float64Value(double value = 0) : PrimitiveValue, INumberValue
{
    internal override string DebuggerString => $"(Float64) {Value}";

    public double Value { get; set; } = value;
    
    /// <summary>
    /// Compare the content of this value with another value.
    /// Two floating point numbers are considered equal if their difference is less than 1e-12.
    /// </summary>
    public override bool ContentEquals(SnapshotValue? value)
        => value is IFloat64Number other && Math.Abs(Value - other.Value) < 1e-12;
    
    public override int GetContentHashCode() => Value.GetHashCode();
    
    public static implicit operator double(Float64Value value) => value.Value;
    
    public static implicit operator Float64Value(double value) => new(value);

    int IInteger32Number.Value
    {
        get => (int)Value;
        set => Value = value;
    }

    long IInteger64Number.Value
    {
        get => (long)Value;
        set => Value = value;
    }

    decimal IDecimalNumber.Value
    {
        get => (decimal)Value;
        set => Value = decimal.ToDouble(value);
    }
}