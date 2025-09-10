﻿namespace SnapshotExpert.Framework.Values.Primitives;

public class BooleanValue(bool value = false) : PrimitiveValue
{
    internal override string DebuggerString => $"(Boolean) {Value}";

    public bool Value { get; set; } = value;
    
    public override int GetContentHashCode() => Value.GetHashCode();

    public override bool ContentEquals(SnapshotValue? value)
        => value is BooleanValue other && Value == other.Value;
    
    public static implicit operator bool(BooleanValue value) => value.Value;
    
    public static implicit operator BooleanValue(bool value) => new(value);
}