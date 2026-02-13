using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedRunner.UI;
using System.Linq;

namespace RedRunner
{
    public enum UIScreenInfo
    {
        LOADING_SCREEN,
        START_SCREEN,
        END_SCREEN,
        PAUSE_SCREEN,
        IN_GAME_SCREEN
    }

    public class UIManager : MonoBehaviour
    {

        private static UIManager m_Singleton;

        public static UIManager Singleton
        {
            get
            {
                return m_Singleton;
            }
        }

        [SerializeField]
        private List<UIScreen> m_Screens = new List<UIScreen>();
        private UIScreen m_ActiveScreen;
        private UIWindow m_ActiveWindow;
		[SerializeField]
		private Texture2D m_CursorDefaultTexture;
		[SerializeField]
		private Texture2D m_CursorClickTexture;

        [SerializeField] private float m_CursorHideDelay = 3f;

        // The cursor timer, which will keep track of how long the cursor has been inactive
        private float cursorTimer;
        // The cursor position, we will use this to compare the cursor position to check if it has moved.
        private Vector3 lastCursorPosition;

        public List<UIScreen> UISCREENS
        {
            get
            {
                return m_Screens;
            }
        }

        public UIScreen GetUIScreen(UIScreenInfo screenInfo)
        {
            if (m_Screens == null)
                return null;
            
            foreach (var screen in m_Screens)
            {
                if (screen != null && screen.ScreenInfo == screenInfo)
                {
                    return screen;
                }
            }
            return null;
        }

        void Awake()
        {
            if (m_Singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            m_Singleton = this;
            Cursor.SetCursor(m_CursorDefaultTexture, Vector2.zero, CursorMode.Auto);
        }

        void Start()
        {
            Init();
        }

        public void Init()
        {
            var loadingScreen = GetUIScreen(UIScreenInfo.LOADING_SCREEN);
            OpenScreen(loadingScreen);
        }

        void Update()
        {
            // ESC / pause menu disabled

            if (Input.GetMouseButtonDown(0))
            {
                // If the player clicks, also let the cursor be visible
                Cursor.visible = true;
                cursorTimer = 0;
                Cursor.SetCursor(m_CursorClickTexture, Vector2.zero, CursorMode.Auto);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Cursor.SetCursor(m_CursorDefaultTexture, Vector2.zero, CursorMode.Auto);
            }


            // CURSOR VISIBILITY CODE
            // Check if the cursor has moved
            // If it hasn't moved, add the time between frames to the timer using DeltaTime

            if (lastCursorPosition == Input.mousePosition)
            {
                // Check if the timer is lower than the threshold (M_cursorhidedelay in this case)
                if (cursorTimer < m_CursorHideDelay)
                {
                    // Add the time difference between frames to the counter
                    cursorTimer += Time.deltaTime;
                }
                // If we crossed the timer, set the cursor visibility to false.
                else
                {
                    Cursor.visible = false;
                }
            }
            // If the cursor did move, set it to visible and reset the timer 
            else
            {
                Cursor.visible = true;
                cursorTimer = 0;
            }

            // finally, update the last cursor position
            lastCursorPosition = Input.mousePosition;


        }

        public void OpenWindow(UIWindow window)
        {
            window.Open();
            m_ActiveWindow = window;
        }

        public void CloseWindow(UIWindow window)
        {
            if (m_ActiveWindow == window)
            {
                m_ActiveWindow = null;
            }
            window.Close();
        }

        public void CloseActiveWindow()
        {
            if (m_ActiveWindow != null)
            {
                CloseWindow(m_ActiveWindow);
            }
        }

        public void OpenScreen(UIScreen screen)
        {
            if (screen == null) return;
            CloseAllScreens();
            screen.UpdateScreenStatus(true);
            m_ActiveScreen = screen;

            // Set default selected button for arcade navigation
            var button = screen.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        public void CloseScreen(UIScreen screen)
        {
            if (screen == null) return;
            if (m_ActiveScreen == screen)
            {
                m_ActiveScreen = null;
            }
            screen.UpdateScreenStatus(false);
        }

        public void CloseAllScreens()
        {
            if (m_Screens == null)
                return;
            foreach (var screen in m_Screens)
                CloseScreen(screen);
        }

        bool IsAsScreenOpen()
        {
            if (m_Screens == null)
                return false;
            foreach (var screen in m_Screens)
            {
                if (screen.IsOpen)
                    return true;
            }

            return false;
        }
    }

}
