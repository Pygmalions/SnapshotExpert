using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Serializers.Primitives;

/// <summary>
/// Snapshot serializer for arrays. <br/>
/// This serializer uses reflection to read and write elements of an array.
/// It also supports jagged arrays such as <c>T[][]</c> and <c>T[][][]</c>; in such cases,
/// the type should be <c>ArraySnapshotSerializer{T[]}</c> for <c>T[][]</c>,
/// and <c>ArraySnapshotSerializer{T[][]}</c> for <c>T[][][]</c>, and so on.
/// </summary>
/// <typeparam name="TElement">Type of the array element.</typeparam>
public class ArraySnapshotSerializer<TElement> : SnapshotSerializerClassTypeBase<TElement[]>
{
    public required SnapshotSerializer<TElement> ElementSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema()
    {
        return new ArraySchema
        {
            Title = "Array",
            Items = ElementSerializer.Schema
        };
    }

    public override void NewInstance(out TElement[] instance) => instance = [];

    protected override void OnSaveSnapshot(in TElement[] target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        var array = new ArrayValue(target.Length);
        foreach (var element in target)
            ElementSerializer.SaveSnapshot(element, array.CreateNode(), scope);
    }

    protected override void OnLoadSnapshot(ref TElement[] target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        var array = snapshot.RequireValue<ArrayValue>();

        // In patching mode, if the array size is the same and the mode is not specified to other than patching.
        if (snapshot.Mode == SnapshotModeType.Patching && array.Count == target.Length)
        {
            for (var index = 0; index < array.Count; index++)
                ElementSerializer.LoadSnapshot(ref target[index], array[index], scope);

            return;
        }

        // In replacing mode.
        target = new TElement[array.Count];
        for (var index = 0; index < array.Count; index++)
        {
            ElementSerializer.NewInstance(out target[index]);
            ElementSerializer.LoadSnapshot(ref target[index], array[index], scope);
        }
    }
}