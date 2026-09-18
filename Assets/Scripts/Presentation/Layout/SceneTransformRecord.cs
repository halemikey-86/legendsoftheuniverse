using System;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [Serializable]
    public sealed class SceneTransformRecord
    {
        public string path;
        public float px;
        public float py;
        public float pz;
        public float rx;
        public float ry;
        public float rz;
        public float sx = 1f;
        public float sy = 1f;
        public float sz = 1f;

        public static SceneTransformRecord FromTransform(Transform root, Transform target)
        {
            var local = target.localPosition;
            var euler = target.localEulerAngles;
            var scale = target.localScale;
            return new SceneTransformRecord
            {
                path = TableLayoutHierarchy.GetPath(root, target),
                px = local.x,
                py = local.y,
                pz = local.z,
                rx = euler.x,
                ry = euler.y,
                rz = euler.z,
                sx = scale.x,
                sy = scale.y,
                sz = scale.z,
            };
        }

        public void ApplyTo(Transform target)
        {
            if (target == null)
                return;

            target.localPosition = new Vector3(px, py, pz);
            target.localEulerAngles = new Vector3(rx, ry, rz);
            target.localScale = new Vector3(sx, sy, sz);
        }
    }
}
