using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace SnowballPlanet
{
    public class SnowballController : ThirdPersonOrbitalController
    {
        [SerializeField] private float GrowthSpeed = 0.4f;

        private Coroutine _growCoroutine;
        private float _growthEndTime;
        private float _baseSize;
        private float _size;
        private float _baseCameraOffset;
        private Vector3 _cameraOffset;
        private float _baseOrbitRadiusOffset;

        private Transform _snowballRollTransform;
        private ParentConstraint _mainCameraConstraint;
        private ThirdPersonOrbitalController _orbitalController;

        private void Start()
        {
            _mainCameraConstraint = Camera.main.GetComponent<ParentConstraint>();
            _snowballRollTransform = GetComponentInChildren<SnowballRoll>().transform;
            _orbitalController = GetComponentInParent<ThirdPersonOrbitalController>();

            _size = transform.localScale.x;
            _baseSize = _size;
            _cameraOffset = _mainCameraConstraint.GetTranslationOffset(0);
            _baseCameraOffset = _cameraOffset.y;
            _baseOrbitRadiusOffset = _orbitalController.Radius;
        }

        private IEnumerator Grow()
        {
            var growthStartTime = Time.time;
            var currentTime = growthStartTime;
            var lastScale = transform.localScale;
            var lastCameraOffset = _cameraOffset.y;
            var lastOrbitRadiusOffset = _orbitalController.Radius;

            while (currentTime < _growthEndTime)
            {
                currentTime += Time.deltaTime;
                var elapsed = Mathf.InverseLerp(growthStartTime, _growthEndTime, currentTime);
                transform.localScale = Vector3.Lerp(lastScale, Vector3.one * _size, elapsed);
                _cameraOffset.y = Mathf.Lerp(lastCameraOffset, _baseCameraOffset + (_size - _baseSize) * 2, elapsed);
                _orbitalController.Radius = Mathf.Lerp(lastOrbitRadiusOffset, _baseOrbitRadiusOffset + (_size - _baseSize), elapsed);
                _mainCameraConstraint.SetTranslationOffset(0, _cameraOffset);

                yield return null;
            }

            transform.localScale = Vector3.one * _size;
            _growCoroutine = null;
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
            parentConstraint.SetTranslationOffset(0,  _snowballRollTransform.InverseTransformPoint(item.transform.position));
            parentConstraint.constraintActive = true;

            _growthEndTime = Time.time + GrowthSpeed;
            _size += item.GrowthAmount;

            if (_growCoroutine == null)
                _growCoroutine = StartCoroutine(Grow());
        }

        #region PhysicEvents
        private void OnCollisionEnter(Collision other)
        {
            var item = other.gameObject.GetComponentInParent<PickableItem>();

            if (item && _size > item.PickSize)
                PickUpItem(item);
        }
        #endregion PhysicEvents
    }
}
