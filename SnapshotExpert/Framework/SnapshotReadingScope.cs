﻿using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework;

public class SnapshotReadingScope
{
    /// <summary>
    /// Root snapshot of this scope.
    /// </summary>
    public SnapshotNode Root { get; }

    /// <summary>
    /// Functor to resolve references by their identifiers.
    /// </summary>
    public Func<string, object?>? ExternalReferences { get; init; }
    
    /// <summary>
    /// Create a new reading scope with the specified root snapshot node.
    /// </summary>
    /// <param name="root">This node will be considered as the root node for this scope.</param>
    public SnapshotReadingScope(SnapshotNode root)
    {
        Root = root;
    }

    /// <summary>
    /// Create a new reading scope with the specified root snapshot node.
    /// </summary>
    /// <param name="root">
    /// The node that this value is bound to will be considered as the root node for this scope,
    /// if the value is not bound to any node, a new root node will be created to hold this value.
    /// </param>
    public SnapshotReadingScope(SnapshotValue root)
    {
        var node = root.Node ?? new SnapshotNode
        {
            Value = root
        };
        Root = node;
    }
    
    /// <summary>
    /// Search for the referenced object from a reference value.
    /// </summary>
    /// <param name="reference">Reference value to search.</param>
    /// <returns>Referenced object, or null if not found.</returns>
    public object? SearchObject(ReferenceValue reference)
    {
        return reference switch
        {
            InternalReferenceValue internalReference => 
                internalReference.Reference?.Object,
            ExternalReferenceValue { Identifier: not null } externalReference => 
                ExternalReferences?.Invoke(externalReference.Identifier),
            _ => null
        };
    }
}