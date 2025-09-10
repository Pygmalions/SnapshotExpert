using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class TimeSpanSnapshotSerializer : SnapshotSerializer<TimeSpan>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new StringSchema()
        {
            Title = "Time Span",
            Format = StringSchema.BuiltinFormats.Duration
        };
    }

    public override void NewInstance(out TimeSpan instance) => instance = TimeSpan.Zero;

    public override void SaveSnapshot(in TimeSpan target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new StringValue(target.ToString());

    public override void LoadSnapshot(ref TimeSpan target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = TimeSpan.Parse(snapshot.RequireValue<StringValue>().Value);
}