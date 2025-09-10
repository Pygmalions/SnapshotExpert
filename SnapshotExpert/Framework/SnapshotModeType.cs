namespace SnapshotExpert.Framework;

public enum SnapshotModeType
{
    /// <summary>
    /// Use the snapshot to patch the target instance in place if possible.
    /// If not possible, replace the target instance with a new instance.
    /// This snapshot is considered as the partial snapshot of the target instance.
    /// </summary>
    Patching,
    /// <summary>
    /// Always replace the target instance with a new instance restored from the snapshot.
    /// This snapshot is considered as the complete snapshot of the target instance.
    /// </summary>
    Replacing,
}