using System;
using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Screen-space Accept / Decline panel anchored beside a world-space card.
    /// </summary>
    public class CardChoiceModal
    {
        static readonly Color PanelColor = new(0.08f, 0.10f, 0.16f, 0.94f);
        static readonly Color TextColor = new(0.95f, 0.90f, 0.78f, 1f);
        static readonly Color AcceptColor = new(0.35f, 0.62f, 0.42f, 1f);
        static readonly Color DeclineColor = new(0.55f, 0.28f, 0.28f, 1f);

        const float EdgeGap = 40f;
        const float ScreenMargin = 16f;
        const float ScreenVerticalOffset = 28f;

        GameObject root;
        RectTransform panel;
        Text titleText;
        Button acceptButton;
        Button declineButton;

        Action onAccept;
        Action onDecline;
        CardView anchorCard;
        Camera worldCamera;

        public bool IsVisible => root != null && root.activeSelf;

        public void EnsureBuilt(Transform canvasRoot, Camera camera)
        {
            worldCamera = camera != null ? camera : Camera.main;
            if (root != null)
                return;

            root = new GameObject("CardChoiceModal");
            root.transform.SetParent(canvasRoot, false);

            panel = root.AddComponent<RectTransform>();
            panel.sizeDelta = new Vector2(220f, 168f);
            panel.pivot = new Vector2(0f, 0.5f);

            var background = root.AddComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = true;

            titleText = CreateText(root.transform, "Title", 22, FontStyle.Bold, new Vector2(0.5f, 0.78f), new Vector2(200f, 56f));

            acceptButton = CreateButton(root.transform, "AcceptButton", "Accept", AcceptColor, new Vector2(0.5f, 0.36f), OnAcceptClicked);
            HoverTooltipTrigger.Attach(acceptButton.gameObject, "Accept this card");
            declineButton = CreateButton(root.transform, "DeclineButton", "Decline", DeclineColor, new Vector2(0.5f, 0.12f), OnDeclineClicked);
            HoverTooltipTrigger.Attach(declineButton.gameObject, "Decline this card");

            root.SetActive(false);
        }

        public void Show(CardView card, string cardName, Action accept, Action decline)
        {
            if (root == null || card == null)
                return;

            anchorCard = card;
            onAccept = accept;
            onDecline = decline;
            titleText.text = FormatCardName(cardName);
            root.SetActive(true);
            UpdatePosition();
        }

        public void Hide()
        {
            if (root == null)
                return;

            root.SetActive(false);
            anchorCard = null;
            onAccept = null;
            onDecline = null;
        }

        public void Tick()
        {
            if (!IsVisible || anchorCard == null)
                return;

            UpdatePosition();
        }

        void UpdatePosition()
        {
            if (worldCamera == null || panel == null || anchorCard == null)
                return;

            var transform = anchorCard.transform;
            var centerScreen = worldCamera.WorldToScreenPoint(transform.position);
            if (centerScreen.z < 0f)
                return;

            var halfWidth = anchorCard.GetWorldSize().x * 0.5f;
            var rightWorld = transform.position + Vector3.right * halfWidth;
            var leftWorld = transform.position - Vector3.right * halfWidth;
            var rightScreen = worldCamera.WorldToScreenPoint(rightWorld);
            var leftScreen = worldCamera.WorldToScreenPoint(leftWorld);

            var panelWidth = panel.sizeDelta.x;
            var cardSize = anchorCard.GetWorldSize();
            var bottomWorld = transform.position + new Vector3(0f, 0f, -cardSize.z * 0.5f);
            var bottomScreen = worldCamera.WorldToScreenPoint(bottomWorld);
            var y = bottomScreen.y - ScreenVerticalOffset;

            var rightAnchorX = rightScreen.x + EdgeGap;
            if (rightAnchorX + panelWidth <= Screen.width - ScreenMargin)
            {
                panel.pivot = new Vector2(0f, 0.5f);
                panel.position = new Vector3(rightAnchorX, y, 0f);
                return;
            }

            var leftAnchorX = leftScreen.x - EdgeGap;
            panel.pivot = new Vector2(1f, 0.5f);
            panel.position = new Vector3(leftAnchorX, y, 0f);
        }

        void OnAcceptClicked()
        {
            var callback = onAccept;
            Hide();
            callback?.Invoke();
        }

        void OnDeclineClicked()
        {
            var callback = onDecline;
            Hide();
            callback?.Invoke();
        }

        static string FormatCardName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return "Legendary Icon";

            const string prefix = "Legendary Icon ";
            return rawName.StartsWith(prefix, StringComparison.Ordinal)
                ? rawName.Substring(prefix.Length)
                : rawName;
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
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY.y);
            rect.anchorMax = new Vector2(0.5f, anchorY.y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 anchorY, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY.y);
            rect.anchorMax = new Vector2(0.5f, anchorY.y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(180f, 40f);
            rect.anchoredPosition = Vector2.zero;

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = GameFonts.Default;
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
