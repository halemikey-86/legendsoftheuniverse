using System.Collections;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public class CardDealDemo : MonoBehaviour
    {
        [SerializeField] CardView cardPrefab;
        [SerializeField] Transform deckPoint;
        [SerializeField] Transform handPoint;
        [SerializeField] float dealDuration = 0.55f;
        [SerializeField] float flipDelay = 0.35f;
        [SerializeField] float flipDuration = 0.4f;
        [SerializeField] bool playOnStart = false;

        void Start()
        {
            if (playOnStart)
                PlayRandomCardDemo();
        }

        [ContextMenu("Play Random Card Demo")]
        public void PlayRandomCardDemo()
        {
            StartCoroutine(PlayRandomCardDemoRoutine());
        }

        IEnumerator PlayRandomCardDemoRoutine()
        {
            if (cardPrefab == null || deckPoint == null || handPoint == null)
                yield break;

            var fronts = CardCatalog.LoadAllCardFronts();
            if (fronts.Count == 0)
                yield break;

            var card = Instantiate(cardPrefab, deckPoint.position, CardView.TableRotation, transform);
            var view = card.GetComponent<CardView>();
            var animator = card.GetComponent<CardAnimator>();

            view.SetFrontTexture(fronts[Random.Range(0, fronts.Count)]);
            view.SetFaceUpImmediate(false);

            yield return animator.DealFaceDownRoutine(deckPoint.position, handPoint.position, dealDuration);
            yield return new WaitForSeconds(flipDelay);
            yield return animator.FlipFaceUpRoutine(flipDuration);
        }
    }
}
