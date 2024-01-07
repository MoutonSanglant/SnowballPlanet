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
        [SerializeField] private float MinVelocity = 0.32f;
        [SerializeField] private float MinAngularVelocity = 0.12f;
        [SerializeField] private float ForwardAccelerationRate = 2f;
        [SerializeField] private float BackwardAccelerationRate = 2f;
        [SerializeField] private float DecelerationRate = 2f;
        [SerializeField] private float ForwardBrakeRate = 1f;
        [SerializeField] private float BackwardBrakeRate = 1f;
        [SerializeField] private float RotationRate = 2f;
        [SerializeField] private float MovementSpeed = 1f;
        [SerializeField] private float RotationSpeed = 1f;
        [SerializeField] private AnimationCurve SpeedScaleAdjustment;
        [SerializeField] private float FrictionCorrection = 1f;

        protected Transform orbitCenter;
        protected float orbitRadius = 1f;
        protected Vector3 previousPosition;

        private Vector2 _velocity;
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
                _velocity = value ? Vector2.zero : _velocity;

                OnControllerMove.Invoke(_velocity);
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
            _previousForward = transform.forward;
            previousPosition = _rigidbody.position - transform.forward;
        }

        protected virtual void FixedUpdate()
        {
            if (_moveAmount.y > 0f)
            {
                if (Mathf.Abs(_velocity.y) < 0.01f)
                    _velocity.y = MinVelocity;

                if (_velocity.y > 0f)
                    _velocity.y += Time.fixedDeltaTime * ForwardAccelerationRate;
                else
                    _velocity.y += Time.fixedDeltaTime * DecelerationRate * ForwardBrakeRate;

                _velocity.y = Mathf.Clamp(_velocity.y, -MaxVelocity, MaxVelocity);
            }
            else if (_moveAmount.y < 0f)
            {
                if (Mathf.Abs(_velocity.y) < 0.01f)
                    _velocity.y = -MinVelocity;

                if (_velocity.y < 0f)
                    _velocity.y -= Time.fixedDeltaTime * BackwardAccelerationRate;
                else
                    _velocity.y -= Time.fixedDeltaTime * DecelerationRate * BackwardBrakeRate;

                _velocity.y = Mathf.Clamp(_velocity.y, -MaxVelocity, MaxVelocity);
            }
            else
            {
                if (_velocity.y < -0.01f)
                    _velocity.y += Time.fixedDeltaTime * DecelerationRate;
                else if (_velocity.y > 0.01f)
                    _velocity.y -= Time.fixedDeltaTime * DecelerationRate;
                else
                    _velocity.y = 0f;
            }

            if (_moveAmount.x > 0f)
            {
                if (Mathf.Abs(_velocity.x) < 0.01f)
                    _velocity.x = MinAngularVelocity;

                _velocity.x += Time.fixedDeltaTime * RotationRate;
                _velocity.x = Mathf.Clamp(_velocity.x, 0f, MaxVelocity * Mathf.Abs(_velocity.y));
            }
            else if (_moveAmount.x < 0f)
            {
                if (Mathf.Abs(_velocity.x) < 0.01f)
                    _velocity.x = -MinAngularVelocity;

                _velocity.x -= Time.fixedDeltaTime * RotationRate;
                _velocity.x = Mathf.Clamp(_velocity.x, -MaxVelocity * Mathf.Abs(_velocity.y), 0f);
            }
            else
            {
                if (_velocity.x < -0.01f)
                    _velocity.x += Time.fixedDeltaTime * RotationRate;
                else if (_velocity.x > 0.01f)
                    _velocity.x -= Time.fixedDeltaTime * RotationRate;
                else
                    _velocity.x = 0f;
            }

            var controllerPosition = _rigidbody.position;
            var controllerTransform = transform;
            var controllerScale = controllerTransform.localScale.x;
            var controllerForward = controllerTransform.forward;
            var controllerUpward = (controllerPosition - orbitCenter.position).normalized;

            if (_velocity.y > 0f)
            {
                if (_alignCamera)
                    controllerForward = _previousForward;
                else if (_lastVelocityY > 0f)
                    controllerForward = (_rigidbody.position - previousPosition).normalized;

                _alignCamera = false;
                _lastVelocityY = _velocity.y;
            }
            else if (_velocity.y < 0f)
            {
                if (_alignCamera)
                    controllerForward = _previousForward;
                else if (_lastVelocityY < 0f)
                    controllerForward = (previousPosition - _rigidbody.position).normalized;

                _alignCamera = false;
                _lastVelocityY = _velocity.y;
            }
            else if (Mathf.Abs(_velocity.x) > 0f)
            {
                _alignCamera = true;
                _lastVelocityY = 0;
            }
            else
                _lastVelocityY = 0;

            controllerForward = Quaternion.AngleAxis(_velocity.x * Time.fixedDeltaTime * RotationSpeed, controllerUpward) * controllerForward;

            var speed = MovementSpeed * SpeedScaleAdjustment.Evaluate(Mathf.Min(controllerScale, 1f));
            var desiredPosition = controllerPosition + controllerForward * (_velocity.y * Time.fixedDeltaTime * speed);
            var desiredDirection = (desiredPosition - controllerPosition).normalized;

            // Don't try to move the controller when it's going too slow
            if (Mathf.Abs((desiredPosition - controllerPosition).magnitude) < 0.003f)
            {
                _previousForward = controllerTransform.forward;
                _alignCamera = true;
                OnControllerMove.Invoke(Vector2.zero);

                return;
            }

            var colliders = Physics.OverlapSphere(desiredPosition, controllerScale * 0.5f, _collisionMask);

            if (colliders.Length > 0)
            {
                foreach (var collider in colliders)
                {
                    var item = collider.transform.GetComponentInParent<PickableItem>();

                    if (item && controllerTransform.localScale.x >= item.PickSize * 0.5f)
                        continue;

                    var colliderDirection = (collider.transform.position - controllerPosition).normalized;
                    var hits = Physics.RaycastAll(controllerPosition, colliderDirection, controllerScale, _collisionMask);

                    if (hits.Length < 1)
                        continue;

                    var hit = hits.First();
                    var projectedNormal = Vector3.ProjectOnPlane(hit.normal, controllerTransform.up);
                    var repulsionNormal = ((projectedNormal.normalized + desiredDirection) * 0.5f).normalized;
                    var constrainedPosition = hit.point + repulsionNormal * controllerScale;
                    var newDirection = constrainedPosition - controllerPosition;

                    desiredPosition = controllerPosition + newDirection * (Mathf.Abs(_velocity.y) * Time.fixedDeltaTime * speed * FrictionCorrection);

                    // Prevents the camera to turn
                    _lastVelocityY = 0;

                    var projectedForward = Vector3.ProjectOnPlane(controllerForward, desiredPosition - orbitCenter.position);

                    controllerForward = Quaternion.AngleAxis(_velocity.x * Time.fixedDeltaTime * RotationSpeed, controllerUpward) * projectedForward.normalized;

                    break;
                }
            }

            CartesianToSpherical(desiredPosition, out var radius, out var polar, out var elevation);
            SphericalToCartesian(orbitRadius, polar, elevation, out var cartesianCoordinates);
            OnControllerMove.Invoke(_velocity);

            _rigidbody.Move(cartesianCoordinates, Quaternion.LookRotation(controllerForward, controllerUpward));
            _previousForward = controllerTransform.forward;
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
