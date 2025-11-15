using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas;

public abstract record class PrimitiveSchema : SnapshotSchema
{
    protected PrimitiveSchema(params Span<JsonValueType> types)
    {
        var valueTypes = new HashSet<JsonValueType>();
        foreach (var type in types)
            valueTypes.Add(type);
        AllowedTypes = valueTypes;
    }

    /// <summary>
    /// Non-nullable value types that this schema allows.
    /// </summary>
    public IReadOnlySet<JsonValueType> AllowedTypes { get; } 

    public override void Generate(ObjectValue schema)
    {
        var typeNode = schema.CreateNode("type");
        var typeCount = AllowedTypes.Count + (IsNullable ? 1 : 0);
        switch (typeCount)
        {
            case < 1:
                throw new Exception("Types cannot be empty.");
            case 1:
                typeNode.AssignValue(AllowedTypes.First().ToTypeName());
                break;
            default:
            {
                var typesArray = typeNode.AssignArray();
                foreach (var type in AllowedTypes)
                    typesArray.CreateNode().AssignValue(type.ToTypeName());
                break;
            }
        }

        base.Generate(schema);
    }
}