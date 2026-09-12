using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Screen-space list modal for the Store — replaces the physical card river. Shows this round's
    /// purchasable cards (from the real engine Store) with a Buy button per row.
    /// </summary>
    public class MarketModalView : MonoBehaviour
    {
        const int RowCount = 7;
        static readonly Vector2 PanelSize = new(420f, 520f);
        static readonly Vector2 RowSize = new(380f, 52f);
        static readonly Color PanelColor = new(0.06f, 0.07f, 0.11f, 0.97f);
        static readonly Color RowColor = new(0.12f, 0.14f, 0.20f, 0.92f);
        static readonly Color BuyColor = new(0.20f, 0.42f, 0.24f, 0.96f);
        static readonly Color CloseColor = new(0.38f, 0.20f, 0.20f, 0.95f);
        static readonly Color TextColor = new(0.95f, 0.90f, 0.78f, 1f);

        sealed class RowUi
        {
            public GameObject go;
            public Text nameText;
            public Text costText;
            public Button buyButton;
        }

        GameObject root;
        RectTransform panel;
        readonly List<RowUi> rows = new();
        TableMatchBridge matchBridge;
        HandView handView;

        public bool IsVisible => root != null && root.activeSelf;

        public void EnsureBuilt(Transform canvasRoot, TableMatchBridge bridge, HandView hand)
        {
            matchBridge = bridge;
            handView = hand;

            if (root != null)
                return;

            root = new GameObject("MarketModal");
            root.transform.SetParent(canvasRoot, false);

            panel = root.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = PanelSize;
            panel.anchoredPosition = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = true;

            var title = CreateText(panel, "Title", "Market — This Round", 26,
                new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(360f, 44f));
            title.fontStyle = FontStyle.Bold;

            var closeButton = CreateButton(panel, "CloseButton", "Close", CloseColor,
                new Vector2(1f, 1f), new Vector2(-46f, -30f), new Vector2(72f, 40f));
            closeButton.onClick.AddListener(Hide);

            for (var i = 0; i < RowCount; i++)
                rows.Add(CreateRow(i));

            root.SetActive(false);
        }

        RowUi CreateRow(int index)
        {
            var rowObject = new GameObject($"Row{index}");
            rowObject.transform.SetParent(panel, false);

            var rect = rowObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = RowSize;
            rect.anchoredPosition = new Vector2(0f, -84f - (index * 58f));

            var bg = rowObject.AddComponent<Image>();
            bg.color = RowColor;

            var nameText = CreateText(rowObject.transform, "Name", string.Empty, 18,
                new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(190f, 44f));
            nameText.alignment = TextAnchor.MiddleLeft;

            var costText = CreateText(rowObject.transform, "Cost", string.Empty, 16,
                new Vector2(0.66f, 0.5f), Vector2.zero, new Vector2(80f, 44f));

            var buyButton = CreateButton(rowObject.transform, "BuyButton", "Buy", BuyColor,
                new Vector2(1f, 0.5f), new Vector2(-46f, 0f), new Vector2(80f, 40f));
            var slotIndex = index;
            buyButton.onClick.AddListener(() => OnBuyClicked(slotIndex));

            rowObject.SetActive(false);
            return new RowUi { go = rowObject, nameText = nameText, costText = costText, buyButton = buyButton };
        }

        public void Show()
        {
            if (root == null)
                return;

            root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void Toggle()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        public void Refresh()
        {
            if (root == null || !root.activeSelf || matchBridge == null || !matchBridge.IsActive)
                return;

            var store = matchBridge.Runner.Match.Store;
            for (var i = 0; i < rows.Count; i++)
            {
                var card = i < store.Length ? store[i] : null;
                if (card == null)
                {
                    rows[i].go.SetActive(false);
                    continue;
                }

                rows[i].go.SetActive(true);
                rows[i].nameText.text = card.Printing.Name;
                rows[i].costText.text = $"{card.Printing.StoreWorth} Worth";
            }
        }

        void OnBuyClicked(int slotIndex)
        {
            if (matchBridge == null || !matchBridge.TryStoreBuy(slotIndex, out _))
                return;

            Refresh();

            var player = matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId);
            if (player != null)
                handView?.SyncHandFromEngine(player.Hand);
        }

        static Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = GameFonts.Bold;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = TextColor;
            label.raycastTarget = false;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return label;
        }

        static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            CreateText(buttonObject.transform, "Label", label, 16, new Vector2(0.5f, 0.5f), Vector2.zero, size);

            return button;
        }
    }
}
