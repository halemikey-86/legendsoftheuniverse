using UnityEngine;

namespace Willbound.Table
{
    /// <summary>Click a Field body during Clash declare to Press the opposing Icon.</summary>
    [DisallowMultipleComponent]
    public sealed class ClashPressClickHandler : MonoBehaviour
    {
        TableRoot tableRoot;
        CardView tableCard;

        public void Init(TableRoot root)
        {
            tableRoot = root;
            tableCard = GetComponent<CardView>();
        }

        void Awake()
        {
            if (tableCard == null)
                tableCard = GetComponent<CardView>();
        }

        void OnMouseDown()
        {
            if (tableRoot == null)
                tableRoot = FindAnyObjectByType<TableRoot>();
            if (tableRoot == null || tableCard == null || tableCard.InstanceId <= 0)
                return;

            tableRoot.HandleClashCardClicked(tableCard.InstanceId);
        }
    }
}
