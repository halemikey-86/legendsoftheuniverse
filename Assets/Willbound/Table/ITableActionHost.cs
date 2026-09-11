using Willbound.Engine;

namespace Willbound.Table
{
    /// <summary>
    /// Engine C round-trip seam. Offline: TableMatchBridge. Online: WillboundNetworkClient (future).
    /// </summary>
    public interface ITableActionHost
    {
        bool IsActive { get; }
        int LocalPlayerId { get; }
        ApplyResult TryApply(PlayerAction action);
        TableSnapshot GetSnapshot();
    }
}
