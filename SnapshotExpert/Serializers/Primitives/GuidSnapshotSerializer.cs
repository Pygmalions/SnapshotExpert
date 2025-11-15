using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Composite;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class GuidSnapshotSerializer : SnapshotSerializer<Guid>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new OneOfSchema
        {
            Title = "GUID",
            Schemas =
            [
                new StringSchema
                {
                    Format = StringSchema.BuiltinFormats.Uuid,
                    Title = "GUID (string representation)",
                },
                new BinarySchema
                {
                    Title = "GUID (binary representation)",
                }
            ]
        };
    }

    public override void NewInstance(out Guid instance) => instance = Guid.Empty;

    public override void SaveSnapshot(in Guid target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        switch (scope.Format)
        {
            case SnapshotDataFormat.Textual:
                snapshot.Value = new StringValue(target.ToString());
                break;
            case SnapshotDataFormat.Binary:
            default:
                snapshot.Value = new BinaryValue(target);
                break;
        }
    }

    public override void LoadSnapshot(ref Guid target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        switch (snapshot.Value)
        {
            case null:
                throw new InvalidOperationException(
                    "Failed to load snapshot for Guid: no value is bound to the snapshot node.");
            case StringValue stringValue:
                if (!Guid.TryParse(stringValue.Value, out target))
                    throw new InvalidOperationException(
                        $"Failed to load snapshot for Guid: invalid string value '{stringValue.Value}'.");
                break;
            case BinaryValue binaryValue:
                if (binaryValue.Value.Length != 16)
                    throw new InvalidOperationException(
                        "Failed to load snapshot for Guid: invalid bytes length " +
                        $"{binaryValue.Value.Length}, expected 16.");
                target = binaryValue.AsGuid;
                break;
            default:
                throw new InvalidOperationException(
                    $"Failed to load snapshot for Guid: unexpected value type '{snapshot.Value.GetType()}'.");
        }
    }
}