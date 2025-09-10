using System.Reflection;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers;

public abstract class SnapshotSerializerClassTypeBase<TTarget>
    : SnapshotSerializer<TTarget> where TTarget : class
{
    protected abstract void OnSaveSnapshot(in TTarget target, SnapshotNode snapshot, SnapshotWritingScope scope);

    protected abstract void OnLoadSnapshot(ref TTarget target, SnapshotNode snapshot, SnapshotReadingScope scope);

    private readonly bool _hasDefaultConstructor;

    private readonly bool _enabledTypeRedirection;

    /// <summary>
    /// This constructor will configure the behavior of default <see cref="NewInstance"/>
    /// and type redirection during serialization.
    /// </summary>
    /// <param name="useNullInitialization">
    /// Whether to set the target to null in <see cref="NewInstance"/>.
    /// If null (default), it will be true if the target type is abstract or interface,
    /// or if the target type does not have a default constructor; otherwise false.
    /// </param>
    /// <param name="enableTypeRedirection">
    /// Whether to enable type redirection during serialization.
    /// If null (default), it will be false if the target type is abstract or interface;
    /// otherwise true.
    /// In summary, if this serializer uses an abstract or interface type as its nominal type,
    /// then this serializer likely works in a by-interface or by-contract manner;
    /// in that case, type redirection is not needed and should be disabled by default.
    /// </param>
    protected SnapshotSerializerClassTypeBase(
        bool? useNullInitialization = null,
        bool? enableTypeRedirection = null)
    {
        var targetType = typeof(TTarget);

        _hasDefaultConstructor = useNullInitialization ??
                                 (targetType is { IsAbstract: false, IsInterface: false } &&
                                  targetType.GetConstructor(Type.EmptyTypes) != null);
        /* If this serializer uses an abstract or interface type as its nominal type,
         * then this serializer works in a by-interface or by-contract manner;
         * then type redirection is likely not needed and should be disabled by default.
         */
        _enabledTypeRedirection = enableTypeRedirection ??
                                  (!targetType.IsAbstract || !targetType.IsInterface);
    }

    protected SnapshotSerializerClassTypeBase() : this(null, null)
    {
    }

    public override void NewInstance(out TTarget instance)
        => instance = _hasDefaultConstructor ? Activator.CreateInstance<TTarget>() : null!;

    public override void SaveSnapshot(in TTarget target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        // Handle null target.
        if (target == null!)
        {
            snapshot.Value = new NullValue();
            return;
        }

        // Store the object in the snapshot node.
        snapshot.Object = target;

        // Check if the target object is a reference to an existing object in the scope.
        if (scope.RecordObject(snapshot, target) is { } reference)
        {
            snapshot.Value = reference;
            return;
        }

        // Check if the target object is of a subtype of the target type.
        if (_enabledTypeRedirection)
        {
            var actualType = target.GetType();
            if (target.GetType() != typeof(TTarget))
            {
                // Store the actual type in the snapshot node.
                snapshot.Type = target.GetType();
                // Redirect the serialization to the serializer for the actual type.
                Context.RequireSerializer(actualType).SaveSnapshot(target, snapshot, scope);
                return;
            }
        }

        OnSaveSnapshot(target, snapshot, scope);
    }

    public override void LoadSnapshot(ref TTarget target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        // Store the object in the snapshot node for potential reference resolution.
        snapshot.Object = target;

        switch (snapshot.Value)
        {
            // Handle null value.
            case NullValue:
                target = null!;
                return;
            // Handle internal reference.
            case InternalReferenceValue internalReference:
                var referencedNode = internalReference.Reference;
                if (referencedNode is null)
                    throw new Exception("Failed to resolve internal reference: reference is null.");
                if (referencedNode.Value is not TTarget referencedObject)
                    throw new Exception("Failed to resolve internal reference: referenced object is of incorrect type.")
                    {
                        Data =
                        {
                            ["Path"] = referencedNode.Path,
                            ["TargetType"] = TargetType,
                            ["ActualType"] = referencedNode.Value?.GetType()
                        }
                    };
                target = referencedObject;
                break;
            // Handle external reference.
            case ExternalReferenceValue externalReference:
                if (externalReference.Identifier == null)
                    throw new Exception("Failed to resolve external reference: identifier is null.");
                if (scope.ExternalReferences == null)
                    throw new Exception(
                        "Failed to resolve external reference: no external references provided in scope.");
                var untypedReference = scope.ExternalReferences(externalReference.Identifier);
                if (untypedReference == null)
                    throw new Exception("Failed to resolve external reference: external reference not found.")
                    {
                        Data =
                        {
                            ["Identifier"] = externalReference.Identifier
                        }
                    };
                if (untypedReference is not TTarget typedReference)
                    throw new Exception("Failed to resolve external reference: referenced object is of incorrect type.")
                    {
                        Data =
                        {
                            ["Identifier"] = externalReference.Identifier,
                            ["TargetType"] = TargetType,
                            ["ActualType"] = untypedReference.GetType()
                        }
                    };
                target = typedReference;
                break;
            default:
                // Handle type redirection.
                if (snapshot.Type != null && snapshot.Type != TargetType)
                {
                    // Redirect the deserialization to the serializer for the actual type.
                    object untypedTarget = target;
                    Context.RequireSerializer(snapshot.Type).LoadSnapshot(ref untypedTarget, snapshot, scope);
                    break;
                }

                if (target == null!)
                    NewInstance(out target);
                OnLoadSnapshot(ref target, snapshot, scope);
                break;
        }

        // Store the object in the snapshot node for potential reference resolution.
        snapshot.Object = target;
    }
}