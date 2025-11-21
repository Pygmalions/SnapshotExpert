using InjectionExpert;

namespace SnapshotExpert.Adapters;

public class SerializerProviderFromInjectionProviderAdapter(IInjectionProvider provider) : ISerializerProvider
{
    public SnapshotSerializer? GetSerializer(Type targetType)
        => (SnapshotSerializer?)provider.GetInjection(typeof(SnapshotSerializer<>).MakeGenericType(targetType));
}

public static class SerializerProviderFromInjectionProviderAdapterExtensions
{
    extension(IInjectionProvider provider)
    {
        public ISerializerProvider AsSerializers() 
            => new SerializerProviderFromInjectionProviderAdapter(provider);
    }
}