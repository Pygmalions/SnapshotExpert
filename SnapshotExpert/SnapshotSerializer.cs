using System.Diagnostics.CodeAnalysis;
using SnapshotExpert.Data;

namespace SnapshotExpert;

public abstract class SnapshotSerializer
{
    /// <summary>
    /// Type of the target instance that this serializer can handle.
    /// </summary>  
    public abstract Type TargetType { get; }

    /// <summary>
    /// Context of this snapshot serializer.
    /// </summary>
    public required SerializerContainer Context { get; init; }

    /// <summary>
    /// Schema of the snapshots that this serializer handles.
    /// </summary>
    [field: MaybeNull]
    public SnapshotSchema Schema => field ??= GenerateSchema();

    /// <summary>
    /// Instantiate a new instance of the target type.
    /// Usually, this instance is used to load snapshot into it.
    /// It is not likely to be fully initialized and ready to use.
    /// </summary>
    /// <param name="instance">Instantiated instance.</param>
    public abstract void NewInstance(out object instance);

    /// <summary>
    /// Save the snapshot for the target instance.
    /// </summary>
    /// <param name="target">
    /// Target instance or boxed value to save snapshot for.
    /// </param>
    /// <param name="snapshot">Root node to write snapshot into.</param>
    /// <param name="scope">Scope for this snapshot writing operation.</param>
    /// <returns>Snapshot for the target instance.</returns>
    public abstract void SaveSnapshot(in object target, SnapshotNode snapshot, SnapshotWritingScope scope);

    /// <summary>
    /// Load the snapshot into the target instance.
    /// </summary>
    /// <param name="target">
    /// Target instance or boxed value to load snapshot into.
    /// </param>
    /// <param name="snapshot">Root node to read snapshot from.</param>
    /// <param name="scope">Scope for this snapshot reading operation.</param>
    public abstract void LoadSnapshot(ref object target, SnapshotNode snapshot, SnapshotReadingScope scope);

    /// <summary>
    /// Generate the schema of the snapshots that this serializer handles.
    /// </summary>
    /// <returns>Snapshot schema.</returns>
    protected abstract SnapshotSchema GenerateSchema();
}

public abstract class SnapshotSerializer<TTarget> : SnapshotSerializer
{
    public override Type TargetType { get; } = typeof(TTarget);

    /// <summary>
    /// Instantiate a new instance of the target type.
    /// Usually, this instance is used to load snapshot into it.
    /// It is not likely to be fully initialized and ready to use.
    /// </summary>
    /// <param name="instance">Instantiated instance.</param>
    public abstract void NewInstance(out TTarget instance);

    /// <summary>
    /// Save the snapshot for the target instance.
    /// </summary>
    /// <param name="target">Target instance to save snapshot for.</param>
    /// <param name="snapshot">Root node to write snapshot into.</param>
    /// <param name="scope">Scope for this snapshot writing operation.</param>
    /// <returns>Snapshot for the target instance.</returns>
    public abstract void SaveSnapshot(
        in TTarget target, SnapshotNode snapshot, SnapshotWritingScope scope);

    /// <summary>
    /// Load the snapshot into the target instance.
    /// </summary>
    /// <param name="target">Target instance to load snapshot into.</param>
    /// <param name="snapshot">Root node to read snapshot from.</param>
    /// <param name="scope">Scope for this snapshot reading operation.</param>
    public abstract void LoadSnapshot(
        ref TTarget target, SnapshotNode snapshot, SnapshotReadingScope scope);

    /// <summary>
    /// Save the snapshot for the target instance.
    /// </summary>
    /// <param name="target">Target instance to save snapshot for.</param>
    /// <param name="snapshot">Root node to write snapshot into.</param>
    /// <returns>Snapshot for the target instance.</returns>
    public void SaveSnapshot(in TTarget target, SnapshotNode snapshot)
    {
        var scope = new SnapshotWritingScope();
        SaveSnapshot(target, snapshot, scope);
    }

    /// <summary>
    /// Load the snapshot into the target instance.
    /// </summary>
    /// <param name="target">Target instance to load snapshot into.</param>
    /// <param name="snapshot">Root node to read snapshot from.</param>
    public void LoadSnapshot(ref TTarget target, SnapshotNode snapshot)
    {
        var scope = new SnapshotReadingScope(snapshot);
        LoadSnapshot(ref target, snapshot, scope);
    }

    public override void NewInstance(out object instance)
    {
        NewInstance(out var typedInstance);
        instance = typedInstance!;
    }

    public override void SaveSnapshot(in object target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        if (target is not TTarget typedTarget)
            throw new InvalidOperationException(
                "Failed to save snapshot: serializer for " +
                $"{TargetType} cannot handle target of type '{target.GetType()}'.");
        SaveSnapshot(in typedTarget, snapshot, scope);
    }

    public override void LoadSnapshot(ref object target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        if (target is not TTarget typedTarget)
            throw new InvalidOperationException(
                "Failed to load snapshot: serializer for " +
                $"{TargetType} cannot handle target of type '{target.GetType()}'.");
        LoadSnapshot(ref typedTarget, snapshot, scope);
        target = typedTarget!;
    }
}