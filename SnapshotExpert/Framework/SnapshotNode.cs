﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SnapshotExpert.Framework;

[DebuggerDisplay("{DebuggerString,nq}")]
public partial class SnapshotNode
{
    /// <summary>
    /// Value that declares this node.
    /// </summary>
    private SnapshotValue? Slot { get; set; }
    
    /// <summary>
    /// Human-readable string for this node to display in the debugger.
    /// </summary>
    internal string DebuggerString => $"\"{Name}\" = {Value?.DebuggerString ?? "[Empty]"}";
    
    /// <summary>
    /// Instantiate a snapshot and mount it to the specified slot.
    /// </summary>
    /// <param name="slot">Slot for this node to mount under.</param>
    /// <param name="name">Name of this slot.</param>
    internal SnapshotNode(SnapshotValue? slot, string name)
    {
        Slot = slot;
        Name = name;
    }

    /// <summary>
    /// Instantiate a root node with the specified name.
    /// </summary>
    /// <param name="name">Name of this node, which is "#" by default.</param>
    public SnapshotNode(string name = "#") : this(null, name)
    {
    }

    /// <summary>
    /// Detach this node from its slot.
    /// After detachment, the node becomes a root node with name '#'.
    /// This method will also invalidate the cached path of this node and its children.
    /// </summary>
    internal void Detach()
    {
        Slot = null;
        Name = "#";
    }

    /// <summary>
    /// Name of this node.
    /// </summary>
    public string Name
    {
        get;
        internal set
        {
            field = value;
            // Invalidated cached path.
            Path = null!;
        }
    }

    /// <summary>
    /// Parent node of this node.
    /// </summary>
    public SnapshotNode? Parent => Slot?.Node;

    /// <summary>
    /// Children node
    /// </summary>
    public IReadOnlyCollection<SnapshotNode> Children => 
        Value as IReadOnlyCollection<SnapshotNode> ?? [];

    /// <summary>
    /// Associated object for the node.
    /// This field is necessary for handling internal references when loading snapshots.
    /// </summary>
    public object? Object { get; set; }

    /// <summary>
    /// Associated type of this node.
    /// If this type is not null, usually a type redirection is needed.
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Mode of this snapshot.
    /// </summary>
    public SnapshotModeType Mode { get; set; } = SnapshotModeType.Patching;

    /// <summary>
    /// The value bound to this node.
    /// </summary>
    public SnapshotValue? Value
    {
        get;
        set
        {
            field?.Node = null;
            field = value;
            value?.Node = this;
        }
    }

    /// <summary>
    /// Path of this node from the root node.
    /// The path is a '/'-separated string of node names and starts from the root node.
    /// </summary>
    /// <remarks>
    /// The path is cached until the name of this node or any of its ancestor nodes changes.
    /// </remarks>
    [field: MaybeNull]
    public string Path
    {
        get
        {
            if (field != null)
                return field;
            // Construct the path string and cache it.
            var path = new LinkedList<SnapshotNode>();
            for (var current = this; current != null; current = current.Parent)
                path.AddFirst(current);
            field = string.Join('/', path.Select(node => node.Name));
            return field;
        }
        private set
        {
            field = value;
            // Broadcast the invalidation to child nodes, if any.
            if (Value is not IEnumerable<SnapshotNode> collection)
                return;
            foreach (var node in collection)
                node.Path = null!;
        }
    }

    /// <summary>
    /// Locate a node by the specified path.
    /// The path is a sequence of node names,
    /// and starts with the name of this node.
    /// </summary>
    /// <param name="path">Path to the specified node.</param>
    /// <returns>Node pointed by the path, or null if not found.</returns>
    public SnapshotNode? Locate(IEnumerable<string> path)
    {
        using var enumerator = path.GetEnumerator();
        if (!enumerator.MoveNext() || enumerator.Current != Name)
            return null;
        var current = this;
        while (enumerator.MoveNext())
        {
            if (current == null)
                return null;
            var name = enumerator.Current;
            current = name switch
            {
                "" => null,
                "." => current,
                ".." => current.Parent,
                _ => current.Value?[name]
            };
        }

        return current;
    }

    /// <summary>
    /// Locate a node by the specified path.
    /// The path is a '/'-separated string of node names,
    /// and starts with the name of this node.
    /// </summary>
    /// <param name="path">Path to the specified node.</param>
    /// <returns>Node pointed by the path, or null if not found.</returns>
    public SnapshotNode? Locate(string path)
    {
        return path switch
        {
            "" => null,
            "." => this,
            ".." => Parent,
            _ => Locate(path.Split('/'))
        };
    }
}