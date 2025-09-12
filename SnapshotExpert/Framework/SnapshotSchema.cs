using MongoDB.Bson;
using MongoDB.Bson.IO;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework;

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
            schema.CreateNode("title").AssignValue(Title);

        if (!string.IsNullOrEmpty(Description))
            schema.CreateNode("description").AssignValue(Description);

        if (DefaultValue != null)
            schema.CreateNode("default", DefaultValue);

        if (ConstantValue != null)
            schema.CreateNode("const", ConstantValue);

        if (EnumValues != null)
        {
            var enumsArray = schema.CreateNode("enum").AssignArray();
            foreach (var enumValue in EnumValues)
            {
                enumsArray.CreateNode(enumValue);
            }
        }

        if (IsReadOnly != null)
            schema.CreateNode("readOnly").AssignValue(IsReadOnly.Value);

        if (IsWriteOnly != null)
            schema.CreateNode("writeOnly").AssignValue(IsWriteOnly.Value);

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
    public static BsonDocument ToBsonDocument(this SnapshotSchema model)
    {
        var node = new SnapshotNode();
        model.Generate(node.AssignObject());
        return node.ToBsonDocument();
    }

    public static string ToJson(this SnapshotSchema model, JsonWriterSettings? settings = null)
    {
        var node = new SnapshotNode();
        model.Generate(node.AssignObject());
        return node.ToJson(settings ?? new JsonWriterSettings { Indent = true });
    }

    public static byte[] ToBson(this SnapshotSchema model)
    {
        var node = new SnapshotNode();
        model.Generate(node.AssignObject());
        return node.ToBson();
    }
}