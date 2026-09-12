using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// During opening hand review, shows a recycle control under each card for one free redraw per slot.
    /// The replaced card returns to the supply; the new card is drawn from the deck.
    /// </summary>
    [DisallowMultipleComponent]
    public class OpeningHandRedrawView : MonoBehaviour
    {
        const float ButtonSize = 44f;
        const float ScreenGap = 10f;

        readonly Dictionary<CardView, Button> buttons = new();
        readonly HashSet<int> usedRedrawSlots = new();

        HandView handView;
        Transform uiRoot;
        Camera tableCamera;
        System.Func<Vector3> deckPosition;
        bool isActive;
        bool busy;

        public bool IsActive => isActive;

        public void Begin(
            HandView hand,
            Transform canvasRoot,
            Camera camera,
            System.Func<Vector3> getDeckPosition)
        {
            handView = hand;
            uiRoot = canvasRoot;
            tableCamera = camera;
            deckPosition = getDeckPosition;
            isActive = true;
            busy = false;
            usedRedrawSlots.Clear();
            RebuildButtons();
        }

        public void End()
        {
            isActive = false;
            busy = false;
            ClearButtons();
        }

        void LateUpdate()
        {
            if (!isActive || tableCamera == null)
                return;

            foreach (var pair in buttons)
            {
                var card = pair.Key;
                var button = pair.Value;
                if (card == null || button == null)
                    continue;

                PositionButtonUnderCard(button, card);
            }
        }

        void RebuildButtons()
        {
            ClearButtons();

            if (handView == null || uiRoot == null)
                return;

            for (var i = 0; i < handView.OpeningHandCards.Count; i++)
            {
                if (usedRedrawSlots.Contains(i))
                    continue;

                var card = handView.OpeningHandCards[i];
                if (card == null || buttons.ContainsKey(card))
                    continue;

                buttons[card] = CreateRecycleButton(card, i);
            }
        }

        void ClearButtons()
        {
            foreach (var button in buttons.Values)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            buttons.Clear();
        }

        Button CreateRecycleButton(CardView card, int slotIndex)
        {
            var buttonObject = new GameObject($"Redraw_{slotIndex}");
            buttonObject.transform.SetParent(uiRoot, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.22f, 0.92f);
            image.raycastTarget = true;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnRecycleClicked(card, slotIndex));

            var labelObject = new GameObject("Icon");
            labelObject.transform.SetParent(buttonObject.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.text = "\u267B";
            label.font = GameFonts.Bold;
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.85f, 0.92f, 0.78f, 1f);
            label.raycastTarget = false;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);

            PositionButtonUnderCard(button, card);
            HoverTooltipTrigger.Attach(buttonObject, "Redraw this card once");
            return button;
        }

        void PositionButtonUnderCard(Button button, CardView card)
        {
            if (button == null || tableCamera == null || card == null)
                return;

            var rect = button.GetComponent<RectTransform>();
            var cardSize = card.GetWorldSize();
            var bottomWorld = card.transform.position + new Vector3(0f, 0f, -cardSize.z * 0.5f);
            var bottomScreen = tableCamera.WorldToScreenPoint(bottomWorld);

            if (bottomScreen.z <= 0f)
            {
                button.gameObject.SetActive(false);
                return;
            }

            button.gameObject.SetActive(true);
            rect.position = new Vector3(bottomScreen.x, bottomScreen.y - ScreenGap, bottomScreen.z);
        }

        void OnRecycleClicked(CardView card, int slotIndex)
        {
            if (!isActive || busy || handView == null)
                return;

            if (usedRedrawSlots.Contains(slotIndex))
                return;

            StartCoroutine(RedrawRoutine(card, slotIndex));
        }

        IEnumerator RedrawRoutine(CardView card, int slotIndex)
        {
            busy = true;
            usedRedrawSlots.Add(slotIndex);

            if (buttons.TryGetValue(card, out var button) && button != null)
                Destroy(button.gameObject);

            buttons.Remove(card);

            var beforeCard = card;
            yield return handView.RedrawOpeningCardRoutine(
                card,
                slotIndex,
                deckPosition != null ? deckPosition() : PlaymatZones.Supply);

            var replacement = handView.GetOpeningHandCardAt(slotIndex);
            if (replacement == null || replacement == beforeCard)
            {
                usedRedrawSlots.Remove(slotIndex);
                if (replacement != null && !buttons.ContainsKey(replacement))
                    buttons[replacement] = CreateRecycleButton(replacement, slotIndex);
            }

            busy = false;
        }
    }
}
