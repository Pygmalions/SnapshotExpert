using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data;

/// <summary>
/// Interface for extension methods of 'As...', such as 'AsBoolean' and 'AsString'.
/// </summary>
public interface ISnapshotConvertible
{
    SnapshotValue? Value { get; }
}

public static class SnapshotConvertibleExtensions
{
    extension(ISnapshotConvertible? self)
    {
        /// <summary>
        /// Check if this node is empty, which means it is not bound to any value.
        /// </summary>
        public bool IsEmpty => self?.Value is null;

        /// <summary>
        /// Check if this node is bound to a null value.
        /// </summary>
        public bool IsNull => self?.Value is NullValue;
        
        public ObjectValue AsObject => self?.Value as ObjectValue ?? throw new InvalidCastException(
            $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to a object value.");

        public ArrayValue AsArray => self?.Value as ArrayValue ?? throw new InvalidCastException(
            $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to an array value.");

        public bool AsBoolean => self?.Value is BooleanValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'bool'.");

        public string AsString => self?.Value is StringValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'string'.");

        public byte AsByte => self?.Value is IInteger32Value value
            ? (byte)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'byte'.");
        
        public sbyte AsSByte => self?.Value is IInteger32Value value
            ? (sbyte)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'sbyte'.");
        
        public short AsInt16 => self?.Value is IInteger32Value value
            ? (short)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'short'.");
        
        public ushort AsUInt16 => self?.Value is IInteger32Value value
            ? (ushort)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'ushort'.");
        
        public int AsInt32 => self?.Value is IInteger32Value value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'int'.");

        public uint AsUInt32 => self?.Value is IInteger32Value value
            ? (uint)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'uint'.");
        
        public long AsInt64 => self?.Value is IInteger64Value value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'long'.");

        public ulong AsUInt64 => self?.Value is IInteger64Value value
            ? (ulong)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'ulong'.");
        
        public double AsDouble => self?.Value is IFloat64Value value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'float'.");

        public float AsFloat => self?.Value is IFloat64Value value
            ? (float)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'double'.");

        public decimal AsDecimal => self?.Value is IDecimalValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'decimal'.");

        public DateTimeOffset AsDateTimeOffset => self?.Value is DateTimeValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'DateTimeOffset'.");

        public byte[] AsBytes => self?.Value is BinaryValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'byte[]'.");

        public Guid AsGuid => self?.Value is BinaryValue value
            ? value.AsGuid
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'Guid'.");
    }
}