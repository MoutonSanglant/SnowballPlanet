using System.Collections;
using UnityEngine;

namespace SnowballPlanet
{
    public class SnowballRoll : MonoBehaviour
    {
        [SerializeField] private float RollSpeed = 1f;
        [SerializeField] private ParticleSystem SnowParticles;

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
            var isTurning = Mathf.Abs(_rollAmount.y) > 0;
            var isMoving = Mathf.Abs(_rollAmount.x) > 0;

            if (isTurning)
                transform.RotateAround(transform.position,  _parent.right, _rollAmount.y * Time.deltaTime * RollSpeed);

            if (isMoving)
                transform.RotateAround(transform.position, _parent.forward, _rollAmount.x * Time.deltaTime * RollSpeed);

            if (!(isTurning || isMoving))
                SnowParticles.Stop();
            else if (SnowParticles.isStopped)
                SnowParticles.Play();
        }
    }
}