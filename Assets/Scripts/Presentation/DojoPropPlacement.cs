using System;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [Serializable]
    public struct DojoPropPlacement
    {
        public GameObject model;
        public Texture2D albedo;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public float uniformScale;
        [Tooltip("Target width/depth on the playmat in world units.")]
        public float targetFootprint;
    }
}
