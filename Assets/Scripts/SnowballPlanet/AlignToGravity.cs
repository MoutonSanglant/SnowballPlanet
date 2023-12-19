using UnityEngine;

namespace SnowballPlanet
{
    public class PropOrbitalPosition : MonoBehaviour
    {
        [Tooltip("The origin of the spherical coordinate system")]
        [SerializeField] private Transform OrbitCenter;

        private void Awake()
        {
            transform.up = (transform.position - OrbitCenter.position).normalized;
        }
    }
}
