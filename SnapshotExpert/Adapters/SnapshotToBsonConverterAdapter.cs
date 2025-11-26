using MongoDB.Bson.Serialization;
using SnapshotExpert.Data;
using SnapshotExpert.Data.IO;

namespace SnapshotExpert.Adapters;

public class SnapshotToBsonConverterAdapter<TTarget>(SnapshotSerializer<TTarget> serializer) : IBsonSerializer<TTarget>
{
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var node = SnapshotNode.Parse(context.Reader);
        serializer.NewInstance(out var target);
        serializer.LoadSnapshot(ref target, node);
        return target!;
    }

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        var node = new SnapshotNode();
        serializer.SaveSnapshot((TTarget)value, node);
        node.Dump(context.Writer);
    }

    public Type ValueType => serializer.TargetType;

    public TTarget Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var node = SnapshotNode.Parse(context.Reader);
        serializer.NewInstance(out var target);
        serializer.LoadSnapshot(ref target, node);
        return target!;
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TTarget value)
    {
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        node.Dump(context.Writer);
    }
}

public class SnapshotToBsonConverterFactory(ISerializerProvider provider) : IBsonSerializationProvider
{
    public IBsonSerializer GetSerializer(Type type)
    {
        if (provider.GetSerializer(type) is not { } serializer)
            return null!;
        return (IBsonSerializer)Activator.CreateInstance(
            typeof(SnapshotToBsonConverterAdapter<>).MakeGenericType(type), serializer)!;
    }
}

public class AsyncLocalSnapshotToBsonConverterFactory : IBsonSerializationProvider
{
    private static readonly AsyncLocal<ISerializerProvider> LocalProvider = new();

    public static ISerializerProvider? Provider
    {
        get => LocalProvider.Value;
        set => LocalProvider.Value = value!;
    }

    public IBsonSerializer GetSerializer(Type type)
    {
        if (LocalProvider.Value?.GetSerializer(type) is not { } serializer)
            return null!;
        return (IBsonSerializer)Activator.CreateInstance(
            typeof(SnapshotToBsonConverterAdapter<>).MakeGenericType(type), serializer)!;
    }
}

public static class SnapshotToBsonConverterAdapterExtensions
{
    extension(BsonSerializerRegistry self)
    {
        public BsonSerializerRegistry WithSnapshotSerializers(ISerializerProvider provider)
        {
            self.RegisterSerializationProvider(new SnapshotToBsonConverterFactory(provider));
            return self;
        }
    }
}