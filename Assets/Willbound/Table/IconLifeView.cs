using LegendsOfTheUniverse.Presentation;
using UnityEngine;

namespace Willbound.Table
{
    /// <summary>HP chip that follows an Icon, plus damage taken this Clash.</summary>
    [DisallowMultipleComponent]
    public sealed class IconLifeView : MonoBehaviour
    {
        WorldAnchoredUi ui;
        int current = 1;
        int max = 1;
        int clashTaken;

        public void Configure(Transform anchor, string title)
        {
            if (ui == null)
            {
                // Just the "current/max" number, centered below the Icon card (not overlapping its
                // art). +Z is up-screen under the table's top-down camera, so a negative Z offset
                // past the card's own half-depth (~1.9 for CardScale 3.8) clears it downward.
                ui = WorldAnchoredUi.CreateValueOnly(
                    anchor,
                    new Vector3(0f, 0.55f, -3.2f),
                    new Vector2(160f, 56f),
                    26);
            }
            else
            {
                ui.WorldAnchor = anchor;
            }

            Refresh();
        }

        public void SetHealth(int currentHp, int maxHp)
        {
            current = Mathf.Max(0, currentHp);
            max = Mathf.Max(1, maxHp);
            Refresh();
        }

        public void SetClashTaken(int amount)
        {
            clashTaken = Mathf.Max(0, amount);
            Refresh();
        }

        public void ClearClash()
        {
            clashTaken = 0;
            Refresh();
        }

        void Refresh()
        {
            if (ui == null)
                return;

            ui.Value = $"{current}/{max}";
        }

        void OnDestroy()
        {
            if (ui != null)
                Destroy(ui.gameObject);
        }
    }
}
