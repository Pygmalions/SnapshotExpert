using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class StringSnapshotSerializer : SnapshotSerializer<string>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new StringSchema();
    }

    public override void NewInstance(out string instance) => instance = string.Empty;

    public override void SaveSnapshot(in string target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new StringValue(target);

    public override void LoadSnapshot(ref string target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.AsString;
}

public class CharacterSnapshotSerializer : SnapshotSerializer<char>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new StringSchema
        {
            Description = "String of a single character.",
            MaxLength = 1
        };
    }
    
    public override void NewInstance(out char instance) => instance = '\0';

    public override void SaveSnapshot(in char target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new StringValue(Convert.ToString(target));

    public override void LoadSnapshot(ref char target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        var value = snapshot.AsString;
        if (value.Length != 1)
            throw new InvalidOperationException(
                "Failed to load snapshot for 'char': snapshot value is not a single character.");
        target = value[0];
    }
}