using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using global::Willbound.Engine;

namespace LegendsOfTheUniverse.Willbound.Table
{
    using EngineCardView = CardView;

    /// <summary>
    /// Engine A orchestrator — mat zones, drag table, cinema hooks. Path A: hides setup UI after match.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-110)]
    public sealed class TableRoot : MonoBehaviour
    {
        [Header("Engine")]
        [SerializeField] TableMatchBridge matchBridge;
        [SerializeField] OfflineTableActionHost actionHost;
        [SerializeField] TableBinder binder;
        [SerializeField] DragAgent dragAgent;
        [SerializeField] WillPoolView willPoolView;
        [SerializeField] ClashLines clashLines;

        [Header("Presentation")]
        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] HandView handView;
        [SerializeField] LegendsOfTheUniverse.Presentation.CardView cardPrefab;
        [SerializeField] Transform engineHandRoot;
        [SerializeField] Camera tableCamera;

        [Header("Setup UI to hide after match")]
        [SerializeField] GameObject[] hideAfterMatch = System.Array.Empty<GameObject>();

        readonly Dictionary<int, EngineCardView> engineHandCards = new();
        readonly Dictionary<int, EngineCardView> engineFieldCards = new();
        Transform engineFieldRoot;
        bool matchTableEnabled;

        public bool IsMatchTableEnabled => matchTableEnabled;

        void Awake()
        {
            ResolveReferences();
        }

        void OnEnable()
        {
            if (matchBridge != null)
                matchBridge.EngineEventsApplied += OnEngineEvents;
        }

        void OnDisable()
        {
            if (matchBridge != null)
                matchBridge.EngineEventsApplied -= OnEngineEvents;
        }

        void ResolveReferences()
        {
            if (matchBridge == null)
                matchBridge = GetComponent<TableMatchBridge>();
            if (actionHost == null)
                actionHost = GetComponent<OfflineTableActionHost>() ?? gameObject.AddComponent<OfflineTableActionHost>();
            if (binder == null)
                binder = GetComponent<TableBinder>() ?? gameObject.AddComponent<TableBinder>();
            if (dragAgent == null)
                dragAgent = GetComponent<DragAgent>() ?? gameObject.AddComponent<DragAgent>();
            if (willPoolView == null)
                willPoolView = GetComponent<WillPoolView>() ?? gameObject.AddComponent<WillPoolView>();
            if (clashLines == null)
                clashLines = GetComponent<ClashLines>() ?? gameObject.AddComponent<ClashLines>();
            if (GetComponent<PassStoneView>() == null)
                gameObject.AddComponent<PassStoneView>();
            if (playmatZones == null)
                playmatZones = GetComponent<PlaymatZonesView>();
            if (handView == null)
                handView = GetComponent<HandView>();
            if (cardPrefab == null && handView != null)
                cardPrefab = handView.CardPrefab;
            if (tableCamera == null)
                tableCamera = Camera.main;

            if (engineHandRoot == null)
            {
                var root = transform.Find("EngineHandRoot");
                if (root == null)
                {
                    var go = new GameObject("EngineHandRoot");
                    go.transform.SetParent(transform, false);
                    engineHandRoot = go.transform;
                }
                else
                {
                    engineHandRoot = root;
                }
            }

            if (binder != null && actionHost != null)
                binder.BindHost(actionHost);
        }

        public void EnableMatchTable()
        {
            if (matchTableEnabled)
                return;

            matchTableEnabled = true;
            ResolveReferences();

            if (hideAfterMatch != null)
            {
                for (var i = 0; i < hideAfterMatch.Length; i++)
                {
                    if (hideAfterMatch[i] != null)
                        hideAfterMatch[i].SetActive(false);
                }
            }

            if (matchBridge != null)
            {
                matchBridge.EngineEventsApplied -= OnEngineEvents;
                matchBridge.EngineEventsApplied += OnEngineEvents;
            }

            binder?.RefreshSnapshot();
            SyncEngineHandViews();
            SyncEngineFieldViews();
        }

        void OnEngineEvents(IReadOnlyList<GameEvent> events)
        {
            if (events == null)
                return;

            binder?.RefreshSnapshot();
            SyncEngineHandViews();
            SyncEngineFieldViews();

            for (var i = 0; i < events.Count; i++)
                HandleCinemaEvent(events[i]);
        }

        void HandleCinemaEvent(GameEvent e)
        {
            if (e == null)
                return;

            binder?.OnEngineEvent(e);
            clashLines?.OnEngineEvent(e);

            switch (e.Kind)
            {
                case EventKind.WillSet:
                    if (TryInt(e, "amount", out var will))
                        willPoolView?.SetWill(will);
                    break;
                case EventKind.StackPushed:
                    HandleStackPushed(e);
                    break;
                case EventKind.Exhausted:
                    if (TryInt(e, "instanceId", out var exhaustedId) && binder.TryGetCardView(exhaustedId, out var exhaustedView))
                        exhaustedView.SetExhausted(true);
                    break;
                case EventKind.Readied:
                    if (TryInt(e, "instanceId", out var readiedId) && binder.TryGetCardView(readiedId, out var readiedView))
                        readiedView.SetExhausted(false);
                    break;
                case EventKind.HealthChanged:
                    if (TryInt(e, "instanceId", out var healthId)
                        && TryInt(e, "current", out var current)
                        && TryInt(e, "printed", out var printed)
                        && binder.TryGetCardView(healthId, out var healthView))
                        healthView.SetHealth(current, printed);
                    break;
                case EventKind.Silenced:
                    HandleSilenceRefund(e);
                    break;
            }
        }

        void HandleSilenceRefund(GameEvent e)
        {
            if (matchBridge == null || !matchBridge.IsActive)
                return;

            // Refund path uses WillSet on controller; cinema flies coins back if card had paid Will.
            if (!TryInt(e, "stackId", out var stackId))
                return;

            var match = matchBridge.Runner.Match;
            for (var i = 0; i < match.Stack.Count; i++)
            {
                var obj = match.Stack[i];
                if (obj.StackId != stackId)
                    continue;

                if (obj.PaidWill > 0
                    && obj.SourceInstanceId.HasValue
                    && binder.TryGetCardView(obj.SourceInstanceId.Value, out var view))
                    willPoolView?.PlayWillRefund(obj.PaidWill, view.transform.position);
                break;
            }
        }

        void HandleStackPushed(GameEvent e)
        {
            if (matchBridge == null || !matchBridge.IsActive || !TryInt(e, "stackId", out var stackId))
                return;

            var match = matchBridge.Runner.Match;
            for (var i = 0; i < match.Stack.Count; i++)
            {
                var obj = match.Stack[i];
                if (obj.StackId != stackId || obj.Type != StackObjectType.PlayCard || obj.PaidWill <= 0)
                    continue;

                if (binder != null
                    && obj.SourceInstanceId.HasValue
                    && binder.TryGetCardView(obj.SourceInstanceId.Value, out var view))
                    willPoolView?.PlayWillPayment(obj.PaidWill, view.transform.position);
                break;
            }
        }

        void SyncEngineHandViews()
        {
            if (!matchTableEnabled || binder == null || handView == null)
                return;

            ClearEngineHandDuplicates();

            var snap = binder.Snapshot;
            var presentationHand = handView.KeptHandCards;
            var bindCount = Mathf.Min(presentationHand.Count, snap.LocalHand.Count);

            if (presentationHand.Count != snap.LocalHand.Count)
            {
                Debug.LogWarning(
                    $"[TableRoot] Hand size mismatch — UI={presentationHand.Count}, engine={snap.LocalHand.Count}. " +
                    $"Binding by index for the first {bindCount} cards.");
            }

            var engineIds = new HashSet<int>();
            for (var i = 0; i < snap.LocalHand.Count; i++)
                engineIds.Add(snap.LocalHand[i].InstanceId);

            for (var i = presentationHand.Count - 1; i >= 0; i--)
            {
                var presentationCard = presentationHand[i];
                if (presentationCard == null)
                    continue;

                var binding = presentationCard.GetComponent<TableCardBinding>();
                if (binding != null && binding.InstanceId > 0 && !engineIds.Contains(binding.InstanceId))
                    handView.RemoveCardAfterPlayed(binding.InstanceId);
            }

            for (var i = 0; i < bindCount; i++)
            {
                var presentationCard = presentationHand[i];
                if (presentationCard == null)
                    continue;

                var engineCard = snap.LocalHand[i];
                BindPresentationCard(presentationCard, engineCard);
            }
        }

        void BindPresentationCard(LegendsOfTheUniverse.Presentation.CardView presentationCard, CardSnapshot engineCard)
        {
            var binding = presentationCard.GetComponent<TableCardBinding>()
                ?? presentationCard.gameObject.AddComponent<TableCardBinding>();
            binding.Bind(engineCard.InstanceId);

            var tableCard = presentationCard.GetComponent<EngineCardView>()
                ?? presentationCard.gameObject.AddComponent<EngineCardView>();
            tableCard.BindInstance(engineCard.InstanceId);
            tableCard.SetExhausted(engineCard.Exhausted);
            tableCard.SetHealth(engineCard.CurrentHealth, engineCard.Health);
            binder.RegisterCardView(engineCard.InstanceId, tableCard);
        }

        void ClearEngineHandDuplicates()
        {
            foreach (var pair in engineHandCards)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            engineHandCards.Clear();
        }

        void SyncEngineFieldViews()
        {
            if (!matchTableEnabled || binder == null || cardPrefab == null)
                return;

            if (engineFieldRoot == null)
            {
                var existing = transform.Find("EngineFieldRoot");
                engineFieldRoot = existing != null
                    ? existing
                    : new GameObject("EngineFieldRoot").transform;
                engineFieldRoot.SetParent(transform, false);
            }

            var snap = binder.Snapshot;
            var allField = new List<CardSnapshot>(snap.LocalField);
            for (var i = 0; i < snap.OpponentField.Count; i++)
                allField.Add(snap.OpponentField[i]);

            var seen = new HashSet<int>();
            for (var i = 0; i < allField.Count; i++)
            {
                var card = allField[i];
                seen.Add(card.InstanceId);

                if (!engineFieldCards.TryGetValue(card.InstanceId, out var view) || view == null)
                {
                    var shell = Instantiate(cardPrefab, engineFieldRoot);
                    shell.name = $"EngineField_{card.InstanceId}";
                    view = shell.gameObject.GetComponent<CardView>()
                        ?? shell.gameObject.AddComponent<CardView>();
                    var binding = shell.gameObject.GetComponent<TableCardBinding>()
                        ?? shell.gameObject.AddComponent<TableCardBinding>();
                    binding.Bind(card.InstanceId);
                    engineFieldCards[card.InstanceId] = view;
                    binder.RegisterCardView(card.InstanceId, view);
                }

                var slotIndex = i % PlaymatZones.FieldSlotCount;
                var pos = PlaymatZones.GetFieldSlot(slotIndex);
                if (card.ControllerId != snap.LocalPlayerId)
                {
                    PlaymatZones.DecomposeFieldSlotIndex(slotIndex, out var row, out var column);
                    pos = PlaymatZones.GetFieldDropSlotPosition(row, column);
                    pos.z += PlaymatZones.FieldDropSlotSpacing.y;
                }

                view.transform.position = pos;
                view.transform.rotation = LegendsOfTheUniverse.Presentation.CardView.TableRotation;
                view.SetExhausted(card.Exhausted);
                view.SetHealth(card.CurrentHealth, card.Health);
                view.RememberHome();
            }

            var remove = new List<int>();
            foreach (var pair in engineFieldCards)
            {
                if (!seen.Contains(pair.Key))
                {
                    if (pair.Value != null)
                        Destroy(pair.Value.gameObject);
                    remove.Add(pair.Key);
                }
            }

            for (var i = 0; i < remove.Count; i++)
                engineFieldCards.Remove(remove[i]);
        }

        static Vector3 HandSlotPosition(int index, int count)
        {
            var spacing = PlaymatZones.HandSpreadSpacing;
            var startX = -(count - 1) * spacing * 0.5f;
            return new Vector3(
                PlaymatZones.HandCenter.x + startX + (index * spacing),
                PlaymatZones.HandCenter.y,
                PlaymatZones.HandCenter.z);
        }

        static bool TryInt(GameEvent e, string key, out int value)
        {
            value = 0;
            if (e?.Data == null || !e.Data.TryGetValue(key, out var raw) || raw == null)
                return false;

            try
            {
                value = System.Convert.ToInt32(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
