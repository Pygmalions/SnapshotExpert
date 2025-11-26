using MongoDB.Bson;
using MongoDB.Bson.IO;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Data.IO;

public static class SnapshotBsonWriterExtensions
{
    private static void DumpNodeMetadata(
        IBsonWriter writer, SnapshotNode node,
        bool withType = true, bool withMode = true)
    {
        if (withType && node.Type != null)
        {
            if (node.Type.MinimalAssemblyQualifiedName is not { } typeString)
                throw new Exception(
                    $"Failed to write node: associated type '{node.Type}' has no assembly qualified name.");
            writer.WriteName(SnapshotNode.Keywords.Type);
            writer.WriteString(typeString);
        }

        if (withMode && node.Mode != SnapshotModeType.Patching)
        {
            writer.WriteName(SnapshotNode.Keywords.Mode);
            writer.WriteString(node.Mode.ToString());
        }
    }

    private static void WritePrimitiveValue(IBsonWriter writer, PrimitiveValue target)
    {
        switch (target)
        {
            case NullValue:
                writer.WriteNull();
                break;
            case BooleanValue booleanValue:
                writer.WriteBoolean(booleanValue.Value);
                break;
            case StringValue stringValue:
                writer.WriteString(stringValue.Value);
                break;
            case BinaryValue binaryValue:
                writer.WriteBinaryData(new BsonBinaryData(binaryValue.Value, binaryValue.ContentType switch
                {
                    BinaryValue.BinaryContentType.Unknown => BsonBinarySubType.UserDefined,
                    BinaryValue.BinaryContentType.Hash => BsonBinarySubType.MD5,
                    BinaryValue.BinaryContentType.Guid => BsonBinarySubType.UuidStandard,
                    BinaryValue.BinaryContentType.Vector => BsonBinarySubType.Vector,
                    BinaryValue.BinaryContentType.Function => BsonBinarySubType.Function,
                    BinaryValue.BinaryContentType.Encrypted => BsonBinarySubType.Encrypted,
                    BinaryValue.BinaryContentType.Sensitive => BsonBinarySubType.Sensitive,
                    _ => throw new Exception($"Unsupported binary content type: {binaryValue.ContentType}.")
                }));
                break;
            case Integer32Value integer32Value:
                writer.WriteInt32(integer32Value.Value);
                break;
            case Integer64Value integer64Value:
                writer.WriteInt64(integer64Value.Value);
                break;
            case Float64Value float64Value:
                writer.WriteDouble(float64Value.Value);
                break;
            case DecimalValue decimalValue:
                writer.WriteDecimal128(decimalValue.Value);
                break;
            case DateTimeValue dateTimeValue:
                writer.WriteDateTime(dateTimeValue.ToUtcTime.Millisecond);
                break;
            default:
                throw new Exception($"Unsupported primitive self.Value type: '{target.GetType()}'.");
        }
    }

    extension(SnapshotNode self)
    {
        /// <summary>
        /// Write the BSON data of this snapshot node tree
        /// (including this node and its descendents) into the specified writer.
        /// </summary>
        /// <param name="writer">Writer to write BSON data into.</param>
        /// <exception cref="InvalidOperationException">
        /// Throw if no value is bound to this node, a.k.a. <see cref="SnapshotNode.Value"/> is null.
        /// </exception>
        /// <exception cref="Exception">
        /// Throw if the value contains an unsupported part.
        /// </exception>
        public void Dump(IBsonWriter writer)
        {
            switch (self.Value)
            {
                case null:
                    throw new InvalidOperationException("Failed to write node: node has no self.Value assigned.");
                case PrimitiveValue primitive:
                {
                    var hasValueMetadata = self.Type != null;
                    if (hasValueMetadata)
                    {
                        writer.WriteStartDocument();
                        DumpNodeMetadata(writer, self, withMode: false);
                        writer.WriteName(SnapshotNode.Keywords.Value);
                    }

                    WritePrimitiveValue(writer, primitive);
                    if (hasValueMetadata)
                        writer.WriteEndDocument();
                    return;
                }
                case ReferenceValue reference:
                    writer.WriteStartDocument();
                    writer.WriteName(SnapshotNode.Keywords.Reference);
                    switch (reference)
                    {
                        case InternalReferenceValue internalReference:
                            if (internalReference.Reference?.Path is { } path)
                                writer.WriteString(path);
                            else
                                writer.WriteNull();
                            break;
                        case ExternalReferenceValue externalReference:
                            if (externalReference.Identifier is { } identifier)
                                writer.WriteString(identifier);
                            else
                                writer.WriteNull();
                            break;
                        default:
                            throw new Exception($"Unsupported reference self.Value type '{reference.GetType()}'.");
                    }

                    writer.WriteEndDocument();
                    break;
                case ObjectValue document:
                    writer.WriteStartDocument();
                    DumpNodeMetadata(writer, self);
                    foreach (var child in document.DeclaredNodes)
                    {
                        writer.WriteName(child.Name);
                        child.Dump(writer);
                    }

                    writer.WriteEndDocument();
                    break;
                case ArrayValue array:
                    var hasArrayMetadata = self.Type != null || self.Mode != SnapshotModeType.Patching;
                    if (hasArrayMetadata)
                    {
                        writer.WriteStartDocument();
                        DumpNodeMetadata(writer, self);
                        writer.WriteName(SnapshotNode.Keywords.Value);
                    }

                    writer.WriteStartArray();
                    foreach (var child in array.DeclaredNodes)
                        child.Dump(writer);
                    writer.WriteEndArray();
                    if (hasArrayMetadata)
                        writer.WriteEndDocument();
                    break;
                default:
                    throw new Exception($"Unsupported self.Value type '{self.Value.GetType()}'.");
            }
        }

        /// <summary>
        /// Convert this snapshot node tree into BSON bytes.
        /// </summary>
        /// <returns>BSON representation of this snapshot node tree.</returns>
        public byte[] DumpToBsonBytes()
        {
            if (self.Value == null)
                throw new InvalidOperationException(
                    "Failed to convert node to BSON bytes: no self.Value is bound to this node.");
            var stream = new MemoryStream();
            using var writer = new BsonBinaryWriter(stream);
            if (self.Value is not ObjectValue)
            {
                writer.WriteStartDocument();
                writer.WriteName(SnapshotNode.Keywords.Value);
                self.Dump(writer);
                writer.WriteEndDocument();
            }
            else
                self.Dump(writer);

            return stream.ToArray();
        }

        /// <summary>
        /// Convert this snapshot node tree into a <see cref="BsonDocument"/>.
        /// </summary>
        /// <returns>BSON document of this snapshot node tree.</returns>
        public BsonDocument DumpToBsonDocument()
        {
            if (self.Value == null)
                throw new InvalidOperationException(
                    "Failed to convert node to BsonDocument: no self.Value is bound to this node.");
            var document = new BsonDocument();
            using var writer = new BsonDocumentWriter(document);
            if (self.Value is not ObjectValue)
            {
                writer.WriteStartDocument();
                writer.WriteName(SnapshotNode.Keywords.Value);
                self.Dump(writer);
                writer.WriteEndDocument();
            }
            else
                self.Dump(writer);

            return document;
        }
    }
}