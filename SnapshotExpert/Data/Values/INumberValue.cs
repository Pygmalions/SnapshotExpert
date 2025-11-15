namespace SnapshotExpert.Data.Values;

public interface INumberInterface
{
}

public interface IInteger32Number : INumberInterface
{
    int Value { get; set; }
}

public interface IInteger64Number : INumberInterface
{
    long Value { get; set; }
}

public interface IFloat64Number : INumberInterface
{
    double Value { get; set; }
}

public interface IDecimalNumber : INumberInterface
{
    decimal Value { get; set; }
}

// ReSharper disable once PossibleInterfaceMemberAmbiguity
public interface INumberValue : IInteger32Number, IInteger64Number, IFloat64Number, IDecimalNumber
{}