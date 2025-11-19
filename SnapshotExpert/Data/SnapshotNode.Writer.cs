using MongoDB.Bson;
using MongoDB.Bson.IO;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data;

public partial class SnapshotNode
{
    /// <summary>
    /// Write the BSON data of this snapshot node tree
    /// (including this node and its descendents) into the specified writer.
    /// </summary>
    /// <param name="writer">Writer to write BSON data into.</param>
    /// <exception cref="InvalidOperationException">
    /// Throw if no value is bound to this node, a.k.a. <see cref="Value"/> is null.
    /// </exception>
    /// <exception cref="Exception">
    /// Throw if the value contains an unsupported part.
    /// </exception>
    public void Dump(IBsonWriter writer)
    {
        var value = Value;
        switch (value)
        {
            case null:
                throw new InvalidOperationException("Failed to write node: node has no value assigned.");
            case PrimitiveValue primitive:
            {
                var hasValueMetadata = Type != null;
                if (hasValueMetadata)
                {
                    writer.WriteStartDocument();
                    WriteNodeMetadata(withMode: false);
                    writer.WriteName(Keywords.Value);
                }
                
                WritePrimitiveValue(primitive);
                
                if (hasValueMetadata)
                    writer.WriteEndDocument();
                return;
            }
            case ReferenceValue reference:
                writer.WriteStartDocument();
                writer.WriteName(Keywords.Reference);
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
                        throw new Exception($"Unsupported reference value type '{reference.GetType()}'.");
                }
                writer.WriteEndDocument();
                break;
            case ObjectValue document:
                writer.WriteStartDocument();
                WriteNodeMetadata();
                foreach (var (name, child) in document.Nodes)
                {
                    writer.WriteName(name);
                    child.Dump(writer);
                }
                writer.WriteEndDocument();
                break;
            case ArrayValue array:
                var hasArrayMetadata = Type != null || Mode != SnapshotModeType.Patching;
                if (hasArrayMetadata)
                {
                    writer.WriteStartDocument();
                    WriteNodeMetadata();
                    writer.WriteName(Keywords.Value);
                }

                writer.WriteStartArray();
                foreach (var child in array.Nodes)
                {
                    child.Dump(writer);
                }

                writer.WriteEndArray();

                if (hasArrayMetadata)
                    writer.WriteEndDocument();
                break;
            default:
                throw new Exception($"Unsupported value type '{value.GetType()}'.");
        }

        return;
        
        void WriteNodeMetadata(bool withType = true, bool withMode = true)
        {
            if (withType && Type != null)
            {
                if (Type.AssemblyQualifiedName is not { } typeString)
                    throw new Exception(
                        $"Failed to write node: associated type '{Type}' has no assembly qualified name.");
                typeString = string.Join(',', typeString.Split(',')[..2]);
                writer.WriteName(Keywords.Type);
                writer.WriteString(typeString);
            }

            if (withMode && Mode != SnapshotModeType.Patching)
            {
                writer.WriteName(Keywords.Mode);
                writer.WriteString(Mode.ToString());
            }
        }

        void WritePrimitiveValue(PrimitiveValue target)
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
                    throw new Exception($"Unsupported primitive value type: '{target.GetType()}'.");
            }
        }
    }

    public override string ToString()
        => ToJson();

    public string ToString(bool indent)
        => ToJson(new JsonWriterSettings { Indent = indent });

    /// <summary>
    /// Convert this snapshot node tree into a JSON string.
    /// </summary>
    /// <param name="settings">Settings for the JSON writer.</param>
    /// <returns>JSON representation of this snapshot node tree.</returns>
    public string ToJson(JsonWriterSettings? settings = null)
    {
        if (Value == null)
            throw new InvalidOperationException(
                "Failed to convert node to JSON: no value is bound to this node.");
        settings ??= JsonWriterSettings.Defaults;
        var text = new StringWriter();
        using var writer = new JsonWriter(text, settings);
        Dump(writer);
        return text.ToString();
    }

    /// <summary>
    /// Convert this snapshot node tree into BSON bytes.
    /// </summary>
    /// <returns>BSON representation of this snapshot node tree.</returns>
    public byte[] ToBson()
    {
        if (Value == null)
            throw new InvalidOperationException(
                "Failed to convert node to BSON bytes: no value is bound to this node.");
        var stream = new MemoryStream();
        using var writer = new BsonBinaryWriter(stream);
        if (Value is not ObjectValue)
        {
            writer.WriteStartDocument();
            writer.WriteName(Keywords.Value);
            Dump(writer);
            writer.WriteEndDocument();
        }
        else
            Dump(writer);

        return stream.ToArray();
    }

    /// <summary>
    /// Convert this snapshot node tree into a <see cref="BsonDocument"/>.
    /// </summary>
    /// <returns>BSON document of this snapshot node tree.</returns>
    public BsonDocument ToBsonDocument()
    {
        if (Value == null)
            throw new InvalidOperationException(
                "Failed to convert node to BsonDocument: no value is bound to this node.");
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        if (Value is not ObjectValue)
        {
            writer.WriteStartDocument();
            writer.WriteName(Keywords.Value);
            Dump(writer);
            writer.WriteEndDocument();
        }
        else
            Dump(writer);

        return document;
    }
}