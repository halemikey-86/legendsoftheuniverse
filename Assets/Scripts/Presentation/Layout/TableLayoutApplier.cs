using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Pushes <see cref="TableLayoutSettings"/> into live table components.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-310)]
    public sealed class TableLayoutApplier : MonoBehaviour
    {
        [SerializeField] TableView tableView;
        [SerializeField] HandView handView;
        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] TableDropZonesView dropZones;
        [SerializeField] SupplyDeckView supplyDeck;
        [SerializeField] IconSlotView iconSlot;

        void Awake()
        {
            if (tableView == null)
                tableView = GetComponent<TableView>();
            if (handView == null)
                handView = GetComponent<HandView>();
            if (playmatZones == null)
                playmatZones = GetComponent<PlaymatZonesView>();
            if (dropZones == null)
                dropZones = GetComponent<TableDropZonesView>();
            if (supplyDeck == null)
                supplyDeck = GetComponentInChildren<SupplyDeckView>();
            if (iconSlot == null)
                iconSlot = GetComponent<IconSlotView>();

            TableLayoutSettings.EnsureLoaded();
            TableLayoutSettings.LayoutChanged += OnLayoutChanged;
        }

        void Start()
        {
            ApplyLayoutLive();
            ApplySavedSceneSnapshots();
        }

        void OnDestroy()
        {
            TableLayoutSettings.LayoutChanged -= OnLayoutChanged;
        }

        void OnLayoutChanged() => ApplyLayoutLive();

        public void ApplyAll()
        {
            ApplyLayoutLive();
            ApplySavedSceneSnapshots();
        }

        /// <summary>Sliders and layout fields — updates live table components.</summary>
        public void ApplyLayoutLive()
        {
            var data = TableLayoutSettings.Active;
            tableView?.ApplyLayoutSettings();
            handView?.ApplyLayoutSettings();
            playmatZones?.ApplyLayoutSettings();
            if (!TableLayoutSceneCapture.HasSavedFieldZoneLayout(data))
                dropZones?.RebuildFromLayout();
            supplyDeck?.ApplyLayoutSettings();
            iconSlot?.ApplyLayoutSettings();
        }

        /// <summary>Restore saved per-object transforms captured from scene editing.</summary>
        public void ApplySavedSceneSnapshots()
        {
            TableLayoutSceneCapture.ApplyScene(transform, TableLayoutSettings.Active);
        }

    }
}
