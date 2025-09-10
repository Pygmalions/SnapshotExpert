using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;

namespace SnapshotExpert.Serializers;

/// <summary>
/// This serializer always redirect the serialization and deserialization to the actual type of the target instances,
/// it is useful for abstract or interface types.
/// </summary>
/// <typeparam name="TTarget">Type of instances that this redirector can handle.</typeparam>
public class SnapshotSerializerRedirector<TTarget> : SnapshotSerializer<TTarget> where TTarget : class
{
    public override void NewInstance(out TTarget instance)
        => instance = null!;

    public override void LoadSnapshot(ref TTarget target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        if (snapshot.Type == null)
            throw new Exception($"Failed to load snapshot for {TargetType}: missing necessary '$type' field.");
        object untypedTarget = target;
        Context.RequireSerializer(snapshot.Type).LoadSnapshot(ref untypedTarget, snapshot, scope);
        target = (TTarget)untypedTarget;
    }

    public override void SaveSnapshot(in TTarget target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        var targetType = target.GetType();
        snapshot.Type = targetType;
        Context.RequireSerializer(targetType).SaveSnapshot(target, snapshot, scope);
    }

    protected override SnapshotSchema GenerateSchema()
    {
        return new ObjectSchema();
    }
}