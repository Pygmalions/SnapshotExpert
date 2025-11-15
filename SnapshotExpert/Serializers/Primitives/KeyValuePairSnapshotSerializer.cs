using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Serializers.Primitives;

public class KeyValuePairSnapshotSerializer<TKey, TValue> :
    SnapshotSerializerValueTypeBase<KeyValuePair<TKey, TValue>>
{
    public required SnapshotSerializer<TKey> KeySerializer { get; init; }

    public required SnapshotSerializer<TValue> ValueSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema()
    {
        return new ObjectSchema
        {
            Title = "Key-Value Pair",
            RequiredProperties = new OrderedDictionary<string, SnapshotSchema>()
            {
                ["Key"] = KeySerializer.Schema,
                ["Value"] = ValueSerializer.Schema
            }
        };
    }

    public override void SaveSnapshot(in KeyValuePair<TKey, TValue> target, SnapshotNode snapshot,
        SnapshotWritingScope scope)
    {
        var objectValue = new ObjectValue();
        var keyNode = objectValue.CreateNode("Key");
        KeySerializer.SaveSnapshot(target.Key, keyNode, scope);
        var valueNode = objectValue.CreateNode("Value");
        ValueSerializer.SaveSnapshot(target.Value, valueNode, scope);
        snapshot.Value = objectValue;
    }

    public override void LoadSnapshot(ref KeyValuePair<TKey, TValue> target, SnapshotNode snapshot,
        SnapshotReadingScope scope)
    {
        var objectValue = snapshot.RequireValue<ObjectValue>();
        var keyNode = objectValue.RequireNode("Key");
        var key = target.Key;
        KeySerializer.LoadSnapshot(ref key, keyNode, scope);
        var valueNode = objectValue.RequireNode("Value");
        var value = target.Value;
        ValueSerializer.LoadSnapshot(ref value, valueNode, scope);
        target = new KeyValuePair<TKey, TValue>(key, value);
    }
}