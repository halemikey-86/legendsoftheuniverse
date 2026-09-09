using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardView))]
    public class IconSlotClickHandler : MonoBehaviour
    {
        IconSlotView iconSlotView;
        CardView cardView;

        public void Init(IconSlotView view)
        {
            iconSlotView = view;
        }

        void Awake()
        {
            cardView = GetComponent<CardView>();
        }

        void OnMouseEnter()
        {
            if (iconSlotView != null && cardView != null)
                CardHoverAudio.PlayHoverFlip();
        }

        void OnMouseDown()
        {
            if (iconSlotView != null && cardView != null)
                iconSlotView.HandleIconClicked(cardView);
        }
    }
}
