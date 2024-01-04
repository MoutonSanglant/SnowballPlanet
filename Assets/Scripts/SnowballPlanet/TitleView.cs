using UnityEngine;

namespace SnowballPlanet
{
    public class TitleView : MonoBehaviour
    {
        public void StartGame()
        {
            SceneTransitionManager.LoadScene(SceneTransitionManager.Scene.Game);
        }
    }
}
