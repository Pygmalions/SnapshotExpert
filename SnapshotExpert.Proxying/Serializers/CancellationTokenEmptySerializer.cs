using OneOf.Types;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Remoting.Serializers;

public class CancellationTokenEmptySerializer : SnapshotSerializerValueTypeBase<CancellationToken>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new NullSchema
        {
            Title = "Cancellation Token",
            Description = "A new cancellation token is created every time a snapshot is loaded."
        };
    }

    public override void SaveSnapshot(in CancellationToken target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        snapshot.AssignValue(new NullValue());
    }

    public override void LoadSnapshot(ref CancellationToken target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
       target = CancellationToken.None;
    }
}