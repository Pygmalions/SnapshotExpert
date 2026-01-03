using System.Globalization;

namespace SnapshotExpert.Data.Values.Primitives;

public class DecimalValue(decimal value = 0) : PrimitiveValue, INumberConvertibleValue, IStringConvertibleValue
{
    private string _value;
    public override string DebuggerString => $"{Value}";

    public decimal Value { get; set; } = value;

    int IInteger32ConvertibleValue.Value
    {
        get => decimal.ToInt32(Value);
        set => Value = value;
    }

    long IInteger64ConvertibleValue.Value
    {
        get => decimal.ToInt64(Value);
        set => Value = value;
    }

    double IFloat64ConvertibleValue.Value
    {
        get => decimal.ToDouble(Value);
        set => Value = (decimal)value;
    }

    string IStringConvertibleValue.Value
    {
        get => Value.ToString(CultureInfo.InvariantCulture);
        set => Value = decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public override bool ContentEquals(SnapshotValue? value)
        => value is IDecimalConvertibleValue other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator decimal(DecimalValue value) => value.Value;

    public static implicit operator DecimalValue(decimal value) => new(value);
}