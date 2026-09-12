using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Guarantees a rendering camera and exactly one enabled AudioListener per scene.
    /// </summary>
    public static class AudioListenerBootstrap
    {
        static readonly Color MenuBackground = new(0.05f, 0.08f, 0.18f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            EnsureExists();
        }

        public static void EnsureExists()
        {
            var camera = EnsureMainCamera();
            if (camera != null)
                AttachToCamera(camera);
            else
                EnsureStandaloneListener();
        }

        public static Camera EnsureMainCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
                return ActivateCamera(mainCamera);

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var camera in cameras)
            {
                if (camera == null)
                    continue;

                camera.tag = "MainCamera";
                return ActivateCamera(camera);
            }

            return CreateFallbackCamera();
        }

        public static void AttachToCamera(Camera camera)
        {
            if (camera == null)
                return;

            EnsureUrpCamera(camera);

            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();

            KeepOnly(camera.GetComponent<AudioListener>());
        }

        static Camera ActivateCamera(Camera camera)
        {
            camera.gameObject.SetActive(true);
            camera.enabled = true;
            EnsureUrpCamera(camera);
            return camera;
        }

        static Camera CreateFallbackCamera()
        {
            var host = new GameObject("Main Camera");
            host.tag = "MainCamera";

            var camera = host.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = MenuBackground;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.depth = -1;
            EnsureUrpCamera(camera);

            return camera;
        }

        static void EnsureUrpCamera(Camera camera)
        {
            if (camera == null)
                return;

            if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        static void EnsureStandaloneListener()
        {
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            if (listeners.Length > 0)
            {
                KeepOnly(listeners[0]);
                return;
            }

            var host = new GameObject("AudioListener");
            host.AddComponent<AudioListener>();
        }

        static void KeepOnly(AudioListener activeListener)
        {
            if (activeListener == null)
                return;

            activeListener.enabled = true;

            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            foreach (var listener in listeners)
            {
                if (listener != null && listener != activeListener)
                    listener.enabled = false;
            }
        }
    }
}
