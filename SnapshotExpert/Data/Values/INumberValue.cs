using System.ComponentModel;

namespace SnapshotExpert.Data.Values;

/// <summary>
/// Snapshot values that can be converted to <see cref="int"/>.
/// </summary>
public interface IInteger32Value
{
    int Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to <see cref="long"/>.
/// </summary>
public interface IInteger64Value
{
    long Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to <see cref="double"/>.
/// </summary>
public interface IFloat64Value
{
    double Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to <see cref="decimal"/>.
/// </summary>
public interface IDecimalValue
{
    decimal Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to any supported numeric type.
/// </summary>
// ReSharper disable once PossibleInterfaceMemberAmbiguity
public interface INumberValue : 
    IInteger32Value, IInteger64Value, IFloat64Value, IDecimalValue
{}