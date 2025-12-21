using System.Data;
using System.Text;
using System.Text.Json;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data.IO;

public static class SnapshotJsonReaderExtensions
{
    extension(SnapshotNode)
    {
        private static void ParseDocument(SnapshotNode node, SnapshotNode root, Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new DataException("Failed to parse object: reader is not pointed to the start of an object.");

            reader.Read();

            ObjectValue? document = null;
            var isInHeader = true;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new DataException($"Failed to parse object: unexpected token '{reader.TokenType}'.");

                var name = reader.GetString()
                           ?? throw new DataException("Failed to parse object: property name is null.");

                if (isInHeader)
                {
                    switch (name)
                    {
                        case SnapshotNode.Keywords.Reference:
                            if (reader.TokenType != JsonTokenType.String ||
                                reader.GetString() is not { } referenceString)
                                throw new DataException("Failed to parse node: invalid $ref field type.");
                            if (root.Locate(referenceString) is { } referencedNode)
                                node.Value = new InternalReferenceValue(referencedNode);
                            else
                                // If the reference string cannot be resolved as a path,
                                // then treat it as the identifier of an external reference.
                                node.Value = new ExternalReferenceValue(referenceString);
                            continue;

                        case SnapshotNode.Keywords.Type:
                            if (reader.TokenType != JsonTokenType.String ||
                                reader.GetString() is not { } typeString)
                                throw new DataException("Failed to parse node: invalid $type field type.");
                            node.Type = Type.GetType(typeString) ??
                                        throw new DataException(
                                            $"Failed to parse node: cannot resolve type '{typeString}'.");
                            continue;

                        case SnapshotNode.Keywords.Mode:
                            if (reader.TokenType != JsonTokenType.String ||
                                reader.GetString() is not { } modeString)
                                throw new DataException("Failed to parse node: invalid $mode field type.");
                            if (!Enum.TryParse<SnapshotModeType>(modeString, out var mode))
                                throw new DataException($"Failed to parse node: unsupported mode '{modeString}'.");
                            node.Mode = mode;
                            continue;

                        case SnapshotNode.Keywords.Value:
                            if (document != null)
                                throw new DataException(
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

            reader.Read();
        }

        /// <summary>
        /// Parse JSON data from the specified reader into a snapshot node.
        /// </summary>
        /// <param name="reader">Reader to read JSON data from.</param>
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
        /// <exception cref="DataException">
        /// Throw when the JSON data contains invalid or unsupported part.
        /// </exception>
        public static void ParseToNode(SnapshotNode node, Utf8JsonReader reader, SnapshotNode? root = null)
        {
            if (reader.TokenType == JsonTokenType.None)
                reader.Read();

            root ??= node;

            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    node.Name = reader.GetString() ?? string.Empty;
                    SnapshotNode.ParseToNode(node, reader, root);
                    break;
                // Supported primitive types:

                case JsonTokenType.Null:
                    node.Value = new NullValue();
                    break;
                case JsonTokenType.True:
                    node.Value = new BooleanValue(true);
                    break;
                case JsonTokenType.False:
                    node.Value = new BooleanValue(false);
                    break;
                case JsonTokenType.String:
                    node.Value = new StringValue(reader.GetString() ?? string.Empty);
                    break;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out var integer32))
                        node.Value = new Integer32Value(integer32);
                    else if (reader.TryGetInt64(out var integer64))
                        node.Value = new Integer64Value(integer64);
                    else if (reader.TryGetDouble(out var float64))
                        node.Value = new Float64Value(float64);
                    else if (reader.TryGetDecimal(out var decimal128))
                        node.Value = new DecimalValue(decimal128);
                    else
                        throw new DataException($"Failed to parse node: unsupported number type '{reader.TokenType}'.");
                    break;
                case JsonTokenType.StartObject:
                    SnapshotNode.ParseDocument(node, root, reader);
                    break;
                case JsonTokenType.StartArray:
                    var array = new ArrayValue();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        SnapshotNode.ParseToNode(array.CreateNode(), reader, root);
                    node.Value = array;
                    break;

                // Ignored tokens:

                case JsonTokenType.None:
                case JsonTokenType.Comment:
                    break;

                // Invalid tokens at the current position:

                case JsonTokenType.EndArray:
                case JsonTokenType.EndObject:
                    throw new DataException($"Failed to parse node: unexpected token '{reader.TokenType}'.");

                default:
                    throw new DataException(
                        $"Failed to parse node: unsupported or invalid JSON type: {reader.TokenType}.");
            }
        }

        public static SnapshotNode Parse(Utf8JsonReader reader)
        {
            var node = new SnapshotNode();
            SnapshotNode.ParseToNode(node, reader);
            return node;
        }

        public static SnapshotNode ParseFromJsonBytes(ReadOnlySpan<byte> json)
            =>
                SnapshotNode.Parse(new Utf8JsonReader(json));

        public static SnapshotNode ParseFromJsonText(string json)
            =>
                SnapshotNode.Parse(new Utf8JsonReader(Encoding.UTF8.GetBytes(json)));
    }
}