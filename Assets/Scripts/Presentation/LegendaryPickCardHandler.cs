using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardView))]
    public class LegendaryPickCardHandler : MonoBehaviour
    {
        LegendaryPickView pickView;
        CardView cardView;

        public void Init(LegendaryPickView view)
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
            {
                CardHoverAudio.PlayHoverFlip();
                HoverTooltipView.ShowCard(cardView);
            }
        }

        void OnMouseExit()
        {
            HoverTooltipView.Hide();
        }

        void OnMouseDown()
        {
            if (pickView != null && cardView != null)
                pickView.HandlePickCardClicked(cardView);
        }
    }
}
