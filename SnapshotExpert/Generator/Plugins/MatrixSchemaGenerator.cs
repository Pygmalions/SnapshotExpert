using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;

namespace SnapshotExpert.Generator.Plugins;

public static class MatrixSchemaGenerator
{
    public static SnapshotSchema GenerateSchema(int rank, SnapshotSchema elementSchema)
    {
        var current = new ArraySchema()
        {
            Title = $"Matrix Dimension {rank - 1}",
            Items = elementSchema
        };
        for (var dimension = rank - 2; dimension >= 0; --dimension)
        {
            current = new ArraySchema
            {
                Title = $"Matrix Dimension {dimension}",
                Items = current
            };
        }

        return current;
    }
}