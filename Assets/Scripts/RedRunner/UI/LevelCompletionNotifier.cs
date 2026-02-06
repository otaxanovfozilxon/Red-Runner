using System.Collections;
using UnityEngine;
using TMPro;

namespace RedRunner.UI
{
    public class LevelCompletionNotifier : MonoBehaviour
    {
        public static LevelCompletionNotifier Instance { get; private set; }

        [Header("References")]
        [SerializeField]
        private TextMeshProUGUI m_NotificationText;

        [Header("Settings")]
        [SerializeField]
        private float m_DisplayDuration = 3f;
        [SerializeField]
        private float m_FadeDuration = 0.5f;

        private Coroutine m_NotificationRoutine;

        private void Awake()
        {
            // Scene-only Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (m_NotificationText == null)
            {
                m_NotificationText = GetComponent<TextMeshProUGUI>();
            }

            // Ensure text starts invisible
            if (m_NotificationText != null)
            {
                Color c = m_NotificationText.color;
                c.a = 0f;
                m_NotificationText.color = c;
            }
        }

        public void ShowLevelComplete(int levelNumber)
        {
            if (m_NotificationText == null)
                return;

            if (m_NotificationRoutine != null)
            {
                StopCoroutine(m_NotificationRoutine);
            }

            m_NotificationRoutine = StartCoroutine(
                ShowNotificationRoutine(levelNumber)
            );
        }

        private IEnumerator ShowNotificationRoutine(int levelNumber)
        {
            // Update text with leading zero
            m_NotificationText.text = $"You beat Level {levelNumber:00}";

            // Fade In
            yield return StartCoroutine(
                FadeText(0f, 1f, m_FadeDuration)
            );

            // Wait (Realtime, ignores timeScale)
            yield return new WaitForSecondsRealtime(m_DisplayDuration);

            // Fade Out
            yield return StartCoroutine(
                FadeText(1f, 0f, m_FadeDuration)
            );

            m_NotificationRoutine = null;
        }

        private IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;

            Color color = m_NotificationText.color;
            color.a = startAlpha;
            m_NotificationText.color = color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Color c = m_NotificationText.color;
                c.a = Mathf.Lerp(startAlpha, endAlpha, t);
                m_NotificationText.color = c;

                yield return null;
            }

            // Ensure final alpha
            Color finalColor = m_NotificationText.color;
            finalColor.a = endAlpha;
            m_NotificationText.color = finalColor;
        }
    }
}
