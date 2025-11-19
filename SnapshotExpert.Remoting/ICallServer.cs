using System.Reflection;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Remoting;

public interface ICallServer
{
    /// <summary>
    /// Handle a remote call.
    /// </summary>
    /// <param name="target">Target instance to invoke the method on.</param>
    /// <param name="method">
    /// Metadata token of the method to invoke.
    /// <seealso cref="MethodInfo.MetadataToken"/>
    /// </param>
    /// <param name="arguments">
    /// Optional arguments to pass to the method.
    /// </param>
    /// <returns>
    /// The return value, or null if the method returns void.
    /// </returns>
    SnapshotValue? HandleCall(object target, int method, ObjectValue? arguments);
}

public static class CallServerExtensions
{
    extension(ICallServer self)
    {
        public SnapshotValue? HandleCall(object target, MethodInfo method, ObjectValue arguments) 
            => self.HandleCall(target, method.MetadataToken, arguments);
    }
}