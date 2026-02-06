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
                var InGameScreen = uiManager.UISCREENS.Find(el => el.ScreenInfo == UIScreenInfo.IN_GAME_SCREEN);
                if (InGameScreen != null)
                {
                    uiManager.OpenScreen(InGameScreen);
                    GameManager.Singleton.StartGame();
                }
            });

            ExitButton.SetButtonAction(() =>
            {
                GameManager.Singleton.ExitGame();
            });

            FixTextRaycast();
        }

        private void FixTextRaycast()
        {
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text.GetComponentInParent<Button>() == null)
                {
                    text.raycastTarget = false;
                }
            }
        }
        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
        }
    }
}