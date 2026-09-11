using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Sprites from Assets/UI Assets, loaded at runtime via Resources.
    /// </summary>
    public static class PlaymatUiSprites
    {
        static Sprite LoadIcon(string name) => Resources.Load<Sprite>($"UI/Icons/{name}");
        static Sprite LoadLabel(string name) => Resources.Load<Sprite>($"UI/Labels/{name}");

        public static Sprite Buy => LoadIcon("Buy");
        public static Sprite Sell => LoadIcon("Sell");
        public static Sprite Trade => LoadIcon("Trade");
        public static Sprite HideDeck => LoadIcon("HideDeck");

        public static Sprite LabelWill => LoadLabel("Will");
        public static Sprite LabelRound => LoadLabel("Round");
        public static Sprite LabelWorth => LoadLabel("Worth");
        public static Sprite LabelHonor => LoadLabel("Honor");
        public static Sprite LabelBanished => LoadLabel("Banished");
        public static Sprite LabelSettings => LoadLabel("Settings");
        public static Sprite LabelTimer => LoadLabel("Timer");
        public static Sprite LabelNextPhase => LoadIcon("NextPhase") ?? LoadLabel("NextPhase");
        public static Sprite LabelEndTurn => LoadIcon("EndTurn") ?? LoadLabel("EndTurn");

        public static Sprite GetZoneLabel(string zoneLabel)
        {
            return zoneLabel switch
            {
                "Banished" => LabelBanished,
                _ => null,
            };
        }

        public static Sprite GetDialLabel(DialKind kind)
        {
            return kind switch
            {
                DialKind.WillRound => LabelWill,
                DialKind.Worth => LabelWorth,
                DialKind.Honor => LabelHonor,
                _ => null,
            };
        }
    }
}
