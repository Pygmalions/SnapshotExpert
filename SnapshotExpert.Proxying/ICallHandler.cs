using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Remoting;

public interface ICallHandler
{
    /// <summary>
    /// Handle a serialized call.
    /// </summary>
    /// <param name="arguments">
    /// Arguments to pass to the method.
    /// If the proxied delegate has no arguments, then null should be passed.
    /// </param>
    /// <param name="cancellation">Cancellation token to pass to the proxied delegate.</param>
    /// <returns>
    /// The return value, or null if the method returns void.
    /// </returns>
    ValueTask<SnapshotValue?> HandleCall(
        ObjectValue? arguments,
        CancellationToken cancellation = default);
}