using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Composite;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Generics;

/// <summary>
/// This serializer handles nullable value types of <see cref="Nullable{T}"/>.
/// It handles serialization of null values and
/// delegates the serialization of non-null values to the provided <see cref="ValueSerializer"/>.
/// </summary>
/// <typeparam name="TValue">Value type as the generic argument for <see cref="Nullable{T}"/>.</typeparam>
public class NullableValueSnapshotSerializer<TValue> : SnapshotSerializer<TValue?>
    where TValue : struct
{
    /// <summary>
    /// Serialization for non-null values will be redirected to this serializer.
    /// </summary>
    public required SnapshotSerializer<TValue> ValueSerializer { get; init; }

    protected override SnapshotSchema GenerateSchema()
    {
        return ValueSerializer.Schema with { IsNullable = true };
    }

    public override void NewInstance(out TValue? instance) => instance = null;

    public override void SaveSnapshot(in TValue? target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        if (target == null)
        {
            snapshot.Value = new NullValue();
            return;
        }

        ValueSerializer.SaveSnapshot(target.Value, snapshot, scope);
    }

    public override void LoadSnapshot(ref TValue? target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        if (snapshot.Value is NullValue)
        {
            target = null;
            return;
        }

        TValue value;
        if (target != null)
            value = target.Value;
        else
            ValueSerializer.NewInstance(out value);
        ValueSerializer.LoadSnapshot(ref value, snapshot, scope);
        target = value;
    }
}