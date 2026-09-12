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
        [SerializeField] IconSlotView iconSlotView;
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
        IconLifeView localIconLife;
        IconLifeView opponentIconLife;
        WorldAnchoredUi clashScoreUi;
        Transform clashScoreAnchor;
        int localIconInstanceId = -1;
        int opponentIconInstanceId = -1;
        int damageToLocalIconThisClash;
        int damageToEnemyIconThisClash;

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
            if (iconSlotView == null)
                iconSlotView = FindAnyObjectByType<IconSlotView>();
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

            CreateZone("FieldDrop", TableZoneKind.Field, PlaymatZones.FieldCenter, new Vector3(16f, 0.5f, 14f));
            CreateZone("WillwellDrop", TableZoneKind.Willwell, PlaymatZones.Willwell, new Vector3(4f, 0.4f, 8f));
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
            RefreshIconLifeFromMatch();

            for (var i = 0; i < events.Count; i++)
                HandleCinemaEvent(events[i]);

            RefreshIconLifeFromMatch();
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
                        && TryInt(e, "printed", out var printed))
                    {
                        if (binder.TryGetCardView(healthId, out var healthView))
                            healthView.SetHealth(current, printed);
                        ApplyIconHealth(healthId, current, printed);
                    }
                    break;
                case EventKind.DamageDealt:
                    HandleDamageDealt(e);
                    break;
                case EventKind.PhaseChanged:
                    if (e.Data != null && e.Data.TryGetValue("clashPhase", out var clashRaw)
                        && clashRaw != null && clashRaw.ToString().Contains("C1"))
                        ResetClashDamage();
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
                if (handView != null && handView.OwnsInstance(card.InstanceId))
                    continue;

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
                view.Presentation?.SetPlayableOutline(
                    matchBridge != null && matchBridge.CanPlayCard(card.InstanceId));
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
            var allField = new List<CardSnapshot>();
            AddUniqueFieldCards(allField, snap.LocalField);
            AddUniqueFieldCards(allField, snap.OpponentField);
            AddUniqueFieldCards(allField, snap.LocalWillwell);
            AddUniqueFieldCards(allField, snap.OpponentWillwell);

            var seen = new HashSet<int>();
            var localSlot = 0;
            var opponentSlot = 0;
            var localWell = 0;
            var opponentWell = 0;
            var declare = snap.ClashPhase == ClashPhase.C1_ActiveDeclare;

            for (var i = 0; i < allField.Count; i++)
            {
                var card = allField[i];
                seen.Add(card.InstanceId);

                var isLocal = card.ControllerId == snap.LocalPlayerId;
                var view = BindOrSpawnFieldCard(card, isLocal);
                if (view == null)
                    continue;

                Vector3 pos;
                if (card.Type == CardType.Icon)
                    pos = isLocal ? PlaymatZones.Icon : PlaymatZones.OpponentIcon;
                else if (card.Type == CardType.WillSite)
                    pos = isLocal ? PlaymatZones.GetWillwellSlot(localWell++) : PlaymatZones.GetOpponentWillwellSlot(opponentWell++);
                else
                    pos = isLocal ? PlaymatZones.GetFieldSlot(localSlot++) : PlaymatZones.GetOpponentFieldSlot(opponentSlot++);

                var skipMove = view.IsDragging
                    || (isLocal && card.Type == CardType.Icon && iconSlotView != null && iconSlotView.HasInspectSelection);
                if (!skipMove)
                {
                    view.transform.position = pos;
                    view.transform.rotation = LegendsOfTheUniverse.Presentation.CardView.TableRotation;
                    view.SetExhausted(card.Exhausted);
                    view.RememberHome();
                }

                view.SetHealth(card.CurrentHealth, card.Health);
                if (card.Type == CardType.Icon)
                    BindIconLife(card, view, isLocal);

                var canDeclare = declare && isLocal && binder.IsBodyInDeclareQueue(card.InstanceId);
                var isPressTarget = declare && !isLocal
                    && card.Type is CardType.Icon or CardType.Companion or CardType.Token;
                view.Presentation?.SetPlayableOutline(canDeclare || isPressTarget);
                view.Presentation?.SetClickable(true);
            }

            var remove = new List<int>();
            foreach (var pair in engineFieldCards)
            {
                if (!seen.Contains(pair.Key))
                {
                    if (pair.Value != null && (iconSlotView == null || pair.Value.Presentation != iconSlotView.IconCard))
                        Destroy(pair.Value.gameObject);
                    remove.Add(pair.Key);
                }
            }

            for (var i = 0; i < remove.Count; i++)
                engineFieldCards.Remove(remove[i]);
        }

        static void AddUniqueFieldCards(List<CardSnapshot> dest, IReadOnlyList<CardSnapshot> source)
        {
            if (source == null)
                return;

            for (var i = 0; i < source.Count; i++)
            {
                var card = source[i];
                if (card == null)
                    continue;

                var exists = false;
                for (var j = 0; j < dest.Count; j++)
                {
                    if (dest[j].InstanceId == card.InstanceId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    dest.Add(card);
            }
        }

        Willbound.Table.CardView BindOrSpawnFieldCard(CardSnapshot card, bool isLocal)
        {
            if (engineFieldCards.TryGetValue(card.InstanceId, out var existing) && existing != null)
                return existing;

            if (isLocal && card.Type == CardType.Icon && iconSlotView != null && iconSlotView.IconCard != null)
            {
                var localIcon = iconSlotView.IconCard.GetComponent<Willbound.Table.CardView>()
                    ?? iconSlotView.IconCard.gameObject.AddComponent<Willbound.Table.CardView>();
                engineFieldCards[card.InstanceId] = localIcon;
                binder.RegisterCardView(card.InstanceId, localIcon);
                iconSlotView.IconCard.SetEngineCardInstanceId(card.InstanceId);
                iconSlotView.IconCard.SetClickable(true);
                return localIcon;
            }

            if (TryAdoptPresentationCard(card.InstanceId, out var adopted) && adopted != null)
                return adopted;

            var shell = Instantiate(cardPrefab, engineFieldRoot);
            shell.name = card.Type == CardType.Icon
                ? (isLocal ? "LocalIcon" : "OpponentIcon")
                : $"EngineField_{card.InstanceId}";
            var view = shell.gameObject.GetComponent<Willbound.Table.CardView>()
                ?? shell.gameObject.AddComponent<Willbound.Table.CardView>();
            engineFieldCards[card.InstanceId] = view;
            binder.RegisterCardView(card.InstanceId, view);

            ApplyFieldCardArt(shell, card);
            shell.SetFaceUpImmediate(true);
            shell.SetClickable(true);
            return view;
        }

        bool TryAdoptPresentationCard(int instanceId, out Willbound.Table.CardView adopted)
        {
            adopted = null;
            LegendsOfTheUniverse.Presentation.CardView played = null;
            if (handView == null || !handView.TryTakeLeavingCard(instanceId, out played) || played == null)
            {
                var all = FindObjectsByType<LegendsOfTheUniverse.Presentation.CardView>();
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].EngineCardInstanceId == instanceId)
                    {
                        played = all[i];
                        break;
                    }
                }
            }

            if (played == null)
                return false;

            played.transform.SetParent(engineFieldRoot, true);
            adopted = played.GetComponent<Willbound.Table.CardView>()
                ?? played.gameObject.AddComponent<Willbound.Table.CardView>();
            engineFieldCards[instanceId] = adopted;
            binder.RegisterCardView(instanceId, adopted);
            played.SetEngineCardInstanceId(instanceId);
            played.SetClickable(true);
            played.SetFaceUpImmediate(true);
            var clickHandler = played.GetComponent<CardClickHandler>();
            if (clickHandler != null)
                Destroy(clickHandler);
            return true;
        }

        void BindIconLife(CardSnapshot card, Willbound.Table.CardView view, bool isLocal)
        {
            var life = view.GetComponent<IconLifeView>() ?? view.gameObject.AddComponent<IconLifeView>();
            life.Configure(view.transform, isLocal ? "Your Icon" : "Enemy Icon");
            life.SetHealth(card.CurrentHealth, card.Health);
            if (isLocal)
            {
                localIconLife = life;
                localIconInstanceId = card.InstanceId;
                life.SetClashTaken(damageToLocalIconThisClash);
            }
            else
            {
                opponentIconLife = life;
                opponentIconInstanceId = card.InstanceId;
                life.SetClashTaken(damageToEnemyIconThisClash);
            }

            EnsureClashScoreUi();
        }

        void EnsureClashScoreUi()
        {
            if (clashScoreAnchor == null)
            {
                var go = new GameObject("ClashScoreAnchor");
                go.transform.SetParent(transform, false);
                go.transform.position = PlaymatZones.FieldCenter + new Vector3(0f, 0.25f, 0f);
                clashScoreAnchor = go.transform;
            }

            if (clashScoreUi == null)
            {
                clashScoreUi = WorldAnchoredUi.CreateLabeled(
                    clashScoreAnchor,
                    "Clash damage to Icons",
                    "You deal  ·  they deal",
                    new Vector3(0f, 0.35f, 0f),
                    new Vector2(320f, 96f),
                    26);
            }

            RefreshClashScore();
        }

        void RefreshClashScore()
        {
            if (clashScoreUi == null)
                return;

            clashScoreUi.Value = $"{damageToEnemyIconThisClash}  →  {damageToLocalIconThisClash}";
            clashScoreUi.Subtitle = "You deal  ·  they deal";
        }

        void ResetClashDamage()
        {
            damageToLocalIconThisClash = 0;
            damageToEnemyIconThisClash = 0;
            localIconLife?.ClearClash();
            opponentIconLife?.ClearClash();
            RefreshClashScore();
        }

        void HandleDamageDealt(GameEvent e)
        {
            if (!TryInt(e, "target", out var targetId) || !TryInt(e, "amount", out var amount) || amount <= 0)
                return;
            if (matchBridge == null || !matchBridge.IsActive)
                return;

            var target = matchBridge.Runner.Match.GetCard(targetId);
            if (target?.Printing == null || target.Printing.Type != CardType.Icon)
                return;

            if (targetId == localIconInstanceId || target.ControllerId == matchBridge.LocalPlayerId)
            {
                damageToLocalIconThisClash += amount;
                localIconLife?.SetClashTaken(damageToLocalIconThisClash);
                localIconLife?.SetHealth(target.CurrentHealth, target.Health);
            }
            else
            {
                damageToEnemyIconThisClash += amount;
                opponentIconLife?.SetClashTaken(damageToEnemyIconThisClash);
                opponentIconLife?.SetHealth(target.CurrentHealth, target.Health);
            }

            RefreshClashScore();
        }

        void ApplyIconHealth(int instanceId, int current, int printed)
        {
            if (instanceId == localIconInstanceId)
                localIconLife?.SetHealth(current, printed);
            else if (instanceId == opponentIconInstanceId)
                opponentIconLife?.SetHealth(current, printed);
        }

        void RefreshIconLifeFromMatch()
        {
            if (matchBridge == null || !matchBridge.IsActive)
                return;

            var match = matchBridge.Runner.Match;
            var local = match.GetPlayer(matchBridge.LocalPlayerId);
            if (local?.Icon != null)
            {
                localIconInstanceId = local.Icon.InstanceId;
                localIconLife?.SetHealth(local.Icon.CurrentHealth, local.Icon.Health);
            }

            var opponent = match.GetPlayer(matchBridge.LocalPlayerId == 0 ? 1 : 0);
            if (opponent?.Icon != null)
            {
                opponentIconInstanceId = opponent.Icon.InstanceId;
                opponentIconLife?.SetHealth(opponent.Icon.CurrentHealth, opponent.Icon.Health);
            }
        }

        void ApplyFieldCardArt(LegendsOfTheUniverse.Presentation.CardView shell, CardSnapshot card)
        {
            CardPrinting printing = null;
            if (matchBridge != null && matchBridge.IsActive)
                printing = matchBridge.Runner.Match.GetCard(card.InstanceId)?.Printing;

            if (printing != null)
            {
                shell.BindPrinting(printing);
                var art = EngineCatalog.GetCardArt(printing);
                if (art != null)
                    shell.SetFrontTexture(art);
            }
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
