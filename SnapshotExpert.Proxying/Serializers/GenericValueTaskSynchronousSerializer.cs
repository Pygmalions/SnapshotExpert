using SnapshotExpert.Data;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Remoting.Serializers;

public class GenericValueTaskByWaitingSerializer<TContent> : SnapshotSerializerValueTypeBase<ValueTask<TContent>>
{
    public required SnapshotSerializer<TContent> ContentSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema() => ContentSerializer.Schema;

    public override void SaveSnapshot(in ValueTask<TContent> target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        ContentSerializer.SaveSnapshot(
            target.IsCompleted ? target.Result : target.AsTask().Result,
            snapshot, scope);
    }

    public override void LoadSnapshot(ref ValueTask<TContent> target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        ContentSerializer.NewInstance(out var value);
        ContentSerializer.LoadSnapshot(ref value, snapshot, scope);
        target = ValueTask.FromResult(value);
    }
}