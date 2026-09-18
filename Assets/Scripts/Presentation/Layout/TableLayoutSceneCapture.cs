using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public static class TableLayoutSceneCapture
    {
        public static void CaptureScene(Transform tableRoot, TableLayoutData data)
        {
            if (tableRoot == null || data == null)
                return;

            var records = new List<SceneTransformRecord>();
            CaptureRecursive(tableRoot, tableRoot, records);
            data.sceneTransforms = records.ToArray();
            SyncLayoutFieldsFromScene(tableRoot, data);
        }

        public static void ApplyScene(Transform tableRoot, TableLayoutData data)
        {
            if (tableRoot == null || data?.sceneTransforms == null)
                return;

            for (var i = 0; i < data.sceneTransforms.Length; i++)
            {
                var record = data.sceneTransforms[i];
                if (record == null || string.IsNullOrEmpty(record.path))
                    continue;

                if (record.path == ".")
                    continue;

                var target = TableLayoutHierarchy.Find(tableRoot, record.path);
                record.ApplyTo(target);
            }

            SyncOverlayViewsFromLayout(data);
        }

        public static bool HasSavedFieldZoneLayout(TableLayoutData data)
        {
            if (data?.sceneTransforms == null)
                return false;

            for (var i = 0; i < data.sceneTransforms.Length; i++)
            {
                var path = data.sceneTransforms[i]?.path;
                if (string.IsNullOrEmpty(path))
                    continue;

                if (path.Contains("FieldZone", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void UpsertTransform(Transform tableRoot, Transform target, TableLayoutData data)
        {
            if (tableRoot == null || target == null || data == null)
                return;

            if (target == tableRoot)
                return;

            var record = SceneTransformRecord.FromTransform(tableRoot, target);
            var list = data.sceneTransforms != null
                ? new List<SceneTransformRecord>(data.sceneTransforms)
                : new List<SceneTransformRecord>();

            var replaced = false;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i]?.path != record.path)
                    continue;

                list[i] = record;
                replaced = true;
                break;
            }

            if (!replaced)
                list.Add(record);

            data.sceneTransforms = list.ToArray();
        }

        public static void CommitTransform(Transform tableRoot, Transform target, TableLayoutData data)
        {
            UpsertTransform(tableRoot, target, data);
            SyncLayoutFieldsFromScene(tableRoot, data);
        }

        static void CaptureRecursive(Transform root, Transform current, List<SceneTransformRecord> records)
        {
            records.Add(SceneTransformRecord.FromTransform(root, current));
            for (var i = 0; i < current.childCount; i++)
                CaptureRecursive(root, current.GetChild(i), records);
        }

        public static void SyncLayoutFieldsFromScene(Transform tableRoot, TableLayoutData data)
        {
            var playmat = FindByName(tableRoot, "Playmat");
            if (playmat != null)
            {
                var scale = playmat.localScale;
                var pos = playmat.localPosition;
                data.matScaleX = scale.x;
                data.matScaleY = scale.y;
                data.matScaleZ = scale.z;
                data.matPosX = pos.x;
                data.matPosY = pos.y;
                data.matPosZ = pos.z;
            }

            SyncPoint(data, FindByName(tableRoot, "DeckZone"), ref data.deckX, ref data.deckY, ref data.deckZ);
            SyncPointXZ(data, FindByName(tableRoot, "DiscardZone"), ref data.discardX, ref data.discardZ);
            SyncPointXZ(data, FindByName(tableRoot, "SupplyDeckView") ?? FindByName(tableRoot, "Supply"), ref data.supplyX, ref data.supplyZ);
            SyncPointXZ(data, FindByName(tableRoot, "RoundDial"), ref data.roundTrackX, ref data.roundTrackZ);
            SyncPointXZ(data, FindByName(tableRoot, "WorthDial"), ref data.worthTrackX, ref data.worthTrackZ);
            SyncPointXZ(data, FindByName(tableRoot, "WillDial"), ref data.willTrackX, ref data.willTrackZ);
            SyncPointXZ(data, FindByName(tableRoot, "HonorDial"), ref data.honorX, ref data.honorZ);
            SyncPointXZ(data, FindByName(tableRoot, "IconZone") ?? FindByName(tableRoot, "Icon"), ref data.iconX, ref data.iconZ);
            SyncPointXZ(data, FindByName(tableRoot, "Relic/BondZone") ?? FindByName(tableRoot, "RelicBondZone"), ref data.relicBondCenterX, ref data.relicBondCenterZ);
            SyncPointXZ(data, FindByName(tableRoot, "StackDrop"), ref data.stackWellX, ref data.stackWellZ);
            SyncPointXZ(data, FindByName(tableRoot, "WillwellDrop"), ref data.willwellX, ref data.willwellZ);
            SyncPointXZ(data, FindByName(tableRoot, "HoldDrop"), ref data.holdPlateX, ref data.holdPlateZ);
            SyncOverlayOffsetsFromScene(tableRoot, data);
        }

        public static void SyncOverlayOffsetsFromScene(Transform tableRoot, TableLayoutData data)
        {
            if (tableRoot == null || data == null)
                return;

            var supply = tableRoot.GetComponentInChildren<SupplyDeckView>();
            if (supply != null)
            {
                var offset = supply.GetCountWorldOffset();
                data.supplyNumberOffsetX = offset.x;
                data.supplyNumberOffsetY = offset.y;
            }

            var dials = tableRoot.GetComponentsInChildren<DialView>();
            for (var i = 0; i < dials.Length; i++)
                dials[i].PushOffsetToLayout(data);
        }

        static void SyncOverlayViewsFromLayout(TableLayoutData data)
        {
            if (data == null)
                return;

            var supply = Object.FindFirstObjectByType<SupplyDeckView>();
            supply?.ApplyLayoutSettings();

            var dials = Object.FindObjectsByType<DialView>(FindObjectsSortMode.None);
            for (var i = 0; i < dials.Length; i++)
                dials[i].ApplyOverlaySettings();
        }

        static Transform FindByName(Transform root, string objectName)
        {
            if (root == null)
                return null;

            if (root.name == objectName)
                return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindByName(root.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        static void SyncPoint(TableLayoutData data, Transform t, ref float x, ref float y, ref float z)
        {
            if (t == null)
                return;

            x = t.position.x;
            y = t.position.y;
            z = t.position.z;
        }

        static void SyncPointXZ(TableLayoutData data, Transform t, ref float x, ref float z)
        {
            if (t == null)
                return;

            x = t.position.x;
            z = t.position.z;
        }

    }
}
