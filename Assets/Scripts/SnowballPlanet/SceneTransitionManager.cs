using System;
using System.Collections;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowballPlanet
{
    [RequireComponent(typeof(AudioSource))]
    public class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private Transform ScreenTransition;
        [SerializeField] private SceneReference Title;
        [SerializeField] private SceneReference Game;
        [SerializeField] private SceneReference Credits;

        private static SceneTransitionManager _instance;
        private static readonly int SpeedAnimatorParameter = Animator.StringToHash("Speed");

        private AudioSource _audioSource;

        private void Awake()
        {
            _instance = this;
            _audioSource = GetComponent<AudioSource>();

            SceneManager.sceneLoaded += (_, _) =>
            {
                StartCoroutine(Fade(-1f, Scene.None));
            };

            DontDestroyOnLoad(gameObject);
        }

        public static void LoadScene(Scene scene)
        {
            _instance.StartCoroutine(_instance.Fade(1f, scene));
        }

        private IEnumerator Fade(float speed, Scene scene)
        {
            _audioSource.Play();

            ScreenTransition.gameObject.SetActive(true);
            ScreenTransition.GetComponent<Animator>().SetFloat(SpeedAnimatorParameter, speed);

            var animationState = ScreenTransition.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            var animationLength = animationState.length;

            yield return new WaitForSeconds(animationLength);

            if (scene != Scene.None)
                SceneManager.LoadScene(GetSceneIndex(scene).BuildIndex);
            else
                ScreenTransition.gameObject.SetActive(false);
        }

        private static SceneReference GetSceneIndex(Scene scene)
        {
            return scene switch
            {
                Scene.Title => _instance.Title,
                Scene.Game => _instance.Game,
                Scene.Credits => _instance.Credits,
                _ => _instance.Title
            };
        }

        public enum Scene
        {
            None,
            Title,
            Game,
            Credits,
        }

    }
}
