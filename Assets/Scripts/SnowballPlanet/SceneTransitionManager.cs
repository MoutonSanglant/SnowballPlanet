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
        [SerializeField] private bool Preload;
        [SerializeField] private float PreloadDuration;

        private static readonly int PreloadAnimatorParameter = Animator.StringToHash("Preload");
        private static readonly int FadeInAnimatorParameter = Animator.StringToHash("FadeIn");
        private static readonly int FadeOutAnimatorParameter = Animator.StringToHash("FadeOut");
        private static readonly int SpeedAnimatorParameter = Animator.StringToHash("Speed");
        private static SceneTransitionManager _instance;

        private AudioSource _audioSource;
        private Animator _animator;

        private void Awake()
        {
            _instance = this;
            _audioSource = GetComponent<AudioSource>();
            _animator = ScreenTransition.GetComponent<Animator>();

            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (Preload)
                yield return new WaitForSeconds(PreloadDuration);

            SceneManager.sceneLoaded += (_, _) =>
            {
                StartCoroutine(Fade(-1f, Scene.None));
            };

            yield return Fade(-1f, Scene.None);
        }

        public static void LoadScene(Scene scene)
        {
            _instance.StartCoroutine(_instance.Fade(1f, scene));
        }

        private IEnumerator Fade(float speed, Scene scene)
        {
            ScreenTransition.gameObject.SetActive(true);

            _audioSource.Play();
            _animator.Play(FadeInAnimatorParameter, 0, speed > 0 ? 0f : 1f);
            _animator.SetFloat(SpeedAnimatorParameter, speed);

            var animationState = _animator.GetCurrentAnimatorStateInfo(0);
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
