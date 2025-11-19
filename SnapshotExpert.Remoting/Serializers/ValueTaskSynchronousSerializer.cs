using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Remoting.Serializers;

public class ValueTaskSynchronousSerializer : SnapshotSerializerValueTypeBase<ValueTask>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new NullSchema();
    }

    public override void SaveSnapshot(in ValueTask target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        if (!target.IsCompleted)
            target.AsTask().Wait();
        snapshot.AssignNull();
    }

    public override void LoadSnapshot(ref ValueTask target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        target = ValueTask.CompletedTask;
    }
}