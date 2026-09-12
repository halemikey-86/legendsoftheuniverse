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
                    new Vector3(0f, 0.45f, -1.4f),
                    new Vector2(176f, 96f),
                    28);
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
            ui.Subtitle = clashTaken > 0 ? $"This Clash −{clashTaken}" : "Health";
        }

        void OnDestroy()
        {
            if (ui != null)
                Destroy(ui.gameObject);
        }
    }
}
