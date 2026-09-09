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

        RectTransform panelRect;
        Text labelText;
        Text valueText;
        Transform worldAnchor;

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

        public static WorldAnchoredUi Create(Transform anchor, string label, Vector3 offset, Vector2 size, int labelFontSize = 16, bool withValue = true)
        {
            EnsureSharedCanvas();

            var host = new GameObject($"{label}Label");
            host.transform.SetParent(sharedCanvas.transform, false);
            var ui = host.AddComponent<WorldAnchoredUi>();
            ui.worldAnchor = anchor;
            ui.worldOffset = offset;
            ui.panelSize = size;
            ui.fontSize = labelFontSize;
            ui.showValue = withValue;
            ui.BuildUi(label);
            instanceCount++;
            return ui;
        }

        void BuildUi(string label)
        {
            panelRect = gameObject.AddComponent<RectTransform>();
            panelRect.sizeDelta = panelSize;
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0.05f, 0.06f, 0.12f, 0.88f);
            background.raycastTarget = false;

            var labelObject = new GameObject("Title");
            labelObject.transform.SetParent(transform, false);
            labelText = labelObject.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = fontSize;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.92f, 0.84f, 0.58f, 1f);
            labelText.raycastTarget = false;
            labelText.text = label;

            var labelRect = labelText.rectTransform;
            if (showValue)
            {
                labelRect.anchorMin = new Vector2(0f, 0.52f);
                labelRect.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
            }

            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            if (!showValue)
                return;

            var valueObject = new GameObject("Value");
            valueObject.transform.SetParent(transform, false);
            valueText = valueObject.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.fontSize = fontSize + 6;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.color = new Color(0.98f, 0.96f, 0.90f, 1f);
            valueText.raycastTarget = false;
            valueText.text = "0";

            var valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, 0.48f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
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
            sharedCanvas.sortingOrder = 180;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }
    }
}
