using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Face-down supply deck pile with a screen-space counter beneath it.
    /// </summary>
    [DisallowMultipleComponent]
    public class SupplyDeckView : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] CardView cardPrefab;

        [Header("Stack")]
        [SerializeField] float deckCardScale = PlaymatZones.CardScale;
        [SerializeField] int maxVisibleLayers = 6;
        [SerializeField] float layerHeightOffset = 0.004f;
        [SerializeField] float layerDepthOffset = 0.045f;

        [Header("Counter")]
        [SerializeField] Vector3 countWorldOffset = new(0f, 0f, -1.2f);
        [SerializeField] Vector2 countScreenSize = new(88f, 40f);
        [SerializeField] int countFontSize = 28;

        Transform stackRoot;
        RectTransform counterRect;
        Text countText;
        Canvas counterCanvas;
        readonly List<CardView> stackCards = new();

        void OnEnable()
        {
            CardDeck.CountChanged += OnDeckCountChanged;
        }

        void Start()
        {
            EnsureScreenCounter();
            Refresh();
        }

        void LateUpdate()
        {
            UpdateCounterPosition();
        }

        void OnDisable()
        {
            CardDeck.CountChanged -= OnDeckCountChanged;
        }

        void OnDestroy()
        {
            if (counterCanvas != null)
                Destroy(counterCanvas.gameObject);
        }

        void OnDeckCountChanged(int count)
        {
            UpdateCountLabel(count);
            RebuildStack(count);
        }

        public void Refresh()
        {
            EnsureStackRoot();
            EnsureScreenCounter();
            var remaining = CardDeck.Remaining;
            RebuildStack(remaining);
            UpdateCountLabel(remaining);
            UpdateCounterPosition();
        }

        void EnsureStackRoot()
        {
            if (stackRoot != null)
                return;

            var rootObject = new GameObject("Stack");
            rootObject.transform.SetParent(transform, false);
            stackRoot = rootObject.transform;
        }

        void EnsureScreenCounter()
        {
            if (counterRect != null)
                return;

            var canvasObject = new GameObject("SupplyDeckCounter");
            counterCanvas = canvasObject.AddComponent<Canvas>();
            counterCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            counterCanvas.sortingOrder = 200;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            counterRect = panelObject.AddComponent<RectTransform>();
            counterRect.sizeDelta = countScreenSize;
            counterRect.pivot = new Vector2(0.5f, 0.5f);

            var background = panelObject.AddComponent<Image>();
            background.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);
            background.raycastTarget = false;

            var textObject = new GameObject("Count");
            textObject.transform.SetParent(panelObject.transform, false);
            countText = textObject.AddComponent<Text>();
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = countFontSize;
            countText.fontStyle = FontStyle.Bold;
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = new Color(0.98f, 0.94f, 0.82f, 1f);
            countText.raycastTarget = false;
            countText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        void UpdateCounterPosition()
        {
            if (counterRect == null)
                return;

            var camera = Camera.main;
            if (camera == null)
            {
                counterRect.gameObject.SetActive(false);
                return;
            }

            counterRect.gameObject.SetActive(true);
            var worldPosition = transform.position + countWorldOffset;
            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            counterRect.position = screenPosition;
        }

        void RebuildStack(int remaining)
        {
            EnsureStackRoot();

            for (var i = stackCards.Count - 1; i >= 0; i--)
            {
                if (stackCards[i] != null)
                    Destroy(stackCards[i].gameObject);
            }

            stackCards.Clear();

            if (cardPrefab == null || remaining <= 0)
                return;

            var layers = Mathf.Min(remaining, maxVisibleLayers);
            for (var i = 0; i < layers; i++)
            {
                var card = Instantiate(cardPrefab, stackRoot);
                card.transform.localPosition = new Vector3(0f, i * layerHeightOffset, i * layerDepthOffset);
                card.transform.localRotation = CardView.TableRotation;
                card.SetFaceUpImmediate(false);
                card.SetCardScale(deckCardScale);
                card.SetClickable(false);
                stackCards.Add(card);
            }
        }

        void UpdateCountLabel(int remaining)
        {
            if (countText == null)
                return;

            countText.text = remaining.ToString();
        }
    }
}
