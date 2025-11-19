using InjectionExpert;
using SnapshotExpert.Remoting.Serializers;

namespace SnapshotExpert.Remoting;

public static class SerializerContainerExtensionsForRemoting
{
    private static SnapshotSerializer? CreateSerializerForRemoting(
        Type targetType, IInjectionProvider provider)
    {
        if (!targetType.IsGenericType)
            return null;
        var definition = targetType.GetGenericTypeDefinition();
        var arguments = targetType.GetGenericArguments();
        if (definition == typeof(Task<>))
            return (SnapshotSerializer?)provider.NewObject(
                typeof(GenericTaskSynchronousSerializer<>).MakeGenericType(arguments[0]));
        if (definition == typeof(ValueTask<>))
            return (SnapshotSerializer?)provider.NewObject(
                typeof(GenericValueTaskByWaitingSerializer<>).MakeGenericType(arguments[0]));
        return null;
    }

    public static TContainer UseRemotingSerializers<TContainer>(this TContainer container)
        where TContainer : ISerializerContainer
    {
        container.WithSerializer<CancellationToken, CancellationTokenEmptySerializer>();
        container.WithSerializer<Task, TaskSynchronousSerializer>();
        container.WithSerializer<ValueTask, ValueTaskSynchronousSerializer>();
        container.WithFactory(CreateSerializerForRemoting);
        
        return container;
    }
}