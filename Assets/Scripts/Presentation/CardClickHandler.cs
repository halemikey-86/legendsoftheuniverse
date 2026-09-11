using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardView))]
    public class CardClickHandler : MonoBehaviour
    {
        HandView handView;
        CardView cardView;

        public void Init(HandView hand)
        {
            handView = hand;
        }

        void Awake()
        {
            cardView = GetComponent<CardView>();
        }

        void OnMouseEnter()
        {
            if (handView != null && cardView != null)
                handView.HandleCardHoverEnter(cardView);
        }

        void OnMouseExit()
        {
            if (handView != null && cardView != null)
                handView.HandleCardHoverExit(cardView);
        }

        void OnMouseDown()
        {
            if (handView != null && cardView != null)
                handView.HandleCardPointerDown(cardView);
        }

        void OnMouseDrag()
        {
            if (handView != null && cardView != null)
                handView.HandleCardDrag(cardView);
        }

        void OnMouseUp()
        {
            if (handView != null && cardView != null)
                handView.HandleCardPointerUp(cardView);
        }
    }
}
