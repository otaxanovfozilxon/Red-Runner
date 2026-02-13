using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using BayatGames.SaveGameFree;
using BayatGames.SaveGameFree.Serializers;

using RedRunner.Characters;
using RedRunner.Collectables;
using RedRunner.TerrainGeneration;
using RedRunner.UI;

namespace RedRunner
{
    public sealed class GameManager : MonoBehaviour
    {
        public delegate void AudioEnabledHandler(bool active);

        public delegate void ScoreHandler(float newScore, float highScore, float lastScore);

        public delegate void ResetHandler();

        public static event ResetHandler OnReset;
        public static event ScoreHandler OnScoreChanged;
        public static event AudioEnabledHandler OnAudioEnabled;
        public delegate void LifeHandler(int lives);
        public static event LifeHandler OnLifeChanged;

        private static GameManager m_Singleton;

        public static GameManager Singleton
        {
            get
            {
                return m_Singleton;
            }
        }

        [SerializeField]
        private Character m_MainCharacter;
        [SerializeField]
        [TextArea(3, 30)]
        private string m_ShareText;
        [SerializeField]
        private string m_ShareUrl;
        [SerializeField]
        private int m_Lives = 3;
        private float m_HighScore = 0f;
        private float m_LastScore = 0f;
        private float m_Score = 0f;
        private Vector3 m_LastCheckpointPosition;
        private bool m_HasCheckpoint = false;
        private int m_LevelCount = 0;
        [SerializeField]
        private GameObject m_LeaderboardPanel;

        private bool m_GameStarted = false;
        private bool m_GameRunning = false;
        private bool m_AudioEnabled = true;

        /// <summary>
        /// This is my developed callbacks compoents, because callbacks are so dangerous to use we need something that automate the sub/unsub to functions
        /// with this in-house developed callbacks feature, we garantee that the callback will be removed when we don't need it.
        /// </summary>
        public Property<int> m_Coin = new Property<int>(0);


        #region Getters
        public int Lives
        {
            get
            {
                return m_Lives;
            }
        }
        public bool gameStarted
        {
            get
            {
                return m_GameStarted;
            }
        }

        public bool gameRunning
        {
            get
            {
                return m_GameRunning;
            }
        }

        public bool audioEnabled
        {
            get
            {
                return m_AudioEnabled;
            }
        }

        public float Score
        {
            get
            {
                return m_Score;
            }
        }
        #endregion

        void Awake()
        {
            if (m_Singleton != null)
            {
                Destroy(gameObject);
                return;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveGame.Serializer = new SaveGameJsonSerializer();
#else
            SaveGame.Serializer = new SaveGameBinarySerializer();
#endif
            m_Singleton = this;
            m_Score = 0f;
            m_Lives = 3;

            m_Lives = 3;
            m_Coin.Value = 0;
            if (SaveGame.Exists("audioEnabled"))
            {
                SetAudioEnabled(SaveGame.Load<bool>("audioEnabled"));
            }
            else
            {
                SetAudioEnabled(true);
            }
            if (SaveGame.Exists("lastScore"))
            {
                m_LastScore = SaveGame.Load<float>("lastScore");
            }
            else
            {
                m_LastScore = 0f;
            }
            if (SaveGame.Exists("highScore"))
            {
                m_HighScore = SaveGame.Load<float>("highScore");
            }
            else
            {
                m_HighScore = 0f;
            }

        }

        void UpdateDeathEvent(bool isDead)
        {
            if (isDead)
            {
                m_Lives--;
                if (OnLifeChanged != null)
                {
                    OnLifeChanged(m_Lives);
                }

                if (m_Lives > 0)
                {
                    Vector3 respawnPos;
                    if (m_HasCheckpoint)
                    {
                        respawnPos = new Vector3(m_LastCheckpointPosition.x, m_LastCheckpointPosition.y + 10f, m_MainCharacter.transform.position.z);
                    }
                    else
                    {
                        // Custom spawn point if no checkpoint touched yet
                        respawnPos = new Vector3(7.24f, 12.59f, m_MainCharacter.transform.position.z);
                    }
                    TerrainGeneration.TerrainGenerator.Singleton.ResetPathFollowers();
                    StartCoroutine(RespawnCrt(respawnPos));
                }
                else
                {
                    StartCoroutine(DeathCrt());
                }
            }
            else
            {
                StopCoroutine("DeathCrt");
                StopCoroutine("RespawnCrt");
            }
        }

        IEnumerator RespawnCrt(Vector3 respawnPosition)
        {
            yield return new WaitForSeconds(1f);
            RespawnMainCharacter(respawnPosition);
        }

        IEnumerator DeathCrt()
        {
            m_LastScore = m_Score;
            if (m_Score > m_HighScore)
            {
                m_HighScore = m_Score;
            }
            if (OnScoreChanged != null)
            {
                OnScoreChanged(m_Score, m_HighScore, m_LastScore);
            }

            yield return new WaitForSecondsRealtime(1.5f);

            EndGame();

            // Track level end and show leaderboard
            if (LuxoddIntegrationManager.Singleton != null && LuxoddIntegrationManager.Singleton.IsConnected)
            {
                LuxoddIntegrationManager.Singleton.TrackLevelEnd(m_LevelCount, (int)m_Score, () => {
                    LuxoddIntegrationManager.Singleton.RefreshBalance();
                    if (m_LeaderboardPanel != null)
                    {
                        m_LeaderboardPanel.SetActive(true);
                    }
                    // After 5 seconds of leaderboard, request transaction
                    StartCoroutine(RequestTransactionAfterLeaderboard());
                });
            }
            else
            {
                // Not connected — show leaderboard then end screen
                if (m_LeaderboardPanel != null)
                {
                    m_LeaderboardPanel.SetActive(true);
                }
                ShowEndScreen();
            }
        }

        IEnumerator RequestTransactionAfterLeaderboard()
        {
            // Show leaderboard for 5 seconds
            yield return new WaitForSecondsRealtime(5f);

            // Hide leaderboard
            if (m_LeaderboardPanel != null)
            {
                m_LeaderboardPanel.SetActive(false);
            }

            // Request session continue from Luxodd (keep score, checkpoint — system handles billing)
            LuxoddIntegrationManager.Singleton.RequestSessionContinue(
                () =>
                {
                    // Allowed — respawn at last checkpoint, restore lives, keep score
                    ContinueAfterTransaction();
                },
                () =>
                {
                    // Denied — end session and return to arcade menu
                    Debug.LogWarning("[Luxodd] Continue denied - ending session");
                    LuxoddIntegrationManager.Singleton.EndSessionAndReturnToSystem();
                }
            );
        }

        void ContinueAfterTransaction()
        {
            StartCoroutine(ContinueAfterTransactionCrt());
        }

        IEnumerator ContinueAfterTransactionCrt()
        {
            // Resume time first so physics and camera work
            m_GameStarted = true;
            ResumeGame();

            // Restore lives but keep score and checkpoint
            m_Lives = 3;
            if (OnLifeChanged != null)
            {
                OnLifeChanged(m_Lives);
            }

            // Determine respawn position (same as normal respawn in UpdateDeathEvent)
            Vector3 respawnPos;
            if (m_HasCheckpoint)
            {
                respawnPos = new Vector3(m_LastCheckpointPosition.x, m_LastCheckpointPosition.y + 10f, m_MainCharacter.transform.position.z);
            }
            else
            {
                respawnPos = new Vector3(7.24f, 12.59f, m_MainCharacter.transform.position.z);
            }

            // Reset path followers (same as normal respawn)
            TerrainGeneration.TerrainGenerator.Singleton.ResetPathFollowers();

            // Switch to in-game screen first
            var ingameScreen = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
            if (ingameScreen != null)
            {
                UIManager.Singleton.OpenScreen(ingameScreen);
            }

            // Reset camera: stop fast-move, snap to respawn position
            if (Utilities.CameraController.Singleton != null)
            {
                Utilities.CameraController.Singleton.fastMove = false;
                var cam = Utilities.CameraController.Singleton.transform;
                cam.position = new Vector3(respawnPos.x, respawnPos.y, cam.position.z);
            }

            // Wait 1 second before respawn (same delay as normal RespawnCrt)
            yield return new WaitForSeconds(1f);

            // Respawn character at checkpoint
            RespawnMainCharacter(respawnPos);

            if (LuxoddIntegrationManager.Singleton != null)
            {
                LuxoddIntegrationManager.Singleton.TrackLevelBegin(m_LevelCount);
            }
        }

        void ShowEndScreen()
        {
            if (UIManager.Singleton != null && UIManager.Singleton.UISCREENS != null)
            {
                UIScreen endScreen = null;
                foreach (var screen in UIManager.Singleton.UISCREENS)
                {
                    if (screen != null && screen.ScreenInfo == UIScreenInfo.END_SCREEN)
                    {
                        endScreen = screen;
                        break;
                    }
                }

                if (endScreen != null)
                {
                    UIManager.Singleton.OpenScreen(endScreen);
                }
            }
        }

        private void Start()
        {
            m_MainCharacter.IsDead.AddEventAndFire(UpdateDeathEvent, this);
            m_Coin.AddEventAndFire(OnCoinChanged, this);
            Init();
        }

        void OnCoinChanged(int coin)
        {
            UpdateScore();
        }

        void UpdateScore()
        {
            m_Score = (float)m_Coin.Value;
            if (OnScoreChanged != null)
            {
                OnScoreChanged(m_Score, m_HighScore, m_LastScore);
            }
        }

        public void Init()
        {
            EndGame();
            UIManager.Singleton.Init();
            StartCoroutine(Load());
        }

        void Update()
        {
            if (m_GameRunning)
            {
                
            }
        }

        IEnumerator Load()
        {
            yield return new WaitForSecondsRealtime(3f);

            // Wait for terrain block prefabs to finish pre-loading across frames
            if (TerrainGeneration.TerrainGenerator.Singleton != null)
            {
                while (!TerrainGeneration.TerrainGenerator.Singleton.IsPreLoaded)
                    yield return null;
            }

            if (UIManager.Singleton != null && UIManager.Singleton.UISCREENS != null)
            {
                UIScreen startScreen = null;
                foreach (var screen in UIManager.Singleton.UISCREENS)
                {
                    if (screen != null && screen.ScreenInfo == UIScreenInfo.START_SCREEN)
                    {
                        startScreen = screen;
                        break;
                    }
                }
                
                if (startScreen != null)
                {
                    UIManager.Singleton.OpenScreen(startScreen);
                }
            }
        }

        void OnApplicationQuit()
        {
            if (m_Score > m_HighScore)
            {
                m_HighScore = m_Score;
            }
            SaveGame.Save<float>("lastScore", m_Score);
            SaveGame.Save<float>("highScore", m_HighScore);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void ToggleAudioEnabled()
        {
            SetAudioEnabled(!m_AudioEnabled);
        }

        public void SetAudioEnabled(bool active)
        {
            m_AudioEnabled = active;
            AudioListener.volume = active ? 1f : 0f;
            if (OnAudioEnabled != null)
            {
                OnAudioEnabled(active);
            }
        }

        public void StartGame()
        {
            m_GameStarted = true;
            if (LuxoddIntegrationManager.Singleton != null)
            {
                LuxoddIntegrationManager.Singleton.TrackLevelBegin(m_LevelCount);
            }
            ResumeGame();
        }

        public void StopGame()
        {
            m_GameRunning = false;
            // Force-stop ragdoll bone simulation BEFORE timeScale=0.
            // Without this, bones stay simulated=true at frozen physics,
            // causing infinite "Invalid worldAABB" errors every frame (Left Hand etc.)
            if (m_MainCharacter != null && m_MainCharacter.Skeleton != null)
            {
                m_MainCharacter.Skeleton.ForceStopSimulation();
            }
            // CRITICAL FOR WEBGL: Stop() all audio sources to fully release WebGL channels
            // BEFORE setting timeScale to 0. Pause() keeps channels alive, and WebGL still
            // calls JS_Sound_SetPitch on all alive channels when timeScale changes — causing
            // 86+ errors and freezing when pitch drops below the browser minimum (0.0625).
            StopAllAudioSources();
            AudioListener.pause = true;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            m_GameRunning = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            // Restart music since StopGame uses Stop() which releases the channel
            if (AudioManager.Singleton != null)
            {
                AudioManager.Singleton.PlayMusic();
            }
        }

        private void StopAllAudioSources()
        {
            // Use Stop() instead of Pause() — Stop() releases the WebGL audio channel,
            // while Pause() keeps it alive and vulnerable to JS_Sound_SetPitch errors.
            var sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var source in sources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }

        public void EndGame()
        {
            m_GameStarted = false;
            StopGame();
        }

        public void RespawnMainCharacter(Vector3 position)
        {
            RespawnCharacter(m_MainCharacter, position);
        }

        public void RespawnCharacter(Character character, Vector3 position)
        {
            character.transform.position = position;
            character.Reset();
        }

        public void Reset()
        {
            m_Coin.Value = 0;
            m_Score = 0f;
            m_Lives = 3;
            m_HasCheckpoint = false;
            m_LevelCount = 0; // Reset level count
            if (m_LeaderboardPanel != null)
            {
                m_LeaderboardPanel.SetActive(false);
            }
            if (OnLifeChanged != null)
            {
                OnLifeChanged(m_Lives);
            }
            if (OnReset != null)
            {
                OnReset();
            }
        }

        public void SetCheckpoint(Vector3 position)
        {
            m_LastCheckpointPosition = position;
            m_HasCheckpoint = true;
            m_LevelCount++;
            if (UI.LevelCompletionNotifier.Instance != null)
            {
                UI.LevelCompletionNotifier.Instance.ShowLevelComplete(m_LevelCount);
            }
        }

        public void Share(string url)
        {
            Application.OpenURL(string.Format(url, m_ShareText, m_ShareUrl));
        }

        [System.Serializable]
        public class LoadEvent : UnityEvent
        {

        }

    }

}