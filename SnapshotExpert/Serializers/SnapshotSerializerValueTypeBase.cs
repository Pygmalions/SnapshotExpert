using System.Runtime.CompilerServices;
using SnapshotExpert.Data;

namespace SnapshotExpert.Serializers;

public abstract class SnapshotSerializerValueTypeBase<TTarget>
    : SnapshotSerializer<TTarget> where TTarget : struct
{
    public override void NewInstance(out TTarget instance) => instance = default;

    public override void NewInstance(out object instance) => instance = default(TTarget);

    public override void SaveSnapshot(in object target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        if (target.GetType() != TargetType)
            throw new InvalidOperationException(
                "Failed to save snapshot: serializer for " +
                $"{TargetType} cannot handle target of type '{target.GetType()}'.");
        SaveSnapshot(Unsafe.Unbox<TTarget>(target), snapshot, scope);
    }

    public override void LoadSnapshot(ref object target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        if (target.GetType() != TargetType)
            throw new InvalidOperationException(
                "Failed to load snapshot: serializer for " +
                $"{TargetType} cannot handle target of type '{target.GetType()}'.");
        if (!TargetType.IsPrimitive)
        {
            LoadSnapshot(ref Unsafe.Unbox<TTarget>(target), snapshot, scope);
            return;
        }

        var typedTarget = (TTarget)target;
        LoadSnapshot(ref typedTarget, snapshot, scope);
        target = typedTarget!;
    }
}