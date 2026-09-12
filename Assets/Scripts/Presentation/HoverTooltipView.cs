using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using UnityEngine.UI;
using Willbound.Engine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Fast translucent hover bubble for table buttons and face-up cards.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(250)]
    public sealed class HoverTooltipView : MonoBehaviour
    {
        static readonly Color PanelColor = new(0.05f, 0.06f, 0.09f, 0.72f);
        static readonly Color TextColor = new(0.96f, 0.94f, 0.88f, 1f);
        static readonly Vector2 ScreenOffset = new(18f, 22f);

        static HoverTooltipView instance;

        RectTransform panelRect;
        Text label;
        CanvasGroup canvasGroup;
        bool visible;
        object currentSource;

        public static void Show(string text, object source = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Hide(source);
                return;
            }

            var view = Ensure();
            view.currentSource = source;
            view.ShowInternal(text);
        }

        public static void ShowCard(CardView card)
        {
            if (card == null || !card.IsFaceUp)
            {
                Hide(card);
                return;
            }

            var printing = card.BoundPrinting;
            if (printing == null)
                EngineCatalog.TryGetPrintingByArt(card.FrontTexture, out printing);

            var text = FormatCard(printing);
            if (string.IsNullOrEmpty(text))
            {
                Hide(card);
                return;
            }

            Show(text, card);
        }

        public static void Hide(object source = null)
        {
            if (instance == null)
                return;

            if (source != null && instance.currentSource != null && !ReferenceEquals(instance.currentSource, source))
                return;

            instance.currentSource = null;
            instance.HideInternal();
        }

        static HoverTooltipView Ensure()
        {
            if (instance != null)
                return instance;

            var host = new GameObject("HoverTooltip");
            var canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            canvas.pixelPerfect = true;

            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            instance = host.AddComponent<HoverTooltipView>();
            instance.Build();
            return instance;
        }

        void Build()
        {
            var panelObject = new GameObject("Bubble");
            panelObject.transform.SetParent(transform, false);

            panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.sizeDelta = new Vector2(120f, 40f);

            var background = panelObject.AddComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(panelObject.transform, false);
            label = textObject.AddComponent<Text>();
            label.font = GameFonts.Bold;
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = TextColor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            var textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);

            canvasGroup = panelObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0f;
            panelObject.SetActive(false);
        }

        void ShowInternal(string text)
        {
            if (label == null)
                return;

            label.text = text;
            var width = Mathf.Clamp(label.preferredWidth + 28f, 88f, 280f);
            var height = Mathf.Max(32f, label.preferredHeight + 18f);
            panelRect.sizeDelta = new Vector2(width, height);
            visible = true;
            panelRect.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            FollowPointer();
        }

        void HideInternal()
        {
            visible = false;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            if (panelRect != null)
                panelRect.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (visible)
                FollowPointer();
        }

        void FollowPointer()
        {
            if (panelRect == null)
                return;

            var mouse = ReadMouseScreen();
            var width = panelRect.sizeDelta.x;
            var height = panelRect.sizeDelta.y;
            var x = mouse.x + ScreenOffset.x;
            var y = mouse.y + ScreenOffset.y;

            x = Mathf.Clamp(x, 8f, Screen.width - width - 8f);
            y = Mathf.Clamp(y, 8f, Screen.height - height - 8f);
            panelRect.position = new Vector3(x, y, 0f);
        }

        static Vector2 ReadMouseScreen()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
#else
            return Input.mousePosition;
#endif
        }

        public static string FormatCard(CardPrinting printing)
        {
            if (printing == null)
                return null;

            if (!HasBodyStats(printing))
                return $"Will {printing.WillCost}";

            return $"Will {printing.WillCost}\nS {printing.Strike}   G {printing.Guard}   H {printing.Health}";
        }

        static bool HasBodyStats(CardPrinting printing)
        {
            switch (printing.Type)
            {
                case CardType.Companion:
                case CardType.Icon:
                case CardType.Token:
                    return true;
                default:
                    return printing.Strike > 0 || printing.Guard > 0 || printing.Health > 0;
            }
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
