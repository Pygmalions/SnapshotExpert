using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives;

public class Integer8SnapshotSerializer : SnapshotSerializer<sbyte>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Signed 8-bit Integer",
            Minimum = sbyte.MinValue,
            Maximum = sbyte.MaxValue,
        };
    }

    public override void NewInstance(out sbyte instance) => instance = 0;

    public override void SaveSnapshot(in sbyte target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer32Value(target);

    public override void LoadSnapshot(ref sbyte target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = (sbyte)snapshot.RequireNumber<IInteger32Number>().Value;
}

public class UnsignedInteger8SnapshotSerializer : SnapshotSerializer<byte>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Unsigned 8-bit Integer",
            Minimum = byte.MinValue,
            Maximum = byte.MaxValue,
        };
    }
    
    public override void NewInstance(out byte instance) => instance = 0;

    public override void SaveSnapshot(in byte target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer32Value(target);

    public override void LoadSnapshot(ref byte target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = (byte)snapshot.RequireNumber<IInteger32Number>().Value;
}

public class Integer16SnapshotSerializer : SnapshotSerializer<short>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Signed 16-bit Integer",
            Minimum = short.MinValue,
            Maximum = short.MaxValue,
        };
    }
    
    public override void NewInstance(out short instance) => instance = 0;

    public override void SaveSnapshot(in short target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer32Value(target);

    public override void LoadSnapshot(ref short target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = (short)snapshot.RequireNumber<IInteger32Number>().Value;
}

public class UnsignedInteger16SnapshotSerializer : SnapshotSerializer<ushort>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Unsigned 16-bit Integer",
            Minimum = ushort.MinValue,
            Maximum = ushort.MaxValue,
        };
    }
    
    public override void NewInstance(out ushort instance) => instance = 0;

    public override void SaveSnapshot(in ushort target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer32Value(target);

    public override void LoadSnapshot(ref ushort target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = (ushort)snapshot.RequireNumber<IInteger32Number>().Value;
}

public class Integer32SnapshotSerializer : SnapshotSerializer<int>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Signed 32-bit Integer",
        };
    }
    
    public override void NewInstance(out int instance) => instance = 0;

    public override void SaveSnapshot(in int target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer32Value(target);

    public override void LoadSnapshot(ref int target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.RequireNumber<IInteger32Number>().Value;
}

public class UnsignedInteger32SnapshotSerializer : SnapshotSerializer<uint>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Unsigned 32-bit Integer",
        };
    }
    
    public override void NewInstance(out uint instance) => instance = 0;

    public override void SaveSnapshot(in uint target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer32Value((int)target);

    public override void LoadSnapshot(ref uint target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = (uint)snapshot.RequireNumber<IInteger32Number>().Value;
}


public class Integer64SnapshotSerializer : SnapshotSerializer<long>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Signed 64-bit Integer",
        };
    }
    
    public override void NewInstance(out long instance) => instance = 0;

    public override void SaveSnapshot(in long target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer64Value(target);

    public override void LoadSnapshot(ref long target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = snapshot.RequireNumber<IInteger32Number>().Value;
}

public class UnsignedInteger64SnapshotSerializer : SnapshotSerializer<ulong>
{
    protected override SnapshotSchema GenerateSchema()
    {
        return new IntegerSchema
        {
            Title = "Unsigned 64-bit Integer",
            Minimum = 0,
        };
    }
    
    public override void NewInstance(out ulong instance) => instance = 0;

    public override void SaveSnapshot(in ulong target, SnapshotNode snapshot, SnapshotWritingScope scope)
        => snapshot.Value = new Integer64Value((long)target);

    public override void LoadSnapshot(ref ulong target, SnapshotNode snapshot, SnapshotReadingScope scope)
        => target = (ulong)snapshot.RequireNumber<IInteger64Number>().Value;
}