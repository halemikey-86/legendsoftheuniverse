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
                ui = WorldAnchoredUi.CreateLabeled(
                    anchor,
                    title,
                    "Health",
                    new Vector3(2.4f, 0.55f, 0f),
                    new Vector2(200f, 108f),
                    32);
            }
            else
            {
                ui.WorldAnchor = anchor;
                ui.Label = title;
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
            if (clashTaken > 0)
                ui.Subtitle = $"This Clash −{clashTaken}";
            else if (current < max)
                ui.Subtitle = $"Damaged (−{max - current})";
            else
                ui.Subtitle = "Health";
        }

        void OnDestroy()
        {
            if (ui != null)
                Destroy(ui.gameObject);
        }
    }
}
