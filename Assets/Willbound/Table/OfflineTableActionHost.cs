using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using Willbound.Engine;

namespace Willbound.Table
{
    /// <summary>Path A solo host — delegates to TableMatchBridge.</summary>
    [DisallowMultipleComponent]
    public sealed class OfflineTableActionHost : MonoBehaviour, ITableActionHost
    {
        [SerializeField] TableMatchBridge bridge;

        public bool IsActive => bridge != null && bridge.IsActive;
        public int LocalPlayerId => bridge != null ? bridge.LocalPlayerId : 0;

        void Awake()
        {
            if (bridge == null)
                bridge = GetComponent<TableMatchBridge>();
        }

        public ApplyResult TryApply(PlayerAction action)
        {
            if (bridge == null)
                return ApplyResult.Fail("Engine bridge missing.", null);

            return bridge.TryApply(action);
        }

        public TableSnapshot GetSnapshot()
        {
            return bridge != null && bridge.IsActive
                ? TableBinder.BuildSnapshot(bridge.Runner.Match, bridge.LocalPlayerId)
                : new TableSnapshot();
        }
    }
}
