using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Runtime layout tuner — press F9 in Play mode. Save writes JSON that reloads on next run.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TableLayoutTunerView : MonoBehaviour
    {
        public const float PanelPixelWidth = 220f;
        const float PanelMargin = 4f;
        const float CollapsedPanelHeight = 56f;

        static readonly Color PanelColor = new(0.04f, 0.05f, 0.08f, 0.98f);
        static readonly Color BodyTextColor = Color.white;
        static readonly Color MutedTextColor = new(0.82f, 0.86f, 0.92f, 1f);
        static readonly Color RowColor = new(0.12f, 0.14f, 0.20f, 1f);
        static readonly Color SectionColor = new(1f, 0.88f, 0.45f, 1f);
        static readonly Color AccentColor = new(0.55f, 0.78f, 1f, 1f);
        static readonly Color SaveColor = new(0.22f, 0.62f, 0.38f, 1f);
        static readonly Color StatusColor = new(0.75f, 1f, 0.78f, 1f);

        static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static void StyleReadableText(Text text, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            text.font = UiFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.supportRichText = false;

            if (text.GetComponent<Outline>() == null)
            {
                var outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1.2f, -1.2f);
            }
        }

        GameObject root;
        RectTransform panelRect;
        GameObject scrollArea;
        GameObject statusObject;
        RectTransform contentRoot;
        Text statusLabel;
        Text collapseButtonLabel;
        TableLayoutSceneManipulator sceneManipulator;
        bool slidersExpanded = false;
        bool isOpen;

        public bool IsOpen => isOpen;

        void Awake()
        {
            sceneManipulator = GetComponent<TableLayoutSceneManipulator>();
            if (sceneManipulator == null)
                sceneManipulator = gameObject.AddComponent<TableLayoutSceneManipulator>();

            sceneManipulator.SelectionChanged += OnSceneSelectionChanged;
        }

        void OnDestroy()
        {
            if (sceneManipulator != null)
                sceneManipulator.SelectionChanged -= OnSceneSelectionChanged;
        }

        void Update()
        {
            if (WasTogglePressed())
                Toggle();
        }

        static bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F9);
#endif
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
            RebuildUiIfNeeded();
            slidersExpanded = false;
            ApplyPanelLayout();
            isOpen = true;
            root.SetActive(true);
            sceneManipulator?.SetEditModeActive(true);
            SetStatus("Click object, drag, Save. + for sliders.");
        }

        public void Close()
        {
            isOpen = false;
            sceneManipulator?.SetEditModeActive(false);
            if (root != null)
                root.SetActive(false);
            GetComponent<TableLayoutApplier>()?.ApplyLayoutLive();
        }

        void OnSceneSelectionChanged(Transform target)
        {
            if (!isOpen)
                return;

            if (target == null)
            {
                SetStatus("Click table object. Drag. Save.");
                return;
            }

            SetStatus($"Selected: {target.name}");
        }

        void RebuildUiIfNeeded()
        {
            if (root != null)
            {
                if (panelRect != null && Mathf.Abs(panelRect.rect.width - PanelPixelWidth) < 2f && scrollArea != null)
                    return;

                Destroy(root.transform.parent.gameObject);
                root = null;
                panelRect = null;
                scrollArea = null;
                contentRoot = null;
                statusLabel = null;
            }

            EnsureUi();
        }

        void EnsureUi()
        {
            if (root != null)
                return;

            var canvasObject = new GameObject("TableLayoutTuner");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            root = new GameObject("Panel");
            root.transform.SetParent(canvasObject.transform, false);
            var panelImage = root.AddComponent<Image>();
            panelImage.color = PanelColor;
            panelRect = root.GetComponent<RectTransform>();
            ApplyPanelLayout();

            CreateHeader(root.transform);
            CreateToolbar(root.transform);
            scrollArea = CreateScrollArea(root.transform);
            ApplyPanelLayout();
            root.SetActive(false);
        }

        void ApplyPanelLayout()
        {
            if (panelRect == null)
                return;

            if (slidersExpanded)
            {
                panelRect.pivot = new Vector2(0f, 0.5f);
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.offsetMin = new Vector2(PanelMargin, PanelMargin);
                panelRect.offsetMax = new Vector2(PanelPixelWidth, -PanelMargin);
            }
            else
            {
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.anchoredPosition = new Vector2(PanelMargin, -PanelMargin);
                panelRect.sizeDelta = new Vector2(PanelPixelWidth, CollapsedPanelHeight);
            }

            if (scrollArea != null)
                scrollArea.SetActive(slidersExpanded);

            if (statusObject != null)
                statusObject.SetActive(slidersExpanded);

            if (collapseButtonLabel != null)
                collapseButtonLabel.text = slidersExpanded ? "−" : "+";
        }

        void ToggleSlidersExpanded()
        {
            slidersExpanded = !slidersExpanded;
            ApplyPanelLayout();
        }

        void CreateHeader(Transform panel)
        {
            var titleObject = new GameObject("Title");
            titleObject.transform.SetParent(panel, false);
            var title = titleObject.AddComponent<Text>();
            StyleReadableText(title, 16, BodyTextColor, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleLeft;
            title.text = "Layout";
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.04f, 1f);
            titleRect.anchorMax = new Vector2(0.44f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -4f);
            titleRect.sizeDelta = new Vector2(0f, 22f);

            collapseButtonLabel = CreateToolbarButton(panel, "−", new Color(0.28f, 0.32f, 0.42f, 1f), 0.46f, 0.58f, ToggleSlidersExpanded, 1f, 1f, -4f, -26f);
            CreateToolbarButton(panel, "X", new Color(0.45f, 0.28f, 0.28f, 1f), 0.60f, 0.72f, Close, 1f, 1f, -4f, -26f);
        }

        void CreateToolbar(Transform panel)
        {
            var bar = new GameObject("Toolbar");
            bar.transform.SetParent(panel, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.04f, 1f);
            barRect.anchorMax = new Vector2(0.96f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -28f);
            barRect.sizeDelta = new Vector2(0f, 22f);

            CreateToolbarButton(bar.transform, "Save", SaveColor, 0f, 0.24f, OnSaveClicked);
            CreateToolbarButton(bar.transform, "Set", AccentColor, 0.25f, 0.49f, OnSetSelectionClicked);
            CreateToolbarButton(bar.transform, "Apply", new Color(0.35f, 0.48f, 0.62f, 1f), 0.50f, 0.74f, OnApplyClicked);
            CreateToolbarButton(bar.transform, "Reset", new Color(0.45f, 0.28f, 0.28f, 1f), 0.75f, 1f, OnResetClicked);

            statusObject = new GameObject("Status");
            statusObject.transform.SetParent(panel, false);
            statusLabel = statusObject.AddComponent<Text>();
            StyleReadableText(statusLabel, 11, StatusColor, FontStyle.Bold);
            statusLabel.alignment = TextAnchor.UpperLeft;
            statusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusLabel.verticalOverflow = VerticalWrapMode.Overflow;
            var statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.04f, 1f);
            statusRect.anchorMax = new Vector2(0.96f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -52f);
            statusRect.sizeDelta = new Vector2(0f, 28f);
        }

        static Text CreateToolbarButton(
            Transform parent,
            string label,
            Color color,
            float anchorMinX,
            float anchorMaxX,
            UnityEngine.Events.UnityAction onClick,
            float anchorMinY = 0f,
            float anchorMaxY = 1f,
            float anchoredPosY = 0f,
            float anchoredPosYOffset = 0f)
        {
            var buttonObject = new GameObject(label + "Button");
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = color;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            if (Mathf.Abs(anchoredPosYOffset) > 0.01f)
            {
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, anchoredPosY);
                rect.sizeDelta = new Vector2(0f, -anchoredPosYOffset);
            }
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.text = label;
            StyleReadableText(text, 12, BodyTextColor, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return text;
        }

        GameObject CreateScrollArea(Transform panel)
        {
            var scrollObject = new GameObject("Scroll");
            scrollObject.transform.SetParent(panel, false);
            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            var scrollTransform = scrollObject.GetComponent<RectTransform>();
            scrollTransform.anchorMin = new Vector2(0.03f, 0f);
            scrollTransform.anchorMax = new Vector2(0.97f, 1f);
            scrollTransform.offsetMin = new Vector2(0f, 4f);
            scrollTransform.offsetMax = new Vector2(0f, -84f);

            const float scrollbarWidth = 16f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObject.transform, false);
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-scrollbarWidth - 4f, 0f);

            var verticalScrollbar = CreateVerticalScrollbar(scrollObject.transform, scrollbarWidth);

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            contentRoot = content.AddComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(0f, 0f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.scrollSensitivity = 28f;

            BuildControls();
            return scrollObject;
        }

        static Scrollbar CreateVerticalScrollbar(Transform parent, float width)
        {
            var scrollbarObject = new GameObject("VerticalScrollbar");
            scrollbarObject.transform.SetParent(parent, false);

            var scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 1f);
            scrollbarRect.sizeDelta = new Vector2(width, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;

            var trackImage = scrollbarObject.AddComponent<Image>();
            trackImage.color = new Color(0.10f, 0.12f, 0.18f, 1f);

            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            var slidingRect = slidingArea.AddComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(4f, 6f);
            slidingRect.offsetMax = new Vector2(-4f, -6f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(slidingArea.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = AccentColor;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        void BuildControls()
        {
            TableLayoutSettings.EnsureLoaded();
            var data = TableLayoutSettings.Active;

            AddSection("Playmat");
            AddSlider("Mat Scale X", 1f, 14f, data.matScaleX, v => data.matScaleX = v);
            AddSlider("Mat Scale Z", 1f, 10f, data.matScaleZ, v => data.matScaleZ = v);
            AddSlider("Mat Pos X", -20f, 20f, data.matPosX, v => data.matPosX = v);
            AddSlider("Mat Pos Z", -20f, 20f, data.matPosZ, v => data.matPosZ = v);

            AddSection("Camera");
            AddSlider("Camera Height", 4f, 30f, data.cameraHeight, v => data.cameraHeight = v);
            AddSlider("Ortho Size", 8f, 40f, data.orthographicSize, v => data.orthographicSize = v);
            AddSlider("Zoom Multiplier", 0.5f, 2f, data.cameraZoomMultiplier, v => data.cameraZoomMultiplier = v);
            AddSlider("Camera Margin", 0f, 8f, data.playmatCameraMargin, v => data.playmatCameraMargin = v);

            AddSection("Heights & Card Scale");
            AddSlider("Card Y", 0f, 2f, data.cardY, v => data.cardY = v);
            AddSlider("Hand Y", 0f, 2f, data.handY, v => data.handY = v);
            AddSlider("Card Scale", 1f, 5f, data.cardScale, v => data.cardScale = v);
            AddSlider("Deck Card Scale", 1f, 5f, data.deckCardScale, v => data.deckCardScale = v);
            AddSlider("Icon Scale", 1f, 6f, data.iconScale, v => data.iconScale = v);

            AddSection("Hand");
            AddSlider("Hand Center X", -30f, 30f, data.handCenterX, v => data.handCenterX = v);
            AddSlider("Hand Center Z", -20f, 10f, data.handCenterZ, v => data.handCenterZ = v);
            AddSlider("Opening Hand Z", -10f, 10f, data.openingHandCenterZ, v => data.openingHandCenterZ = v);
            AddSlider("Kept Hand Spread", 1f, 10f, data.keptHandSpreadSpacing, v => data.keptHandSpreadSpacing = v);
            AddSlider("Deal Hand Spread", 1f, 10f, data.dealHandSpreadSpacing, v => data.dealHandSpreadSpacing = v);

            AddSection("Deck & Piles");
            AddSlider("Deck X", 0f, 40f, data.deckX, v => data.deckX = v);
            AddSlider("Deck Z", -15f, 15f, data.deckZ, v => data.deckZ = v);
            AddSlider("Discard X", 0f, 35f, data.discardX, v => data.discardX = v);
            AddSlider("Discard Z", -20f, 5f, data.discardZ, v => data.discardZ = v);
            AddSlider("Icon X", -35f, 0f, data.iconX, v => data.iconX = v);
            AddSlider("Icon Z", -20f, 5f, data.iconZ, v => data.iconZ = v);
            AddSlider("Supply X", -35f, 0f, data.supplyX, v => data.supplyX = v);
            AddSlider("Supply Z", 0f, 20f, data.supplyZ, v => data.supplyZ = v);

            AddSection("Battle Ground (2×5)");
            AddSlider("Field Center X", -20f, 20f, data.fieldGridCenterX, v => data.fieldGridCenterX = v);
            AddSlider("Field Center Z", 0f, 20f, data.fieldGridCenterZ, v => data.fieldGridCenterZ = v);
            AddSlider("Field Slot Spacing X", 2f, 10f, data.fieldSlotSpacingX, v => data.fieldSlotSpacingX = v);
            AddSlider("Field Slot Spacing Z", 2f, 12f, data.fieldSlotSpacingZ, v => data.fieldSlotSpacingZ = v);
            AddSlider("Field Collider X", 2f, 10f, data.fieldColliderX, v => data.fieldColliderX = v);
            AddSlider("Field Collider Z", 2f, 12f, data.fieldColliderZ, v => data.fieldColliderZ = v);

            AddSection("Trackers (zone position)");
            AddSlider("Round X", 0f, 30f, data.roundTrackX, v => data.roundTrackX = v);
            AddSlider("Round Z", 0f, 20f, data.roundTrackZ, v => data.roundTrackZ = v);
            AddSlider("Worth X", 0f, 30f, data.worthTrackX, v => data.worthTrackX = v);
            AddSlider("Worth Z", 0f, 20f, data.worthTrackZ, v => data.worthTrackZ = v);
            AddSlider("Will X", 0f, 35f, data.willTrackX, v => data.willTrackX = v);
            AddSlider("Will Z", 0f, 20f, data.willTrackZ, v => data.willTrackZ = v);
            AddSlider("Honor X", -30f, 0f, data.honorX, v => data.honorX = v);
            AddSlider("Honor Z", -10f, 10f, data.honorZ, v => data.honorZ = v);

            AddSection("Number overlays (fine-tune)");
            AddSlider("Supply # X", -8f, 8f, data.supplyNumberOffsetX, v => data.supplyNumberOffsetX = v);
            AddSlider("Supply # Y", -4f, 4f, data.supplyNumberOffsetY, v => data.supplyNumberOffsetY = v);
            AddSlider("Round # X", -8f, 8f, data.roundNumberOffsetX, v => data.roundNumberOffsetX = v);
            AddSlider("Round # Y", -4f, 4f, data.roundNumberOffsetY, v => data.roundNumberOffsetY = v);
            AddSlider("Worth # X", -8f, 8f, data.worthNumberOffsetX, v => data.worthNumberOffsetX = v);
            AddSlider("Worth # Y", -4f, 4f, data.worthNumberOffsetY, v => data.worthNumberOffsetY = v);
            AddSlider("Will # X", -8f, 8f, data.willNumberOffsetX, v => data.willNumberOffsetX = v);
            AddSlider("Will # Y", -4f, 4f, data.willNumberOffsetY, v => data.willNumberOffsetY = v);
            AddSlider("Honor # X", -8f, 8f, data.honorNumberOffsetX, v => data.honorNumberOffsetX = v);
            AddSlider("Honor # Y", -4f, 4f, data.honorNumberOffsetY, v => data.honorNumberOffsetY = v);

            AddSection("Bonds / Wells");
            AddSlider("Relic Bond Z", -10f, 10f, data.relicBondCenterZ, v => data.relicBondCenterZ = v);
            AddSlider("Stack Well Z", 0f, 15f, data.stackWellZ, v => data.stackWellZ = v);
            AddSlider("Willwell X", -25f, 0f, data.willwellX, v => data.willwellX = v);
            AddSlider("Willwell Z", 0f, 12f, data.willwellZ, v => data.willwellZ = v);
        }

        void AddSection(string title)
        {
            var row = CreateRow(title.Replace(" ", string.Empty), 22f);
            var rowImage = row.GetComponent<Image>();
            if (rowImage != null)
            {
                rowImage.color = new Color(0f, 0f, 0f, 0f);
                rowImage.raycastTarget = false;
            }

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(row.transform, false);
            var label = labelObject.AddComponent<Text>();
            StyleReadableText(label, 13, SectionColor, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleLeft;
            label.text = title;
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.04f, 0f);
            rect.anchorMax = new Vector2(0.96f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void AddSlider(string label, float min, float max, float initial, Action<float> apply)
        {
            var row = CreateRow(label.Replace(" ", string.Empty), 36f);
            var rowImage = row.GetComponent<Image>();
            if (rowImage != null)
                rowImage.color = RowColor;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(row.transform, false);
            var labelText = labelObject.AddComponent<Text>();
            StyleReadableText(labelText, 12, BodyTextColor);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.03f, 0.52f);
            labelRect.anchorMax = new Vector2(0.97f, 0.98f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var valueObject = new GameObject("Value");
            valueObject.transform.SetParent(row.transform, false);
            var valueText = valueObject.AddComponent<Text>();
            StyleReadableText(valueText, 12, AccentColor, FontStyle.Bold);
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.text = initial.ToString("0.##");
            var valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0.55f, 0.52f);
            valueRect.anchorMax = new Vector2(0.97f, 0.98f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;

            var sliderObject = new GameObject("Slider");
            sliderObject.transform.SetParent(row.transform, false);
            var sliderRect = sliderObject.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.03f, 0.06f);
            sliderRect.anchorMax = new Vector2(0.97f, 0.48f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            var background = new GameObject("Background");
            background.transform.SetParent(sliderObject.transform, false);
            background.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.28f, 1f);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(8f, 8f);
            fillAreaRect.offsetMax = new Vector2(-8f, -8f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = AccentColor;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = BodyTextColor;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14f, 20f);

            var slider = sliderObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = initial;
            slider.onValueChanged.AddListener(v =>
            {
                apply(v);
                valueText.text = v.ToString("0.##");
                TableLayoutSettings.NotifyChanged();
            });
        }

        GameObject CreateRow(string name, float height)
        {
            var row = new GameObject(name);
            row.transform.SetParent(contentRoot, false);
            row.AddComponent<Image>().color = RowColor;
            var layout = row.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
            return row;
        }

        void OnSetSelectionClicked()
        {
            if (sceneManipulator?.Selected == null)
            {
                SetStatus("Select an object first.");
                return;
            }

            sceneManipulator.CommitSelectedTransform();
            SetStatus($"{sceneManipulator.Selected.name} placed. Press Save to keep it.");
        }

        void OnSaveClicked()
        {
            if (sceneManipulator?.Selected != null)
                sceneManipulator.CommitSelectedTransform();

            if (TableLayoutSettings.Save(transform))
                SetStatus("Saved. Layout will load next time you play.");
            else
                SetStatus("Save failed. Check the Console.");
        }

        void OnApplyClicked()
        {
            GetComponent<TableLayoutApplier>()?.ApplyLayoutLive();
            SetStatus("Layout applied.");
        }

        void OnResetClicked()
        {
            TableLayoutSettings.ResetToDefaults();
            RebuildControls();
            SetStatus("Reset to defaults.");
        }

        void RebuildControls()
        {
            if (contentRoot == null)
                return;

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            BuildControls();
        }

        void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
