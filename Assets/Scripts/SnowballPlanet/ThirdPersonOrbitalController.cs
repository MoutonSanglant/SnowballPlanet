using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SnowballPlanet
{
    /// <summary>
    /// A third-person controller using a spherical coordinate system
    /// </summary>
    /// <see cref="https://en.wikipedia.org/wiki/Spherical_coordinate_system"/>
    public class ThirdPersonOrbitalController : MonoBehaviour
    {
        [SerializeField] private float MovementSpeed = 1f;
        [SerializeField] private float RotationSpeed = 1f;

        protected Transform orbitCenter;
        protected float orbitRadius = 1f;
        protected Vector3 previousPosition;

        private Vector2 _moveAmount;
        private float _lastMoveAmountY;
        private Vector3 _previousForward;
        private bool _alignCamera;
        private bool _locked;

        public Action<Vector2> OnControllerMove = moveAmount => { };

        protected bool Locked
        {
            get
            {
                return _locked;
            }
            set
            {
                _locked = value;
                _moveAmount = value ? Vector2.zero : _moveAmount;
                OnControllerMove.Invoke(_moveAmount);
            }
        }

        // Cache
        protected Rigidbody _rigidbody;
        private int _collisionMask;

        #region UnityEvents
        protected virtual void Awake()
        {
            _collisionMask = LayerMask.GetMask("Colliders");
            _rigidbody = GetComponent<Rigidbody>();
            previousPosition = _rigidbody.position - transform.forward;
        }

        protected virtual void FixedUpdate()
        {
            var upward = (transform.position - orbitCenter.position).normalized;
            var forward = transform.forward;

            if (_moveAmount.y > 0f)
            {
                if (_alignCamera)
                    forward = _previousForward;
                else if (_lastMoveAmountY > 0f)
                    forward = (_rigidbody.position - previousPosition).normalized;

                _alignCamera = false;
                _lastMoveAmountY = _moveAmount.y;
            }
            else if (_moveAmount.y < 0f)
            {
                if (_alignCamera)
                    forward = _previousForward;
                else if (_lastMoveAmountY < 0f)
                    forward = (previousPosition - _rigidbody.position).normalized;

                _alignCamera = false;
                _lastMoveAmountY = _moveAmount.y;
            }
            else if (Mathf.Abs(_moveAmount.x) > 0f)
            {
                _alignCamera = true;
                _lastMoveAmountY = 0;
            }
            else
                _lastMoveAmountY = 0;

            forward = Quaternion.AngleAxis(_moveAmount.x * Time.fixedDeltaTime * RotationSpeed, upward) * forward;

            var desiredPosition = _rigidbody.position + forward * (_moveAmount.y * Time.fixedDeltaTime * MovementSpeed);
            var direction = (desiredPosition - transform.position).normalized;

            // Manual collision check
            var hits = Physics.RaycastAll(transform.position, direction, transform.localScale.x * 0.9f, _collisionMask);

            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    var item = hit.transform.GetComponentInParent<PickableItem>();

                    if (item && transform.localScale.x >= item.PickSize * 0.5f)
                        continue;

                    var constrainedPosition = hit.point + hit.normal * (transform.localScale.x * 0.9f * 0.5f);
                    direction = constrainedPosition - _rigidbody.position;
                    desiredPosition = _rigidbody.position + direction * (Mathf.Abs(_moveAmount.y) * Time.fixedDeltaTime * MovementSpeed * 2);

                    // Prevents the camera to turn
                    _lastMoveAmountY = 0;

                    break;
                }
            }

            CartesianToSpherical(desiredPosition, out var radius, out var polar, out var elevation);
            SphericalToCartesian(orbitRadius, polar, elevation, out var cartesianCoordinates);

            _rigidbody.Move(cartesianCoordinates, Quaternion.LookRotation(forward, upward));
            _previousForward = transform.forward;
            previousPosition = _rigidbody.position;
        }
        #endregion UnityEvents

        #region InputsEvents
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveAmount = Locked ? Vector2.zero : context.ReadValue<Vector2>();

            OnControllerMove.Invoke(_moveAmount);
        }
        #endregion UnityEvents

        #region SphericalSystemHelpers
        /// <summary>
        /// Converts a coordinate from a spherical system to a cartesian system
        /// </summary>
        /// <see cref="https://github.com/mortennobel/CameraLib4U/blob/master/Assets/_CameraLib4U/Scripts/SphericalCoordinates.cs"/>
        protected static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3 outCart){
            var a = radius * Mathf.Cos(elevation);

            outCart.x = a * Mathf.Cos(polar);
            outCart.y = radius * Mathf.Sin(elevation);
            outCart.z = a * Mathf.Sin(polar);
        }

        /// <summary>
        /// Converts a coordinate from a cartesian system to a spherical system
        /// </summary>
        /// <see cref="https://github.com/mortennobel/CameraLib4U/blob/master/Assets/_CameraLib4U/Scripts/SphericalCoordinates.cs"/>
        protected static void CartesianToSpherical(Vector3 cartCoords, out float outRadius, out float outPolar, out float outElevation){
            if (cartCoords.x == 0)
                cartCoords.x = Mathf.Epsilon;
            outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                                   + (cartCoords.y * cartCoords.y)
                                   + (cartCoords.z * cartCoords.z));
            outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
            if (cartCoords.x < 0)
                outPolar += Mathf.PI;
            outElevation = Mathf.Asin(cartCoords.y / outRadius);
        }
        #endregion SphericalSystemHelpers
    }
}
