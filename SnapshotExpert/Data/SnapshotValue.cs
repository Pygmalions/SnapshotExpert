using System.Diagnostics;

namespace SnapshotExpert.Data;

[DebuggerDisplay("{DebuggerString,nq}")]
public abstract partial class SnapshotValue : ISnapshotConvertible
{
    internal SnapshotValue()
    {
    }

    /// <summary>
    /// The node that this value is bound to.
    /// By assigning this value to the <see cref="SnapshotNode.Value"/> property,
    /// the value is bound to the node.
    /// </summary>
    internal SnapshotNode? DeclaringNode { get; set; }

    /// <summary>
    /// Nodes declared by this value.
    /// </summary>
    internal virtual IEnumerable<SnapshotNode> DeclaredNodes => [];

    /// <summary>
    /// Get the declared node with the specified name.
    /// </summary>
    /// <param name="name">Name of the node declared by this value.</param>
    /// <returns>Located node, or null if not found.</returns>
    internal virtual SnapshotNode? GetDeclaredNode(string name) => null;
    
    /// <summary>
    /// Human-readable string for this node to display in the debugger.
    /// </summary>
    public abstract string DebuggerString { get; }

    /// <summary>
    /// Compare the content of this value with another value.
    /// </summary>
    /// <param name="value">Value to compare to.</param>
    /// <returns>
    /// True if the contents of the two values are equal; otherwise false.
    /// </returns>
    public abstract bool ContentEquals(SnapshotValue? value);

    /// <summary>
    /// Get the content hash code of this value.
    /// </summary>
    public abstract int GetContentHashCode();

    /// <summary>
    /// Compares the content of two snapshot values.
    /// Two values are considered equal if they are both null or their content is equal.
    /// </summary>
    public static bool ContentEquals(SnapshotValue? x, SnapshotValue? y)
        => ReferenceEquals(x, y) || (x?.ContentEquals(y) ?? y == null);

    SnapshotValue ISnapshotConvertible.Value => this;
}