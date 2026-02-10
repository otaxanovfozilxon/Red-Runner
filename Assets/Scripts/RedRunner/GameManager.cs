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
            if (LuxoddIntegrationManager.Singleton != null)
            {
                LuxoddIntegrationManager.Singleton.TrackLevelEnd(m_LevelCount, (int)m_Score, () => {
                    if (m_LeaderboardPanel != null)
                    {
                        m_LeaderboardPanel.SetActive(true);
                    }
                });
            }
            else
            {
                if (m_LeaderboardPanel != null)
                {
                    m_LeaderboardPanel.SetActive(true);
                }
            }
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
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            m_GameRunning = true;
            Time.timeScale = 1f;
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