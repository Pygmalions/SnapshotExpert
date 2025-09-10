namespace SnapshotExpert.Framework.Values;

public interface IInteger32Number
{
    int Value { get; set; }
}

public interface IInteger64Number
{
    long Value { get; set; }
}

public interface IFloat64Number
{
    double Value { get; set; }
}

public interface IDecimalNumber
{
    decimal Value { get; set; }
}

// ReSharper disable once PossibleInterfaceMemberAmbiguity
public interface INumberValue : IInteger32Number, IInteger64Number, IFloat64Number, IDecimalNumber
{}