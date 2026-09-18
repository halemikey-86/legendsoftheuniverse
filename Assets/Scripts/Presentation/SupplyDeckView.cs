using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Supply count overlay — number only (label is on the official playmat art).
    /// </summary>
    [DisallowMultipleComponent]
    public class SupplyDeckView : MonoBehaviour
    {
        [Header("Counter")]
        [SerializeField] Vector3 countWorldOffset = new(2.8f, 0.22f, 0f);
        [SerializeField] Vector2 countScreenSize = new(72f, 40f);
        [SerializeField] int countFontSize = 30;

        WorldAnchoredUi counterUi;

        void OnEnable()
        {
            CardDeck.CountChanged += OnDeckCountChanged;
        }

        void Start()
        {
            EnsureCounter();
            Refresh();
        }

        void OnDisable()
        {
            CardDeck.CountChanged -= OnDeckCountChanged;
        }

        void OnDeckCountChanged(int count)
        {
            UpdateCountLabel(count);
        }

        public void SetStackVisible(bool visible)
        {
            // Card stack hidden — official mat shows the supply zone art only.
        }

        public void Refresh()
        {
            EnsureCounter();
            UpdateCountLabel(CardDeck.Remaining);
        }

        public void ApplyLayoutSettings()
        {
            countWorldOffset = PlaymatZones.SupplyNumberOffset;
            counterUi?.SetWorldOffset(countWorldOffset);
        }

        public Vector3 GetCountWorldOffset() =>
            counterUi != null ? counterUi.WorldOffset : countWorldOffset;

        void EnsureCounter()
        {
            if (counterUi != null)
                return;

            counterUi = WorldAnchoredUi.CreateValueOnly(
                transform,
                countWorldOffset,
                countScreenSize,
                countFontSize);
        }

        void UpdateCountLabel(int remaining)
        {
            if (counterUi == null)
                return;

            counterUi.Value = remaining.ToString();
        }
    }
}
