using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RedRunner.Characters;

namespace RedRunner.UI
{
    public class ScreenFadeController : MonoBehaviour
    {
        public static ScreenFadeController Instance { get; private set; }

        [Header("References")]
        public Image fadeImage;  // BU MAYDONNI INSPECTOR'DA TO'LDIRING!

        [Header("Settings")]
        public float fadeDuration = 0.9f;
        public float holdDuration = 0.6f;

        private int _CenterID;
        private int _RadiusID;

        private Material m_Material;
        private RedCharacter m_Player;
        private bool m_PlayerSearchDone = false;

        private Coroutine m_FadeRoutine;

        private const float FULLY_OPEN_RADIUS = 1.6f;
        private const float FULLY_CLOSED_RADIUS = 0.0f;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _CenterID = Shader.PropertyToID("_Center");
            _RadiusID = Shader.PropertyToID("_Radius");

            if (fadeImage == null)
            {
                Debug.LogError("ScreenFadeController: Fade Image not assigned! Assign it in Inspector.");
                return;
            }

            m_Material = new Material(fadeImage.material);
            fadeImage.material = m_Material;

            SetRadius(FULLY_OPEN_RADIUS);
            fadeImage.color = new Color(0, 0, 0, 1);
            fadeImage.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (m_Material == null || fadeImage == null) return;

            UpdateCenterToPlayer();
        }

        private void UpdateCenterToPlayer()
        {
            if (m_Player == null)
            {
                if (m_PlayerSearchDone)
                {
                    m_Material.SetVector(_CenterID, new Vector4(0.5f, 0.5f, 0, 0));
                    return;
                }
                m_Player = FindFirstObjectByType<RedCharacter>();
                if (m_Player == null)
                {
                    m_PlayerSearchDone = true;
                    m_Material.SetVector(_CenterID, new Vector4(0.5f, 0.5f, 0, 0));
                    return;
                }
            }
            m_PlayerSearchDone = false;

            if (Camera.main != null)
            {
                Vector3 vp = Camera.main.WorldToViewportPoint(m_Player.transform.position);
                m_Material.SetVector(_CenterID, new Vector4(vp.x, vp.y, 0, 0));
            }
        }

        private void SetRadius(float radius)
        {
            if (m_Material != null)
                m_Material.SetFloat(_RadiusID, radius);
        }

        public void PlayCheckpointFade()
        {
            if (m_FadeRoutine != null)
                StopCoroutine(m_FadeRoutine);

            if (fadeImage == null || m_Material == null)
            {
                Debug.LogError("Cannot play fade: fadeImage or material is null!");
                return;
            }

            m_FadeRoutine = StartCoroutine(FadeSequence());
        }

        private IEnumerator FadeSequence()
        {
            if (m_Player != null)
                m_Player.IsInputBlocked = true;

            UpdateCenterToPlayer();

            yield return AnimateRadius(FULLY_OPEN_RADIUS, FULLY_CLOSED_RADIUS, fadeDuration);

            yield return new WaitForSecondsRealtime(holdDuration);

            yield return AnimateRadius(FULLY_CLOSED_RADIUS, FULLY_OPEN_RADIUS, fadeDuration);

            SetRadius(FULLY_OPEN_RADIUS);

            if (m_Player != null)
                m_Player.IsInputBlocked = false;

            m_FadeRoutine = null;
        }

        private IEnumerator AnimateRadius(float start, float end, float duration)
        {
            if (m_Material == null) yield break;

            float time = 0f;
            SetRadius(start);

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = time / duration;
                t = t * t * (3f - 2f * t);  // Smooth easing

                SetRadius(Mathf.Lerp(start, end, t));
                yield return null;
            }

            SetRadius(end);
        }

        [ContextMenu("Test Fade")]
        private void TestFade()
        {
            PlayCheckpointFade();
        }
    }
}