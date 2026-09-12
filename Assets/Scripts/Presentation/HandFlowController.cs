using System.Collections;
using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using LegendsOfTheUniverse.Presentation.Menu;
using LegendsOfTheUniverse.Rules;
using Willbound.Table;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class HandFlowController : MonoBehaviour
    {
        static readonly Color BuyColor = new(0.85f, 0.72f, 0.28f, 0.98f);
        static readonly Color SellColor = new(0.95f, 0.55f, 0.45f, 0.98f);
        static readonly Color TradeColor = new(0.90f, 0.78f, 0.40f, 0.98f);

        [Header("References")]
        [SerializeField] HandView handView;
        [SerializeField] StoreView storeView;
        [SerializeField] StoreActionController storeActions;
        [SerializeField] LegendaryPickView legendaryPickView;
        [SerializeField] CardBackPickView cardBackPickView;
        [SerializeField] OpeningHandRedrawView openingHandRedrawView;
        [SerializeField] IconSlotView iconSlotView;
        [SerializeField] Transform deckPoint;
        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] SupplyDeckView supplyDeckView;
        [SerializeField] TurnFlowController turnFlow;
        [SerializeField] TableMatchBridge matchBridge;
        [SerializeField] TableRoot tableRoot;
        MarketModalView marketModal;

        Text pickPromptText;
        Button keepButton;
        Button mulliganButton;
        Button listViewButton;
        Button buyButton;
        Button sellButton;
        Button tradeButton;
        Button nextPhaseButton;
        Button endTurnButton;
        GameObject handUiRoot;
        bool matchStarted;
        bool mulliganUsed;

        Sprite buyIcon;
        Sprite sellIcon;
        Sprite tradeIcon;
        Sprite nextPhaseIcon;
        Sprite endTurnIcon;

        const float StoreActionButtonSize = 48f;
        const float KeepButtonAnchorY = 0.12f;
        static readonly Vector2 AcceptHandButtonSize = new(220f, 64f);
        static readonly Vector2 MulliganButtonSize = new(190f, 56f);
        static readonly Vector3 AcceptHandButtonPosition = new(115f, 0f, 0f);
        static readonly Vector3 MulliganButtonPosition = new(-115f, 0f, 0f);
        static readonly Color AcceptHandButtonColor = new(0.18f, 0.42f, 0.24f, 0.96f);
        static readonly Color MulliganButtonColor = new(0.18f, 0.22f, 0.32f, 0.92f);
        static readonly Color IconNormalTint = new(0.92f, 0.88f, 0.78f, 1f);

        static readonly Vector3 RiverToggleButtonPosition = new(-60f, 17f, 0f);
        static readonly Vector3 BuyButtonPosition = new(21f, 25f, 0f);
        static readonly Vector3 SellButtonPosition = new(-23.2f, 25f, 0f);
        static readonly Vector3 TradeButtonPosition = new(60f, 17f, 0f);
        const float RiverToggleButtonScale = 0.65f;
        const float BuyButtonScale = 0.93f;
        const float SellButtonScale = 0.93f;
        const float TradeButtonScale = 0.65f;

        const float PhaseButtonSize = 72f;
        const float PhaseButtonBottomOffset = 40f;
        const float EndTurnButtonInset = 48f;
        const float NextPhaseButtonInset = 132f;
        static readonly Color PhaseButtonEnabledTint = Color.white;
        static readonly Color PhaseButtonDisabledTint = new(1f, 1f, 1f, 0.35f);

        void Reset()
        {
            handView = GetComponent<HandView>();
            storeView = GetComponent<StoreView>();
            storeActions = GetComponent<StoreActionController>();
            legendaryPickView = GetComponent<LegendaryPickView>();
            cardBackPickView = GetComponent<CardBackPickView>();
            iconSlotView = GetComponent<IconSlotView>();
        }

        void Awake()
        {
            if (handView == null)
                handView = GetComponent<HandView>();
            if (storeView == null)
                storeView = GetComponent<StoreView>();
            if (storeActions == null)
                storeActions = GetComponent<StoreActionController>();
            if (legendaryPickView == null)
                legendaryPickView = GetComponent<LegendaryPickView>();
            if (cardBackPickView == null)
                cardBackPickView = GetComponent<CardBackPickView>();
            if (openingHandRedrawView == null)
                openingHandRedrawView = GetComponent<OpeningHandRedrawView>();
            if (openingHandRedrawView == null)
                openingHandRedrawView = gameObject.AddComponent<OpeningHandRedrawView>();
            if (iconSlotView == null)
                iconSlotView = GetComponent<IconSlotView>();
            if (playmatZones == null)
                playmatZones = GetComponent<PlaymatZonesView>();
            if (playmatZones == null)
                playmatZones = gameObject.AddComponent<PlaymatZonesView>();
            if (supplyDeckView == null)
                supplyDeckView = GetComponentInChildren<SupplyDeckView>();
            if (turnFlow == null)
                turnFlow = GetComponent<TurnFlowController>();
            if (turnFlow == null)
                turnFlow = gameObject.AddComponent<TurnFlowController>();

            if (matchBridge == null)
                matchBridge = GetComponent<TableMatchBridge>();
            if (tableRoot == null)
                tableRoot = GetComponent<TableRoot>();
            if (tableRoot == null)
                tableRoot = gameObject.AddComponent<TableRoot>();
            if (matchBridge == null)
                matchBridge = gameObject.AddComponent<TableMatchBridge>();

            if (storeActions == null)
                storeActions = gameObject.AddComponent<StoreActionController>();
            if (marketModal == null)
                marketModal = GetComponent<MarketModalView>();
            if (marketModal == null)
                marketModal = gameObject.AddComponent<MarketModalView>();

            turnFlow.Configure(handView, playmatZones, storeView, SetPickPrompt);
            turnFlow.BindEngine(matchBridge);
            turnFlow.PhaseStateChanged += OnTurnPhaseChanged;
            matchBridge.EngineError += OnEngineError;
            matchBridge.EngineEventsApplied += OnEngineEventsApplied;

            storeActions.Init(handView, storeView, matchBridge);
            handView?.BindStoreActions(storeActions);
            handView?.BindStoreView(storeView);
            handView?.BindMatchBridge(matchBridge);
            storeView?.BindStoreActions(storeActions);
            storeView?.BindHandView(handView);

            CardDeck.ClearSession();
            handView?.ResetSession();
            storeView?.ClearStoreRuntime();
            storeView?.EnsureRowEmpty();
            supplyDeckView?.SetStackVisible(false);
            iconSlotView?.ClearIcon();
            mulliganUsed = false;
            EnsureEventSystem();
        }

        void Start()
        {
            StartCoroutine(BootTableRoutine());
        }

        IEnumerator BootTableRoutine()
        {
            while (!TablePresentation.IsReady || GetTableCamera() == null)
                yield return null;

            var cardCount = CardCatalog.LoadAllCardFronts().Count;
            var backCount = CardCatalog.LoadCardBacks().Count;
            if (cardCount == 0 || backCount == 0)
            {
                Debug.LogError(
                    $"HandFlowController: Missing card assets (fronts={cardCount}, backs={backCount}). " +
                    "Check Assets/Cards is imported.");
            }

            if (handView != null && handView.CardPrefab == null)
                Debug.LogError("HandFlowController: HandView.cardPrefab is not assigned on Table.");

            EnsureHandUi();
            yield return BeginOpeningHand();
        }

        Camera GetTableCamera()
        {
            var tableView = GetComponent<TableView>();
            return tableView != null ? tableView.TableCamera : Camera.main;
        }

        void OnDestroy()
        {
            if (turnFlow != null)
                turnFlow.PhaseStateChanged -= OnTurnPhaseChanged;

            if (matchBridge != null)
            {
                matchBridge.EngineError -= OnEngineError;
                matchBridge.EngineEventsApplied -= OnEngineEventsApplied;
            }

            if (handUiRoot != null)
                Destroy(handUiRoot);
        }

        void Update()
        {
            legendaryPickView?.TickModal();
            cardBackPickView?.Tick();

            if (tableRoot != null && tableRoot.IsMatchTableEnabled)
                return;

            if (!WasPrimaryClickThisFrame() || IsPointerOverUi())
                return;

            if (TryRaycastCard(out var clickedCard))
            {
                if (TryHandleTableCardClick(clickedCard))
                    return;
            }

            if (cardBackPickView != null && cardBackPickView.IsInspecting)
            {
                cardBackPickView.DeclineInspect();
                return;
            }

            if (legendaryPickView != null && legendaryPickView.IsInspecting)
            {
                legendaryPickView.DeclineInspect();
                return;
            }

            if (iconSlotView != null && iconSlotView.HasInspectSelection)
            {
                iconSlotView.ClearInspectSelection();
                return;
            }

            if (handView == null || !handView.HandKept || handView.IsDragging)
                return;

            var handInspecting = handView.HasInspectSelection;
            var storeInspecting = storeView != null && storeView.HasInspectSelection;
            if (!handInspecting && !storeInspecting)
                return;

            DismissExpandedCards();
        }

        void DismissExpandedCards()
        {
            if (handView != null && handView.HasInspectSelection)
            {
                handView.ClearInspectSelection();
                return;
            }

            storeView?.ClearInspectSelection();
        }

        static bool WasPrimaryClickThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        bool TryRaycastCard(out CardView card)
        {
            card = null;
            var camera = Camera.main;
            if (camera == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            var screenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
#else
            var screenPosition = Input.mousePosition;
#endif

            var ray = camera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 100f))
                return false;

            card = hit.collider.GetComponentInParent<CardView>();
            return card != null;
        }

        bool TryHandleTableCardClick(CardView card)
        {
            if (card == null)
                return false;

            if (cardBackPickView != null && cardBackPickView.IsPickActive)
            {
                cardBackPickView.HandlePickCardClicked(card);
                return true;
            }

            if (legendaryPickView != null && legendaryPickView.IsPickActive)
            {
                legendaryPickView.HandlePickCardClicked(card);
                return true;
            }

            if (handView != null && handView.ContainsHandCard(card))
            {
                handView.HandleTableClick(card);
                return true;
            }

            if (storeView != null && storeView.CanInteractCards)
            {
                storeView.HandleStoreCardClicked(card);
                return true;
            }

            return false;
        }

        IEnumerator BeginOpeningHand()
        {
            if (handView == null)
                yield break;

            CardDeck.SetSelectedCardBack(CardCatalog.GetCardBackByName(GameSettings.CardBackName));

            var deckPosition = GetDeckPosition();
            CardDeck.PrepareSetupPool();

            yield return handView.DealOpeningHandRoutine(deckPosition);

            CardDeck.CommitRemainderToSupply();
            supplyDeckView?.Refresh();

            SetPickPrompt("Review your hand — \u267B redraw once per card, then Keep", true);

            openingHandRedrawView?.Begin(handView, handUiRoot.transform, GetTableCamera(), GetDeckPosition);

            if (keepButton != null)
                keepButton.gameObject.SetActive(true);

            EnsureMulliganButton(true);
        }

        List<string> CollectKeptHandPrintingIds()
        {
            var ids = new List<string>();
            if (handView == null)
                return ids;

            var keptCards = handView.OpeningHandCards;
            for (var i = 0; i < keptCards.Count; i++)
            {
                var printing = keptCards[i]?.BoundPrinting;
                if (printing != null)
                    ids.Add(printing.Id);
            }

            return ids;
        }

        void OnEngineError(string message) => SetPickPrompt(message, true);

        void OnEngineEventsApplied(System.Collections.Generic.IReadOnlyList<Willbound.Engine.GameEvent> events)
        {
            if (events == null || marketModal == null || !marketModal.IsVisible)
                return;

            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Kind != Willbound.Engine.EventKind.TurnStarted)
                    continue;

                marketModal.Refresh();
                break;
            }
        }

        void SetPickPrompt(string message, bool visible)
        {
            if (pickPromptText == null)
                return;

            if (!string.IsNullOrEmpty(message))
                pickPromptText.text = message;

            pickPromptText.gameObject.SetActive(visible);
        }

        void OnKeepClicked()
        {
            StartCoroutine(AfterKeepSequence());
        }

        void OnMulliganClicked()
        {
            if (mulliganUsed || handView == null || handView.HandKept || handView.IsDealing)
                return;

            mulliganUsed = true;
            StartCoroutine(MulliganSequence());
        }

        IEnumerator MulliganSequence()
        {
            EnsureMulliganButton(false);
            openingHandRedrawView?.End();

            yield return handView.MulliganOpeningHandRoutine(GetDeckPosition());

            SetPickPrompt("Review your hand — ♻ redraw once per card, then Keep", true);
            openingHandRedrawView?.Begin(handView, handUiRoot.transform, GetTableCamera(), GetDeckPosition);
        }

        IEnumerator AfterKeepSequence()
        {
            if (handView == null)
                yield break;

            if (keepButton != null)
                keepButton.gameObject.SetActive(false);
            if (mulliganButton != null)
                mulliganButton.gameObject.SetActive(false);

            openingHandRedrawView?.End();

            yield return handView.RunKeepHandRoutine();
            handView.TuckForOverlayPick();

            SetPickPrompt("Choose your Legendary Icon", true);

            if (legendaryPickView != null)
                yield return legendaryPickView.RunPickRoutine();

            SetPickPrompt(null, false);
            yield return handView.RestoreHandUnlessInspecting();

            string pickedIconPrintingId = null;

            if (legendaryPickView != null && legendaryPickView.HasSelected)
            {
                var icon = legendaryPickView.SelectedIcon;
                var fromPosition = legendaryPickView.GetSelectedCardPosition();
                var unpicked = legendaryPickView.TakeUnpickedCards();
                CardDeck.SetSelectedLegendary(icon);
                legendaryPickView.ClearPickCards();

                if (EngineCatalog.TryGetPrintingByArt(icon, out var iconPrinting))
                    pickedIconPrintingId = iconPrinting.Id;

                if (iconSlotView != null)
                    yield return iconSlotView.PlaceIconRoutine(icon, fromPosition);

                if (unpicked.Count > 0)
                    yield return ReturnUnpickedIconsToSupplyRoutine(unpicked, GetSupplyPosition());

                supplyDeckView?.Refresh();
            }

            if (matchBridge != null)
                matchBridge.ConfigureLocalPlayer(pickedIconPrintingId, CollectKeptHandPrintingIds());

            matchStarted = true;
            turnFlow?.BeginMatchAfterSetup();

            if (matchBridge != null && matchBridge.IsActive)
                handView?.SyncHandFromEngine(matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId).Hand);

            tableRoot?.EnableMatchTable();
            SetStoreButtonsVisible(true);
            UpdateTurnButtons();
        }

        void OnNextPhaseClicked()
        {
            turnFlow?.OnNextPhaseClicked();
            UpdateTurnButtons();
        }

        void OnEndTurnClicked()
        {
            turnFlow?.OnEndTurnClicked();
            UpdateTurnButtons();
        }

        void OnTurnPhaseChanged(TurnStep step, bool discardPending)
        {
            var localMain = step == TurnStep.Main && !discardPending
                && (matchBridge == null || !matchBridge.IsActive || matchBridge.IsLocalActivePlayer);
            SetStoreButtonsVisible(localMain);
            UpdateTurnButtons();
            handView?.RefreshPlayableOutlines();
        }

        void UpdateTurnButtons()
        {
            if (turnFlow == null)
                return;

            if (!matchStarted)
            {
                SetPhaseButtonState(nextPhaseButton, true, false);
                SetPhaseButtonState(endTurnButton, true, false);
                return;
            }

            var discardPending = turnFlow.IsDiscardPending;
            SetPhaseButtonState(nextPhaseButton, !discardPending, turnFlow.ShouldShowNextPhaseButton);
            SetPhaseButtonState(endTurnButton, !discardPending, turnFlow.ShouldShowEndTurnButton);
        }

        static void SetPhaseButtonState(Button button, bool visible, bool interactable)
        {
            if (button == null)
                return;

            button.gameObject.SetActive(visible);
            button.interactable = interactable;

            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = interactable ? PhaseButtonEnabledTint : PhaseButtonDisabledTint;
        }

        void OnMarketButtonClicked() => marketModal?.Toggle();

        void OnBuyClicked() => marketModal?.Toggle();

        void OnSellClicked() => SetStoreActionMode(StoreActionMode.Sell);

        void OnTradeClicked() => SetStoreActionMode(StoreActionMode.Trade);

        void SetStoreActionMode(StoreActionMode mode)
        {
            storeActions?.SetMode(mode);
            UpdateActionButtonHighlights();
        }

        void SetStoreButtonsVisible(bool visible)
        {
            if (listViewButton != null)
                listViewButton.gameObject.SetActive(visible);

            if (buyButton != null)
                buyButton.gameObject.SetActive(visible);
            if (sellButton != null)
                sellButton.gameObject.SetActive(visible);
            if (tradeButton != null)
                tradeButton.gameObject.SetActive(visible);

            if (!visible)
                SetStoreActionMode(StoreActionMode.None);
            else
                UpdateActionButtonHighlights();
        }

        void UpdateActionButtonHighlights()
        {
            var mode = storeActions != null ? storeActions.CurrentMode : StoreActionMode.None;
            SetButtonHighlighted(buyButton, BuyColor, mode == StoreActionMode.Buy);
            SetButtonHighlighted(sellButton, SellColor, mode == StoreActionMode.Sell);
            SetButtonHighlighted(tradeButton, TradeColor, mode == StoreActionMode.Trade);
        }

        static void SetButtonHighlighted(Button button, Color activeTint, bool active)
        {
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? activeTint : IconNormalTint;
        }

        void LoadButtonIcons()
        {
            buyIcon = PlaymatUiSprites.Buy;
            sellIcon = PlaymatUiSprites.Sell;
            tradeIcon = PlaymatUiSprites.Trade;
            nextPhaseIcon = PlaymatUiSprites.LabelNextPhase;
            endTurnIcon = PlaymatUiSprites.LabelEndTurn;

            if (buyIcon == null || sellIcon == null || tradeIcon == null)
                Debug.LogWarning("HandFlowController: UI action symbols not found at Resources/UI/Icons/");
            if (nextPhaseIcon == null || endTurnIcon == null)
                Debug.LogWarning("HandFlowController: Phase labels not found at Resources/UI/Labels/");
        }

        Vector3 GetDeckPosition()
        {
            return deckPoint != null ? deckPoint.position : PlaymatZones.Deck;
        }

        Vector3 GetSupplyPosition()
        {
            return supplyDeckView != null
                ? supplyDeckView.transform.position
                : PlaymatZones.Supply;
        }

        IEnumerator ReturnUnpickedIconsToSupplyRoutine(IReadOnlyList<CardView> unpicked, Vector3 supplyPosition)
        {
            var fronts = new List<Texture2D>(unpicked.Count);
            const float returnDuration = 0.35f;

            for (var i = 0; i < unpicked.Count; i++)
            {
                var card = unpicked[i];
                if (card == null)
                    continue;

                if (card.FrontTexture != null)
                    fronts.Add(card.FrontTexture);

                CardHoverAudio.PlayCardMove();

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    yield return animator.AnimateToRoutine(
                        supplyPosition,
                        card.CardScale * 0.85f,
                        returnDuration,
                        CardView.TableRotation);
                else
                    card.transform.position = supplyPosition;

                Destroy(card.gameObject);
            }

            CardDeck.ReturnManyToSupplyAndShuffle(fronts);
            supplyDeckView?.Refresh();
        }

        void EnsureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                    Destroy(legacyModule);

                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        void EnsureHandUi()
        {
            if (handUiRoot == null)
            {
                handUiRoot = new GameObject("HandUI");
                var canvas = handUiRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 300;
                var scaler = handUiRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                handUiRoot.AddComponent<GraphicRaycaster>();
            }

            LoadButtonIcons();
            EnsurePickPrompt();
            var tableCamera = GetTableCamera();
            legendaryPickView?.InitPickUi(handUiRoot.transform, tableCamera);
            cardBackPickView?.InitPickUi(
                handUiRoot.transform,
                tableCamera,
                legendaryPickView != null ? legendaryPickView.CardPrefab : null);
            marketModal?.EnsureBuilt(handUiRoot.transform, matchBridge, handView);

            EnsureKeepButton(false);
            EnsureStoreActionButton(ref listViewButton, "MarketButton", RiverToggleButtonPosition, RiverToggleButtonScale, null, OnMarketButtonClicked, false);

            EnsureStoreActionButton(ref buyButton, "BuyButton", BuyButtonPosition, BuyButtonScale, buyIcon, OnBuyClicked, false);
            EnsureStoreActionButton(ref sellButton, "SellButton", SellButtonPosition, SellButtonScale, sellIcon, OnSellClicked, false);
            EnsureStoreActionButton(ref tradeButton, "TradeButton", TradeButtonPosition, TradeButtonScale, tradeIcon, OnTradeClicked, false);
            EnsurePhaseButton(ref nextPhaseButton, "NextPhaseButton", NextPhaseButtonInset, nextPhaseIcon, OnNextPhaseClicked);
            EnsurePhaseButton(ref endTurnButton, "EndTurnButton", EndTurnButtonInset, endTurnIcon, OnEndTurnClicked);
            UpdateTurnButtons();
        }

        void EnsurePickPrompt()
        {
            if (pickPromptText != null || handUiRoot == null)
                return;

            var promptObject = new GameObject("LegendaryPickPrompt");
            promptObject.transform.SetParent(handUiRoot.transform, false);

            pickPromptText = promptObject.AddComponent<Text>();
            pickPromptText.text = "Review your hand — \u267B redraw once per card, then Keep";
            pickPromptText.font = GameFonts.Bold;
            pickPromptText.fontSize = 32;
            pickPromptText.fontStyle = FontStyle.Bold;
            pickPromptText.alignment = TextAnchor.MiddleCenter;
            pickPromptText.color = new Color(0.95f, 0.90f, 0.78f, 1f);

            var rect = promptObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.88f);
            rect.anchorMax = new Vector2(0.5f, 0.88f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640f, 48f);
            rect.anchoredPosition = Vector2.zero;

            promptObject.SetActive(false);
        }

        void EnsureKeepButton(bool visible)
        {
            if (keepButton == null)
            {
                keepButton = CreateTextButton("KeepButton", "Accept Hand?", AcceptHandButtonPosition, AcceptHandButtonSize, AcceptHandButtonColor);
                keepButton.onClick.AddListener(OnKeepClicked);
            }
            else
            {
                keepButton.onClick.RemoveAllListeners();
                keepButton.onClick.AddListener(OnKeepClicked);
            }

            keepButton.gameObject.SetActive(visible);
            HoverTooltipTrigger.Attach(keepButton.gameObject, "Confirm this opening hand and start the match");

            EnsureMulliganButton(visible);
        }

        void EnsureMulliganButton(bool visible)
        {
            if (mulliganButton == null)
            {
                mulliganButton = CreateTextButton("MulliganButton", "Mulligan Hand", MulliganButtonPosition, MulliganButtonSize, MulliganButtonColor);
                mulliganButton.onClick.AddListener(OnMulliganClicked);
            }
            else
            {
                mulliganButton.onClick.RemoveAllListeners();
                mulliganButton.onClick.AddListener(OnMulliganClicked);
            }

            mulliganButton.gameObject.SetActive(visible && !mulliganUsed);
            HoverTooltipTrigger.Attach(mulliganButton.gameObject, "Discard this whole hand and draw a fresh one (once)");
        }

        void EnsureStoreActionButton(
            ref Button button,
            string name,
            Vector3 anchoredPosition,
            float scale,
            Sprite icon,
            UnityEngine.Events.UnityAction onClick,
            bool visible)
        {
            if (button == null)
            {
                button = CreateStoreActionButton(name, anchoredPosition, scale, icon);
                button.onClick.AddListener(onClick);
            }
            else
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);
                ApplyStoreActionButtonLayout(button.GetComponent<RectTransform>(), anchoredPosition, scale);

                var image = button.GetComponent<Image>();
                if (image != null && icon != null)
                    image.sprite = icon;
            }

            button.gameObject.SetActive(visible);
            HoverTooltipTrigger.Attach(button.gameObject, StoreActionTooltip(name));
        }

        static string StoreActionTooltip(string buttonName)
        {
            switch (buttonName)
            {
                case "BuyButton":
                    return "Buy a card from the store";
                case "SellButton":
                    return "Sell a card from your hand";
                case "TradeButton":
                    return "Trade a card with the store";
                case "MarketButton":
                    return "See this round's cards for sale";
                default:
                    return buttonName.Replace("Button", string.Empty);
            }
        }

        Button CreateTextButton(string name, string label, Vector3 anchoredPosition, Vector2 size, Color tint)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(handUiRoot.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = true;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            AddButtonCaption(buttonObject.transform, label);

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, KeepButtonAnchorY);
            buttonRect.anchorMax = new Vector2(0.5f, KeepButtonAnchorY);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition3D = anchoredPosition;
            buttonRect.localScale = Vector3.one;
            buttonRect.sizeDelta = size;
            return button;
        }

        void EnsurePhaseButton(
            ref Button button,
            string name,
            float insetFromRight,
            Sprite icon,
            UnityEngine.Events.UnityAction onClick)
        {
            if (button == null)
            {
                button = CreatePhaseButton(name, insetFromRight, icon);
                button.onClick.AddListener(onClick);
            }
            else
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);
                ApplyPhaseButtonLayout(button.GetComponent<RectTransform>(), insetFromRight);

                var image = button.GetComponent<Image>();
                if (image != null && icon != null)
                    image.sprite = icon;
            }

            SetPhaseButtonState(button, true, false);
            HoverTooltipTrigger.Attach(
                button.gameObject,
                name == "EndTurnButton" ? "End your turn" : "Go to the next phase");
        }

        Button CreatePhaseButton(string name, float insetFromRight, Sprite icon)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(handUiRoot.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.sprite = icon;
            image.color = icon != null ? PhaseButtonEnabledTint : new Color(0.12f, 0.16f, 0.24f, 0.96f);
            image.preserveAspect = true;
            image.raycastTarget = true;

            if (icon == null)
            {
                var caption = name.Replace("Button", string.Empty);
                if (caption == "NextPhase")
                    caption = "Next";
                else if (caption == "EndTurn")
                    caption = "End";
                AddButtonCaption(buttonObject.transform, caption);
            }

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ApplyPhaseButtonLayout(buttonObject.GetComponent<RectTransform>(), insetFromRight);
            return button;
        }

        static void ApplyPhaseButtonLayout(RectTransform buttonRect, float insetFromRight)
        {
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(PhaseButtonSize, PhaseButtonSize);
            buttonRect.anchoredPosition3D = new Vector3(-insetFromRight, PhaseButtonBottomOffset, 0f);
            buttonRect.localScale = Vector3.one;
        }

        Button CreateStoreActionButton(string name, Vector3 anchoredPosition, float scale, Sprite icon)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(handUiRoot.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.sprite = icon;
            image.color = icon != null ? IconNormalTint : new Color(0.12f, 0.16f, 0.24f, 0.96f);
            image.preserveAspect = true;
            image.raycastTarget = true;

            if (icon == null)
                AddButtonCaption(buttonObject.transform, name.Replace("Button", string.Empty));

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ApplyStoreActionButtonLayout(buttonObject.GetComponent<RectTransform>(), anchoredPosition, scale);
            return button;
        }

        static void AddButtonCaption(Transform parent, string label)
        {
            var textObject = new GameObject("Label");
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = GameFonts.Bold;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.98f, 0.94f, 0.82f, 1f);
            text.raycastTarget = false;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ApplyStoreActionButtonLayout(RectTransform buttonRect, Vector3 anchoredPosition, float scale)
        {
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(StoreActionButtonSize, StoreActionButtonSize);
            buttonRect.anchoredPosition3D = anchoredPosition;
            buttonRect.localScale = Vector3.one * scale;
        }
    }
}
