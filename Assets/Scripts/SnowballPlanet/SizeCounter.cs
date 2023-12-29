using TMPro;
using UnityEngine;

namespace SnowballPlanet
{
    public class SizeCounter : MonoBehaviour
    {
        [SerializeField] private SnowballController Controller;
        [SerializeField] private TMP_Text SizeCounterLabel;

        private void Awake()
        {
            Controller.OnSnowballGrow += OnSizeChange;
        }

        private void OnSizeChange(float size)
        {
            size *= 2f;
            SizeCounterLabel.text = size > 1f ? size.ToString("0.00")  + " m": (size * 100).ToString("0") + " cm";
        }
    }
}
