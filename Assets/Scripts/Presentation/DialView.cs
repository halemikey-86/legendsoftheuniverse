using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public enum DialKind
    {
        WillRound,
        Worth,
        Honor,
    }

    /// <summary>
    /// Playmat trackers with readable titles for Will, Worth, and Honor.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialView : MonoBehaviour
    {
        [SerializeField] DialKind dialKind = DialKind.Worth;
        [SerializeField] Vector3 labelWorldOffset = new(0f, 0.22f, 0f);
        [SerializeField] Vector2 panelSize = new(128f, 78f);

        WorldAnchoredUi labelUi;
        int currentValue = 3;
        int currentRound = 1;

        public DialKind Kind => dialKind;
        public int Value => currentValue;
        public int Round => currentRound;

        public void Configure(DialKind kind, Vector3 worldPosition)
        {
            dialKind = kind;
            transform.position = worldPosition;
            BuildDial();
        }

        void Awake()
        {
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

            labelUi = WorldAnchoredUi.CreateLabeled(
                transform,
                GetDialTitle(dialKind),
                GetDialSubtitle(dialKind),
                labelWorldOffset,
                panelSize);
        }

        static string GetDialTitle(DialKind kind)
        {
            return kind switch
            {
                DialKind.WillRound => "Will",
                DialKind.Worth => "Worth",
                DialKind.Honor => "Honor",
                _ => "Tracker",
            };
        }

        static string GetDialSubtitle(DialKind kind)
        {
            return kind switch
            {
                DialKind.WillRound => "Spend to play cards",
                DialKind.Worth => "Buy from the store",
                DialKind.Honor => "Prestige",
                _ => string.Empty,
            };
        }

        void ApplyDefaults()
        {
            switch (dialKind)
            {
                case DialKind.WillRound:
                    currentRound = 1;
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
            currentRound = Mathf.Clamp(round, 1, PlaymatZones.MaxRound);
            RefreshDisplay();
        }

        public void SetWillPool(int will)
        {
            if (dialKind != DialKind.WillRound)
                return;

            currentValue = Mathf.Clamp(will, 0, Rules.GameConstants.MaxRoundWill);
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (labelUi == null)
                return;

            if (dialKind == DialKind.WillRound)
            {
                labelUi.Subtitle = $"Round {currentRound} · spend to play cards";
                labelUi.Value = currentValue.ToString();
                return;
            }

            labelUi.Subtitle = GetDialSubtitle(dialKind);
            labelUi.Value = currentValue.ToString();
        }
    }
}
