using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Luxodd.Game;
using Luxodd.Game.Scripts.Input;

namespace RedRunner.UI
{
    public class StartScreen : UIScreen
    {
        [SerializeField]
        protected Button PlayButton = null;
        [SerializeField]
        protected Button HelpButton = null;
        [SerializeField]
        protected Button InfoButton = null;
        [SerializeField]
        protected Button ExitButton = null;
        [SerializeField]
        protected Text m_CountdownText = null;

        private const float AutoStartDelay = 30f;
        private float m_AutoStartTimer;

        private void Start()
        {
            PlayButton.SetButtonAction(() =>
            {
                LaunchGame();
            });


            if (ExitButton != null)
            {
                ExitButton.SetButtonAction(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }

            FixTextRaycast();
        }

        private void LaunchGame()
        {
            if (AudioManager.Singleton != null)
            {
                AudioManager.Singleton.StopMenuMusic();
            }
            var uiManager = UIManager.Singleton;
            if (uiManager != null && uiManager.UISCREENS != null)
            {
                UIScreen inGameScreen = null;
                foreach (var screen in uiManager.UISCREENS)
                {
                    if (screen != null && screen.ScreenInfo == UIScreenInfo.IN_GAME_SCREEN)
                    {
                        inGameScreen = screen;
                        break;
                    }
                }

                if (inGameScreen != null)
                {
                    uiManager.OpenScreen(inGameScreen);
                    GameManager.Singleton.StartGame();
                }
            }
        }

        private void FixTextRaycast()
        {
            var texts = GetComponentsInChildren<Text>(true);
            if (texts != null)
            {
                foreach (var text in texts)
                {
                    if (text != null && text.GetComponentInParent<Button>() == null)
                    {
                        text.raycastTarget = false;
                    }
                }
            }
        }
        private void Update()
        {
            if (!IsOpen)
                return;

            // Allow any arcade button to start the game from the start screen
            if (ArcadeControls.GetButtonDown(ArcadeButtonColor.Red) ||
                ArcadeControls.GetButtonDown(ArcadeButtonColor.Black))
            {
                LaunchGame();
                return;
            }

            // Auto-start the game after 30 seconds of inactivity
            m_AutoStartTimer += Time.unscaledDeltaTime;
            if (m_CountdownText != null)
            {
                int remaining = Mathf.CeilToInt(AutoStartDelay - m_AutoStartTimer);
                if (remaining < 0) remaining = 0;
                m_CountdownText.text = "The game starts in " + remaining + "s";
            }
            if (m_AutoStartTimer >= AutoStartDelay)
            {
                LaunchGame();
            }
        }

        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
            if (open)
            {
                m_AutoStartTimer = 0f;
                if (m_CountdownText != null)
                {
                    m_CountdownText.gameObject.SetActive(true);
                    m_CountdownText.text = "Game starts in 30s";
                }
                if (AudioManager.Singleton != null)
                {
                    AudioManager.Singleton.PlayMenuMusic();
                }
            }
            else
            {
                if (m_CountdownText != null)
                {
                    m_CountdownText.gameObject.SetActive(false);
                }
            }
        }
    }
}