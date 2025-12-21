using System.Text.Json;
using System.Text.Json.Serialization;
using SnapshotExpert.Data;
using SnapshotExpert.Data.IO;

namespace SnapshotExpert.Adapters;

public class SnapshotToJsonConverterAdapter<TTarget>(SnapshotSerializer<TTarget> serializer) : JsonConverter<TTarget>
{
    public override TTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = SnapshotNode.Parse(ref reader);
        serializer.NewInstance(out var target);
        serializer.LoadSnapshot(ref target, node);
        return target;
    }

    public override void Write(Utf8JsonWriter writer, TTarget value, JsonSerializerOptions options)
    {
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        node.Dump(writer);
    }
}

public class SnapshotToJsonConverterFactory(ISerializerProvider provider) : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => provider.GetSerializer(typeToConvert) is not null;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (provider.GetSerializer(typeToConvert) is not { } serializer)
            return null;
        return (JsonConverter?)Activator.CreateInstance(
            typeof(SnapshotToJsonConverterAdapter<>).MakeGenericType(typeToConvert),
            serializer);
    }
}

public static class SnapshotToJsonConverterAdapterExtensions
{
    extension(JsonSerializerOptions self)
    {
        public JsonSerializerOptions WithSnapshotSerializers(ISerializerProvider provider)
        {
            self.Converters.Add(new SnapshotToJsonConverterFactory(provider));
            return self;
        }
    }
}