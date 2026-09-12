using UnityEngine;
using UnityEngine.EventSystems;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// UI hover bubble for buttons and other EventSystem targets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] string tooltipText;

        public static void Attach(GameObject target, string text)
        {
            if (target == null || string.IsNullOrWhiteSpace(text))
                return;

            var trigger = target.GetComponent<HoverTooltipTrigger>();
            if (trigger == null)
                trigger = target.AddComponent<HoverTooltipTrigger>();

            trigger.tooltipText = text;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(tooltipText))
                HoverTooltipView.Show(tooltipText, this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverTooltipView.Hide(this);
        }

        void OnDisable()
        {
            HoverTooltipView.Hide(this);
        }
    }
}
