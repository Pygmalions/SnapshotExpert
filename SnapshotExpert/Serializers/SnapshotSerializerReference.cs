using SnapshotExpert.Data;

namespace SnapshotExpert.Serializers;

/// <summary>
/// This class represents a reference to another serializer for a specific target type,
/// which redirects all operations to the referenced serializer.
/// It is useful to track and replace dependency serializers injected into other serializers.
/// </summary>
public class SnapshotSerializerReference<TTarget> : SnapshotSerializer<TTarget>
{
    private readonly Source _source;

    private SnapshotSerializerReference(Source source)
    {
        _source = source;
    }

    public SnapshotSerializer<TTarget> ReferencedSerializer =>
        _source.Serializer ??
        throw new NullReferenceException("Referenced serializer is null.");

    protected override SnapshotSchema GenerateSchema()
        => ReferencedSerializer.Schema;

    public override void NewInstance(out TTarget instance)
        => ReferencedSerializer.NewInstance(out instance);

    public override void SaveSnapshot(in TTarget target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => ReferencedSerializer.SaveSnapshot(target, snapshot, scope);

    public override void LoadSnapshot(ref TTarget target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => ReferencedSerializer.LoadSnapshot(ref target, snapshot, scope);

    /// <summary>
    /// Source of the serializer reference.
    /// </summary>
    public class Source
    {
        /// <summary>
        /// Create a new source of serializer reference.
        /// </summary>
        /// <param name="serializer">Initial serializer to reference.</param>
        public Source(SnapshotSerializer<TTarget>? serializer = null)
        {
            Serializer = serializer!;
            Reference = new SnapshotSerializerReference<TTarget>(this);
        }

        public SnapshotSerializer<TTarget> Serializer { get; set; }

        public SnapshotSerializerReference<TTarget> Reference { get; }
    }
}