using MongoDB.Bson;
using MongoDB.Bson.IO;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data.IO;

public static class SnapshotBsonReaderExtensions
{
    extension(SnapshotNode)
    {
        private static void ParseDocument(SnapshotNode node, SnapshotNode root, IBsonReader reader)
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
                        case SnapshotNode.Keywords.Reference:
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

                        case SnapshotNode.Keywords.Type:
                            if (reader.CurrentBsonType != BsonType.String)
                                throw new Exception("Failed to parse node: invalid $type field type.");
                            var typeString = reader.ReadString();
                            node.Type = Type.GetType(typeString) ??
                                        throw new Exception(
                                            $"Failed to parse node: cannot resolve type '{typeString}'.");
                            continue;

                        case SnapshotNode.Keywords.Mode:
                            if (reader.CurrentBsonType != BsonType.String)
                                throw new Exception("Failed to parse node: invalid $mode field type.");
                            var modeString = reader.ReadString();
                            if (!Enum.TryParse<SnapshotModeType>(modeString, out var mode))
                                throw new Exception($"Failed to parse node: unsupported mode '{modeString}'.");
                            node.Mode = mode;
                            continue;

                        case SnapshotNode.Keywords.Value:
                            if (document != null)
                                throw new Exception(
                                    "Failed to parse node: $value field is not in the header part.");
                            SnapshotNode.ParseToNode(node, reader, root);
                            continue;

                        default:
                            // Mark exiting the header part.
                            isInHeader = false;
                            break;
                    }
                }

                document ??= node.AssignValue(new ObjectValue());
                var element = document.CreateNode(name);
                SnapshotNode.ParseToNode(element, reader, root);
            }

            reader.ReadEndDocument();
        }

        /// <summary>
        /// Parse BSON data from the specified reader into a snapshot node.
        /// </summary>
        /// <param name="reader">Reader to read BSON data from.</param>
        /// <param name="root">
        /// The root node of this snapshot node tree, used to resolve by-path references.
        /// If it is null, then the current node will be used as the root node.
        /// </param>
        /// <param name="node">
        /// Optional node to parse data into.
        /// If it is null, a root node will be created and used.
        /// </param>
        /// <returns>
        /// Specified node, or a new root node if the specified node is null.
        /// </returns>
        /// <exception cref="Exception">
        /// Throw when the BSON data contains invalid or unsupported part.
        /// </exception>
        public static void ParseToNode(SnapshotNode node, IBsonReader reader, SnapshotNode? root = null)
        {
            if (reader.State == BsonReaderState.Initial)
                reader.ReadBsonType();

            root ??= node;

            if (reader.State == BsonReaderState.Name)
                node.Name = reader.ReadName();

            switch (reader.CurrentBsonType)
            {
                case BsonType.EndOfDocument:
                    throw new Exception("Failed to parse node: unexpected end of document.");

                // Supported primitive types:
                case BsonType.Null:
                    node.Value = new NullValue();
                    break;
                case BsonType.Boolean:
                    node.Value = new BooleanValue(reader.ReadBoolean());
                    break;
                case BsonType.String:
                    node.Value = new StringValue(reader.ReadString());
                    break;
                case BsonType.Binary:
                    var binary = reader.ReadBinaryData();
                    node.Value = new BinaryValue(binary.Bytes, binary.SubType switch
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
                    node.Value = new DateTimeValue(DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadDateTime()));
                    break;
                case BsonType.Double:
                    node.Value = new Float64Value(reader.ReadDouble());
                    break;
                case BsonType.Int32:
                    node.Value = new Integer32Value(reader.ReadInt32());
                    break;
                case BsonType.Int64:
                    node.Value = new Integer64Value(reader.ReadInt64());
                    break;
                case BsonType.Decimal128:
                    node.Value = new DecimalValue(Decimal128.ToDecimal(reader.ReadDecimal128()));
                    break;

                // Supported complex types:

                case BsonType.Document:
                    SnapshotNode.ParseDocument(node, root, reader);
                    break;

                case BsonType.Array:
                    var array = node.AssignValue(new ArrayValue());
                    reader.ReadStartArray();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                        SnapshotNode.ParseToNode(array.CreateNode(), reader, root);
                    reader.ReadEndArray();
                    break;

                // Compatible types:

                case BsonType.Timestamp:
                    // Timestamp as long.
                    node.Value = new Integer64Value(reader.ReadTimestamp());
                    break;
                case BsonType.JavaScript:
                    // JavaScript code as string.
                    node.Value = new StringValue(reader.ReadJavaScript());
                    break;
                case BsonType.JavaScriptWithScope:
                    // JavaScript code with scope as a string (scope is ignored).
                    node.Value = new StringValue(reader.ReadJavaScriptWithScope());
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
        }

        public static SnapshotNode Parse(IBsonReader reader)
        {
            var node = new SnapshotNode();
            SnapshotNode.ParseToNode(node, reader);
            return node;
        }

        public static SnapshotNode ParseFromBsonText(string bson)
        {
            using var reader = new JsonReader(bson);
            return SnapshotNode.Parse(reader);
        }

        public static SnapshotNode ParseFromBsonBytes(byte[] bson)
        {
            using var stream = new MemoryStream(bson);
            using var reader = new BsonBinaryReader(stream);
            return SnapshotNode.Parse(reader);
        }

        public static SnapshotNode ParseFromBsonDocument(BsonDocument document)
        {
            using var reader = new BsonDocumentReader(document);
            return SnapshotNode.Parse(reader);
        }
    }
}