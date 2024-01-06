using System;
using System.Linq;
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
        [SerializeField] private float MaxVelocity = 2f;
        [SerializeField] private float AccelerationRate = 2f;
        [SerializeField] private float RotationRate = 2f;
        [SerializeField] private float MovementSpeed = 1f;
        [SerializeField] private float RotationSpeed = 1f;
        [SerializeField] private AnimationCurve SpeedScaleAdjustment;
        [SerializeField] private float FrictionCorrection = 1f;

        protected Transform orbitCenter;
        protected float orbitRadius = 1f;
        protected Vector3 previousPosition;

        protected Vector2 velocity;
        private Vector2 _moveAmount;
        private float _lastVelocityY;
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
                velocity = value ? Vector2.zero : velocity;
                OnControllerMove.Invoke(velocity);
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
            if (_moveAmount.y > 0f)
            {
                velocity.y += Time.fixedDeltaTime * AccelerationRate;
                velocity.y = Mathf.Clamp(velocity.y, 0f, MaxVelocity);
            }
            else if (_moveAmount.y < 0f)
            {
                velocity.y -= Time.fixedDeltaTime * AccelerationRate * 0.5f;
                velocity.y = Mathf.Clamp(velocity.y, -MaxVelocity, 0f);
            }
            else
            {
                if (velocity.y < 0f)
                    velocity.y += Time.fixedDeltaTime * RotationRate;
                else if (velocity.y > 0f)
                    velocity.y -= Time.fixedDeltaTime * RotationRate;

                if (Mathf.Abs(velocity.y) < 0.001f)
                    velocity.y = 0f;
            }

            if (_moveAmount.x > 0f)
            {
                velocity.x += Time.fixedDeltaTime * RotationRate;
                velocity.x = Mathf.Clamp(velocity.x, 0f, MaxVelocity * Mathf.Abs(velocity.y));
            }
            else if (_moveAmount.x < 0f)
            {
                velocity.x -= Time.fixedDeltaTime * RotationRate;
                velocity.x = Mathf.Clamp(velocity.x, -MaxVelocity * Mathf.Abs(velocity.y), 0f);
            }
            else
            {
                if (velocity.x < 0f)
                    velocity.x += Time.fixedDeltaTime * RotationRate;
                else if (velocity.x > 0f)
                    velocity.x -= Time.fixedDeltaTime * RotationRate;

                if (Mathf.Abs(velocity.x) < 0.001f)
                    velocity.x = 0f;
            }

            Debug.Log(velocity);

            var upward = (transform.position - orbitCenter.position).normalized;
            var forward = transform.forward;

            if (velocity.y > 0f)
            {
                if (_alignCamera)
                    forward = _previousForward;
                else if (_lastVelocityY > 0f)
                    forward = (_rigidbody.position - previousPosition).normalized;

                _alignCamera = false;
                _lastVelocityY = velocity.y;
            }
            else if (velocity.y < 0f)
            {
                if (_alignCamera)
                    forward = _previousForward;
                else if (_lastVelocityY < 0f)
                    forward = (previousPosition - _rigidbody.position).normalized;

                _alignCamera = false;
                _lastVelocityY = velocity.y;
            }
            else if (Mathf.Abs(velocity.x) > 0f)
            {
                _alignCamera = true;
                _lastVelocityY = 0;
            }
            else
                _lastVelocityY = 0;

            forward = Quaternion.AngleAxis(velocity.x * Time.fixedDeltaTime * RotationSpeed, upward) * forward;

            var bodyPosition = _rigidbody.position;
            var bodyScale = transform.localScale.x;
            var sphereRadius = bodyScale * 0.5f;

            var speed = MovementSpeed * SpeedScaleAdjustment.Evaluate(Mathf.Min(bodyScale, 1f));
            var desiredPosition = _rigidbody.position + forward * (velocity.y * Time.fixedDeltaTime * speed);
            var desiredDirection = (desiredPosition - bodyPosition).normalized;

            var colliders = Physics.OverlapSphere(desiredPosition, sphereRadius, _collisionMask);

            if (colliders.Length > 0)
            {
                foreach (var collider in colliders)
                {
                    var item = collider.transform.GetComponentInParent<PickableItem>();

                    if (item && transform.localScale.x >= item.PickSize * 0.5f)
                        continue;

                    var colliderDirection = (collider.transform.position - bodyPosition).normalized;
                    var hits = Physics.RaycastAll(bodyPosition, colliderDirection, bodyScale, _collisionMask);

                    if (hits.Length < 1)
                        continue;

                    var hit = hits.First();
                    var projectedNormal = Vector3.ProjectOnPlane(hit.normal, transform.up);
                    var repulsionNormal = ((projectedNormal.normalized + desiredDirection) * 0.5f).normalized;
                    var constrainedPosition = hit.point + repulsionNormal * bodyScale;
                    var newDirection = constrainedPosition - bodyPosition;

                    desiredPosition = bodyPosition + newDirection * (Mathf.Abs(velocity.y) * Time.fixedDeltaTime * speed * FrictionCorrection);

                    // Prevents the camera to turn
                    _lastVelocityY = 0;

                    var projectedForward = Vector3.ProjectOnPlane(forward, desiredPosition - orbitCenter.position);

                    forward = Quaternion.AngleAxis(velocity.x * Time.fixedDeltaTime * RotationSpeed, upward) * projectedForward.normalized;

                    break;
                }
            }

            CartesianToSpherical(desiredPosition, out var radius, out var polar, out var elevation);
            SphericalToCartesian(orbitRadius, polar, elevation, out var cartesianCoordinates);
            OnControllerMove.Invoke(velocity);

            _rigidbody.Move(cartesianCoordinates, Quaternion.LookRotation(forward, upward));
            _previousForward = transform.forward;
            previousPosition = _rigidbody.position;
        }
        #endregion UnityEvents

        #region InputsEvents
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveAmount = Locked ? Vector2.zero : context.ReadValue<Vector2>();
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
