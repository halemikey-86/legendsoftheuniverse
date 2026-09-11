using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Screen-space UI panel that follows a world-space anchor (top-down table).
    /// </summary>
    public class WorldAnchoredUi : MonoBehaviour
    {
        static Canvas sharedCanvas;
        static int instanceCount;

        [SerializeField] Vector3 worldOffset = Vector3.zero;
        [SerializeField] Vector2 panelSize = new(120f, 36f);
        [SerializeField] int fontSize = 16;
        [SerializeField] bool showValue = true;
        [SerializeField] bool showTitle = true;

        RectTransform panelRect;
        Text labelText;
        Text subtitleText;
        Image labelImage;
        Text valueText;
        Transform worldAnchor;
        string hostName = "Label";

        public Transform WorldAnchor
        {
            get => worldAnchor;
            set => worldAnchor = value;
        }

        public string Label
        {
            get => labelText != null ? labelText.text : string.Empty;
            set
            {
                if (labelText != null)
                    labelText.text = value;
            }
        }

        public string Value
        {
            get => valueText != null ? valueText.text : string.Empty;
            set
            {
                if (valueText != null)
                    valueText.text = value;
            }
        }

        public string Subtitle
        {
            get => subtitleText != null ? subtitleText.text : string.Empty;
            set
            {
                if (subtitleText != null)
                    subtitleText.text = value ?? string.Empty;
            }
        }

        public static WorldAnchoredUi CreateLabeled(
            Transform anchor,
            string title,
            string subtitle,
            Vector3 offset,
            Vector2 size,
            int valueFontSize = 24)
        {
            var ui = CreateInternal(anchor, title, null, offset, size, 14, withValue: true, showTitle: true);
            ui.BuildSubtitle(subtitle);
            if (ui.valueText != null)
                ui.valueText.fontSize = valueFontSize;
            return ui;
        }

        public static WorldAnchoredUi Create(Transform anchor, string label, Vector3 offset, Vector2 size, int labelFontSize = 16, bool withValue = true)
        {
            return CreateInternal(anchor, label, null, offset, size, labelFontSize, withValue, showTitle: true);
        }

        public static WorldAnchoredUi Create(Transform anchor, Sprite labelSprite, string hostName, Vector3 offset, Vector2 size, bool withValue = true)
        {
            return CreateInternal(anchor, hostName, labelSprite, offset, size, 16, withValue, showTitle: true);
        }

        /// <summary>Value-only overlay for playmat counters (titles are baked into the mat art).</summary>
        public static WorldAnchoredUi CreateValueOnly(Transform anchor, Vector3 offset, Vector2 size, int valueFontSize = 22)
        {
            return CreateInternal(anchor, "Counter", null, offset, size, valueFontSize, withValue: true, showTitle: false);
        }

        static WorldAnchoredUi CreateInternal(
            Transform anchor,
            string hostName,
            Sprite labelSprite,
            Vector3 offset,
            Vector2 size,
            int labelFontSize,
            bool withValue,
            bool showTitle)
        {
            EnsureSharedCanvas();

            var host = new GameObject($"{hostName}Label");
            host.transform.SetParent(sharedCanvas.transform, false);
            var ui = host.AddComponent<WorldAnchoredUi>();
            ui.worldAnchor = anchor;
            ui.worldOffset = offset;
            ui.panelSize = size;
            ui.fontSize = labelFontSize;
            ui.showValue = withValue;
            ui.showTitle = showTitle;
            ui.hostName = hostName;
            ui.BuildUi(hostName, labelSprite);
            instanceCount++;
            return ui;
        }

        void BuildUi(string label, Sprite labelSprite)
        {
            panelRect = gameObject.AddComponent<RectTransform>();
            panelRect.sizeDelta = panelSize;
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            var background = gameObject.AddComponent<Image>();
            background.color = labelSprite != null
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(0.08f, 0.09f, 0.14f, 0.94f);
            background.raycastTarget = false;

            if (showTitle)
            {
                if (labelSprite != null)
                    BuildSpriteLabel(labelSprite);
                else
                    BuildTextLabel(label);
            }

            if (!showValue)
                return;

            var valueObject = new GameObject("Value");
            valueObject.transform.SetParent(transform, false);
            valueText = valueObject.AddComponent<Text>();
            valueText.font = GameFonts.Bold;
            valueText.fontSize = showTitle ? fontSize + 8 : fontSize;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.color = new Color(0.98f, 0.96f, 0.90f, 1f);
            valueText.raycastTarget = false;
            valueText.text = "0";

            var valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, subtitleText != null ? 0.34f : 0.42f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
        }

        void BuildSubtitle(string subtitle)
        {
            if (string.IsNullOrEmpty(subtitle))
                return;

            var subtitleObject = new GameObject("Subtitle");
            subtitleObject.transform.SetParent(transform, false);
            subtitleText = subtitleObject.AddComponent<Text>();
            subtitleText.font = GameFonts.Bold;
            subtitleText.fontSize = Mathf.Max(10, fontSize - 2);
            subtitleText.fontStyle = FontStyle.Italic;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.78f, 0.82f, 0.92f, 0.95f);
            subtitleText.raycastTarget = false;
            subtitleText.text = subtitle;

            var subtitleRect = subtitleText.rectTransform;
            subtitleRect.anchorMin = new Vector2(0f, 0.36f);
            subtitleRect.anchorMax = new Vector2(1f, 0.56f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;

            if (labelText != null)
            {
                var labelRect = labelText.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0.58f);
                labelRect.anchorMax = new Vector2(1f, 1f);
            }

            if (valueText != null)
            {
                var valueRect = valueText.rectTransform;
                valueRect.anchorMin = new Vector2(0f, 0f);
                valueRect.anchorMax = new Vector2(1f, 0.34f);
            }
        }

        void BuildTextLabel(string label)
        {
            var labelObject = new GameObject("Title");
            labelObject.transform.SetParent(transform, false);
            labelText = labelObject.AddComponent<Text>();
            labelText.font = GameFonts.Bold;
            labelText.fontSize = fontSize;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.92f, 0.84f, 0.58f, 1f);
            labelText.raycastTarget = false;
            labelText.text = label;

            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.52f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        void BuildSpriteLabel(Sprite labelSprite)
        {
            var labelObject = new GameObject("TitleSprite");
            labelObject.transform.SetParent(transform, false);
            labelImage = labelObject.AddComponent<Image>();
            labelImage.sprite = labelSprite;
            labelImage.preserveAspect = true;
            labelImage.raycastTarget = false;
            labelImage.color = Color.white;

            var labelRect = labelImage.rectTransform;
            if (showValue)
            {
                labelRect.anchorMin = new Vector2(0.04f, 0.48f);
                labelRect.anchorMax = new Vector2(0.96f, 0.98f);
            }
            else
            {
                labelRect.anchorMin = new Vector2(0.04f, 0.04f);
                labelRect.anchorMax = new Vector2(0.96f, 0.96f);
            }

            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            if (panelRect == null || worldAnchor == null)
                return;

            var camera = Camera.main;
            if (camera == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            panelRect.position = camera.WorldToScreenPoint(worldAnchor.position + worldOffset);
        }

        void OnDestroy()
        {
            instanceCount--;
            if (instanceCount <= 0 && sharedCanvas != null)
            {
                Destroy(sharedCanvas.gameObject);
                sharedCanvas = null;
                instanceCount = 0;
            }
        }

        static void EnsureSharedCanvas()
        {
            if (sharedCanvas != null)
                return;

            var canvasObject = new GameObject("PlaymatZoneUi");
            sharedCanvas = canvasObject.AddComponent<Canvas>();
            sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            sharedCanvas.sortingOrder = 500;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }
    }
}
