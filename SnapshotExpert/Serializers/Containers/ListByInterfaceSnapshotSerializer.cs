using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Serializers.Containers;

public class ListByInterfaceSnapshotSerializer<TElement, TTarget, TUnderlying>
    : SnapshotSerializerClassTypeBase<TTarget>
    where TTarget : class, IList<TElement>
    where TUnderlying : class, TTarget, new()
{
    public required SnapshotSerializer<TElement> ElementSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema()
    {
        return new ArraySchema
        {
            Title = "List",
            Items = ElementSerializer.Schema,
        };
    }

    public override void NewInstance(out TTarget instance) => instance = new TUnderlying();

    protected override void OnSaveSnapshot(in TTarget target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        var arrayValue = new ArrayValue();

        foreach (var element in target)
            ElementSerializer.SaveSnapshot(in element, arrayValue.CreateNode(), scope);

        snapshot.Value = arrayValue;
    }

    protected override void OnLoadSnapshot(ref TTarget target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        var arrayValue = snapshot.RequireValue<ArrayValue>();

        foreach (var (index, elementNode) in arrayValue.Nodes.Index())
        {
            TElement element;

            if (index < target.Count)
                element = target[index];
            else
                ElementSerializer.NewInstance(out element);

            ElementSerializer.LoadSnapshot(ref element, elementNode, scope);

            if (index < target.Count)
                target[index] = element;
            else
                target.Add(element);
        }

        for (var index = target.Count - 1; index >= arrayValue.Count; --index)
            target.RemoveAt(index);
    }
}

public static class ListByInterfaceSnapshotSerializer
{
    public static bool MatchSerializerType(Type targetType, out Type serializerType)
    {
        serializerType = typeof(void);

        // Target type must implement IList<>.
        if (!targetType.TryMatchInterface(typeof(IList<>), out var matchedListInterface))
            return false;

        var genericArguments = matchedListInterface.GetGenericArguments();

        var underlyingType =
            targetType.IsAbstract || targetType.IsInterface ||
            targetType.GetConstructor(Type.EmptyTypes) == null
                ? typeof(List<>).MakeGenericType(genericArguments)
                : targetType;

        serializerType = typeof(ListByInterfaceSnapshotSerializer<,,>)
            .MakeGenericType(genericArguments[0],
                targetType, underlyingType);
        return true;
    }
}