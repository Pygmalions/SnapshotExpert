namespace SnapshotExpert.Data;

public enum SnapshotDataFormat
{
    /// <summary>
    /// This snapshot will be stored in a binary format, such as BSON.
    /// </summary>
    Binary,
    /// <summary>
    /// This snapshot will be stored in a text-based format, such as JSON.
    /// </summary>
    Textual,
}