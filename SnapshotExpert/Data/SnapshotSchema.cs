using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data;

public abstract record SnapshotSchema
{
    /// <summary>
    /// Title of the schema.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Description of the schema.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Default value for this property.
    /// </summary>
    public SnapshotValue? DefaultValue { get; init; }

    /// <summary>
    /// Constant value for this property; this property can only be this value.
    /// </summary>
    public SnapshotValue? ConstantValue { get; init; }

    /// <summary>
    /// The value of this property must be one of the specified values.
    /// </summary>
    public IReadOnlyCollection<SnapshotValue>? EnumValues { get; init; }

    /// <summary>
    /// If true, then this schema can accept null value.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// This property is write-only.
    /// </summary>
    public bool? IsWriteOnly { get; init; }

    /// <summary>
    /// This property is read-only.
    /// </summary>
    public bool? IsReadOnly { get; init; }

    protected abstract void OnGenerate(ObjectValue schema);

    protected abstract bool OnValidate(SnapshotNode node);

    /// <summary>
    /// Generate the schema into the specified snapshot node.
    /// </summary>
    /// <param name="schema">Object value to generate schema into.</param>
    public virtual void Generate(ObjectValue schema)
    {
        if (!string.IsNullOrEmpty(Title))
            schema.CreateNode("title").BindValue(Title);

        if (!string.IsNullOrEmpty(Description))
            schema.CreateNode("description").BindValue(Description);

        if (DefaultValue != null)
            schema.CreateNode("default", DefaultValue);

        if (ConstantValue != null)
            schema.CreateNode("const", ConstantValue);

        if (EnumValues != null)
        {
            var enumsArray = schema.CreateNode("enum").AssignValue(new ArrayValue());
            foreach (var enumValue in EnumValues)
            {
                enumsArray.CreateNode(enumValue);
            }
        }

        if (IsReadOnly != null)
            schema.CreateNode("readOnly").BindValue(IsReadOnly.Value);

        if (IsWriteOnly != null)
            schema.CreateNode("writeOnly").BindValue(IsWriteOnly.Value);

        OnGenerate(schema);
    }

    /// <summary>
    /// Validate the specified snapshot node against the schema.
    /// </summary>
    /// <param name="node">Snapshot node to validate.</param>
    /// <returns>True if the node is valid; otherwise, false.</returns>
    public virtual bool Validate(SnapshotNode node)
    {
        if (ConstantValue != null && !ConstantValue.ContentEquals(node.Value))
            return false;
        if (EnumValues != null &&
            !EnumValues.Contains(node.Value, SnapshotValueContentEqualityComparer.Instance))
            return false;
        return node.Value is null or NullValue ? IsNullable : OnValidate(node);
    }
}

public static class SchemaModelExtensions
{
    extension(SnapshotSchema self)
    {
        public ObjectValue ToSnapshotValue()
        {
            var value = new ObjectValue();
            self.Generate(value);
            return value;
        }

        /// <summary>
        /// Create a snapshot node bound to this value.
        /// </summary>
        /// <param name="name">Name of this node.</param>
        /// <returns>Created node which bound with this value.</returns>
        public SnapshotNode ToSnapshotNode(string name = "#") => new(name)
        {
            Value = self.ToSnapshotValue()
        };
    }
}