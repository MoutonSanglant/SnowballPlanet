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
        [Tooltip("The origin of the spherical coordinate system")]
        [SerializeField] private Transform OrbitCenter;
        [Tooltip("The radial distance from the origin of the controller")]
        [SerializeField] private float Radius = 1f;
        [SerializeField] private float MovementSpeed = 1f;
        [SerializeField] private float RotationSpeed = 1f;

        private Vector2 _moveAmount;
        private float _lastMoveAmountY;
        private Vector3 _previousPosition;
        private Vector3 _previousForward;
        private bool _alignCamera;

        // Cache
        private Rigidbody _rigidbody;

        #region UnityEvents
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _previousPosition = _rigidbody.position - transform.forward;
        }

        private void FixedUpdate()
        {
            var upward = (transform.position - OrbitCenter.position).normalized;
            var forward = transform.forward;

            if (_moveAmount.y > 0f)
            {
                if (_alignCamera)
                    forward = _previousForward;
                else if (_lastMoveAmountY > 0f)
                    forward = (_rigidbody.position - _previousPosition).normalized;

                _alignCamera = false;
                _previousPosition = _rigidbody.position;
                _lastMoveAmountY = _moveAmount.y;
            }
            else if (_moveAmount.y < 0f)
            {
                if (_alignCamera)
                    forward = _previousForward;
                else if (_lastMoveAmountY < 0f)
                    forward = (_previousPosition - _rigidbody.position).normalized;

                _alignCamera = false;
                _previousPosition = _rigidbody.position;
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

            CartesianToSpherical(desiredPosition, out var radius, out var polar, out var elevation);
            SphericalToCartesian(Radius, polar, elevation, out var cartesianCoordinates);

            _rigidbody.Move(cartesianCoordinates, Quaternion.LookRotation(forward, upward));
            _previousForward = transform.forward;
        }
        #endregion UnityEvents

        #region InputsEvents
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveAmount = context.ReadValue<Vector2>();
        }
        #endregion UnityEvents

        #region SphericalSystemHelpers
        /// <summary>
        /// Converts a coordinate from a spherical system to a cartesian system
        /// </summary>
        /// <see cref="https://github.com/mortennobel/CameraLib4U/blob/master/Assets/_CameraLib4U/Scripts/SphericalCoordinates.cs"/>
        private static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3 outCart){
            var a = radius * Mathf.Cos(elevation);

            outCart.x = a * Mathf.Cos(polar);
            outCart.y = radius * Mathf.Sin(elevation);
            outCart.z = a * Mathf.Sin(polar);
        }

        /// <summary>
        /// Converts a coordinate from a cartesian system to a spherical system
        /// </summary>
        /// <see cref="https://github.com/mortennobel/CameraLib4U/blob/master/Assets/_CameraLib4U/Scripts/SphericalCoordinates.cs"/>
        private static void CartesianToSpherical(Vector3 cartCoords, out float outRadius, out float outPolar, out float outElevation){
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
