namespace SnapshotExpert.Framework.Values.Primitives;

public class DecimalValue(decimal value = 0) : PrimitiveValue, INumberValue
{
    internal override string DebuggerString => $"(Decimal) {Value}";
    
    public decimal Value { get; set; } = value;
    
    public override bool ContentEquals(SnapshotValue? value)
        =>  value is IDecimalNumber other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();
    
    public static implicit operator decimal(DecimalValue value) => value.Value;
    
    public static implicit operator DecimalValue(decimal value) => new(value);

    int IInteger32Number.Value
    {
        get => decimal.ToInt32(Value);
        set => Value = value;
    }
    
    long IInteger64Number.Value
    {
        get => decimal.ToInt64(Value);
        set => Value = value;
    }

    double IFloat64Number.Value
    {
        get => decimal.ToDouble(Value);
        set => Value = (decimal)value;
    }
}