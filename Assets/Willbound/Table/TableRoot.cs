using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using Willbound.Engine;

namespace Willbound.Table
{
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
        [SerializeField] GameObject[] hideAfterMatch;

        readonly Dictionary<int, Willbound.Table.CardView> engineHandCards = new();
        readonly Dictionary<int, Willbound.Table.CardView> engineFieldCards = new();
        Transform dropZonesRoot;
        Transform engineFieldRoot;
        bool matchTableEnabled;

        public bool IsMatchTableEnabled => matchTableEnabled;

        void Awake()
        {
            ResolveReferences();
            BuildDropZones();
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

            binder?.RefreshSnapshot();
            SyncEngineHandViews();
            SyncEngineFieldViews();
        }

        void BuildDropZones()
        {
            if (dropZonesRoot == null)
            {
                var existing = transform.Find("DropZones");
                dropZonesRoot = existing != null
                    ? existing
                    : new GameObject("DropZones").transform;
                dropZonesRoot.SetParent(transform, false);
            }

            CreateZone("FieldDrop", TableZoneKind.Field, PlaymatZones.FieldCenter, new Vector3(6f, 0.2f, 8f));
            CreateZone("WillwellDrop", TableZoneKind.Willwell, PlaymatZones.Willwell, new Vector3(3f, 0.2f, 3f));
            CreateZone("StackWellDrop", TableZoneKind.StackWell, PlaymatZones.StackWell, new Vector3(2.5f, 0.2f, 2.5f));
            CreateZone("HoldPlateDrop", TableZoneKind.HoldPlate, PlaymatZones.HoldPlate, new Vector3(2.5f, 0.2f, 2.5f));
            CreateZone("HandWellDrop", TableZoneKind.Hand, PlaymatZones.HandCenter, new Vector3(8f, 0.2f, 3f));
            CreateZone("StoreDrop", TableZoneKind.Store, PlaymatZones.StoreRowCenter, new Vector3(18f, 0.2f, 2f));
        }

        void CreateZone(string name, TableZoneKind kind, Vector3 center, Vector3 size)
        {
            if (dropZonesRoot.Find(name) != null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(dropZonesRoot, false);
            go.transform.position = new Vector3(center.x, PlaymatZones.CardY, center.z);
            var zone = go.AddComponent<ZoneView>();
            zone.Configure(kind, size);
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

                if (obj.PaidWill > 0 && obj.SourceInstanceId is int refundSourceInstanceId && binder.TryGetCardView(refundSourceInstanceId, out var view))
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

                if (binder != null && obj.SourceInstanceId is int paymentSourceInstanceId && binder.TryGetCardView(paymentSourceInstanceId, out var view))
                    willPoolView?.PlayWillPayment(obj.PaidWill, view.transform.position);
                break;
            }
        }

        void SyncEngineHandViews()
        {
            if (!matchTableEnabled || binder == null || cardPrefab == null || engineHandRoot == null)
                return;

            var snap = binder.Snapshot;
            var seen = new HashSet<int>();

            for (var i = 0; i < snap.LocalHand.Count; i++)
            {
                var card = snap.LocalHand[i];
                seen.Add(card.InstanceId);

                if (!engineHandCards.TryGetValue(card.InstanceId, out var view) || view == null)
                {
                    var shell = Instantiate(cardPrefab, engineHandRoot);
                    shell.name = $"EngineHand_{card.InstanceId}";
                    view = shell.gameObject.GetComponent<Willbound.Table.CardView>();
                    if (view == null)
                        view = shell.gameObject.AddComponent<Willbound.Table.CardView>();

                    engineHandCards[card.InstanceId] = view;
                    binder.RegisterCardView(card.InstanceId, view);
                }

                var pos = HandSlotPosition(i, snap.LocalHand.Count);
                view.transform.position = pos;
                view.transform.rotation = LegendsOfTheUniverse.Presentation.CardView.TableRotation;
                view.SetExhausted(card.Exhausted);
                view.SetHealth(card.CurrentHealth, card.Health);
                view.RememberHome();
            }

            var remove = new List<int>();
            foreach (var pair in engineHandCards)
            {
                if (!seen.Contains(pair.Key))
                {
                    if (pair.Value != null)
                        Destroy(pair.Value.gameObject);
                    remove.Add(pair.Key);
                }
            }

            for (var i = 0; i < remove.Count; i++)
                engineHandCards.Remove(remove[i]);
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
                    view = shell.gameObject.GetComponent<Willbound.Table.CardView>()
                        ?? shell.gameObject.AddComponent<Willbound.Table.CardView>();
                    engineFieldCards[card.InstanceId] = view;
                    binder.RegisterCardView(card.InstanceId, view);
                }

                var slotIndex = i % PlaymatZones.FieldSlotCount;
                var pos = PlaymatZones.GetFieldSlot(slotIndex);
                if (card.ControllerId != snap.LocalPlayerId)
                    pos.z = PlaymatZones.FieldCenter.z + 4.5f + (slotIndex * PlaymatZones.FieldSlotSpacing);

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
