using System.Diagnostics;

namespace SnapshotExpert.Data;

[DebuggerDisplay("{DebuggerString,nq}")]
public abstract partial class SnapshotValue
{
    internal SnapshotValue() { }
    
    /// <summary>
    /// The node that this value is bound to.
    /// </summary>
    public SnapshotNode? Node { get; internal set; }

    /// <summary>
    /// Compare the content of this value with another value.
    /// </summary>
    /// <param name="value">Value to compare to.</param>
    /// <returns>
    /// True if the content of the two values are equal; otherwise false.
    /// </returns>
    public abstract bool ContentEquals(SnapshotValue? value);

    /// <summary>
    /// Get the content hash code of this value.
    /// </summary>
    public abstract int GetContentHashCode();

    /// <summary>
    /// Get the node with the specified name in the content of this value.
    /// </summary>
    /// <param name="name">Name of the node to locate.</param>
    /// <returns>Located node, or null if not found.</returns>
    internal abstract SnapshotNode? this[string name] { get; }

    /// <summary>
    /// Human-readable string for this node to display in the debugger.
    /// </summary>
    internal abstract string DebuggerString { get; }
    
    /// <summary>
    /// Compares the content of two snapshot values.
    /// Two values are considered equal if they are both null or their content is equal.
    /// </summary>
    public static bool ContentEquals(SnapshotValue? x, SnapshotValue? y)
        => ReferenceEquals(x, y) || (x?.ContentEquals(y) ?? y == null);
}