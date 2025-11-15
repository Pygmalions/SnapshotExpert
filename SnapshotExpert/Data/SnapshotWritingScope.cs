using System.Runtime.InteropServices;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data;

public class SnapshotWritingScope
{
    private readonly Dictionary<object, SnapshotNode> _objects = [];
    
    /// <summary>
    /// Indicates the underlying data type of the snapshot,
    /// whether it is stored as text (e.g., JSON) or binary (e.g., BSON).
    /// This may affect how certain values are serialized with specified serializers.
    /// </summary>
    public SnapshotDataFormat Format { get; init; } = SnapshotDataFormat.Binary;
    
    /// <summary>
    /// Functor to query an identifier for an object instance.
    /// If this functor returns null, then the instance will not be treated as a reference.
    /// </summary>
    public Func<object, string?>? ExternalReferences { get; init; }
    
    /// <summary>
    /// Objects that have been recorded in this scope.
    /// </summary>
    public IReadOnlyDictionary<object, SnapshotNode> Objects => _objects;
    
    /// <summary>
    /// Record the specified object instance.
    /// If this instance is a reference to an already recorded instance,
    /// return the corresponding reference value.
    /// </summary>
    /// <param name="instance">Instance to record.</param>
    /// <param name="node">Node for this instance.</param>
    /// <returns>
    /// - Internal, <see cref="InternalReferenceValue"/>: referencing to another node in the same snapshot. <br/>
    /// - External, <see cref="ExternalReferenceValue"/>: referencing to an external object.
    /// </returns>
    public ReferenceValue? RecordObject(SnapshotNode node, object instance)
    {
        if (ExternalReferences?.Invoke(instance) is {} identifier)
            return new ExternalReferenceValue(identifier);
        ref var entry =
            ref CollectionsMarshal.GetValueRefOrAddDefault(_objects, instance, out var exists);
        if (exists)
            return new InternalReferenceValue(entry);
        entry = node;
        return null;
    }
}