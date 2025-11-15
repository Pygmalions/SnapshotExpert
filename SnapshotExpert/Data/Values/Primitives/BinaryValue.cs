using MongoDB.Bson;

namespace SnapshotExpert.Data.Values.Primitives;

public class BinaryValue(
    byte[]? value = null,
    BinaryValue.BinaryContentType contentType = BinaryValue.BinaryContentType.Unknown)
    : PrimitiveValue
{
    public BinaryValue(Guid value) :
        this(GuidConverter.ToBytes(value, GuidRepresentation.Standard), BinaryContentType.Guid)
    {
    }

    internal override string DebuggerString => $"(Binary) {ContentType} - {Value.Length} bytes)";

    public byte[] Value { get; set; } = value ?? [];

    /// <summary>
    /// The semantic meaning of this binary data.
    /// </summary>
    public BinaryContentType ContentType { get; set; } = contentType;

    /// <summary>
    /// Convert these bytes to a GUID.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Throw if the length of the binary data is not 16 bytes.
    /// </exception>
    public Guid AsGuid =>
        Value.Length != 16
            ? throw new InvalidOperationException(
                "Failed to convert binary data to GUID: its length is not 16 bytes.")
            : GuidConverter.FromBytes(Value, GuidRepresentation.Standard);

    public override bool ContentEquals(SnapshotValue? value)
    {
        return ReferenceEquals(this, value) ||
               (value is BinaryValue other &&
                ContentType == other.ContentType &&
                Value.Length == other.Value.Length &&
                Enumerable.SequenceEqual(Value, other.Value));
    }

    public override int GetContentHashCode() => Value.GetHashCode();

    public static implicit operator byte[](BinaryValue value) => value.Value;

    public static implicit operator BinaryValue(byte[] value) => new(value);

    public static implicit operator Guid(BinaryValue value) => value.AsGuid;

    public static implicit operator BinaryValue(Guid value) =>
        new(GuidConverter.ToBytes(value, GuidRepresentation.Standard), BinaryContentType.Guid);

    public enum BinaryContentType
    {
        /// <summary>
        /// Semantic meaning of this binary data is unknown or customized by users.
        /// </summary>
        Unknown,

        /// <summary>
        /// This binary data is a hash value (e.g. MD5).
        /// </summary>
        Hash,

        /// <summary>
        /// This binary data is a global unique identifier.
        /// </summary>
        Guid,

        /// <summary>
        /// This binary represents a vector.
        /// </summary>
        Vector,

        /// <summary>
        /// This binary represents a function (e.g. compiled code).
        /// </summary>
        Function,

        /// <summary>
        /// This binary represents encrypted data.
        /// </summary>
        Encrypted,

        /// <summary>
        /// This binary represents sensitive data.
        /// </summary>
        Sensitive,
    }
}