using UnityEngine;

namespace SnowballPlanet
{
    public class AlignToGravity : MonoBehaviour
    {
        [field: SerializeField] public PlanetInfo Planet { get; private set; }

        private void Awake()
        {
            if (Planet == null)
                Planet = GetComponentInParent<PlanetInfo>();

            transform.up = (transform.position - Planet.transform.position).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (Planet == null)
                return;

            transform.up = (transform.position - Planet.transform.position).normalized;
        }
    }
}
