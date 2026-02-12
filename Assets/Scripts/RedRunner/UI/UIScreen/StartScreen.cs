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
            // Allow any arcade button to start the game from the start screen
            if (IsOpen && (ArcadeControls.GetButtonDown(ArcadeButtonColor.Red) ||
                           ArcadeControls.GetButtonDown(ArcadeButtonColor.Black)))
            {
                LaunchGame();
            }
        }

        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
        }
    }
}