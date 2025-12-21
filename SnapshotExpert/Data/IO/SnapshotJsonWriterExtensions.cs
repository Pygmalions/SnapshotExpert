using System.Data;
using System.Text;
using System.Text.Json;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Data.IO;

public static class SnapshotJsonWriterExtensions
{
    private static void DumpNodeMetadata(
        Utf8JsonWriter writer, SnapshotNode node,
        bool withType = true, bool withMode = true)
    {
        if (withType && node.Type != null)
        {
            if (node.Type.MinimalAssemblyQualifiedName is not { } typeString)
                throw new DataException(
                    $"Failed to write node: associated type '{node.Type}' has no assembly qualified name.");
            writer.WritePropertyName(SnapshotNode.Keywords.Type);
            writer.WriteStringValue(typeString);
        }

        if (withMode && node.Mode != SnapshotModeType.Patching)
        {
            writer.WritePropertyName(SnapshotNode.Keywords.Mode);
            writer.WriteStringValue(node.Mode.ToString());
        }
    }

    private static void WritePrimitiveValue(Utf8JsonWriter writer, PrimitiveValue target)
    {
        switch (target)
        {
            case NullValue:
                writer.WriteNullValue();
                break;
            case BooleanValue booleanValue:
                writer.WriteBooleanValue(booleanValue.Value);
                break;
            case StringValue stringValue:
                writer.WriteStringValue(stringValue.Value);
                break;
            case BinaryValue binaryValue:
                writer.WriteBase64StringValue(binaryValue.Value);
                break;
            case Integer32Value integer32Value:
                writer.WriteNumberValue(integer32Value.Value);
                break;
            case Integer64Value integer64Value:
                writer.WriteNumberValue(integer64Value.Value);
                break;
            case Float64Value float64Value:
                writer.WriteNumberValue(float64Value.Value);
                break;
            case DecimalValue decimalValue:
                writer.WriteNumberValue(decimalValue.Value);
                break;
            case DateTimeValue dateTimeValue:
                writer.WriteStringValue(dateTimeValue.ToString());
                break;
            default:
                throw new DataException($"Unsupported primitive self.Value type: '{target.GetType()}'.");
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
        /// <exception cref="DataException">
        /// Throw if the value contains an unsupported part.
        /// </exception>
        public void Dump(Utf8JsonWriter writer)
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
                        writer.WriteStartObject();
                        DumpNodeMetadata(writer, self, withMode: false);
                        writer.WritePropertyName(SnapshotNode.Keywords.Value);
                    }

                    WritePrimitiveValue(writer, primitive);
                    if (hasValueMetadata)
                        writer.WriteEndObject();
                    return;
                }
                case ReferenceValue reference:
                    writer.WriteStartObject();
                    writer.WritePropertyName(SnapshotNode.Keywords.Reference);
                    switch (reference)
                    {
                        case InternalReferenceValue internalReference:
                            if (internalReference.Reference?.Path is { } path)
                                writer.WriteStringValue(path);
                            else
                                writer.WriteNullValue();
                            break;
                        case ExternalReferenceValue externalReference:
                            if (externalReference.Identifier is { } identifier)
                                writer.WriteStringValue(identifier);
                            else
                                writer.WriteNullValue();
                            break;
                        default:
                            throw new DataException($"Unsupported reference self.Value type '{reference.GetType()}'.");
                    }

                    writer.WriteEndObject();
                    break;
                case ObjectValue document:
                    writer.WriteStartObject();
                    DumpNodeMetadata(writer, self);
                    foreach (var child in document.DeclaredNodes)
                    {
                        writer.WritePropertyName(child.Name);
                        child.Dump(writer);
                    }

                    writer.WriteEndObject();
                    break;
                case ArrayValue array:
                    var hasArrayMetadata = self.Type != null || self.Mode != SnapshotModeType.Patching;
                    if (hasArrayMetadata)
                    {
                        writer.WriteStartObject();
                        DumpNodeMetadata(writer, self);
                        writer.WritePropertyName(SnapshotNode.Keywords.Value);
                    }

                    writer.WriteStartArray();
                    foreach (var child in array.DeclaredNodes)
                        child.Dump(writer);
                    writer.WriteEndArray();
                    if (hasArrayMetadata)
                        writer.WriteEndObject();
                    break;
                default:
                    throw new DataException($"Unsupported self.Value type '{self.Value.GetType()}'.");
            }
        }

        public string DumpToJsonText(bool indent = false)
            => self.DumpToJsonText(new JsonWriterOptions { Indented = indent });

        /// <summary>
        /// Convert this snapshot node tree into a JSON string.
        /// </summary>
        /// <param name="settings">Settings for the JSON writer.</param>
        /// <returns>JSON representation of this snapshot node tree.</returns>
        public string DumpToJsonText(JsonWriterOptions settings)
            => Encoding.UTF8.GetString(self.DumpToJsonBytes(settings));

        public byte[] DumpToJsonBytes(JsonWriterOptions settings)
        {
            if (self.Value == null)
                throw new InvalidOperationException(
                    "Failed to convert node to JSON: no self.Value is bound to this node.");
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, settings);
            self.Dump(writer);
            writer.Flush();
            return stream.ToArray();
        }
    }
}