using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardView))]
    public class CardBackPickCardHandler : MonoBehaviour
    {
        CardBackPickView pickView;
        CardView cardView;

        public void Init(CardBackPickView view)
        {
            pickView = view;
        }

        void Awake()
        {
            cardView = GetComponent<CardView>();
        }

        void OnMouseEnter()
        {
            if (pickView != null && cardView != null)
                CardHoverAudio.PlayHoverFlip();
        }

        void OnMouseDown()
        {
            if (pickView != null && cardView != null)
                pickView.HandlePickCardClicked(cardView);
        }
    }
}
