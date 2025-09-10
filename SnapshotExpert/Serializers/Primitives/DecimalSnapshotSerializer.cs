using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class DecimalSnapshotSerializer : SnapshotSerializer<decimal>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new NumberSchema()
        {
            Title = "Decimal Number",
        };
    }

    public override void NewInstance(out decimal instance) => instance = 0;

    public override void SaveSnapshot(in decimal target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new DecimalValue(target);

    public override void LoadSnapshot(ref decimal target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.RequireValue<DecimalValue>().Value;
}