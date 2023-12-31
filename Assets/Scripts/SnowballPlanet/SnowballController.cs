using System;
using System.Collections;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;

namespace SnowballPlanet
{
    public class SnowballController : ThirdPersonOrbitalController
    {
        [SerializeField] private SceneReference CreditsScene;
        [SerializeField] private PlanetInfo TargetPlanet;
        [SerializeField] private float GrowthSpeed = 0.4f;

        public Action<float> OnSnowballGrow;

        private Coroutine _growCoroutine;
        private float _growthEndTime;
        private float _baseSize;
        private float _size;
        private Vector3 _baseCameraTranslationOffset;
        private Vector3 _cameraTranslationOffset;
        private Vector3 _baseCameraRotationOffset;
        private Vector3 _cameraRotationOffset;
        private float _baseOrbitRadiusOffset;

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
            OnSnowballGrow.Invoke(_size);
        }

        private IEnumerator Grow()
        {
            var growthStartTime = Time.time;
            var currentTime = growthStartTime;
            var lastScale = transform.localScale;
            var lastCameraTranslationOffset = _cameraTranslationOffset;
            var lastCameraRotationOffset = _cameraRotationOffset;
            var lastOrbitRadiusOffset = orbitRadius;

            while (currentTime < _growthEndTime)
            {
                currentTime += Time.deltaTime;
                var elapsed = Mathf.InverseLerp(growthStartTime, _growthEndTime, currentTime);
                transform.localScale = Vector3.Lerp(lastScale, Vector3.one * _size, elapsed);
                _cameraTranslationOffset.y = Mathf.Lerp(lastCameraTranslationOffset.y, _baseCameraTranslationOffset.y + (_size - _baseSize) * 2, elapsed);
                _cameraTranslationOffset.z = Mathf.Lerp(lastCameraTranslationOffset.z, _baseCameraTranslationOffset.z - (_size - _baseSize) * 0.1f, elapsed);
                _cameraRotationOffset.x = Mathf.Lerp(lastCameraRotationOffset.x, _baseCameraRotationOffset.x + (_size - _baseSize) * 1.2f, elapsed);
                orbitRadius = Mathf.Lerp(lastOrbitRadiusOffset, _baseOrbitRadiusOffset + (_size - _baseSize), elapsed);
                _mainCameraConstraint.SetTranslationOffset(0, _cameraTranslationOffset);
                _mainCameraConstraint.SetRotationOffset(0, _cameraRotationOffset);
                OnSnowballGrow.Invoke(_size);

                yield return null;
            }

            transform.localScale = Vector3.one * _size;
            OnSnowballGrow.Invoke(_size);
            _growCoroutine = null;
        }

        private IEnumerator DisplayVictoryAndStartLoadCredits()
        {
            Locked = true;

            yield return new WaitForSeconds(5f);

            SceneManager.LoadScene(CreditsScene.BuildIndex);
        }

        private void PickUpItem(PickableItem item)
        {
            var parentConstraint = item.GetComponent<ParentConstraint>();

            item.GetComponentInChildren<BoxCollider>().enabled = false;
            parentConstraint.AddSource(new ConstraintSource { sourceTransform = _snowballRollTransform, weight = 1 });

            transform.position += (_snowballRollTransform.position - transform.position) * 0.8f;

            parentConstraint.translationAtRest = transform.position;
            parentConstraint.rotationAtRest = transform.rotation.eulerAngles;
            parentConstraint.SetRotationOffset(0, (item.transform.rotation * Quaternion.Inverse(_snowballRollTransform.rotation)).eulerAngles);
            parentConstraint.SetTranslationOffset(0,  _snowballRollTransform.InverseTransformPoint(item.transform.position) * _snowballRollTransform.transform.lossyScale.x);
            parentConstraint.constraintActive = true;

            _growthEndTime = Time.time + GrowthSpeed;
            _size += item.GrowthAmount;

            if (_growCoroutine == null)
                _growCoroutine = StartCoroutine(Grow());

            if (item.IsGoal)
                StartCoroutine(DisplayVictoryAndStartLoadCredits());
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
