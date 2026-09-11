using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Turn start: present legendary Icon choices and wait for the player to pick one.
    /// </summary>
    [DisallowMultipleComponent]
    public class LegendaryPickView : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] CardView cardPrefab;

        [Header("Layout")]
        [SerializeField] Vector3 pickCenter = new(0f, PlaymatZones.CardY, 0.5f);
        [SerializeField] float pickSpacing = 5f;
        [SerializeField] float pickCardScale = PlaymatZones.CardScale;
        [SerializeField] float selectedScale = PlaymatZones.CardScale;
        [SerializeField] float pickAnimDuration = 0.35f;
        [SerializeField] int pickChoiceCount = GameSetupConstants.LegendaryIconChoices;

        [Header("Inspect")]
        [SerializeField] Vector3 inspectPosition = new(-2f, 0.1f, 1.5f);
        [SerializeField] float inspectScale = PlaymatZones.CardScale;
        [SerializeField] float inspectPeerScaleMultiplier = 0.72f;
        [SerializeField] float inspectPeerZOffset = 0.75f;
        [SerializeField] float inspectAnimDuration = 0.25f;

        readonly List<CardView> pickCards = new();
        readonly List<CardView> unpickedCards = new();
        readonly CardChoiceModal choiceModal = new();

        Texture2D selectedIcon;
        CardView inspectingCard;
        bool pickComplete;
        Coroutine confirmRoutine;
        Coroutine inspectRoutine;
        Transform uiRoot;
        Camera tableCamera;

        public CardView CardPrefab => cardPrefab;
        public Texture2D SelectedIcon => selectedIcon;
        public bool HasSelected => selectedIcon != null;
        public bool IsInspecting => inspectingCard != null || choiceModal.IsVisible;
        public bool IsPickActive => pickCards.Count > 0 && !pickComplete;

        public void InitPickUi(Transform canvasRoot, Camera camera)
        {
            uiRoot = canvasRoot;
            tableCamera = camera;
            choiceModal.EnsureBuilt(canvasRoot, camera);
        }

        public void TickModal()
        {
            choiceModal.Tick();
        }

        public IEnumerator RunPickRoutine()
        {
            ClearPickCards();
            selectedIcon = null;
            pickComplete = false;
            inspectingCard = null;
            choiceModal.Hide();

            if (cardPrefab == null)
            {
                Debug.LogWarning("LegendaryPickView: cardPrefab is not assigned.");
                yield break;
            }

            var icons = CardCatalog.LoadLegendaryIconChoices(pickChoiceCount);
            if (icons.Count == 0)
            {
                Debug.LogWarning("LegendaryPickView: No legendary icons found (names must start with \"Legendary Icon\").");
                yield break;
            }

            for (var i = 0; i < icons.Count; i++)
            {
                var position = GetPickPosition(i, icons.Count);
                var card = SpawnPickCard(icons[i], position);
                pickCards.Add(card);
            }

            while (!pickComplete)
                yield return null;

            choiceModal.Hide();

            if (confirmRoutine != null)
                yield return confirmRoutine;

            yield return new WaitForSeconds(0.05f);
        }

        public void HandlePickCardClicked(CardView card)
        {
            if (pickComplete || !pickCards.Contains(card))
                return;

            if (inspectingCard != null && inspectingCard != card)
            {
                choiceModal.Hide();
                inspectingCard = null;
                if (inspectRoutine != null)
                {
                    StopCoroutine(inspectRoutine);
                    inspectRoutine = null;
                }
            }

            BeginInspect(card);
        }

        public void DeclineInspect()
        {
            if (pickComplete || inspectingCard == null)
                return;

            choiceModal.Hide();
            inspectingCard = null;
            StartCoroutine(RestorePickLayoutRoutine());
        }

        void BeginInspect(CardView card)
        {
            inspectingCard = card;
            choiceModal.Show(
                card,
                card.FrontTexture != null ? card.FrontTexture.name : "Legendary Icon",
                AcceptInspect,
                DeclineInspect);

            if (inspectRoutine != null)
                StopCoroutine(inspectRoutine);

            inspectRoutine = StartCoroutine(InspectCardRoutine(card));
        }

        void AcceptInspect()
        {
            if (pickComplete || inspectingCard == null)
                return;

            var chosen = inspectingCard;
            inspectingCard = null;
            selectedIcon = chosen.FrontTexture;
            pickComplete = true;
            confirmRoutine = StartCoroutine(ConfirmPickRoutine(chosen));
        }

        IEnumerator InspectCardRoutine(CardView card)
        {
            card.transform.SetAsLastSibling();

            var animating = 0;
            for (var i = 0; i < pickCards.Count; i++)
            {
                var pickCard = pickCards[i];
                if (pickCard == null)
                    continue;

                animating++;
                if (pickCard == card)
                {
                    StartCoroutine(AnimateCardTo(
                        pickCard,
                        inspectPosition,
                        inspectScale,
                        inspectAnimDuration,
                        () => animating--));
                }
                else
                {
                    var peerPosition = pickCard.transform.position + new Vector3(0f, -0.01f, inspectPeerZOffset);
                    StartCoroutine(AnimateCardTo(
                        pickCard,
                        peerPosition,
                        pickCardScale * inspectPeerScaleMultiplier,
                        inspectAnimDuration,
                        () => animating--));
                }
            }

            while (animating > 0)
                yield return null;

            inspectRoutine = null;
        }

        IEnumerator RestorePickLayoutRoutine()
        {
            var animating = 0;
            for (var i = 0; i < pickCards.Count; i++)
            {
                var pickCard = pickCards[i];
                if (pickCard == null)
                    continue;

                var position = GetPickPosition(i, pickCards.Count);
                animating++;
                StartCoroutine(AnimateCardTo(
                    pickCard,
                    position,
                    pickCardScale,
                    inspectAnimDuration,
                    () => animating--));
            }

            while (animating > 0)
                yield return null;
        }

        public IReadOnlyList<CardView> TakeUnpickedCards()
        {
            var result = new List<CardView>(unpickedCards);
            unpickedCards.Clear();
            return result;
        }

        IEnumerator ConfirmPickRoutine(CardView chosen)
        {
            chosen.transform.SetAsLastSibling();
            yield return AnimateCardTo(chosen, chosen.transform.position, selectedScale, pickAnimDuration);

            unpickedCards.Clear();
            for (var i = pickCards.Count - 1; i >= 0; i--)
            {
                var card = pickCards[i];
                if (card == null || card == chosen)
                    continue;

                var handler = card.GetComponent<LegendaryPickCardHandler>();
                if (handler != null)
                    Destroy(handler);

                card.SetClickable(false);
                unpickedCards.Add(card);
            }

            pickCards.Clear();
            pickCards.Add(chosen);
        }

        CardView SpawnPickCard(Texture2D front, Vector3 position)
        {
            var card = Instantiate(cardPrefab, position, CardView.TableRotation, transform);
            card.SetFrontTexture(front);
            card.SetFaceUpImmediate(true);
            card.SetCardScale(pickCardScale);
            card.SetClickable(true);

            var handler = card.GetComponent<LegendaryPickCardHandler>();
            if (handler == null)
                handler = card.gameObject.AddComponent<LegendaryPickCardHandler>();
            handler.Init(this);

            return card;
        }

        Vector3 GetPickPosition(int index, int count)
        {
            var startX = pickCenter.x - ((count - 1) * pickSpacing * 0.5f);
            return new Vector3(startX + (index * pickSpacing), pickCenter.y, pickCenter.z);
        }

        static IEnumerator AnimateCardTo(CardView card, Vector3 position, float scale, float duration, System.Action onComplete = null)
        {
            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(position, scale, duration, CardView.TableRotation);
            else
            {
                card.transform.position = position;
                card.SetCardScale(scale);
            }

            onComplete?.Invoke();
        }

        public void ClearPickCards()
        {
            if (confirmRoutine != null)
            {
                StopCoroutine(confirmRoutine);
                confirmRoutine = null;
            }

            if (inspectRoutine != null)
            {
                StopCoroutine(inspectRoutine);
                inspectRoutine = null;
            }

            choiceModal.Hide();
            inspectingCard = null;

            for (var i = unpickedCards.Count - 1; i >= 0; i--)
            {
                if (unpickedCards[i] != null)
                    Destroy(unpickedCards[i].gameObject);
            }

            unpickedCards.Clear();

            for (var i = pickCards.Count - 1; i >= 0; i--)
            {
                if (pickCards[i] != null)
                    Destroy(pickCards[i].gameObject);
            }

            pickCards.Clear();
            selectedIcon = null;
            pickComplete = false;
        }

        public Vector3 GetSelectedCardPosition()
        {
            return pickCards.Count > 0 && pickCards[0] != null
                ? pickCards[0].transform.position
                : pickCenter;
        }
    }
}
