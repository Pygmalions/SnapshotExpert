using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Remoting;

public interface ICallProxy
{
    /// <summary>
    /// Proxy a serialized call.
    /// </summary>
    /// <param name="arguments">Arguments </param>
    /// <returns></returns>
    ValueTask<SnapshotValue?> Call(ObjectValue arguments);
}

public static class CallProxyExtensions
{
    private class LambdaCallProxy(Func<ObjectValue, ValueTask<SnapshotValue?>> call) : ICallProxy
    {
        public ValueTask<SnapshotValue?> Call(ObjectValue arguments)
            => call(arguments);
    }

    extension(ICallProxy)
    {
        public static ICallProxy FromFunctor(Func<ObjectValue, ValueTask<SnapshotValue?>> functor)
            => new LambdaCallProxy(functor);

        public static ICallProxy FromFunctor(Func<ObjectValue, SnapshotValue?> action)
            => new LambdaCallProxy(arguments => ValueTask.FromResult(action(arguments)));
    }
}