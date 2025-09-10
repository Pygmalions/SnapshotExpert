﻿namespace SnapshotExpert.Framework.Values;

/// <summary>
/// This value represents a reference to another node in the snapshot or an external object.
/// </summary>
public abstract class ReferenceValue : SnapshotValue
{
    internal ReferenceValue() {}
    
    internal override SnapshotNode? this[string name] => null;
}

/// <summary>
/// This value represents a reference to another node within the same snapshot.
/// </summary>
/// <param name="reference">Referenced node in the same snapshot.</param>
public class InternalReferenceValue(SnapshotNode? reference = null) : ReferenceValue
{
    internal override string DebuggerString => $"(InternalReference) '{Reference?.Path ?? "null"}'";
    
    public SnapshotNode? Reference { get; set; } = reference;

    public override bool ContentEquals(SnapshotValue? value)
    {
        if (value is not InternalReferenceValue other)
            return false;
        return Reference == other.Reference;
    }
    
    private static int DefaultContentHashCode { get; } = typeof(InternalReferenceValue).GetHashCode();

    public override int GetContentHashCode() => Reference?.GetHashCode() ?? DefaultContentHashCode;
}

/// <summary>
/// This value represents a reference to an external object, identified by a string identifier.
/// </summary>
/// <param name="identifier">Identifier to the referenced external object.</param>
public class ExternalReferenceValue(string? identifier = null) : ReferenceValue
{
    private static int DefaultContentHashCode { get; } = typeof(ExternalReferenceValue).GetHashCode();

    internal override string DebuggerString => $"(ExternalReference) '{Identifier ?? "null"}'";

    public string? Identifier { get; set; } = identifier;

    public override bool ContentEquals(SnapshotValue? value)
    {
        if (value is not ExternalReferenceValue other)
            return false;
        return Identifier == other.Identifier;
    }
    
    public override int GetContentHashCode() => Identifier?.GetHashCode() ?? DefaultContentHashCode;
}