using System.Collections;
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
        [SerializeField] IconSlotView iconSlotView;
        [SerializeField] Transform deckPoint;
        [SerializeField] PlaymatZonesView playmatZones;

        Text pickPromptText;
        Button keepButton;
        Button listViewButton;
        Button buyButton;
        Button sellButton;
        Button tradeButton;
        Image listViewButtonIcon;
        GameObject handUiRoot;

        Sprite keepIcon;
        Sprite buyIcon;
        Sprite sellIcon;
        Sprite tradeIcon;
        Sprite listIcon;
        Sprite rowIcon;

        const float IconButtonSize = 72f;
        static readonly Color IconNormalTint = new(0.92f, 0.88f, 0.78f, 1f);

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
            if (cardBackPickView == null)
                cardBackPickView = gameObject.AddComponent<CardBackPickView>();
            if (iconSlotView == null)
                iconSlotView = GetComponent<IconSlotView>();
            if (playmatZones == null)
                playmatZones = GetComponent<PlaymatZonesView>();

            if (storeActions == null)
                storeActions = gameObject.AddComponent<StoreActionController>();

            storeActions.Init(handView, storeView, deckPoint, playmatZones != null ? playmatZones.OutOfPlay : null);
            handView?.BindStoreActions(storeActions);
            handView?.BindStoreView(storeView);
            storeView?.BindStoreActions(storeActions);
            storeView?.BindHandView(handView);

            CardDeck.ClearSession();
            handView?.ResetSession();
            storeView?.ClearStoreRuntime();
            iconSlotView?.ClearIcon();
            EnsureEventSystem();
        }

        void Start()
        {
            EnsureHandUi();
            StartCoroutine(BeginOpeningHand());
        }

        void OnDestroy()
        {
            if (handUiRoot != null)
                Destroy(handUiRoot);
        }

        void Update()
        {
            legendaryPickView?.TickModal();
            cardBackPickView?.Tick();

            if (!WasPrimaryClickThisFrame() || IsPointerOverUi())
                return;

            if (TryRaycastCard(out _))
                return;

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

        IEnumerator BeginOpeningHand()
        {
            if (handView == null)
                yield break;

            SetPickPrompt("Choose your card back", true);

            if (cardBackPickView != null)
                yield return cardBackPickView.RunPickRoutine();

            SetPickPrompt(null, false);

            if (cardBackPickView != null && cardBackPickView.HasSelected)
                CardDeck.SetSelectedCardBack(cardBackPickView.SelectedBack);
            else
                CardDeck.SetSelectedCardBack(CardCatalog.GetDefaultCardBack());

            cardBackPickView?.ClearPickCards();

            var deckPosition = GetDeckPosition();
            CardDeck.PrepareSetupPool();

            if (storeView != null)
                yield return storeView.DealStoreRoutine(deckPosition);

            yield return handView.DealOpeningHandRoutine(deckPosition);

            CardDeck.CommitRemainderToSupply();

            SetPickPrompt("Review your cards, then choose your Legendary Icon", true);

            if (legendaryPickView != null)
                yield return legendaryPickView.RunPickRoutine();

            SetPickPrompt(null, false);

            if (legendaryPickView != null && legendaryPickView.HasSelected)
            {
                var icon = legendaryPickView.SelectedIcon;
                var fromPosition = legendaryPickView.GetSelectedCardPosition();
                CardDeck.SetSelectedLegendary(icon);
                legendaryPickView.ClearPickCards();

                if (iconSlotView != null)
                    yield return iconSlotView.PlaceIconRoutine(icon, fromPosition);
            }

            if (keepButton != null)
                keepButton.gameObject.SetActive(true);
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

        IEnumerator AfterKeepSequence()
        {
            if (handView == null)
                yield break;

            if (keepButton != null)
                keepButton.gameObject.SetActive(false);

            yield return handView.RunKeepHandRoutine();
            SetStoreButtonsVisible(true);
        }

        void OnListViewClicked()
        {
            storeView?.ToggleListView();
            UpdateListViewButtonIcon();
        }

        void OnBuyClicked() => SetStoreActionMode(StoreActionMode.Buy);

        void OnSellClicked() => SetStoreActionMode(StoreActionMode.Sell);

        void OnTradeClicked() => SetStoreActionMode(StoreActionMode.Trade);

        void SetStoreActionMode(StoreActionMode mode)
        {
            storeActions?.SetMode(mode);
            UpdateActionButtonHighlights();
        }

        void UpdateListViewButtonIcon()
        {
            if (listViewButtonIcon == null || storeView == null || listIcon == null || rowIcon == null)
                return;

            listViewButtonIcon.sprite = storeView.IsListView ? rowIcon : listIcon;
        }

        void SetStoreButtonsVisible(bool visible)
        {
            if (listViewButton != null)
            {
                listViewButton.gameObject.SetActive(visible);
                UpdateListViewButtonIcon();
            }

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
            keepIcon = Resources.Load<Sprite>("UI/Icons/Keep");
            buyIcon = Resources.Load<Sprite>("UI/Icons/Buy");
            sellIcon = Resources.Load<Sprite>("UI/Icons/Sell");
            tradeIcon = Resources.Load<Sprite>("UI/Icons/Trade");
            listIcon = Resources.Load<Sprite>("UI/Icons/List");
            rowIcon = Resources.Load<Sprite>("UI/Icons/Row");

            if (keepIcon == null)
                Debug.LogWarning("HandFlowController: UI icons not found at Resources/UI/Icons/");
        }

        Vector3 GetDeckPosition()
        {
            return deckPoint != null ? deckPoint.position : PlaymatZones.Supply;
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
                handUiRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                handUiRoot.AddComponent<GraphicRaycaster>();
            }

            LoadButtonIcons();
            EnsurePickPrompt();
            legendaryPickView?.InitPickUi(handUiRoot.transform, Camera.main);
            cardBackPickView?.InitPickUi(
                handUiRoot.transform,
                Camera.main,
                legendaryPickView != null ? legendaryPickView.CardPrefab : null);

            EnsureIconButton(ref keepButton, "KeepButton", 0.5f, keepIcon, OnKeepClicked, false);
            EnsureIconButton(ref listViewButton, "ListViewButton", 0.18f, listIcon, OnListViewClicked, false);
            listViewButtonIcon = listViewButton != null ? listViewButton.GetComponent<Image>() : null;

            EnsureIconButton(ref buyButton, "BuyButton", 0.40f, buyIcon, OnBuyClicked, false);
            EnsureIconButton(ref sellButton, "SellButton", 0.55f, sellIcon, OnSellClicked, false);
            EnsureIconButton(ref tradeButton, "TradeButton", 0.70f, tradeIcon, OnTradeClicked, false);
        }

        void EnsurePickPrompt()
        {
            if (pickPromptText != null || handUiRoot == null)
                return;

            var promptObject = new GameObject("LegendaryPickPrompt");
            promptObject.transform.SetParent(handUiRoot.transform, false);

            pickPromptText = promptObject.AddComponent<Text>();
            pickPromptText.text = "Review your cards, then choose your Legendary Icon";
            pickPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        void EnsureIconButton(
            ref Button button,
            string name,
            float anchorX,
            Sprite icon,
            UnityEngine.Events.UnityAction onClick,
            bool visible)
        {
            if (button == null)
            {
                button = CreateIconButton(name, anchorX, icon);
                button.onClick.AddListener(onClick);
            }
            else
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);

                var image = button.GetComponent<Image>();
                if (image != null && icon != null)
                    image.sprite = icon;
            }

            button.gameObject.SetActive(visible);
        }

        Button CreateIconButton(string name, float anchorX, Sprite icon)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(handUiRoot.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.sprite = icon;
            image.color = IconNormalTint;
            image.preserveAspect = true;
            image.raycastTarget = true;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(anchorX, 0.1f);
            buttonRect.anchorMax = new Vector2(anchorX, 0.1f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(IconButtonSize, IconButtonSize);
            buttonRect.anchoredPosition = Vector2.zero;

            return button;
        }
    }
}
