using UnityEngine;

namespace SnowballPlanet
{
    public class PickableItem : MonoBehaviour
    {
        [field: SerializeField] public float PickSize { get; private set; } = 1f;
        [field: SerializeField] public float GrowthAmount { get; private set; } = 1f;
    }
}
