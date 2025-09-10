﻿using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class DateTimeOffsetSnapshotSerializer : SnapshotSerializer<DateTimeOffset>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new StringSchema
        {
            Title = "Date Time",
            Format = StringSchema.BuiltinFormats.DateTime
        };
    }

    public override void NewInstance(out DateTimeOffset instance) => instance = default;

    public override void SaveSnapshot(in DateTimeOffset target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new DateTimeValue(target);

    public override void LoadSnapshot(ref DateTimeOffset target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.RequireValue<DateTimeValue>().Value;
}

public class DateTimeSnapshotSerializer : SnapshotSerializer<DateTime>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new StringSchema
        {
            Title = "Date Time",
            Format = StringSchema.BuiltinFormats.DateTime
        };
    }
    
    public override void NewInstance(out DateTime instance) => instance = default;

    public override void SaveSnapshot(in DateTime target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new DateTimeValue(target);

    public override void LoadSnapshot(ref DateTime target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.RequireValue<DateTimeValue>().Value.DateTime.ToLocalTime();
}