using System.Collections;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using Willbound.Engine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Icon zone — bottom-left of the playmat, left of the kept hand.
    /// </summary>
    [DisallowMultipleComponent]
    public class IconSlotView : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] CardView cardPrefab;

        [Header("Slot")]
        [SerializeField] Vector3 slotPosition = PlaymatZones.Icon;
        [SerializeField] float iconScale = PlaymatZones.IconScale;
        [SerializeField] float placeAnimDuration = 0.4f;

        [Header("Inspect")]
        [SerializeField] Vector3 inspectPosition = new(-4f, 0.1f, 1.5f);
        [SerializeField] float inspectScale = PlaymatZones.IconScale;
        [SerializeField] float inspectAnimDuration = 0.25f;

        CardView iconCard;
        bool inspecting;

        public CardView IconCard => iconCard;
        public Texture2D SelectedIcon => iconCard != null ? iconCard.FrontTexture : null;
        public bool HasInspectSelection => inspecting && iconCard != null;

        public IEnumerator PlaceIconRoutine(Texture2D iconFront, Vector3 fromPosition)
        {
            ClearIcon();

            if (cardPrefab == null || iconFront == null)
                yield break;

            iconCard = Instantiate(cardPrefab, fromPosition, CardView.TableRotation, transform);
            iconCard.SetFrontTexture(iconFront);
            iconCard.SetFaceUpImmediate(true);
            iconCard.SetCardScale(iconScale);
            iconCard.SetClickable(true);

            var handler = iconCard.GetComponent<IconSlotClickHandler>();
            if (handler == null)
                handler = iconCard.gameObject.AddComponent<IconSlotClickHandler>();
            handler.Init(this);

            var animator = iconCard.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(slotPosition, iconScale, placeAnimDuration, CardView.TableRotation);
            else
            {
                iconCard.transform.position = slotPosition;
                iconCard.SetCardScale(iconScale);
            }
        }

        public void HandleIconClicked(CardView card)
        {
            if (iconCard == null || card != iconCard)
                return;

            if (IsClashDeclare())
                return;

            if (inspecting)
            {
                ClearInspectSelection();
                return;
            }

            inspecting = true;
            iconCard.transform.SetAsLastSibling();
            StartCoroutine(InspectIconRoutine());
        }

        public void ClearInspectSelection()
        {
            if (!inspecting || iconCard == null)
                return;

            inspecting = false;
            StartCoroutine(RestoreIconRoutine());
        }

        IEnumerator InspectIconRoutine()
        {
            var animator = iconCard.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(inspectPosition, inspectScale, inspectAnimDuration, CardView.TableRotation);
            else
            {
                iconCard.transform.position = inspectPosition;
                iconCard.SetCardScale(inspectScale);
            }
        }

        IEnumerator RestoreIconRoutine()
        {
            var animator = iconCard.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(slotPosition, iconScale, inspectAnimDuration, CardView.TableRotation);
            else
            {
                iconCard.transform.position = slotPosition;
                iconCard.SetCardScale(iconScale);
            }
        }

        public void ClearIcon()
        {
            inspecting = false;

            if (iconCard != null)
                Destroy(iconCard.gameObject);

            iconCard = null;
        }

        static bool IsClashDeclare()
        {
            var bridge = FindAnyObjectByType<TableMatchBridge>();
            if (bridge == null || !bridge.IsActive)
                return false;
            return bridge.Runner.Match.Phase == Phase.Clash
                && bridge.Runner.Match.ClashPhase == ClashPhase.C1_ActiveDeclare;
        }
    }
}
