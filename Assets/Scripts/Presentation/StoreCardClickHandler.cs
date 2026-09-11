using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardView))]
    public class StoreCardClickHandler : MonoBehaviour
    {
        StoreView storeView;
        CardView cardView;

        public void Init(StoreView store)
        {
            storeView = store;
        }

        void Awake()
        {
            cardView = GetComponent<CardView>();
        }

        void OnMouseEnter()
        {
            if (storeView != null && cardView != null)
                storeView.HandleCardHoverEnter(cardView);
        }

        void OnMouseExit()
        {
            if (storeView != null && cardView != null)
                storeView.HandleCardHoverExit(cardView);
        }

        void OnMouseDown()
        {
            if (storeView != null && cardView != null && storeView.CanInteractCards)
                storeView.HandleStoreCardClicked(cardView);
        }
    }
}
