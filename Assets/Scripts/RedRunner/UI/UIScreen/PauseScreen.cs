using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RedRunner.UI
{
    public class PauseScreen : UIScreen
    {
        [SerializeField]
        protected Button ResumeButton = null;
        [SerializeField]
        protected Button HomeButton = null;
        [SerializeField]
        protected Button SoundButton = null;
        [SerializeField]
        protected Button ExitButton = null;

        private void Start()
        {
            ResumeButton.SetButtonAction(() =>
            {
                if (UIManager.Singleton != null && UIManager.Singleton.UISCREENS != null)
                {
                    UIScreen inGameScreen = null;
                    foreach (var screen in UIManager.Singleton.UISCREENS)
                    {
                        if (screen != null && screen.ScreenInfo == UIScreenInfo.IN_GAME_SCREEN)
                        {
                            inGameScreen = screen;
                            break;
                        }
                    }
                    
                    if (inGameScreen != null)
                    {
                        UIManager.Singleton.OpenScreen(inGameScreen);
                        GameManager.Singleton.StartGame();
                    }
                }
            });

            HomeButton.SetButtonAction(() =>
            {
                GameManager.Singleton.Init();
            });
        }

        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
        }
    }
}
