using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SnowballPlanet.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AlignToGravity))]
    public class AlignToGravityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Land"))
            {
                var element = (AlignToGravity)target;

                if (element.Planet == null)
                    Debug.LogWarning("Element has no planet assigned, using parent");


                var planet = element.Planet == null ? element.gameObject.GetComponentInParent<PlanetInfo>() : element.Planet;
                var tmpSphereCollider = planet.gameObject.AddComponent<SphereCollider>();
                tmpSphereCollider.radius = planet.Radius;

                var hits = Physics.RaycastAll(element.transform.position,
                    planet.transform.position - element.transform.position);

                if (hits.Length > 0)
                {
                    var planetHits = hits.Where(hit => hit.transform.GetComponent<PlanetInfo>()).ToArray();

                    if (planetHits.Length > 0)
                        element.transform.position = planetHits[0].point;
                }

                DestroyImmediate(tmpSphereCollider);
            }
        }
    }
}
