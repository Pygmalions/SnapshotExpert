namespace SnapshotExpert.Remoting;

public interface ICallClient
{
    /// <summary>
    /// Call transporter for this proxy to use.
    /// </summary>
    ICallTransporter CallTransporter { get; set; }
}