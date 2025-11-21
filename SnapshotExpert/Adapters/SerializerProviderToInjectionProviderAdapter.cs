using InjectionExpert;

namespace SnapshotExpert.Adapters;

public class SerializerProviderToInjectionProviderAdapter(ISerializerProvider provider) : IInjectionProvider
{
    public InjectionItem? GetInjectionItem(Type type, object? key, InjectionTarget target)
    {
        if (type.IsInstanceOfType(this))
            return InjectionItem.Singleton(this);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SnapshotSerializer<>))
        {
            var serializer = provider.GetSerializer(type.GetGenericArguments()[0]);
            return serializer != null ? InjectionItem.Singleton(serializer) : null;
        }

        return null;
    }

    public IInjectionProvider.IScope NewScope(InjectionTarget target)
        => InjectionScope.New(this, null, target);
}