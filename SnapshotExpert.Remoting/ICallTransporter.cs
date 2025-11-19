using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Remoting;

public interface ICallTransporter
{
    ValueTask<SnapshotValue?> Call(int method, ObjectValue arguments);
}