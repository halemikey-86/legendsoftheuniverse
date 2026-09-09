using UnityEngine;
using UnityEngine.UI;

namespace LegendsOfTheUniverse.Presentation
{
    public enum DialKind
    {
        WillRound,
        Worth,
        Honor,
    }

    /// <summary>
    /// Right-column trackers: Will/Round (1–8), Worth (0–20), Honor (0–10).
    /// </summary>
    [DisallowMultipleComponent]
    public class DialView : MonoBehaviour
    {
        static readonly Color ActiveRound = new(0.95f, 0.78f, 0.28f, 1f);
        static readonly Color IdleRound = new(0.35f, 0.28f, 0.48f, 0.9f);

        [SerializeField] DialKind dialKind = DialKind.Worth;
        [SerializeField] Vector3 labelWorldOffset = new(0f, 0f, -0.7f);
        [SerializeField] Vector2 panelSize = new(130f, 88f);

        WorldAnchoredUi labelUi;
        RectTransform dialPanel;
        Text valueText;
        Image[] roundDots;
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

            var title = dialKind switch
            {
                DialKind.WillRound => "WILL / ROUND",
                DialKind.Worth => "WORTH",
                DialKind.Honor => "HONOR",
                _ => "DIAL",
            };

            labelUi = WorldAnchoredUi.Create(transform, title, labelWorldOffset, panelSize, 13, dialKind != DialKind.WillRound);
            labelUi.Label = title;

            if (dialKind == DialKind.WillRound)
                BuildRoundTrack();
            else
                labelUi.Value = currentValue.ToString();
        }

        void BuildRoundTrack()
        {
            var canvasObject = new GameObject("WillRoundTrack");
            canvasObject.transform.SetParent(labelUi.transform, false);

            dialPanel = canvasObject.AddComponent<RectTransform>();
            dialPanel.anchorMin = new Vector2(0.06f, 0.08f);
            dialPanel.anchorMax = new Vector2(0.94f, 0.48f);
            dialPanel.offsetMin = Vector2.zero;
            dialPanel.offsetMax = Vector2.zero;

            var layout = canvasObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            roundDots = new Image[PlaymatZones.MaxRound];
            for (var i = 0; i < roundDots.Length; i++)
            {
                var dotObject = new GameObject($"Round_{i + 1}");
                dotObject.transform.SetParent(canvasObject.transform, false);

                var dotRect = dotObject.AddComponent<RectTransform>();
                dotRect.sizeDelta = new Vector2(24f, 24f);

                var dotImage = dotObject.AddComponent<Image>();
                dotImage.color = IdleRound;
                dotImage.raycastTarget = false;

                var numberObject = new GameObject("Number");
                numberObject.transform.SetParent(dotObject.transform, false);
                var numberText = numberObject.AddComponent<Text>();
                numberText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                numberText.fontSize = 11;
                numberText.fontStyle = FontStyle.Bold;
                numberText.alignment = TextAnchor.MiddleCenter;
                numberText.color = Color.white;
                numberText.raycastTarget = false;
                numberText.text = (i + 1).ToString();

                var numberRect = numberText.rectTransform;
                numberRect.anchorMin = Vector2.zero;
                numberRect.anchorMax = Vector2.one;
                numberRect.offsetMin = Vector2.zero;
                numberRect.offsetMax = Vector2.zero;

                roundDots[i] = dotImage;
            }
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
            currentValue = Mathf.Min(currentRound, PlaymatZones.MaxRound);
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (labelUi == null)
                return;

            if (dialKind == DialKind.WillRound)
            {
                if (roundDots == null)
                    return;

                for (var i = 0; i < roundDots.Length; i++)
                    roundDots[i].color = i < currentRound ? ActiveRound : IdleRound;

                labelUi.Value = $"Will {currentValue}";
                return;
            }

            labelUi.Value = currentValue.ToString();
        }
    }
}
