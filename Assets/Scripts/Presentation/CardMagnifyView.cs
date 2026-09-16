using System.Text;
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
    /// Right-click any face-up table card to pin a readable, screen-centered magnifier.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(260)]
    public sealed class CardMagnifyView : MonoBehaviour
    {
        static readonly Color DimmerColor = new(0.02f, 0.02f, 0.04f, 0.78f);
        static readonly Color FrameColor = new(0.93f, 0.84f, 0.58f, 0.95f);
        static readonly Color PanelColor = new(0.06f, 0.07f, 0.10f, 0.94f);
        static readonly Color BodyColor = new(0.96f, 0.93f, 0.84f, 1f);
        static readonly Color HintColor = new(0.78f, 0.74f, 0.64f, 0.9f);
        static readonly Vector2 CardSize = new(520f, 726f);

        static CardMagnifyView instance;

        CanvasGroup canvasGroup;
        RawImage cardImage;
        Text detailsText;
        Text hintText;
        CardView currentCard;
        bool visible;
#if ENABLE_INPUT_SYSTEM
        InputAction rightClickAction;
#endif

        public static bool IsOpen => instance != null && instance.visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            Ensure();
        }

        public static void Show(CardView card)
        {
            if (card == null)
                return;

            Ensure().ShowInternal(card);
        }

        public static void Hide()
        {
            if (instance == null)
                return;

            instance.HideInternal();
        }

        public static void NotifyPointerOver(CardView card)
        {
            if (card == null || !WasRightClickThisFrame())
                return;

            Show(card);
        }

        public static CardMagnifyView Ensure()
        {
            if (instance != null)
                return instance;

            var host = new GameObject("CardMagnify");
            var canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 920;
            canvas.pixelPerfect = true;

            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            host.AddComponent<GraphicRaycaster>();

            instance = host.AddComponent<CardMagnifyView>();
            instance.Build();
            return instance;
        }

        void Build()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var dimmerObject = new GameObject("Dimmer");
            dimmerObject.transform.SetParent(transform, false);
            var dimmerRect = dimmerObject.AddComponent<RectTransform>();
            StretchFull(dimmerRect);
            var dimmer = dimmerObject.AddComponent<Image>();
            dimmer.color = DimmerColor;
            dimmer.raycastTarget = true;
            var dimmerButton = dimmerObject.AddComponent<Button>();
            dimmerButton.transition = Selectable.Transition.None;
            dimmerButton.onClick.AddListener(HideInternal);

            var rowObject = new GameObject("Content");
            rowObject.transform.SetParent(transform, false);
            var rowRect = rowObject.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(980f, 760f);
            rowRect.anchoredPosition = new Vector2(0f, 12f);

            var frameObject = new GameObject("CardFrame");
            frameObject.transform.SetParent(rowObject.transform, false);
            var frameRect = frameObject.AddComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0f, 0.5f);
            frameRect.anchorMax = new Vector2(0f, 0.5f);
            frameRect.pivot = new Vector2(0f, 0.5f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.sizeDelta = CardSize + new Vector2(16f, 16f);
            var frame = frameObject.AddComponent<Image>();
            frame.color = FrameColor;
            frame.raycastTarget = true;

            var imageObject = new GameObject("CardImage");
            imageObject.transform.SetParent(frameObject.transform, false);
            var imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(6f, 6f);
            imageRect.offsetMax = new Vector2(-6f, -6f);
            cardImage = imageObject.AddComponent<RawImage>();
            cardImage.color = Color.white;
            cardImage.raycastTarget = false;
            var aspect = imageObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = CardSize.x / CardSize.y;

            var detailsObject = new GameObject("Details");
            detailsObject.transform.SetParent(rowObject.transform, false);
            var detailsRect = detailsObject.AddComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0f, 0.5f);
            detailsRect.anchorMax = new Vector2(0f, 0.5f);
            detailsRect.pivot = new Vector2(0f, 0.5f);
            detailsRect.anchoredPosition = new Vector2(CardSize.x + 44f, 0f);
            detailsRect.sizeDelta = new Vector2(420f, CardSize.y + 16f);
            var detailsBg = detailsObject.AddComponent<Image>();
            detailsBg.color = PanelColor;
            detailsBg.raycastTarget = true;

            detailsText = CreateText(
                detailsObject.transform,
                "DetailsText",
                22,
                FontStyle.Normal,
                BodyColor,
                TextAnchor.UpperLeft,
                new Vector2(0.5f, 0.5f),
                new Vector2(384f, 680f));
            detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailsText.verticalOverflow = VerticalWrapMode.Truncate;
            detailsText.lineSpacing = 1.08f;
            var detailsTextRect = detailsText.rectTransform;
            detailsTextRect.anchorMin = Vector2.zero;
            detailsTextRect.anchorMax = Vector2.one;
            detailsTextRect.offsetMin = new Vector2(20f, 18f);
            detailsTextRect.offsetMax = new Vector2(-20f, -18f);

            hintText = CreateText(
                transform,
                "Hint",
                18,
                FontStyle.Italic,
                HintColor,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.045f),
                new Vector2(720f, 32f));
            hintText.text = "Right-click another card to switch  ·  Click or Esc to close";
            hintText.raycastTarget = false;
        }

        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (rightClickAction == null)
            {
                rightClickAction = new InputAction("CardMagnifyRightClick", InputActionType.Button, "<Mouse>/rightButton");
                rightClickAction.performed += OnRightClickPerformed;
            }

            rightClickAction.Enable();
#endif
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (rightClickAction == null)
                return;

            rightClickAction.performed -= OnRightClickPerformed;
            rightClickAction.Disable();
            rightClickAction.Dispose();
            rightClickAction = null;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        void OnRightClickPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            HandleRightClick();
        }
#endif

        static Text CreateText(
            Transform parent,
            string name,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor alignment,
            Vector2 anchor,
            Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = style == FontStyle.Italic && GameFonts.Italic != null
                ? GameFonts.Italic
                : GameFonts.Bold;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;

            var rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return text;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (WasCancelPressed() && visible)
                HideInternal();
        }

        void HandleRightClick()
        {
            if (TryGetCardUnderPointer(out var card) && CanMagnify(card))
            {
                if (visible && currentCard == card)
                    HideInternal();
                else
                    ShowInternal(card);
                return;
            }

            if (visible)
                HideInternal();
        }

        void LateUpdate()
        {
            if (visible && currentCard == null)
                HideInternal();
        }

        void ShowInternal(CardView card)
        {
            if (!CanMagnify(card))
                return;

            currentCard = card;
            visible = true;
            HoverTooltipView.Hide();

            cardImage.texture = card.FrontTexture;
            cardImage.enabled = card.FrontTexture != null;
            var aspect = cardImage.GetComponent<AspectRatioFitter>();
            if (aspect != null && card.FrontTexture != null)
                aspect.aspectRatio = (float)card.FrontTexture.width / Mathf.Max(1, card.FrontTexture.height);
            detailsText.text = FormatDetails(card);
            detailsText.color = BodyColor;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            CardHoverAudio.PlayHoverFlip();
        }

        void HideInternal()
        {
            visible = false;
            currentCard = null;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        static bool CanMagnify(CardView card)
        {
            return card != null && card.IsFaceUp && card.FrontTexture != null;
        }

        static bool TryGetCardUnderPointer(out CardView card)
        {
            card = null;
            var camera = FindTableCamera();
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(ReadMouseScreen());
            var hits = Physics.RaycastAll(ray, 250f, ~0, QueryTriggerInteraction.Collide);
            var bestDistance = float.MaxValue;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                var found = hit.collider.GetComponentInParent<CardView>();
                if (found == null || !CanMagnify(found) || hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                card = found;
            }

            return card != null;
        }

        static Camera FindTableCamera()
        {
            var tableView = FindAnyObjectByType<TableView>();
            if (tableView != null && tableView.TableCamera != null)
                return tableView.TableCamera;

            return Camera.main;
        }

        public static bool WasRightClickThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        static bool WasCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
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

        static string FormatDetails(CardView card)
        {
            var printing = card.BoundPrinting;
            if (printing == null)
                EngineCatalog.TryGetPrintingByArt(card.FrontTexture, out printing);

            if (printing == null)
                return "Card";

            var builder = new StringBuilder(256);
            builder.AppendLine(string.IsNullOrEmpty(printing.Name) ? printing.Id : printing.Name);

            var typeLine = printing.Type.ToString();
            if (!string.IsNullOrEmpty(printing.Subtype))
                typeLine += " · " + printing.Subtype;
            builder.AppendLine(typeLine);
            builder.AppendLine();
            builder.Append("Will ").Append(printing.WillCost);
            builder.Append("     Worth ").Append(printing.StoreWorth);
            builder.AppendLine();

            if (HasBodyStats(printing))
            {
                builder.Append("Strike ").Append(printing.Strike);
                builder.Append("   Guard ").Append(printing.Guard);
                builder.Append("   Health ").Append(printing.Health);
                builder.AppendLine();
            }

            if (printing.Keywords != null && printing.Keywords.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine(string.Join("  ·  ", printing.Keywords));
            }

            if (printing.Abilities != null)
            {
                for (var i = 0; i < printing.Abilities.Count; i++)
                {
                    var ability = printing.Abilities[i];
                    if (ability == null)
                        continue;

                    builder.AppendLine();
                    if (!string.IsNullOrEmpty(ability.Name))
                        builder.Append(ability.Name);
                    else
                        builder.Append("Ability");

                    builder.Append("  (").Append(ability.Timing).Append(')');
                    if (ability.CostWill > 0)
                        builder.Append("  ·  ").Append(ability.CostWill).Append(" Will");
                    if (ability.OncePerTurn)
                        builder.Append("  ·  once per turn");
                    if (ability.OncePerGame)
                        builder.Append("  ·  once per game");
                    builder.AppendLine();

                    if (!string.IsNullOrEmpty(ability.Text))
                        builder.AppendLine(ability.Text);
                }
            }

            return builder.ToString().TrimEnd();
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
