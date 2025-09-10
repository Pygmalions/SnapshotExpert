using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Serializers.Containers;

public class StringDictionaryByInterfaceSnapshotSerializer<TValue, TTarget, TUnderlying>
    : SnapshotSerializerClassTypeBase<TTarget>
    where TTarget : class, IDictionary<string, TValue>
    where TUnderlying : TTarget, new()
{
    public required SnapshotSerializer<TValue> ValueSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema()
    {
        return new ObjectSchema
        {
            Title = "Dictionary with String Keys",
            AdditionalProperties = ValueSerializer.Schema
        };
    }

    public override void NewInstance(out TTarget instance)
        => instance = new TUnderlying();

    protected override void OnSaveSnapshot(in TTarget target, SnapshotNode snapshot,
        SnapshotWritingScope scope)
    {
        var objectValue = new ObjectValue();

        foreach (var (key, value) in target)
        {
            var valueNode = objectValue.CreateNode(key);
            ValueSerializer.SaveSnapshot(in value, valueNode, scope);
        }

        snapshot.Value = objectValue;
    }

    protected override void OnLoadSnapshot(ref TTarget target, SnapshotNode snapshot,
        SnapshotReadingScope scope)
    {
        var objectValue = snapshot.RequireValue<ObjectValue>();

        // Hash sets to track entries that don't exist in the snapshot and should be removed.
        var keys = target.Select(pair => pair.Key).ToHashSet();

        foreach (var (key, valueNode) in objectValue.Nodes)
        {
            if (target.TryGetValue(key, out var value))
                keys.Remove(key);
            else
                ValueSerializer.NewInstance(out value);
            
            ValueSerializer.LoadSnapshot(ref value, valueNode, scope);

            target[key] = value;
        }

        foreach (var key in keys)
            target.Remove(key);
    }
}