using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestDateTimeSerializers
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(container.RequireSerializer<DateTime>(),
                Is.TypeOf<DateTimeSnapshotSerializer>());
            Assert.That(container.RequireSerializer<DateTimeOffset>(),
                Is.TypeOf<DateTimeOffsetSnapshotSerializer>());
        }
    }

    [Test]
    public void SaveSnapshot_DateTime_Local()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = DateTime.Now;
        var serializer = container.RequireSerializer<DateTime>();

        serializer.SaveSnapshot(value, node);
        // Note: DateTimeSnapshotSerializer converts to DateTimeOffset then back to local time, 
        // precision might be affected depending on how AsDateTimeOffset is implemented, 
        // but it should be equal within a reasonable range or exactly if the value is compatible.
        Assert.That(node.AsDateTimeOffset.DateTime.ToLocalTime(),
            Is.EqualTo(value).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void SaveSnapshot_DateTime_UTC()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = DateTime.UtcNow;
        var serializer = container.RequireSerializer<DateTime>();

        serializer.SaveSnapshot(value, node);
        // Note: DateTimeSnapshotSerializer converts to DateTimeOffset then back to local time, 
        // precision might be affected depending on how AsDateTimeOffset is implemented, 
        // but it should be equal within a reasonable range or exactly if the value is compatible.
        Assert.That(node.AsDateTimeOffset.DateTime,
            Is.EqualTo(value).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void LoadSnapshot_DateTime_Local()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = DateTime.Now;
        node.Value = new DateTimeValue(value);

        var serializer = container.RequireSerializer<DateTime>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized,
            Is.EqualTo(value).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void LoadSnapshot_DateTime_UTC()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = DateTime.UtcNow;
        node.Value = new DateTimeValue(value);

        var serializer = container.RequireSerializer<DateTime>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized.ToUniversalTime(),
            Is.EqualTo(value).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void SaveSnapshot_DateTimeOffset()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = DateTimeOffset.Now;
        var serializer = container.RequireSerializer<DateTimeOffset>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsDateTimeOffset, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot_DateTimeOffset()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = DateTimeOffset.Now;
        node.Value = new DateTimeValue(value);

        var serializer = container.RequireSerializer<DateTimeOffset>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}