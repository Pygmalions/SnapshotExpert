using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class Float32SnapshotSerializer : SnapshotSerializer<float>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new NumberSchema
        {
            Title = "32-bit Floating Point",
        };
    }

    public override void NewInstance(out float instance) => instance = 0;

    public override void SaveSnapshot(in float target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Float64Value(target);

    public override void LoadSnapshot(ref float target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = (float)snapshot.RequireNumber<IFloat64Number>().Value;
}

public class Float64SnapshotSerializer : SnapshotSerializer<double>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new NumberSchema
        {
            Title = "64-bit Floating Point",
        };
    }
    
    public override void NewInstance(out double instance) => instance = 0;

    public override void SaveSnapshot(in double target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Float64Value(target);

    public override void LoadSnapshot(ref double target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.RequireNumber<IFloat64Number>().Value;
}