using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Serializers.Containers;

public class SetByInterfaceSnapshotSerializer<TElement, TTarget, TUnderlying>
    : SnapshotSerializerClassTypeBase<TTarget>
    where TTarget : class, ISet<TElement>
    where TUnderlying : class, TTarget, new()
{
    public required SnapshotSerializer<TElement> ElementSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema()
    {
        return new ArraySchema
        {
            Title = "A Set of Unique Values",
            Items = ElementSerializer.Schema,
            RequiringUniqueItems = true
        };
    }

    public override void NewInstance(out TTarget instance) => instance = new TUnderlying();

    protected override void OnSaveSnapshot(in TTarget target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        var arrayValue = new ArrayValue(target.Count);
        foreach (var element in target)
            ElementSerializer.SaveSnapshot(element, arrayValue.CreateNode(), scope);
        snapshot.Value = arrayValue;
    }

    protected override void OnLoadSnapshot(ref TTarget target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        var array = snapshot.RequireValue<ArrayValue>();

        target.Clear();

        foreach (var elementNode in array.Nodes)
        {
            ElementSerializer.NewInstance(out var element);
            ElementSerializer.LoadSnapshot(ref element, elementNode, scope);
            target.Add(element);
        }
    }
}

public static class SetByInterfaceSnapshotSerializer
{
    public static bool MatchSerializerType(Type targetType, out Type serializerType)
    {
        serializerType = typeof(void);

        // Target type must implement ISet<>.
        if (!targetType.TryMatchInterface(typeof(ISet<>), out var matchedListInterface))
            return false;

        var genericArguments = matchedListInterface.GetGenericArguments();

        var underlyingType =
            targetType.IsAbstract || targetType.IsInterface ||
            targetType.GetConstructor(Type.EmptyTypes) == null
                ? typeof(HashSet<>).MakeGenericType(genericArguments)
                : targetType;

        serializerType = typeof(SetByInterfaceSnapshotSerializer<,,>)
            .MakeGenericType(genericArguments[0], targetType, underlyingType);
        return true;
    }
}