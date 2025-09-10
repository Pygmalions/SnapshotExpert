using System.Text;
using MongoDB.Bson;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public class BinarySchema : SnapshotSchema
{
    private static readonly ObjectSchema Schema = new()
    {
        RequiredProperties = new OrderedDictionary<string, SnapshotSchema>
        {
            ["$binary"] = new ObjectSchema
            {
                RequiredProperties = new OrderedDictionary<string, SnapshotSchema>
                {
                    ["base64"] = new TypesSchema(JsonValueType.String)
                    {
                        Description = "The base64-encoded binary data."
                    },
                    ["subType"] = new TypesSchema(JsonValueType.String, JsonValueType.Integer)
                    {
                        Description = GenerateSubTypeDescription()
                    }
                }
            }
        }
    };

    private static string GenerateSubTypeDescription()
    {
        var description = new StringBuilder();
        description.AppendLine("Semantic meaning of this binary data, encoded as 2-digit hexadecimal number: ");
        description.Append(((int)BsonBinarySubType.UserDefined).ToString("x2"));
        description.Append(" - Unknown or custom bytes; ");
        description.Append(((int)BsonBinarySubType.MD5).ToString("x2"));
        description.Append(" - Hash; ");
        description.Append(((int)BsonBinarySubType.UuidStandard).ToString("x2"));
        description.Append(" - Guid; ");
        description.Append(((int)BsonBinarySubType.Vector).ToString("x2"));
        description.Append(" - Vector; ");
        description.Append(((int)BsonBinarySubType.Function).ToString("x2"));
        description.Append(" - Function; ");
        description.Append(((int)BsonBinarySubType.Encrypted).ToString("x2"));
        description.Append(" - Encrypted bytes; ");
        description.Append(((int)BsonBinarySubType.Sensitive).ToString("x2"));
        description.Append(" - Sensitive bytes; ");

        return description.ToString();
    }

    protected override void OnGenerate(ObjectValue schema)
    {
        Schema.Generate(schema);
    }

    protected override bool OnValidate(SnapshotNode node) 
        => node.Value is BinaryValue;
}