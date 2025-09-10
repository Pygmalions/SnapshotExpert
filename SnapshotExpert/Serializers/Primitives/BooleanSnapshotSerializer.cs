using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class BooleanSnapshotSerializer : SnapshotSerializer<bool>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new BooleanSchema();
    }

    public override void NewInstance(out bool instance) => instance = false;
    
    public override void SaveSnapshot(in bool target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new BooleanValue(target);

    public override void LoadSnapshot(ref bool target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.RequireValue<BooleanValue>().Value;
}