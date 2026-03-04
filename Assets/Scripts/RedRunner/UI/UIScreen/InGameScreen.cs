using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RedRunner.Characters;

namespace RedRunner.UI
{
    public class InGameScreen : UIScreen
    {
        [SerializeField]
        protected Text m_TimerText = null;

        [SerializeField]
        protected Text m_IdleWarningText = null;

        [SerializeField]
        protected Text m_CameraWarningText = null;

        private float m_ElapsedTime;
        private RedCharacter m_CachedCharacter;

        private void Start()
        {

        }

        private void Update()
        {
            if (!IsOpen)
                return;

            m_ElapsedTime += Time.deltaTime;
            int totalSeconds = Mathf.FloorToInt(m_ElapsedTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            if (m_TimerText != null)
            {
                m_TimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            // Show "Move or Die" countdown when 10s remain before idle death
            if (m_IdleWarningText != null)
            {
                if (m_CachedCharacter == null)
                    m_CachedCharacter = FindObjectOfType<RedCharacter>();

                if (m_CachedCharacter != null && m_CachedCharacter.IdleCountdown >= 0f)
                {
                    int remaining = Mathf.CeilToInt(m_CachedCharacter.IdleCountdown);
                    m_IdleWarningText.text = "Move or Die " + remaining + "s";
                    m_IdleWarningText.gameObject.SetActive(true);
                }
                else
                {
                    m_IdleWarningText.gameObject.SetActive(false);
                }
            }

            // Show "The camera moves in Xs" countdown when 15s remain
            if (m_CameraWarningText != null)
            {
                var cam = Utilities.CameraController.Singleton;
                if (cam != null && cam.CameraMoveCountdown >= 0f)
                {
                    int remaining = Mathf.CeilToInt(cam.CameraMoveCountdown);
                    m_CameraWarningText.text = "The camera moves in " + remaining + "s";
                    m_CameraWarningText.gameObject.SetActive(true);
                }
                else
                {
                    m_CameraWarningText.gameObject.SetActive(false);
                }
            }
        }

        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
            if (open)
            {
                m_ElapsedTime = 0f;
                if (m_TimerText != null)
                {
                    m_TimerText.text = "00:00";
                }
                if (m_IdleWarningText != null)
                {
                    m_IdleWarningText.gameObject.SetActive(false);
                }
                if (m_CameraWarningText != null)
                {
                    m_CameraWarningText.gameObject.SetActive(false);
                }
            }
        }
    }

}