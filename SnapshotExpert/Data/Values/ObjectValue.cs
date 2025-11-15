using System.Collections;

namespace SnapshotExpert.Data.Values;

public class ObjectValue() : SnapshotValue,
    IReadOnlyCollection<SnapshotNode>, IEnumerable<KeyValuePair<string, SnapshotValue?>>
{
    private readonly OrderedDictionary<string, SnapshotNode> _nodes = [];

    internal override SnapshotNode? this[string name] => _nodes.GetValueOrDefault(name);

    internal override string DebuggerString => "Object";

    /// <summary>
    /// Content nodes defined in this object value.
    /// </summary>
    public IReadOnlyDictionary<string, SnapshotNode> Nodes => _nodes;

    /// <summary>
    /// Count of nodes in this object value.
    /// </summary>
    public int Count => _nodes.Count;

    /// <summary>
    /// Construct an object value with the specified content.
    /// </summary>
    /// <param name="content">Key-value pairs of snapshot values to add to this object value.</param>
    public ObjectValue(IEnumerable<KeyValuePair<string, SnapshotValue>> content) : this()
    {
        foreach (var (name, member) in content)
            CreateNode(name).Value = member;
    }

    /// <summary>
    /// Construct an object value with the specified content.
    /// This constructor allows using only 'new' keyword to create an object value with initial content.
    /// </summary>
    /// <param name="content">Dictionary of snapshot values to add to this object value.</param>
    public ObjectValue(OrderedDictionary<string, SnapshotValue> content)
        : this((IEnumerable<KeyValuePair<string, SnapshotValue>>)content)
    {
    }

    /// <summary>
    /// Create a node at the specified index in the content of this value.
    /// </summary>
    /// <param name="index">Index to create this node at.</param>
    /// <param name="name">Name for the created node to use.</param>
    /// <returns>Created node.</returns>
    /// <exception cref="ArgumentException">
    /// Throw if a node with the same name already exists in the content of this value.
    /// </exception>
    public SnapshotNode InsertNode(int index, string name)
    {
        var node = new SnapshotNode(this, name);
        if (!_nodes.ContainsKey(name))
        {
            _nodes.Insert(index, name, node);
            return node;
        }

        node.Detach();
        throw new ArgumentException($"Node with name '{name}' already exists in the content of this value.",
            nameof(name));
    }

    /// <summary>
    /// Create a node in the content of this value.
    /// </summary>
    /// <param name="name">Name for the created node to use.</param>
    /// <returns>Created node.</returns>
    /// <exception cref="ArgumentException">
    /// Throw if a node with the same name already exists in the content of this value.
    /// </exception>
    public SnapshotNode CreateNode(string name)
    {
        var node = new SnapshotNode(this, name);
        if (_nodes.TryAdd(name, node))
            return node;
        node.Detach();
        throw new ArgumentException($"Node with name '{name}' already exists in the content of this value.",
            nameof(name));
    }

    /// <summary>
    /// Create a node in the content of this value.
    /// </summary>
    /// <param name="name">Name for the created node to use.</param>
    /// <param name="value">Value to assign to the created node.</param>
    /// <returns>Created node.</returns>
    /// <exception cref="ArgumentException">
    /// Throw if a node with the same name already exists in the content of this value.
    /// </exception>
    public SnapshotNode CreateNode(string name, SnapshotValue value)
    {
        var node = CreateNode(name);
        node.Value = value;
        return node;
    }

    /// <summary>
    /// Delete the node with the specified name from the content of this value.
    /// </summary>
    /// <param name="name">Name of the node to delete.</param>
    /// <returns>True if the node is found and deleted, otherwise false.</returns>
    public bool DeleteNode(string name)
    {
        if (!_nodes.Remove(name, out var node))
            return false;
        node.Detach();
        return true;
    }

    /// <summary>
    /// Compare the content this object value with that of another object value.
    /// These contents are considered as equal when satisfying all requirements: <br/>
    /// - Both values have the same number of nodes in their content. <br/>
    /// - Both values have nodes with the same names in their content. <br/>
    /// - Corresponding nodes in both values have equal content (recursively). <br/>
    /// </summary>
    public override bool ContentEquals(SnapshotValue? value)
    {
        if (value is not ObjectValue other)
            return false;

        if (_nodes.Count != other._nodes.Count)
            return false;

        foreach (var (name, node) in _nodes)
        {
            if (!other._nodes.TryGetValue(name, out var otherNode))
                return false;
            if (node.Value == null && otherNode.Value == null)
                continue;
            if (node.Value == null || otherNode.Value == null)
                return false;
            if (!node.Value.ContentEquals(otherNode.Value))
                return false;
        }

        return true;
    }

    private static int ConstantContentHashCode { get; } = typeof(ArrayValue).GetHashCode();

    public override int GetContentHashCode() => ConstantContentHashCode;

    public IEnumerator<KeyValuePair<string, SnapshotValue?>> GetEnumerator()
        => _nodes
            .Select(pair => new KeyValuePair<string, SnapshotValue?>(
                pair.Key, pair.Value.Value!))
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _nodes.Values.GetEnumerator();

    IEnumerator<SnapshotNode> IEnumerable<SnapshotNode>.GetEnumerator() => _nodes.Values.GetEnumerator();
}

public static class ObjectValueExtensions
{
    /// <summary>
    /// Get the node from the content of the object value with the specified name,
    /// or throw an exception if such node does not exist.
    /// </summary>
    /// <param name="value">Object value to get the node from.</param>
    /// <param name="name">Name of the node to get.</param>
    public static SnapshotNode RequireNode(this ObjectValue value, string name)
    {
        if (value.Nodes.TryGetValue(name, out var node))
            return node;
        throw new KeyNotFoundException(
            $"Required node with name '{name}' does not exist in the content of this object value.");
    }
}