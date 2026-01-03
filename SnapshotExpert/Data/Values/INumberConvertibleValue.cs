namespace SnapshotExpert.Data.Values;

/// <summary>
/// Snapshot values that can be converted to <see cref="int"/>.
/// </summary>
public interface IInteger32ConvertibleValue
{
    int Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to <see cref="long"/>.
/// </summary>
public interface IInteger64ConvertibleValue
{
    long Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to <see cref="double"/>.
/// </summary>
public interface IFloat64ConvertibleValue
{
    double Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to <see cref="decimal"/>.
/// </summary>
public interface IDecimalConvertibleValue
{
    decimal Value { get; set; }
}

/// <summary>
/// Snapshot values that can be converted to any supported numeric type.
/// </summary>
// ReSharper disable once PossibleInterfaceMemberAmbiguity
public interface INumberConvertibleValue :
    IInteger32ConvertibleValue, IInteger64ConvertibleValue, IFloat64ConvertibleValue, IDecimalConvertibleValue
{
}