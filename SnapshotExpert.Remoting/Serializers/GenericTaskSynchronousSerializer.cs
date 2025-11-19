using SnapshotExpert.Data;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Remoting.Serializers;

public class GenericTaskSynchronousSerializer<TContent> : SnapshotSerializerClassTypeBase<Task<TContent>>
{
    public required SnapshotSerializer<TContent> ContentSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema() => ContentSerializer.Schema;

    protected override void OnSaveSnapshot(in Task<TContent> target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        ContentSerializer.SaveSnapshot(target.Result, snapshot, scope);
    }

    protected override void OnLoadSnapshot(ref Task<TContent> target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        ContentSerializer.NewInstance(out var value);
        ContentSerializer.LoadSnapshot(ref value, snapshot, scope);
        target = Task.FromResult(value);
    }
}