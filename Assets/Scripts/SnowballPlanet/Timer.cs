using System;
using TMPro;
using UnityEngine;

namespace SnowballPlanet
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] private TMP_Text TimerLabel;

        private float _startTime;

        private void Awake()
        {
            _startTime = Time.time;

            SnowballController.OnVictory += DisableComponent;
        }

        private void OnDestroy()
        {
            SnowballController.OnVictory -= DisableComponent;
        }

        private void Update()
        {
            var elapsed = Time.time - _startTime;
            var seconds = (Mathf.Floor(elapsed) % 60f).ToString("00");
            var minutes = Mathf.Floor(elapsed / 60f).ToString("00");

            TimerLabel.text = $"{minutes}:{seconds}";
        }

        private void DisableComponent()
        {
            enabled = false;
        }
    }
}
