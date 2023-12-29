using System;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowballPlanet
{
    public class TitleView : MonoBehaviour
    {
        [SerializeField] private SceneReference GameScene;

        public void StartGame()
        {
            SceneManager.LoadScene(GameScene.BuildIndex);
        }

        private void Update()
        {
        }
    }
}
