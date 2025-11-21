using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Remoting.Serializers;

public class TaskSynchronousSerializer : SnapshotSerializerClassTypeBase<Task>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new NullSchema();
    }

    protected override void OnSaveSnapshot(in Task target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        target.Wait();
        snapshot.AssignValue(new NullValue());
    }

    protected override void OnLoadSnapshot(ref Task target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        target = Task.CompletedTask;
    }
}