using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// In-game shop menu — buy, sell, and trade during Main phase. No cards on the table.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StoreMarketMenuView : MonoBehaviour
    {
        static readonly Color PanelColor = new(0.06f, 0.08f, 0.13f, 0.97f);
        static readonly Color HeaderColor = new(0.95f, 0.90f, 0.78f, 1f);
        static readonly Color RowColor = new(0.10f, 0.12f, 0.18f, 0.92f);
        static readonly Color AccentColor = new(0.85f, 0.72f, 0.28f, 1f);
        static readonly Color CloseColor = new(0.55f, 0.28f, 0.28f, 1f);

        StoreView storeView;
        StoreActionController storeActions;
        HandView handView;
        TableMatchBridge matchBridge;

        Transform uiRoot;
        GameObject menuRoot;
        GameObject panelRoot;
        RectTransform cardsListRoot;
        Text worthLabel;
        Text hintLabel;
        bool isOpen;

        public bool IsOpen => isOpen;

        public void Init(
            StoreView store,
            StoreActionController actions,
            HandView hand,
            TableMatchBridge bridge,
            Transform uiRoot)
        {
            storeView = store;
            storeActions = actions;
            handView = hand;
            matchBridge = bridge;
            this.uiRoot = uiRoot;
            EnsureUi(uiRoot);

            if (storeView != null)
                storeView.StoreInventoryChanged += Refresh;
        }

        void OnDestroy()
        {
            if (storeView != null)
                storeView.StoreInventoryChanged -= Refresh;
        }

        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (!CanOpen())
                return;

            EnsureUi(uiRoot);
            isOpen = true;
            if (menuRoot != null)
                menuRoot.SetActive(true);

            Refresh();
        }

        public void Close()
        {
            isOpen = false;
            if (menuRoot != null)
                menuRoot.SetActive(false);

            storeActions?.SetMode(StoreActionMode.None);
        }

        bool CanOpen() => storeView != null && storeView.CanOpenMarketMenu;

        public void Refresh()
        {
            if (!isOpen || cardsListRoot == null || storeView == null)
                return;

            RebuildStoreRows();
            UpdateWorthLabel();
            UpdateHint();
        }

        void UpdateWorthLabel()
        {
            if (worthLabel == null)
                return;

            var worth = matchBridge != null && matchBridge.IsActive
                ? matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId)?.Worth ?? 0
                : Rules.GameConstants.StartingWorth;

            worthLabel.text = $"Worth: {worth}";
        }

        void UpdateHint()
        {
            if (hintLabel == null)
                return;

            hintLabel.text = storeView != null && storeView.CanInteractCards
                ? "Tap a market card to buy or trade. Tap a hand card to sell."
                : "Browse the market now. Buy, sell, and trade unlock during your Main phase.";
        }

        void RebuildStoreRows()
        {
            for (var i = cardsListRoot.childCount - 1; i >= 0; i--)
                Destroy(cardsListRoot.GetChild(i).gameObject);

            for (var slot = 0; slot < storeView.StoreSlotCount; slot++)
            {
                var card = storeView.GetStoreCardAt(slot);
                if (card == null)
                    continue;

                CreateStoreRow(card, slot);
            }

            if (cardsListRoot.childCount == 0)
                CreateEmptyLabel("No cards in the market.");
        }

        void CreateStoreRow(CardView card, int slotIndex)
        {
            var row = CreateRow($"StoreSlot{slotIndex}", cardsListRoot);
            var front = card.FrontTexture;
            var name = StoreCardPricing.GetCardLabel(card);
            var cost = StoreCardPricing.GetStoreWorth(card, storeView, matchBridge);

            CreateThumbnail(row.transform, front);
            CreateRowLabel(row.transform, name, 0.42f, 0.72f, 22, TextAnchor.MiddleLeft);
            CreateRowLabel(row.transform, $"{cost} Worth", 0.42f, 0.28f, 18, TextAnchor.MiddleLeft,
                new Color(0.78f, 0.82f, 0.92f, 1f));

            CreateRowButton(row.transform, "View", AccentColor, 0.88f, () =>
            {
                storeActions?.OpenStoreCardReader(card);
            });
        }

        void CreateEmptyLabel(string message)
        {
            var labelObject = new GameObject("EmptyLabel");
            labelObject.transform.SetParent(cardsListRoot, false);
            var text = labelObject.AddComponent<Text>();
            text.font = GameFonts.Default;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.75f, 0.78f, 0.86f, 1f);
            text.text = message;
            var rect = text.rectTransform;
            rect.sizeDelta = new Vector2(640f, 48f);
        }

        static GameObject CreateRow(string name, Transform parent)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);

            var image = row.AddComponent<Image>();
            image.color = RowColor;

            var layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 96f;
            layout.preferredHeight = 96f;

            var rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 96f);
            return row;
        }

        static void CreateThumbnail(Transform row, Texture2D texture)
        {
            var thumbObject = new GameObject("Thumb");
            thumbObject.transform.SetParent(row, false);

            var raw = thumbObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;

            var rect = raw.rectTransform;
            rect.anchorMin = new Vector2(0.02f, 0.12f);
            rect.anchorMax = new Vector2(0.16f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void CreateRowLabel(
            Transform row,
            string textValue,
            float anchorY,
            float height,
            int fontSize,
            TextAnchor alignment,
            Color? color = null)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(row, false);

            var text = labelObject.AddComponent<Text>();
            text.font = GameFonts.Bold;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color ?? HeaderColor;
            text.text = textValue;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.18f, anchorY - (height * 0.5f));
            rect.anchorMax = new Vector2(0.78f, anchorY + (height * 0.5f));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Button CreateRowButton(Transform row, string label, Color color, float anchorX, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(label + "Button");
            buttonObject.transform.SetParent(row, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorX, 0.18f);
            rect.anchorMax = new Vector2(0.98f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = GameFonts.Bold;
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = HeaderColor;

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        void EnsureUi(Transform parent)
        {
            if (menuRoot != null)
                return;

            var host = parent != null ? parent : transform;

            menuRoot = new GameObject("StoreMarketMenu");
            menuRoot.transform.SetParent(host, false);
            menuRoot.transform.SetAsLastSibling();

            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(menuRoot.transform, false);
            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.45f);
            blockerImage.raycastTarget = true;
            var blockerRect = blocker.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            panelRoot = new GameObject("Panel");
            panelRoot.transform.SetParent(menuRoot.transform, false);
            var panelImage = panelRoot.AddComponent<Image>();
            panelImage.color = PanelColor;

            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 620f);
            panelRect.anchoredPosition = Vector2.zero;

            CreateHeader(panelRoot.transform);
            CreateCloseButton(panelRoot.transform);
            CreateScrollList(panelRoot.transform);

            menuRoot.SetActive(false);
        }

        void CreateHeader(Transform panel)
        {
            var titleObject = new GameObject("Title");
            titleObject.transform.SetParent(panel, false);
            var title = titleObject.AddComponent<Text>();
            title.font = GameFonts.Bold;
            title.fontSize = 34;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = HeaderColor;
            title.text = "Shop";

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.55f, 0.97f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var worthObject = new GameObject("Worth");
            worthObject.transform.SetParent(panel, false);
            worthLabel = worthObject.AddComponent<Text>();
            worthLabel.font = GameFonts.Bold;
            worthLabel.fontSize = 22;
            worthLabel.alignment = TextAnchor.MiddleRight;
            worthLabel.color = AccentColor;

            var worthRect = worthObject.GetComponent<RectTransform>();
            worthRect.anchorMin = new Vector2(0.55f, 0.88f);
            worthRect.anchorMax = new Vector2(0.82f, 0.97f);
            worthRect.offsetMin = Vector2.zero;
            worthRect.offsetMax = Vector2.zero;

            var hintObject = new GameObject("Hint");
            hintObject.transform.SetParent(panel, false);
            hintLabel = hintObject.AddComponent<Text>();
            hintLabel.font = GameFonts.Default;
            hintLabel.fontSize = 16;
            hintLabel.alignment = TextAnchor.MiddleLeft;
            hintLabel.color = new Color(0.72f, 0.76f, 0.86f, 1f);

            var hintRect = hintObject.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.05f, 0.82f);
            hintRect.anchorMax = new Vector2(0.95f, 0.88f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
        }

        void CreateCloseButton(Transform panel)
        {
            var closeObject = new GameObject("CloseButton");
            closeObject.transform.SetParent(panel, false);

            var image = closeObject.AddComponent<Image>();
            image.color = CloseColor;

            var button = closeObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Close);

            var rect = closeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.88f, 0.88f);
            rect.anchorMax = new Vector2(0.96f, 0.97f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(closeObject.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.text = "X";
            label.font = GameFonts.Bold;
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = HeaderColor;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        void CreateScrollList(Transform panel)
        {
            var scrollObject = new GameObject("Scroll");
            scrollObject.transform.SetParent(panel, false);

            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            var scrollTransform = scrollObject.GetComponent<RectTransform>();
            scrollTransform.anchorMin = new Vector2(0.04f, 0.04f);
            scrollTransform.anchorMax = new Vector2(0.96f, 0.80f);
            scrollTransform.offsetMin = Vector2.zero;
            scrollTransform.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.15f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            cardsListRoot = content.AddComponent<RectTransform>();
            cardsListRoot.anchorMin = new Vector2(0f, 1f);
            cardsListRoot.anchorMax = new Vector2(1f, 1f);
            cardsListRoot.pivot = new Vector2(0.5f, 1f);
            cardsListRoot.anchoredPosition = Vector2.zero;
            cardsListRoot.sizeDelta = new Vector2(0f, 0f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = cardsListRoot;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }
    }
}
