using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Enlarged card reader — slides in from the screen edge when any card is clicked.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardSideReaderView : MonoBehaviour
    {
        static readonly Color PanelColor = new(0.06f, 0.07f, 0.11f, 0.96f);
        static readonly Color TextColor = new(0.95f, 0.90f, 0.78f, 1f);
        static readonly Color ActionColor = new(0.85f, 0.72f, 0.28f, 1f);
        static readonly Color DisabledColor = new(0.35f, 0.35f, 0.38f, 0.85f);

        const float SlideDuration = 0.28f;
        const float PanelWidth = 420f;
        const float CardImageHeight = 520f;

        Transform uiRoot;
        GameObject menuRoot;
        RectTransform panel;
        RawImage cardImage;
        Text titleText;
        Text subtitleText;
        Transform actionsRoot;
        CardView currentCard;
        Coroutine slideRoutine;
        Action<CardView> onBuy;
        Action<CardView> onSell;
        Action<CardView> onStoreTrade;
        Action<CardView> onHandTrade;
        Func<CardView, bool> canBuy;
        Func<CardView, bool> canSell;
        Func<CardView, bool> canStoreTrade;
        Func<CardView, bool> canHandTrade;

        public bool IsVisible => UiIsValid && menuRoot != null && menuRoot.activeSelf;
        public CardView CurrentCard => currentCard;

        public static CardSideReaderView Instance { get; private set; }

        bool UiIsValid =>
            panel != null
            && actionsRoot != null
            && titleText != null
            && subtitleText != null
            && cardImage != null;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void EnsureUi()
        {
            if (UiIsValid)
                return;

            panel = null;
            actionsRoot = null;
            titleText = null;
            subtitleText = null;
            cardImage = null;
            BuildUi();
        }

        public void SetUiRoot(Transform root)
        {
            if (uiRoot == root && menuRoot != null)
                return;

            uiRoot = root;
            if (menuRoot != null)
            {
                Destroy(menuRoot);
                menuRoot = null;
                panel = null;
                actionsRoot = null;
                titleText = null;
                subtitleText = null;
                cardImage = null;
            }
        }

        public void ConfigureStoreActions(
            Action<CardView> buy,
            Action<CardView> sell,
            Action<CardView> storeTrade,
            Action<CardView> handTrade,
            Func<CardView, bool> buyEnabled,
            Func<CardView, bool> sellEnabled,
            Func<CardView, bool> storeTradeEnabled,
            Func<CardView, bool> handTradeEnabled)
        {
            onBuy = buy;
            onSell = sell;
            onStoreTrade = storeTrade;
            onHandTrade = handTrade;
            canBuy = buyEnabled;
            canSell = sellEnabled;
            canStoreTrade = storeTradeEnabled;
            canHandTrade = handTradeEnabled;
        }

        public void Show(CardView card, string subtitle = null, bool showStoreActions = false, bool showHandActions = false)
        {
            EnsureUi();
            if (card == null || !UiIsValid)
                return;

            currentCard = card;
            titleText.text = StoreCardPricing.GetCardLabel(card);
            subtitleText.text = subtitle ?? string.Empty;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));

            cardImage.texture = card.FrontTexture;
            cardImage.color = card.FrontTexture != null ? Color.white : new Color(0.25f, 0.25f, 0.3f);

            RebuildActions(showStoreActions, showHandActions);

            if (slideRoutine != null)
                StopCoroutine(slideRoutine);

            panel.anchoredPosition = new Vector2(PanelWidth + 24f, 0f);
            menuRoot.SetActive(true);
            menuRoot.transform.SetAsLastSibling();
            panel.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            slideRoutine = StartCoroutine(SlideRoutine(true));
        }

        public void Hide()
        {
            if (!UiIsValid || !panel.gameObject.activeSelf)
                return;

            if (slideRoutine != null)
                StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(SlideRoutine(false));
        }

        void HideImmediate()
        {
            if (!UiIsValid)
                return;

            if (menuRoot != null)
                menuRoot.SetActive(false);
            panel.anchoredPosition = new Vector2(PanelWidth + 24f, 0f);
            currentCard = null;
        }

        IEnumerator SlideRoutine(bool show)
        {
            if (!UiIsValid)
                yield break;

            var start = panel.anchoredPosition;
            var end = show ? Vector2.zero : new Vector2(PanelWidth + 24f, 0f);
            var elapsed = 0f;

            while (elapsed < SlideDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / SlideDuration);
                panel.anchoredPosition = Vector2.Lerp(start, end, t);
                yield return null;
            }

            panel.anchoredPosition = end;
            if (!show)
            {
                panel.gameObject.SetActive(false);
                currentCard = null;
                if (menuRoot != null)
                    menuRoot.SetActive(false);
            }

            slideRoutine = null;
        }

        void RebuildActions(bool showStore, bool showHand)
        {
            EnsureUi();
            if (actionsRoot == null)
                return;

            for (var i = actionsRoot.childCount - 1; i >= 0; i--)
                Destroy(actionsRoot.GetChild(i).gameObject);

            var options = new List<(string label, Action cb, bool enabled)>();
            if (showStore)
            {
                var cost = StoreCardPricing.GetStoreWorth(currentCard);
                options.Add(($"Buy ({cost} Worth)", () => { Hide(); onBuy?.Invoke(currentCard); }, canBuy?.Invoke(currentCard) ?? false));
                options.Add(("Trade", () => { Hide(); onStoreTrade?.Invoke(currentCard); }, canStoreTrade?.Invoke(currentCard) ?? false));
            }

            if (showHand)
            {
                options.Add(("Sell", () => { Hide(); onSell?.Invoke(currentCard); }, canSell?.Invoke(currentCard) ?? false));
                options.Add(("Trade", () => { Hide(); onHandTrade?.Invoke(currentCard); }, canHandTrade?.Invoke(currentCard) ?? false));
            }

            options.Add(("Close", Hide, true));

            var y = 0f;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                CreateActionButton(option.label, option.cb, option.enabled, y);
                y -= 46f;
            }
        }

        void CreateActionButton(string label, Action callback, bool enabled, float y)
        {
            var buttonObject = new GameObject(label.Replace(" ", string.Empty));
            buttonObject.transform.SetParent(actionsRoot, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = enabled ? ActionColor : DisabledColor;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            button.onClick.AddListener(() => callback?.Invoke());

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(340f, 40f);
            rect.anchoredPosition = new Vector2(0f, y);

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = GameFonts.Default;
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        void BuildUi()
        {
            var host = uiRoot != null ? uiRoot : transform;
            var existing = host.Find("CardSideReader");
            if (existing != null)
                Destroy(existing.gameObject);

            menuRoot = new GameObject("CardSideReader");
            menuRoot.transform.SetParent(host, false);
            menuRoot.transform.SetAsLastSibling();

            var panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(menuRoot.transform, false);
            panel = panelObject.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 0.5f);
            panel.anchorMax = new Vector2(1f, 0.5f);
            panel.pivot = new Vector2(1f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, 720f);
            panel.anchoredPosition = new Vector2(PanelWidth + 24f, 0f);

            var background = panelObject.AddComponent<Image>();
            background.color = PanelColor;

            titleText = CreateText(panelObject.transform, "Title", 24, FontStyle.Bold, new Vector2(0.5f, 0.94f), new Vector2(380f, 40f));
            subtitleText = CreateText(panelObject.transform, "Subtitle", 16, FontStyle.Normal, new Vector2(0.5f, 0.88f), new Vector2(380f, 28f));

            var imageObject = new GameObject("CardImage");
            imageObject.transform.SetParent(panelObject.transform, false);
            cardImage = imageObject.AddComponent<RawImage>();
            cardImage.color = Color.white;
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.42f);
            imageRect.anchorMax = new Vector2(0.5f, 0.42f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = new Vector2(340f, CardImageHeight);

            var actionsObject = new GameObject("Actions");
            actionsObject.transform.SetParent(panelObject.transform, false);
            actionsRoot = actionsObject.transform;
            var actionsRect = actionsObject.AddComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(0.5f, 0.06f);
            actionsRect.anchorMax = new Vector2(0.5f, 0.06f);
            actionsRect.pivot = new Vector2(0.5f, 0f);
            actionsRect.sizeDelta = new Vector2(340f, 220f);
            actionsRect.anchoredPosition = Vector2.zero;

            menuRoot.SetActive(false);
        }

        static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Vector2 anchorY, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = GameFonts.Default;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorY;
            rect.anchorMax = anchorY;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return text;
        }
    }
}
