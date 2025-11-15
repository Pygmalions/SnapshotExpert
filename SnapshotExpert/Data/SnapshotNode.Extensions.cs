using System.Runtime.CompilerServices;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data;

public static class SnapshotNodeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SnapshotNode BindObject(this SnapshotNode node, object? instance)
    {
        node.Object = instance;
        return node;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SnapshotNode BindType(this SnapshotNode node, Type? type)
    {
        node.Type = type;
        return node;
    }
    
    /// <summary>
    /// Acquire the value of the specified snapshot node,
    /// or throw an exception if the value is not of the expected type.
    /// </summary>
    /// <param name="node">Node to acquire snapshot value from.</param>
    /// <typeparam name="TSnapshotValue">Required type of the snapshot value.</typeparam>
    /// <returns>Snapshot value of the required type from the specified snapshot node.</returns>
    /// <exception cref="InvalidOperationException">
    /// Throw if no value is bound to the specified snapshot node,
    /// or if the value is not of the expected type.
    /// </exception>
    public static TSnapshotValue RequireValue<TSnapshotValue>(this SnapshotNode node)
        where TSnapshotValue : SnapshotValue
    {
        if (node.Value is null)
            throw new InvalidOperationException(
                "Unexpected snapshot value: no value is bound to the specified snapshot node.");
        if (node.Value is not TSnapshotValue typedValue)
            throw new InvalidOperationException(
                $"Unexpected snapshot value: snapshot value is of type {node.Value.GetType()}, " +
                $"but {typeof(TSnapshotValue)} is expected.");
        return typedValue;
    }

    /// <summary>
    /// Acquire the value of the specified snapshot node,
    /// or throw an exception if the value is not of the expected type.
    /// </summary>
    /// <param name="node">Node to acquire snapshot value from.</param>
    /// <typeparam name="TNumberInterface">Required number interface of the snapshot value.</typeparam>
    /// <returns>Snapshot value of the required type from the specified snapshot node.</returns>
    /// <exception cref="InvalidOperationException">
    /// Throw if no value is bound to the specified snapshot node,
    /// or if the value is not of the expected type.
    /// </exception>
    public static TNumberInterface RequireNumber<TNumberInterface>(this SnapshotNode node) 
        where TNumberInterface : INumberInterface
    {
        if (node.Value is null)
            throw new InvalidOperationException(
                "Unexpected snapshot value: no value is bound to the specified snapshot node.");
        if (node.Value is not TNumberInterface typedValue)
            throw new InvalidOperationException(
                $"Unexpected snapshot value: snapshot value is of type {node.Value.GetType()}, " +
                $"but {typeof(TNumberInterface)} is expected.");
        return typedValue;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignNull(this SnapshotNode node)
        => node.Value = new NullValue();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ObjectValue AssignObject(
        this SnapshotNode node, 
        Dictionary<string, SnapshotValue>? content = null)
    {
        var value = new ObjectValue();
        node.Value = value;
        if (content == null) 
            return value;
        foreach (var (name, member) in content)
            value.CreateNode(name).Value = member;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayValue AssignArray(this SnapshotNode node, SnapshotValue[]? content = null)
    {
        var value = new ArrayValue();
        node.Value = value;
        if (content == null) 
            return value;
        foreach (var item in content)
            value.CreateNode().Value = item;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignReference(this SnapshotNode node, SnapshotNode reference)
    {
        var value = new InternalReferenceValue(reference);
        node.Value = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignReference(this SnapshotNode node, string identifier)
    {
        var value = new ExternalReferenceValue(identifier);
        node.Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, bool value)
        => node.Value = new BooleanValue(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, string? value)
        => node.Value = value != null ? new StringValue(value) : new NullValue();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, byte[] value, 
        BinaryValue.BinaryContentType content = BinaryValue.BinaryContentType.Unknown)
        => node.Value = new BinaryValue(value, content);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, int value)
        => node.Value = new Integer32Value(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, long value)
        => node.Value = new Integer64Value(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, double value)
        => node.Value = new Float64Value(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, decimal value)
        => node.Value = new DecimalValue(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, DateTimeOffset value)
        => node.Value = new DateTimeValue(value);
    
    public static void AssignValue(this SnapshotNode node, Guid value)
        => node.Value = new BinaryValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, int? value)
    {
        if (value == null)
            node.Value = new NullValue();
        else
            node.Value = new Integer32Value(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, long? value)
    {
        if (value == null)
            node.Value = new NullValue();
        else
            node.Value = new Integer64Value(value.Value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, double? value)
    {
        if (value == null)
            node.Value = new NullValue();
        else
            node.Value = new Float64Value(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, decimal? value)
    {
        if (value == null)
            node.Value = new NullValue();
        else
            node.Value = new DecimalValue(value.Value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssignValue(this SnapshotNode node, DateTime? value)
    {
        if (value == null)
            node.Value = new NullValue();
        else
            node.Value = new DateTimeValue(value.Value);
    }
}