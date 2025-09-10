using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Composite;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class ByteArraySnapshotSerializer : SnapshotSerializer<byte[]>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new OneOfSchema
        {
            Schemas =
            [
                new StringSchema
                {
                    Title = "Byte Array (Base64 Encoded String)",
                    ContentEncoding = StringSchema.ContentEncodingType.Base64
                },
                new BinarySchema
                {
                    Title = "Byte Array (BSON Relaxed Style)",
                }
            ]
        };
    }

    public override void NewInstance(out byte[] instance) => instance = [];

    public override void SaveSnapshot(in byte[] target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new BinaryValue(target);

    public override void LoadSnapshot(ref byte[] target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        target = snapshot.Value switch
        {
            StringValue stringValue => Convert.FromBase64String(stringValue.Value),
            BinaryValue binaryValue => binaryValue.Value,
            null => throw new Exception(
                "Failed to load snapshot for 'byte[]': no value is assigned to the snapshot node."),
            _ => throw new Exception(
                "Failed to load snapshot for 'byte[]': expected a string or binary value, " +
                $"but got '{snapshot.Value?.GetType()}'.")
        };
    }
}