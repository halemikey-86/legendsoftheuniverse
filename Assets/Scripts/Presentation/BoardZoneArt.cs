using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Loads board zone artwork from Assets/UI Assets/BoardZones/.
    /// </summary>
    public static class BoardZoneArt
    {
        public const string Folder = "Assets/UI Assets/BoardZones/";
        public const string OfficialPlaymatFile = "OfficialPlaymat.png";
        public const string WillboundMatFile = "Willbound Mat.png";
        public const string StoreFile = "Store.png";
        public const string DeckFile = "Deck.png";
        public const string SupplyFile = "Supply.png";
        public const string IconZoneFile = "Icon Zone.png";

        public static Texture2D LoadOfficialPlaymat() => Load(OfficialPlaymatFile);
        public static Texture2D LoadWillboundMat() => Load(WillboundMatFile);
        public static Texture2D LoadStore() => Load(StoreFile);
        public static Texture2D LoadDeck() => Load(DeckFile);
        public static Texture2D LoadSupply() => Load(SupplyFile);
        public static Texture2D LoadIconZone() => Load(IconZoneFile);

        public static float GetAspect(Texture2D texture)
        {
            if (texture == null || texture.height <= 0)
                return 1f;

            return texture.width / (float)texture.height;
        }

        static Texture2D Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + fileName);
#else
            return Resources.Load<Texture2D>("BoardZones/" + System.IO.Path.GetFileNameWithoutExtension(fileName));
#endif
        }
    }
}
