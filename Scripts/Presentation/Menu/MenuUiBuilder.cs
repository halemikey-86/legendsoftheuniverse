using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation.Menu
{
    public static class MenuUiBuilder
    {
        public static readonly Color Background = new(0.05f, 0.08f, 0.18f, 1f);
        public static readonly Color PanelColor = new(0.08f, 0.11f, 0.20f, 0.96f);
        public static readonly Color TextColor = new(0.95f, 0.90f, 0.78f, 1f);
        public static readonly Color ButtonColor = new(0.18f, 0.22f, 0.34f, 1f);
        public static readonly Color ButtonHoverColor = new(0.24f, 0.30f, 0.44f, 1f);
        public static readonly Color DisabledColor = new(0.12f, 0.13f, 0.18f, 0.75f);
        public static readonly Color DisabledTextColor = new(0.55f, 0.52f, 0.48f, 1f);

        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Image CreateFullScreenBackground(Transform parent, Color color)
        {
            var background = CreateRect("Background", parent);
            StretchFill(background);
            var image = background.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = CreateRect(name, parent);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.gameObject.AddComponent<Image>();
            image.color = PanelColor;
            return panel.gameObject;
        }

        public static Text CreateTitle(Transform parent, string text, float fontSize)
        {
            var titleObject = CreateRect("Title", parent);
            var rect = titleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.82f);
            rect.anchorMax = new Vector2(0.5f, 0.82f);
            rect.sizeDelta = new Vector2(900f, 80f);

            var title = titleObject.gameObject.AddComponent<Text>();
            title.text = text;
            title.font = DefaultFont;
            title.fontSize = (int)fontSize;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = TextColor;
            return title;
        }

        public static Button CreateMenuButton(Transform parent, string label, float yAnchor, UnityEngine.Events.UnityAction onClick, bool interactable = true)
        {
            var buttonObject = CreateRect(label + "Button", parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, yAnchor);
            rect.anchorMax = new Vector2(0.5f, yAnchor);
            rect.sizeDelta = new Vector2(420f, 56f);

            var image = buttonObject.gameObject.AddComponent<Image>();
            image.color = interactable ? ButtonColor : DisabledColor;

            var button = buttonObject.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;

            var colors = button.colors;
            colors.normalColor = interactable ? ButtonColor : DisabledColor;
            colors.highlightedColor = ButtonHoverColor;
            colors.pressedColor = new Color(0.14f, 0.18f, 0.28f, 1f);
            colors.disabledColor = DisabledColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var textObject = CreateRect("Label", buttonObject);
            StretchFill(textObject);
            var text = textObject.gameObject.AddComponent<Text>();
            text.text = label;
            text.font = DefaultFont;
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = interactable ? TextColor : DisabledTextColor;

            return button;
        }

        public static Text CreateLabel(Transform parent, string text, Vector2 anchor, Vector2 size, int fontSize = 22)
        {
            var labelObject = CreateRect("Label", parent);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var label = labelObject.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = DefaultFont;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = TextColor;
            return label;
        }

        public static Slider CreateSlider(Transform parent, Vector2 anchor, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var sliderObject = CreateRect("Slider", parent);
            var rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(320f, 24f);

            var background = CreateRect("Background", sliderObject);
            var bgRect = background.GetComponent<RectTransform>();
            StretchFill(bgRect);
            bgRect.offsetMin = new Vector2(0f, 8f);
            bgRect.offsetMax = new Vector2(0f, -8f);
            var bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.14f, 0.22f, 1f);

            var fillArea = CreateRect("Fill Area", sliderObject);
            StretchFill(fillArea);
            fillArea.offsetMin = new Vector2(8f, 10f);
            fillArea.offsetMax = new Vector2(-8f, -10f);

            var fill = CreateRect("Fill", fillArea);
            StretchFill(fill);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.45f, 0.58f, 0.78f, 1f);

            var handleSlideArea = CreateRect("Handle Slide Area", sliderObject);
            StretchFill(handleSlideArea);

            var handle = CreateRect("Handle", handleSlideArea);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = TextColor;

            var slider = sliderObject.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        public static Dropdown CreateDropdown(Transform parent, Vector2 anchor, string[] options, int value, UnityEngine.Events.UnityAction<int> onChanged)
        {
            var dropdownObject = CreateRect("Dropdown", parent);
            var rect = dropdownObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(320f, 36f);

            var image = dropdownObject.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            var dropdown = dropdownObject.gameObject.AddComponent<Dropdown>();
            dropdown.targetGraphic = image;

            var labelObject = CreateRect("Label", dropdownObject);
            StretchFill(labelObject);
            labelObject.offsetMin = new Vector2(12f, 0f);
            var label = labelObject.gameObject.AddComponent<Text>();
            label.font = DefaultFont;
            label.fontSize = 20;
            label.color = TextColor;
            label.alignment = TextAnchor.MiddleLeft;
            dropdown.captionText = label;

            var template = CreateRect("Template", dropdownObject);
            template.gameObject.SetActive(false);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 160f);

            var templateImage = template.gameObject.AddComponent<Image>();
            templateImage.color = PanelColor;

            var viewport = CreateRect("Viewport", template);
            StretchFill(viewport);
            var content = CreateRect("Content", viewport);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 28f);

            var item = CreateRect("Item", content);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 28f);
            var itemToggle = item.gameObject.AddComponent<Toggle>();

            var itemLabelObject = CreateRect("Item Label", item);
            StretchFill(itemLabelObject);
            var itemLabel = itemLabelObject.gameObject.AddComponent<Text>();
            itemLabel.font = DefaultFont;
            itemLabel.fontSize = 18;
            itemLabel.color = TextColor;
            itemLabel.alignment = TextAnchor.MiddleLeft;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.options.Clear();
            foreach (var option in options)
                dropdown.options.Add(new Dropdown.OptionData(option));

            dropdown.value = value;
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(onChanged);
            return dropdown;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        public static void StretchFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
