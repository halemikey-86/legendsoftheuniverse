using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Links a presentation CardView to an Engine B instance id for drag/drop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TableCardBinding : MonoBehaviour
    {
        [SerializeField] int instanceId = -1;

        public int InstanceId => instanceId;

        public void Bind(int id)
        {
            instanceId = id;
        }

        public void Clear() => instanceId = -1;
    }
}
