using System.Globalization;

namespace SnapshotExpert.Data.Values.Primitives;

public class Float64Value(double value = 0) : PrimitiveValue, INumberConvertibleValue, IStringConvertibleValue
{
    public override string DebuggerString => $"{Value}";

    public double Value { get; set; } = value;

    int IInteger32ConvertibleValue.Value
    {
        get => (int)Value;
        set => Value = value;
    }

    long IInteger64ConvertibleValue.Value
    {
        get => (long)Value;
        set => Value = value;
    }

    decimal IDecimalConvertibleValue.Value
    {
        get => (decimal)Value;
        set => Value = decimal.ToDouble(value);
    }

    string IStringConvertibleValue.Value
    {
        get => Value.ToString(CultureInfo.InvariantCulture);
        set => Value = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Compare the content of this value with another value.
    /// Two floating point numbers are considered equal if their difference is less than 1e-12.
    /// </summary>
    public override bool ContentEquals(SnapshotValue? value)
        => value is IFloat64ConvertibleValue other && Math.Abs(Value - other.Value) < 1e-12;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator double(Float64Value value) => value.Value;

    public static implicit operator Float64Value(double value) => new(value);
}