using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
                var uiManager = UIManager.Singleton;
                if (uiManager != null && uiManager.UISCREENS != null)
                {
                    UIScreen InGameScreen = null;
                    foreach (var screen in uiManager.UISCREENS)
                    {
                        if (screen != null && screen.ScreenInfo == UIScreenInfo.IN_GAME_SCREEN)
                        {
                            InGameScreen = screen;
                            break;
                        }
                    }
                    
                    if (InGameScreen != null)
                    {
                        uiManager.OpenScreen(InGameScreen);
                        GameManager.Singleton.StartGame();
                    }
                }
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
        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
        }
    }
}