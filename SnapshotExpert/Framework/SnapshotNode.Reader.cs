using MongoDB.Bson;
using MongoDB.Bson.IO;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework;

public partial class SnapshotNode
{
    /// <summary>
    /// Parse BSON data from the specified reader into a snapshot node.
    /// </summary>
    /// <param name="reader">Reader to read BSON data from.</param>
    /// <param name="node">
    /// Optional node to parse data into.
    /// If it is null, a root node will be created and used.
    /// </param>
    /// <param name="root">
    /// Root node of this snapshot node tree, used to resolve by-path references.
    /// If it is null, then current node will be used as the root node.
    /// </param>
    /// <returns>
    /// Specified node, or a new root node if the specified node is null.
    /// </returns>
    /// <exception cref="Exception">
    /// Throw when the BSON data contains invalid or unsupported part.
    /// </exception>
    public static SnapshotNode Parse(IBsonReader reader, SnapshotNode? node = null, SnapshotNode? root = null)
    {
        if (reader.State == BsonReaderState.Initial)
            reader.ReadBsonType();

        node ??= new SnapshotNode();
        root ??= node;

        switch (reader.CurrentBsonType)
        {
            case BsonType.EndOfDocument:
                throw new Exception("Failed to parse node: unexpected end of document.");

            // Supported primitive types:

            case BsonType.Null:
                node.AssignNull();
                break;
            case BsonType.Boolean:
                node.AssignValue(reader.ReadBoolean());
                break;
            case BsonType.String:
                node.AssignValue(reader.ReadString());
                break;
            case BsonType.Binary:
                var binary = reader.ReadBinaryData();
                node.AssignValue(binary.Bytes, binary.SubType switch
                {
                    BsonBinarySubType.Binary => BinaryValue.BinaryContentType.Unknown,
                    BsonBinarySubType.Function => BinaryValue.BinaryContentType.Function,
                    BsonBinarySubType.UuidStandard => BinaryValue.BinaryContentType.Guid,
                    BsonBinarySubType.MD5 => BinaryValue.BinaryContentType.Hash,
                    BsonBinarySubType.Encrypted => BinaryValue.BinaryContentType.Encrypted,
                    BsonBinarySubType.Sensitive => BinaryValue.BinaryContentType.Sensitive,
                    BsonBinarySubType.Vector => BinaryValue.BinaryContentType.Vector,
                    BsonBinarySubType.Column => BinaryValue.BinaryContentType.Unknown,
                    BsonBinarySubType.UuidLegacy => BinaryValue.BinaryContentType.Unknown,
                    BsonBinarySubType.UserDefined => BinaryValue.BinaryContentType.Unknown,
                    _ => throw new Exception($"Unsupported binary subtype '{binary.SubType}'.")
                });
                break;
            case BsonType.DateTime:
                node.AssignValue(DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadDateTime()));
                break;
            case BsonType.Double:
                node.AssignValue(reader.ReadDouble());
                break;
            case BsonType.Int32:
                node.AssignValue(reader.ReadInt32());
                break;
            case BsonType.Int64:
                node.AssignValue(reader.ReadInt64());
                break;
            case BsonType.Decimal128:
                node.AssignValue(Decimal128.ToDecimal(reader.ReadDecimal128()));
                break;

            // Supported complex types:

            case BsonType.Document:
                HandleDocument();
                break;
            
            case BsonType.Array:
                var array = node.AssignArray();
                reader.ReadStartArray();
                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var element = array.CreateNode();
                    Parse(reader, element, root);
                }

                reader.ReadEndArray();
                break;

            // Compatible types:

            case BsonType.Timestamp:
                // Timestamp as long.
                node.AssignValue(reader.ReadTimestamp());
                break;
            case BsonType.JavaScript:
                // JavaScript code as string.
                node.AssignValue(reader.ReadJavaScript());
                break;
            case BsonType.JavaScriptWithScope:
                // JavaScript code with scope as string (scope is ignored).
                node.AssignValue(reader.ReadJavaScriptWithScope());
                break;

            // Unsupported types:

            case BsonType.RegularExpression:
            case BsonType.ObjectId:
            case BsonType.Undefined:
            case BsonType.Symbol:
            case BsonType.MinKey:
            case BsonType.MaxKey:
            default:
                throw new Exception(
                    $"Failed to parse node: unsupported or invalid BSON type: {reader.CurrentBsonType}.");
        }

        return node;

        void HandleDocument()
        {
            reader.ReadStartDocument();
            ObjectValue? document = null;
            var isInHeader = true;
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = reader.ReadName();

                if (isInHeader)
                {
                    switch (name)
                    {
                        case Keywords.Reference:
                            if (reader.CurrentBsonType != BsonType.String)
                                throw new Exception("Failed to parse node: invalid $ref field type.");
                            var referenceString = reader.ReadString();
                            if (root.Locate(referenceString) is { } referencedNode)
                                node.Value = new InternalReferenceValue(referencedNode);
                            else
                                // If the reference string cannot be resolved as a path,
                                // then treat it as the identifier of an external reference.
                                node.Value = new ExternalReferenceValue(referenceString);
                            continue;

                        case Keywords.Type:
                            if (reader.CurrentBsonType != BsonType.String)
                                throw new Exception("Failed to parse node: invalid $type field type.");
                            var typeString = reader.ReadString();
                            node.Type = Type.GetType(typeString) ??
                                        throw new Exception(
                                            $"Failed to parse node: cannot resolve type '{typeString}'.");
                            continue;

                        case Keywords.Mode:
                            if (reader.CurrentBsonType != BsonType.String)
                                throw new Exception("Failed to parse node: invalid $mode field type.");
                            var modeString = reader.ReadString();
                            if (!Enum.TryParse<SnapshotModeType>(modeString, out var mode))
                                throw new Exception($"Failed to parse node: unsupported mode '{modeString}'.");
                            node.Mode = mode;
                            continue;

                        case Keywords.Value:
                            if (document != null)
                                throw new Exception("Failed to parse node: $value field is not in the header part.");
                            Parse(reader, node, root);
                            continue;

                        default:
                            // Mark exiting the header part.
                            isInHeader = false;
                            break;
                    }
                }

                document ??= node.AssignObject();
                var element = document.CreateNode(name);
                Parse(reader, element, root);
            }

            reader.ReadEndDocument();
        }
    }

    public static SnapshotNode Parse(string json, SnapshotNode? node = null, SnapshotNode? root = null)
    {
        using var reader = new JsonReader(json);
        return Parse(reader, node, root);
    }

    public static SnapshotNode Parse(byte[] bson, SnapshotNode? node = null, SnapshotNode? root = null)
    {
        using var stream = new MemoryStream(bson);
        using var reader = new BsonBinaryReader(stream);
        return Parse(reader, node, root);
    }

    public static SnapshotNode Parse(BsonDocument document, SnapshotNode? node = null, SnapshotNode? root = null)
    {
        using var reader = new BsonDocumentReader(document);
        return Parse(reader, node, root);
    }
}