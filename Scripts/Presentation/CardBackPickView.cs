using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Match start: present card back choices before any cards are dealt.
    /// </summary>
    [DisallowMultipleComponent]
    public class CardBackPickView : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] CardView cardPrefab;

        [Header("Layout")]
        [SerializeField] Vector3 pickCenter = new(0f, 0.05f, 0f);
        [SerializeField] float pickSpacing = 6f;
        [SerializeField] float pickCardScale = PlaymatZones.CardScale;
        [SerializeField] float selectedScale = PlaymatZones.CardScale;
        [SerializeField] float pickAnimDuration = 0.35f;

        [Header("Inspect")]
        [SerializeField] Vector3 inspectPosition = new(0f, 0.1f, -0.5f);
        [SerializeField] float inspectScale = PlaymatZones.CardScale * 1.15f;
        [SerializeField] float inspectPeerScaleMultiplier = 0.72f;
        [SerializeField] float inspectPeerZOffset = 0.75f;
        [SerializeField] float inspectAnimDuration = 0.25f;

        readonly List<CardView> pickCards = new();
        readonly CardChoiceModal choiceModal = new();

        Texture2D selectedBack;
        CardView inspectingCard;
        bool pickComplete;
        Coroutine confirmRoutine;
        Coroutine inspectRoutine;

        public Texture2D SelectedBack => selectedBack;
        public bool HasSelected => selectedBack != null;
        public bool IsInspecting => inspectingCard != null || choiceModal.IsVisible;
        public bool IsPickActive => pickCards.Count > 0 && !pickComplete;

        public void InitPickUi(Transform canvasRoot, Camera camera, CardView fallbackPrefab = null)
        {
            if (cardPrefab == null)
                cardPrefab = fallbackPrefab;

            choiceModal.EnsureBuilt(canvasRoot, camera);
        }

        public void Tick()
        {
            choiceModal.Tick();
        }

        public IEnumerator RunPickRoutine()
        {
            ClearPickCards();
            selectedBack = null;
            pickComplete = false;
            inspectingCard = null;
            choiceModal.Hide();

            EnsureCardPrefab();
            if (cardPrefab == null)
            {
                Debug.LogWarning("CardBackPickView: cardPrefab is not assigned.");
                yield break;
            }

            var backs = CardCatalog.LoadCardBacks();
            if (backs.Count == 0)
            {
                Debug.LogWarning("CardBackPickView: No card backs found in Assets/Cards/CardBacks.");
                yield break;
            }

            for (var i = 0; i < backs.Count; i++)
            {
                var position = GetPickPosition(i, backs.Count);
                var card = SpawnPickCard(backs[i], position);
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
                CardCatalog.FormatCardBackName(card.BackTexture != null ? card.BackTexture.name : "Card Back"),
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
            selectedBack = chosen.BackTexture;
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

        IEnumerator ConfirmPickRoutine(CardView chosen)
        {
            chosen.transform.SetAsLastSibling();
            yield return AnimateCardTo(chosen, chosen.transform.position, selectedScale, pickAnimDuration);

            for (var i = pickCards.Count - 1; i >= 0; i--)
            {
                var card = pickCards[i];
                if (card == null || card == chosen)
                    continue;

                Destroy(card.gameObject);
            }

            pickCards.Clear();
            pickCards.Add(chosen);
        }

        CardView SpawnPickCard(Texture2D back, Vector3 position)
        {
            var card = Instantiate(cardPrefab, position, CardView.TableRotation, transform);
            card.SetBackTexture(back);
            card.SetFaceUpImmediate(false);
            card.SetCardScale(pickCardScale);
            card.SetClickable(true);

            var handler = card.GetComponent<CardBackPickCardHandler>();
            if (handler == null)
                handler = card.gameObject.AddComponent<CardBackPickCardHandler>();
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

            for (var i = pickCards.Count - 1; i >= 0; i--)
            {
                if (pickCards[i] != null)
                    Destroy(pickCards[i].gameObject);
            }

            pickCards.Clear();
            selectedBack = null;
            pickComplete = false;
        }

        void EnsureCardPrefab()
        {
            if (cardPrefab != null)
                return;

            var legendaryPick = GetComponent<LegendaryPickView>();
            if (legendaryPick != null)
                cardPrefab = legendaryPick.CardPrefab;
        }
    }
}
