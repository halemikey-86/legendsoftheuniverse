using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public enum DialKind
    {
        Will,
        Worth,
        Honor,
        Round,
    }

    /// <summary>
    /// Screen-anchored HUD counters for Will, Worth, Honor, and Round.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialView : MonoBehaviour
    {
        [SerializeField] DialKind dialKind = DialKind.Worth;
        [SerializeField] Vector2 screenAnchor = new(1f, 1f);
        [SerializeField] Vector2 screenOffset = Vector2.zero;
        [SerializeField] Vector2 panelSize = new(128f, 78f);

        WorldAnchoredUi labelUi;
        int currentValue = 3;

        public DialKind Kind => dialKind;
        public int Value => currentValue;

        /// <summary>Builds the dial's UI. Must run after construction (not in Awake) so the correct
        /// <paramref name="kind"/> is known before the title/subtitle text is ever created.</summary>
        public void Configure(DialKind kind, Vector2 anchor, Vector2 offset)
        {
            dialKind = kind;
            screenAnchor = anchor;
            screenOffset = offset;
            BuildDial();
        }

        void BuildDial()
        {
            EnsureUi();
            ApplyDefaults();
            RefreshDisplay();
        }

        void EnsureUi()
        {
            if (labelUi != null)
                return;

            labelUi = WorldAnchoredUi.CreateLabeledScreenAnchored(
                GetDialTitle(dialKind),
                GetDialSubtitle(dialKind),
                screenAnchor,
                screenOffset,
                panelSize);
        }

        static string GetDialTitle(DialKind kind)
        {
            return kind switch
            {
                DialKind.Will => "Will",
                DialKind.Worth => "Worth",
                DialKind.Honor => "Honor",
                DialKind.Round => "Round",
                _ => "Tracker",
            };
        }

        static string GetDialSubtitle(DialKind kind)
        {
            return kind switch
            {
                DialKind.Will => "Spend to play cards",
                DialKind.Worth => "Buy from the store",
                DialKind.Honor => "Prestige",
                DialKind.Round => "Turn tracker",
                _ => string.Empty,
            };
        }

        void ApplyDefaults()
        {
            switch (dialKind)
            {
                case DialKind.Will:
                    currentValue = 1;
                    break;
                case DialKind.Round:
                    currentValue = 1;
                    break;
                case DialKind.Worth:
                    currentValue = Rules.GameConstants.StartingWorth;
                    break;
                case DialKind.Honor:
                    currentValue = Rules.GameConstants.StartingHonor;
                    break;
            }
        }

        public void SetValue(int value)
        {
            currentValue = value;
            RefreshDisplay();
        }

        public void SetRound(int round)
        {
            if (dialKind != DialKind.Round)
                return;

            currentValue = Mathf.Clamp(round, 1, PlaymatZones.MaxRound);
            RefreshDisplay();
        }

        public void SetWillPool(int will)
        {
            if (dialKind != DialKind.Will)
                return;

            currentValue = Mathf.Clamp(will, 0, Rules.GameConstants.MaxRoundWill);
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (labelUi == null)
                return;

            labelUi.Subtitle = GetDialSubtitle(dialKind);
            labelUi.Value = currentValue.ToString();
        }
    }
}
