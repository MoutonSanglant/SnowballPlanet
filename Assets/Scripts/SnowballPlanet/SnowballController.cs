using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;

namespace SnowballPlanet
{
    public class SnowballController : ThirdPersonOrbitalController
    {
        [SerializeField] private PlanetInfo TargetPlanet;
        [SerializeField] private float GrowthSpeed = 0.4f;
        [SerializeField] private TMP_Text VictoryText;
        [SerializeField] private AnimationCurve VictoryAnimationCurve;
        [SerializeField] private float VictoryApparitionDuration = 2f;
        [SerializeField] private float VictoryDisplayDuration = 3f;

        public static Action OnVictory = () => {};
        public Action<PickableItem> OnItemPickup = (_) => {};
        public Action<float> OnSnowballGrow;

        private bool _startGrow;
        private bool _growing;
        private Coroutine _growCoroutine;
        private float _growthEndTime;
        private float _baseSize;
        private float _size;
        private Vector3 _baseCameraTranslationOffset;
        private Vector3 _cameraTranslationOffset;
        private Vector3 _baseCameraRotationOffset;
        private Vector3 _cameraRotationOffset;
        private float _baseOrbitRadiusOffset;
        private float _growthStartTime;
        private float _currentTime;
        private Vector3 _lastScale;
        private Vector3 _lastCameraTranslationOffset;
        private Vector3 _lastCameraRotationOffset;
        private float _lastOrbitRadiusOffset;

        // Cache
        private Transform _snowballRollTransform;
        private ParentConstraint _mainCameraConstraint;

        protected override void Awake()
        {
            _mainCameraConstraint = Camera.main.GetComponent<ParentConstraint>();
            _snowballRollTransform = GetComponentInChildren<SnowballRoll>().transform;

            _size = transform.localScale.x;
            _baseSize = _size;
            _cameraTranslationOffset = _mainCameraConstraint.GetTranslationOffset(0);
            _baseCameraTranslationOffset = new Vector3(_cameraTranslationOffset.x, _cameraTranslationOffset.y, _cameraTranslationOffset.z);
            _cameraRotationOffset = _mainCameraConstraint.GetRotationOffset(0);
            _baseCameraRotationOffset = new Vector3(_cameraRotationOffset.x, _cameraRotationOffset.y, _cameraRotationOffset.z);
            _baseOrbitRadiusOffset = TargetPlanet.Radius;

            orbitCenter = TargetPlanet.transform;
            orbitRadius = TargetPlanet.Radius;

            base.Awake();
        }

        private void Start()
        {
            OnSnowballGrow.Invoke(transform.localScale.x);
        }

        protected override void FixedUpdate()
        {
            if (_startGrow)
            {
                 _growthStartTime = Time.time;
                 _currentTime = _growthStartTime;
                 _lastScale = transform.localScale;
                 _lastCameraTranslationOffset = _cameraTranslationOffset;
                 _lastCameraRotationOffset = _cameraRotationOffset;
                 _lastOrbitRadiusOffset = orbitRadius;
                 _startGrow = false;
            }

            if (_growing)
            {
                _currentTime += Time.fixedDeltaTime;
                var elapsed = Mathf.InverseLerp(_growthStartTime, _growthEndTime, _currentTime);

                if (_currentTime > _growthEndTime)
                    elapsed = 1f;

                orbitRadius = Mathf.Lerp(_lastOrbitRadiusOffset, _baseOrbitRadiusOffset + (_size - _baseSize), elapsed);
                transform.localScale = Vector3.Lerp(_lastScale, Vector3.one * _size, elapsed);
                _cameraTranslationOffset.y = Mathf.Lerp(_lastCameraTranslationOffset.y, _baseCameraTranslationOffset.y + (_size - _baseSize) * 3, elapsed);
                _cameraTranslationOffset.z = Mathf.Lerp(_lastCameraTranslationOffset.z, _baseCameraTranslationOffset.z - (_size - _baseSize) * 0.34f, elapsed);
                _cameraRotationOffset.x = Mathf.Lerp(_lastCameraRotationOffset.x, _baseCameraRotationOffset.x + (_size - _baseSize) * 3.6f, elapsed);
                _mainCameraConstraint.SetTranslationOffset(0, _cameraTranslationOffset);
                _mainCameraConstraint.SetRotationOffset(0, _cameraRotationOffset);

                OnSnowballGrow.Invoke(transform.localScale.x);
            }

            base.FixedUpdate();

            if (_growing)
            {
                if (_currentTime > _growthEndTime)
                    _growing = false;

                CartesianToSpherical(_rigidbody.position, out var radius, out var polar, out var elevation);
                SphericalToCartesian(orbitRadius, polar, elevation, out previousPosition);
            }
        }

        private IEnumerator DisplayVictoryAndStartLoadCredits()
        {
            OnVictory.Invoke();
            Locked = true;

            var elapsed = 0f;

            while (elapsed < VictoryApparitionDuration)
            {
                elapsed += Time.deltaTime;
                VictoryText.transform.localScale = Vector3.one * VictoryAnimationCurve.Evaluate(elapsed / VictoryApparitionDuration);

                yield return null;
            }

            yield return new WaitForSeconds(VictoryDisplayDuration);

            SceneTransitionManager.LoadScene(SceneTransitionManager.Scene.Credits);
        }

        private void PickUpItem(PickableItem item)
        {
            item.DestroyCollider();

            var parentConstraint = item.GetComponent<ParentConstraint>();

            parentConstraint.AddSource(new ConstraintSource { sourceTransform = _snowballRollTransform, weight = 1 });
            parentConstraint.translationAtRest = transform.position;
            parentConstraint.rotationAtRest = transform.rotation.eulerAngles;
            parentConstraint.SetRotationOffset(0, (item.transform.rotation * Quaternion.Inverse(_snowballRollTransform.rotation)).eulerAngles);
            parentConstraint.SetTranslationOffset(0,  _snowballRollTransform.InverseTransformPoint(item.transform.position) * _snowballRollTransform.transform.lossyScale.x);
            parentConstraint.constraintActive = true;

            _growthEndTime = Time.time + GrowthSpeed;
            _size += item.GrowthAmount * 0.5f;

            if (!_growing)
            {
                _startGrow = true;
                _growing = true;
            }

            if (item.IsGoal)
                StartCoroutine(DisplayVictoryAndStartLoadCredits());
            else
                OnItemPickup.Invoke(item);
        }

        #region PhysicEvents
        private void OnCollisionEnter(Collision other)
        {
            var item = other.gameObject.GetComponentInParent<PickableItem>();

            if (item && _size >= item.PickSize * 0.5f)
                PickUpItem(item);
        }
        #endregion PhysicEvents
    }
}
