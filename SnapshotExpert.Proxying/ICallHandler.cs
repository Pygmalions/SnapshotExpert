using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Remoting;

public interface ICallHandler
{
    /// <summary>
    /// Handle a serialized call.
    /// </summary>
    /// <param name="arguments">
    /// Optional arguments to pass to the method.
    /// </param>
    /// <returns>
    /// The return value, or null if the method returns void.
    /// </returns>
    SnapshotValue? HandleCall(ObjectValue? arguments);
}