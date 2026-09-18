using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Loads and saves table layout JSON. Applied live by <see cref="TableLayoutApplier"/>.
    /// </summary>
    public static class TableLayoutSettings
    {
        public const string AssetRelativePath = "Assets/Settings/TableLayout.json";
        public const string RuntimeFileName = "table-layout.json";

        static TableLayoutData active = TableLayoutData.CreateDefaults();
        static bool loaded;

        public static TableLayoutData Active
        {
            get
            {
                EnsureLoaded();
                return active;
            }
        }

        public static event Action LayoutChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap() => EnsureLoaded();

        public static void EnsureLoaded()
        {
            if (loaded)
                return;

            if (TryLoadFromDisk(out var data))
            {
                active = data;
                loaded = true;
                return;
            }

            // Unity forbids Application path APIs during MonoBehaviour construction.
            // Keep baked defaults until paths are safe, then stop retrying if no file exists.
            if (CanAccessApplicationPaths())
                loaded = true;
        }

        public static void Replace(TableLayoutData data)
        {
            active = data ?? TableLayoutData.CreateDefaults();
            loaded = true;
            LayoutChanged?.Invoke();
        }

        public static void ResetToDefaults()
        {
            active = TableLayoutData.CreateDefaults();
            LayoutChanged?.Invoke();
        }

        public static void NotifyChanged() => LayoutChanged?.Invoke();

        public static void CaptureScene(Transform tableRoot)
        {
            TableLayoutSceneCapture.CaptureScene(tableRoot, active);
        }

        public static bool Save(Transform tableRoot = null)
        {
            try
            {
                if (tableRoot != null)
                    CaptureScene(tableRoot);

                var json = JsonUtility.ToJson(active, true);
                var runtimePath = GetRuntimePath();
                Directory.CreateDirectory(Path.GetDirectoryName(runtimePath)!);
                File.WriteAllText(runtimePath, json);

#if UNITY_EDITOR
                var assetPath = GetAssetPath();
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
                File.WriteAllText(assetPath, json);
                AssetDatabase.Refresh();
#endif
#if UNITY_EDITOR
                Debug.Log($"[TableLayout] Saved layout to {GetAssetPath()} and {runtimePath}");
#else
                Debug.Log($"[TableLayout] Saved layout to {runtimePath}");
#endif
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TableLayout] Save failed: {ex.Message}");
                return false;
            }
        }

        static bool TryLoadFromDisk(out TableLayoutData data)
        {
            data = null;
            if (!CanAccessApplicationPaths())
                return false;

            foreach (var path in GetLoadPaths())
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    continue;

                try
                {
                    var json = File.ReadAllText(path);
                    data = JsonUtility.FromJson<TableLayoutData>(json);
                    if (data != null)
                        return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TableLayout] Failed to load {path}: {ex.Message}");
                }
            }

            return false;
        }

        static bool CanAccessApplicationPaths()
        {
            try
            {
                _ = Application.persistentDataPath;
#if !UNITY_EDITOR
                _ = Application.streamingAssetsPath;
#endif
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        static string GetRuntimePath() =>
            Path.Combine(Application.persistentDataPath, RuntimeFileName);

        static string GetAssetPath() =>
            Path.Combine(Application.dataPath, "Settings", "TableLayout.json");

        static string[] GetLoadPaths()
        {
#if UNITY_EDITOR
            return new[] { GetAssetPath(), GetRuntimePath() };
#else
            return new[]
            {
                Path.Combine(Application.streamingAssetsPath, RuntimeFileName),
                GetRuntimePath(),
            };
#endif
        }
    }
}
