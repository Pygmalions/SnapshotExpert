﻿using System.Collections;

namespace SnapshotExpert.Framework.Values;

public class ArrayValue(int capacity = 0) : SnapshotValue, 
    IReadOnlyCollection<SnapshotNode>, IEnumerable<SnapshotValue>
{
    private readonly List<SnapshotNode> _nodes = new(capacity);

    internal override SnapshotNode? this[string name]
    {
        get
        {
            if (int.TryParse(name, out var index) && index >= 0 && index < _nodes.Count)
                return _nodes[index];
            return null;
        }
    }

    internal override string DebuggerString => "Array";
    
    /// <summary>
    /// Content nodes defined in this array value.
    /// </summary>
    public IReadOnlyList<SnapshotNode> Nodes => _nodes;
    
    /// <summary>
    /// Count of nodes in this array value.
    /// </summary>
    public int Count => _nodes.Count;
    
    /// <summary>
    /// Construct an array value with the specified content.
    /// </summary>
    /// <param name="values">Array of snapshot value to add into this array.</param>
    public ArrayValue(IEnumerable<SnapshotValue> values) : this()
    {
        foreach (var item in values)
            CreateNode().Value = item;
    }
    
    /// <summary>
    /// Create a node in the content of this array value.
    /// </summary>
    /// <returns>Created node.</returns>
    public SnapshotNode CreateNode()
    {
        var node = new SnapshotNode(this, _nodes.Count.ToString());
        _nodes.Add(node);
        return node;
    }
    
    /// <summary>
    /// Create a node in the content of this array value.
    /// </summary>
    /// <param name="value">Value to assign to the created node.</param>
    /// <returns>Created node.</returns>
    public SnapshotNode CreateNode(SnapshotValue value)
    {
        var node = CreateNode();
        node.Value = value;
        return node;
    }

    /// <summary>
    /// Delete the node with the specified name from the content of this value.
    /// </summary>
    /// <param name="index">Index of the node to delete.</param>
    /// <returns>True if the node is found and deleted, otherwise false.</returns>
    public void DeleteNode(int index)
    {
        if (index < 0 || index >= _nodes.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index of the node to delete is out of range.");
        _nodes[index].Detach();
        _nodes.RemoveAt(index);

        // Rename nodes.
        foreach (var pair in _nodes.Index())
            pair.Item.Name = pair.Index.ToString();
    }

    /// <summary>
    /// Access the snapshot node at the specified index in the content of this array value.
    /// </summary>
    /// <param name="index">Index of the specified snapshot node.</param>
    public SnapshotNode this[int index] => _nodes[index];

    /// <summary>
    /// Compare the content this array value with that of another array value.
    /// These contents are considered as equal when satisfying all requirements: <br/>
    /// - Both values have the same number of nodes in their content. <br/>
    /// - Both values have nodes with the same names in their content. <br/>
    /// - Corresponding nodes in both values have equal content (recursively). <br/>
    /// </summary>
    public override bool ContentEquals(SnapshotValue? value)
    {
        if (value is not ArrayValue other)
            return false;
        if (_nodes.Count != other._nodes.Count)
            return false;
        foreach (var (index, node) in _nodes.Index())
        {
            var otherNode = other._nodes[index];
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

    public IEnumerator<SnapshotValue> GetEnumerator() 
        => _nodes.Select(node => node.Value!).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

    IEnumerator<SnapshotNode> IEnumerable<SnapshotNode>.GetEnumerator() => _nodes.GetEnumerator();
}