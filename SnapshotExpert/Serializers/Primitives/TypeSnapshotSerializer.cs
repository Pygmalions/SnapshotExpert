using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class TypeSnapshotSerializer : SnapshotSerializer<Type>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new StringSchema
        {
            Title = "Type Name",
            Description = "Assembly qualified name of the type, " +
                          "consisting of assembly name and type name, " +
                          "separated by comma, e.g. 'System.String, System.Private.CoreLib'."
        };
    }

    public override void NewInstance(out Type instance) => instance = null!;
    
    public override void SaveSnapshot(in Type target, SnapshotNode snapshot, SnapshotWritingScope scope)
    {
        if (target == null!)
        {
            snapshot.Value = new NullValue();
            return;
        }
        if (target.AssemblyQualifiedName is not { } typeString)
            throw new Exception(
                $"Failed to save snapshot: Type '{target}' has no assembly qualified name.");
        typeString = string.Join(',', typeString.Split(',')[..2]);
        snapshot.Value = new StringValue(typeString);
    }

    public override void LoadSnapshot(ref Type target, SnapshotNode snapshot, SnapshotReadingScope scope)
    {
        if (snapshot.Value is NullValue)
        {
            target = null!;
            return;
        }
        var typeString = snapshot.RequireValue<StringValue>().Value;
        target = Type.GetType(typeString) ?? throw new Exception(
            $"Failed to load snapshot: Type '{typeString}' could not be found.");
    }
}