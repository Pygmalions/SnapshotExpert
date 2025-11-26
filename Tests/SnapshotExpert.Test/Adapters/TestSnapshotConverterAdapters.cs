using System.Text.Json;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using SnapshotExpert.Adapters;

namespace SnapshotExpert.Test.Adapters;

[TestFixture]
public class TestSnapshotConverterAdapters
{
    [Test]
    public void Json_SerializeDeserialize_Type_Roundtrip()
    {
        var container = new SerializerContainer();

        var options = new JsonSerializerOptions()
            .WithSnapshotSerializers(container);

        var original = typeof(Dictionary<string, int>);

        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<Type>(json, options);

        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void Bson_SerializeDeserialize_Type_Roundtrip()
    {
        var container = new SerializerContainer();

        // Register the Snapshot serializers as BSON converters
        BsonSerializer.RegisterSerializationProvider(new AsyncLocalSnapshotToBsonConverterFactory());

        AsyncLocalSnapshotToBsonConverterFactory.Provider = container;

        var original = typeof(List<Guid>);

        // Serialize to BSON bytes
        byte[] bytes;
        using (var ms = new MemoryStream())
        using (var writer = new BsonBinaryWriter(ms, new BsonBinaryWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteName("$type");
            // Older MongoDB.Bson API requires passing the writer directly
            BsonSerializer.Serialize(writer, original);
            writer.WriteEndDocument();
            bytes = ms.ToArray();
        }

        // Deserialize from BSON bytes
        Type deserialized;
        using (var ms = new MemoryStream(bytes))
        using (var reader = new BsonBinaryReader(ms, new BsonBinaryReaderSettings()))
        {
            reader.ReadStartDocument();
            reader.ReadName("$type");
            deserialized = BsonSerializer.Deserialize<Type>(reader);
            reader.ReadEndDocument();
        }

        Assert.That(deserialized, Is.EqualTo(original));
    }
}