namespace SnapshotExpert;

/// <summary>
/// Specify that this class should use the designated snapshot serializer.
/// </summary>
/// <param name="serializerType">Type of the snapshot serializer for this type to use.</param>
[AttributeUsage(validOn: AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
public class UsingSnapshotSerializerAttribute(Type serializerType) : Attribute
{
    public readonly Type SerializerType = serializerType;
}

/// <summary>
/// Designate a snapshot serializer to be used for the type marked with this attribute.
/// </summary>
/// <typeparam name="TSerializer">Type of the snapshot serializer for this type to use.</typeparam>
public class UsingSnapshotSerializerAttribute<TSerializer>() : UsingSnapshotSerializerAttribute(typeof(TSerializer))
    where TSerializer : SnapshotSerializer
{
}