using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data;

public partial class SnapshotValue
{
    public static implicit operator SnapshotValue(bool value) => new BooleanValue(value);
    
    public static implicit operator SnapshotValue(string value) => new StringValue(value);
    
    public static implicit operator SnapshotValue(byte[] value) => new BinaryValue(value);
    
    public static implicit operator SnapshotValue(byte value) => new Integer32Value(value);
    
    public static implicit operator SnapshotValue(sbyte value) => new Integer32Value(value);
    
    public static implicit operator SnapshotValue(short value) => new Integer32Value(value);
    
    public static implicit operator SnapshotValue(ushort value) => new Integer32Value(value);
    
    public static implicit operator SnapshotValue(int value) => new Integer32Value(value);
    
    public static implicit operator SnapshotValue(uint value) => new Integer32Value((int)value);
    
    public static implicit operator SnapshotValue(long value) => new Integer64Value(value);
    
    public static implicit operator SnapshotValue(ulong value) => new Integer64Value((long)value);
    
    public static implicit operator SnapshotValue(float value) => new Float64Value(value);
    
    public static implicit operator SnapshotValue(double value) => new Float64Value(value);
    
    public static implicit operator SnapshotValue(decimal value) => new DecimalValue(value);
    
    public static implicit operator SnapshotValue(DateTimeOffset value) => new DateTimeValue(value);
    
    public static implicit operator SnapshotValue(Guid value) => new BinaryValue(value);
}