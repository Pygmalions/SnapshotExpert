using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SnapshotExpert.Data.Values;

public class ObjectValue() : SnapshotValue, IEnumerable<KeyValuePair<string, SnapshotNode>>
{
    private readonly OrderedDictionary<string, SnapshotNode> _nodes = [];

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

    internal override IEnumerable<SnapshotNode> DeclaredNodes => _nodes.Values;

    public override string DebuggerString => "Object";

    /// <summary>
    /// Count of nodes in this object value.
    /// </summary>
    public int Count => _nodes.Count;

    public SnapshotValue this[string name]
    {
        get => GetNode(name)?.Value ??
               throw new KeyNotFoundException($"Cannot find node '{name}' in this object value.");
        set => (GetNode(name) ?? CreateNode(name)).Value = value;
    }

    public IEnumerator<KeyValuePair<string, SnapshotNode>> GetEnumerator()
        => _nodes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _nodes.Values.GetEnumerator();

    internal override SnapshotNode? GetDeclaredNode(string name)
        => _nodes.GetValueOrDefault(name);

    public SnapshotNode? GetNode(int index) => index < Count ? _nodes.GetAt(index).Value : null;

    public SnapshotNode? GetNode(string name) => _nodes.GetValueOrDefault(name);

    public bool TryGetNode(string name, [MaybeNullWhen(false)] out SnapshotNode node)
        => _nodes.TryGetValue(name, out node);

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
        if (_nodes.ContainsKey(name))
            throw new ArgumentException(
                $"Node with name '{name}' already exists in this object value.",
                nameof(name));
        _nodes.Insert(index, name, node);
        return node;
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
        throw new ArgumentException(
            $"Node with name '{name}' already exists in the content of this value.",
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
    public bool Remove(string name)
        => _nodes.Remove(name, out _);

    /// <summary>
    /// Delete the node with the specified name from the content of this value.
    /// </summary>
    /// <param name="name">Name of the node to delete.</param>
    /// <param name="node">Removed node with the specified name.</param>
    /// <returns>True if the node is found and deleted, otherwise false.</returns>
    public bool Remove(string name, [MaybeNullWhen(false)] out SnapshotNode node)
        => _nodes.Remove(name, out node);

    /// <summary>
    /// Add a node to the end of this object value.
    /// </summary>
    /// <param name="node">Node to add.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if a node with the same name already exists in the content of this value.
    /// </exception>
    public void Add(SnapshotNode node)
    {
        if (!_nodes.TryAdd(node.Name, node))
            throw new ArgumentException(
                $"Node with name '{node.Name}' already exists in this object value.",
                nameof(node));
    }

    /// <summary>
    /// Remove the snapshot from this object value.
    /// </summary>
    /// <param name="node">Node to remove.</param>
    /// <returns></returns>
    public bool Remove(SnapshotNode node)
        => _nodes.Remove(node.Name);

    /// <summary>
    /// Compare the content of this object value with that of another object value.
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

    public override int GetContentHashCode() => _nodes.GetHashCode();
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
        if (value.GetNode(name) is { } node)
            return node;
        throw new KeyNotFoundException(
            $"Cannot find the required node '{name}' in this object value.");
    }
}