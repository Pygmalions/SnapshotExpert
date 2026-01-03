using System.Globalization;

namespace SnapshotExpert.Data.Values.Primitives;

public class Integer32Value(int value = 0) : PrimitiveValue,
    INumberConvertibleValue, IStringConvertibleValue
{
    public override string DebuggerString => $"{Value}";

    public int Value { get; set; } = value;

    long IInteger64ConvertibleValue.Value
    {
        get => Value;
        set => Value = (int)value;
    }

    double IFloat64ConvertibleValue.Value
    {
        get => Value;
        set => Value = (int)value;
    }

    decimal IDecimalConvertibleValue.Value
    {
        get => Value;
        set => Value = decimal.ToInt32(value);
    }

    string IStringConvertibleValue.Value
    {
        get => Value.ToString(CultureInfo.InvariantCulture);
        set => Value = int.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
    }

    public override bool ContentEquals(SnapshotValue? value)
        => value is IInteger32ConvertibleValue other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator int(Integer32Value value) => value.Value;

    public static implicit operator Integer32Value(int value) => new(value);
}

public class Integer64Value(long value = 0) : PrimitiveValue, INumberConvertibleValue, IStringConvertibleValue
{
    private string _value;
    public override string DebuggerString => $"(Integer64) {Value}";

    public long Value { get; set; } = value;

    int IInteger32ConvertibleValue.Value
    {
        get => (int)Value;
        set => Value = value;
    }

    double IFloat64ConvertibleValue.Value
    {
        get => Value;
        set => Value = (long)value;
    }

    decimal IDecimalConvertibleValue.Value
    {
        get => Value;
        set => Value = decimal.ToInt64(value);
    }

    string IStringConvertibleValue.Value
    {
        get => Value.ToString(CultureInfo.InvariantCulture);
        set => Value = long.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
    }

    public override bool ContentEquals(SnapshotValue? value)
        => value is IInteger64ConvertibleValue other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator long(Integer64Value value) => value.Value;

    public static implicit operator Integer64Value(long value) => new(value);
}