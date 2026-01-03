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

        public bool AsBoolean
        {
            get
            {
                return self?.Value switch
                {
                    BooleanValue booleanValue => booleanValue.Value,
                    StringValue stringValue when bool.TryParse(stringValue.Value, out var parsedValue) => parsedValue,
                    _ => throw new InvalidCastException(
                        $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'bool'.")
                };
            }
        }

        public string AsString => self?.Value is IStringConvertibleValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'string'.");

        public byte AsByte => self?.Value is IInteger32ConvertibleValue value
            ? (byte)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'byte'.");

        public sbyte AsSByte => self?.Value is IInteger32ConvertibleValue value
            ? (sbyte)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'sbyte'.");

        public short AsInt16 => self?.Value is IInteger32ConvertibleValue value
            ? (short)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'short'.");

        public ushort AsUInt16 => self?.Value is IInteger32ConvertibleValue value
            ? (ushort)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'ushort'.");

        public int AsInt32 => self?.Value is IInteger32ConvertibleValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'int'.");

        public uint AsUInt32 => self?.Value is IInteger32ConvertibleValue value
            ? (uint)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'uint'.");

        public long AsInt64 => self?.Value is IInteger64ConvertibleValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'long'.");

        public ulong AsUInt64 => self?.Value is IInteger64ConvertibleValue value
            ? (ulong)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'ulong'.");

        public double AsDouble => self?.Value is IFloat64ConvertibleValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'float'.");

        public float AsFloat => self?.Value is IFloat64ConvertibleValue value
            ? (float)value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'double'.");

        public decimal AsDecimal => self?.Value is IDecimalConvertibleValue value
            ? value.Value
            : throw new InvalidCastException(
                $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'decimal'.");

        public DateTimeOffset AsDateTimeOffset
        {
            get
            {
                if (self?.Value is DateTimeValue value)
                    return value.Value;
                if (self?.Value is StringValue stringValue &&
                    DateTimeOffset.TryParse(stringValue.Value, out var parsedValue))
                    return parsedValue;
                throw new InvalidCastException(
                    $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'DateTimeOffset'.");
            }
        }

        public byte[] AsBytes
        {
            get
            {
                if (self?.Value is BinaryValue value)
                    return value.Value;
                if (self?.Value is StringValue stringValue)
                    return Convert.FromBase64String(stringValue.Value);
                throw new InvalidCastException(
                    $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'byte[]'.");
            }
        }

        public Guid AsGuid
        {
            get
            {
                if (self?.Value is BinaryValue value)
                    return value.AsGuid;
                if (self?.Value is StringValue stringValue)
                    return Guid.Parse(stringValue.Value);
                throw new InvalidCastException(
                    $"Cannot convert snapshot value '{self?.Value?.GetType().ToString() ?? "<Empty>"}' to 'Guid'.");
            }
        }
    }
}