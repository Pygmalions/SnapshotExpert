using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Serializers.Containers;

/// <summary>
/// This serializer handles dictionary-like types that implement <see cref="IDictionary{TKey,TValue}"/>,
/// using the methods provided by the interface.
/// The underlying type used to create new instances must have a public parameterless constructor,
/// and when the target instance is null, a new instance of the underlying type will be created.
/// </summary>
/// <typeparam name="TKey">Type of the key.</typeparam>
/// <typeparam name="TValue">Type of the key.</typeparam>
/// <typeparam name="TTarget">Type of the target instances that this serializer handles.</typeparam>
/// <typeparam name="TUnderlying">Underlying dictionary type for this serializer to instantiate for null targets.</typeparam>
public class DictionaryByInterfaceSnapshotSerializer<TKey, TValue, TTarget, TUnderlying>
    : SnapshotSerializerClassTypeBase<TTarget>
    where TKey : notnull
    where TTarget : class, IDictionary<TKey, TValue>
    where TUnderlying : TTarget, new()
{
    public required SnapshotSerializer<TKey> KeySerializer { get; init; }

    public required SnapshotSerializer<TValue> ValueSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema()
    {
        return new ArraySchema
        {
            Title = "Dictionary",
            Description = "Represented as an array of key-value pairs.",
            Items = new ObjectSchema()
            {
                Title = "Key-Value Pair",
                RequiredProperties = new OrderedDictionary<string, SnapshotSchema>()
                {
                    ["Key"] = KeySerializer.Schema,
                    ["Value"] = ValueSerializer.Schema
                }
            }
        };
    }

    public override void NewInstance(out TTarget instance) => instance = new TUnderlying();

    protected override void OnSaveSnapshot(in TTarget target, SnapshotNode snapshot,
        SnapshotWritingScope scope)
    {
        var arrayValue = new ArrayValue();

        foreach (var (key, value) in target)
        {
            var elementNode = arrayValue.CreateNode();
            var elementObject = elementNode.AssignObject();
            var keyNode = elementObject.CreateNode("Key");
            KeySerializer.SaveSnapshot(in key, keyNode, scope);
            var valueNode = elementObject.CreateNode("Value");
            ValueSerializer.SaveSnapshot(in value, valueNode, scope);
        }

        snapshot.Value = arrayValue;
    }

    protected override void OnLoadSnapshot(ref TTarget target, SnapshotNode snapshot,
        SnapshotReadingScope scope)
    {
        var arrayValue = snapshot.RequireValue<ArrayValue>();

        // Hash sets to track entries that don't exist in the snapshot and should be removed.
        var keys = target.Select(pair => pair.Key).ToHashSet();

        foreach (var elementNode in arrayValue.Nodes)
        {
            var pairValue = elementNode.RequireValue<ObjectValue>();

            var keyNode = pairValue.RequireNode("Key");
            KeySerializer.NewInstance(out var key);
            KeySerializer.LoadSnapshot(ref key, keyNode, scope);

            if (target.TryGetValue(key, out var value))
                keys.Remove(key);
            else
                ValueSerializer.NewInstance(out value);

            var valueNode = pairValue.RequireNode("Value");
            ValueSerializer.LoadSnapshot(ref value, valueNode, scope);

            target[key] = value;
        }

        foreach (var key in keys)
            target.Remove(key);
    }
}

public static class DictionaryByInterfaceSnapshotSerializer
{
    public static bool MatchSerializerType(Type targetType, out Type serializerType)
    {
        serializerType = typeof(void);

        // Target type must implement IDictionary<,>.
        if (!targetType.TryMatchInterface(typeof(IDictionary<,>), out var matchedDictionaryInterface))
            return false;

        var genericArguments = matchedDictionaryInterface.GetGenericArguments();

        var underlyingType = 
            targetType.IsAbstract || targetType.IsInterface || 
            targetType.GetConstructor(Type.EmptyTypes) == null
            ? typeof(Dictionary<,>).MakeGenericType(genericArguments)
            : targetType;
        
        if (genericArguments[0] == typeof(string))
            serializerType = typeof(StringDictionaryByInterfaceSnapshotSerializer<,,>)
                .MakeGenericType(genericArguments[1],
                    targetType, underlyingType);
        else
            serializerType = typeof(DictionaryByInterfaceSnapshotSerializer<,,,>)
                .MakeGenericType(genericArguments[0], genericArguments[1],
                    targetType, underlyingType);
        return true;
    }
}