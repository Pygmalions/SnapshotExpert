namespace SnapshotExpert.Data.Values.Primitives;

public class DateTimeValue(DateTimeOffset value = default) : PrimitiveValue
{
    public DateTimeValue(DateTime value) : this(new DateTimeOffset(value.ToUniversalTime()))
    {
    }

    public override string DebuggerString => $"{Value}";

    public DateTimeOffset Value { get; set; } = value;

    public DateTime ToLocalTime => Value.LocalDateTime;

    public DateTime ToUtcTime => Value.UtcDateTime;

    public static implicit operator DateTimeOffset(DateTimeValue value) => value.Value;

    public static implicit operator DateTimeValue(DateTimeOffset value) => new(value);

    public override bool ContentEquals(SnapshotValue? value)
        => value is DateTimeValue other && Value == other.Value;

    public override int GetContentHashCode() => Value.GetHashCode();
}