using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public enum DialKind
    {
        Round,
        Will,
        Worth,
        Honor,
        WillRound,
    }

    /// <summary>
    /// Playmat tracker overlays. Official mat art includes labels — only numbers are shown.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialView : MonoBehaviour
    {
        [SerializeField] DialKind dialKind = DialKind.Worth;
        [SerializeField] Vector3 labelWorldOffset = Vector3.zero;
        [SerializeField] Vector2 panelSize = new(56f, 36f);
        [SerializeField] int valueFontSize = 26;

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

            labelUi = WorldAnchoredUi.CreateValueOnly(
                transform,
                labelWorldOffset,
                panelSize,
                valueFontSize);
        }

        void ApplyDefaults()
        {
            switch (dialKind)
            {
                case DialKind.Round:
                case DialKind.WillRound:
                    currentRound = 1;
                    currentValue = 1;
                    break;
                case DialKind.Will:
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

        public void ApplyOverlaySettings()
        {
            var layout = TableLayoutSettings.Active;
            labelWorldOffset = dialKind switch
            {
                DialKind.Round => new Vector3(layout.roundNumberOffsetX, layout.roundNumberOffsetY, 0f),
                DialKind.Worth => new Vector3(layout.worthNumberOffsetX, layout.worthNumberOffsetY, 0f),
                DialKind.Will or DialKind.WillRound => new Vector3(layout.willNumberOffsetX, layout.willNumberOffsetY, 0f),
                DialKind.Honor => new Vector3(layout.honorNumberOffsetX, layout.honorNumberOffsetY, 0f),
                _ => labelWorldOffset,
            };

            labelUi?.SetWorldOffset(labelWorldOffset);
        }

        public void PushOffsetToLayout(TableLayoutData layout)
        {
            if (layout == null)
                return;

            var offset = labelUi != null ? labelUi.WorldOffset : labelWorldOffset;
            switch (dialKind)
            {
                case DialKind.Round:
                    layout.roundNumberOffsetX = offset.x;
                    layout.roundNumberOffsetY = offset.y;
                    break;
                case DialKind.Worth:
                    layout.worthNumberOffsetX = offset.x;
                    layout.worthNumberOffsetY = offset.y;
                    break;
                case DialKind.Will:
                case DialKind.WillRound:
                    layout.willNumberOffsetX = offset.x;
                    layout.willNumberOffsetY = offset.y;
                    break;
                case DialKind.Honor:
                    layout.honorNumberOffsetX = offset.x;
                    layout.honorNumberOffsetY = offset.y;
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
            if (dialKind == DialKind.Round || dialKind == DialKind.WillRound)
                RefreshDisplay();
        }

        public void SetWillPool(int will)
        {
            if (dialKind != DialKind.Will && dialKind != DialKind.WillRound)
                return;

            currentValue = Mathf.Clamp(will, 0, Rules.GameConstants.MaxRoundWill);
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (labelUi == null)
                return;

            labelUi.Value = dialKind switch
            {
                DialKind.Round => currentRound.ToString(),
                DialKind.Will => currentValue.ToString(),
                DialKind.WillRound => currentValue.ToString(),
                _ => currentValue.ToString(),
            };
        }
    }
}
