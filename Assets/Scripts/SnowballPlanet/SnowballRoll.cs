using System.Collections;
using UnityEngine;

namespace SnowballPlanet
{
    public class SnowballRoll : MonoBehaviour
    {
        [SerializeField] private float RollSpeed = 1f;

        private Vector2 _rollAmount;
        private Transform _parent;

        private void Awake()
        {
            _parent = transform.parent;

            var controller = GetComponentInParent<ThirdPersonOrbitalController>();

            controller.OnControllerMove += moveAmount => { _rollAmount = moveAmount; };
        }

        private void Update()
        {
            if (Mathf.Abs(_rollAmount.y) > 0)
            {
                transform.RotateAround(transform.position,  _parent.right, _rollAmount.y * Time.deltaTime * RollSpeed);
            }

            if (Mathf.Abs(_rollAmount.x) > 0)
            {
                transform.RotateAround(transform.position, _parent.forward, _rollAmount.x * Time.deltaTime * RollSpeed);
            }
        }
    }
}