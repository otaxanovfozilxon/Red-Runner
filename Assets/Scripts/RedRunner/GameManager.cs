using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DiagLog(string msg);

        [DllImport("__Internal")]
        private static extern string GetDiagLog();

        [DllImport("__Internal")]
        private static extern void ClearDiagLog();

        [DllImport("__Internal")]
        private static extern void SaveGameState(string json);

        [DllImport("__Internal")]
        private static extern int HasSavedGameState();

        [DllImport("__Internal")]
        private static extern string GetSavedGameState();

        [DllImport("__Internal")]
        private static extern void ClearSavedGameState();
#else
        private static void DiagLog(string msg) { Debug.Log(msg); }
        private static string GetDiagLog() { return ""; }
        private static void ClearDiagLog() { }
        private static void SaveGameState(string json) { }
        private static int HasSavedGameState() { return 0; }
        private static string GetSavedGameState() { return ""; }
        private static void ClearSavedGameState() { }
#endif

        [Serializable]
        private class ContinueState
        {
            public int coins;
            public int levelCount;
            public bool valid;
        }

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
        private List<Vector3> m_Checkpoints = new List<Vector3>();
        private List<GameObject> m_Barriers = new List<GameObject>();
        private int m_LevelCount = 0;
        [SerializeField]
        private GameObject m_LeaderboardPanel;

        private bool m_GameStarted = false;
        private bool m_GameRunning = false;
        private bool m_AudioEnabled = true;
        private Vector3 m_DeathBlockPosition;
        private bool m_HasDeathBlock = false;

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

            // Print previous session diagnostics (survives page navigation)
            string prevDiag = GetDiagLog();
            if (!string.IsNullOrEmpty(prevDiag))
            {
                Debug.Log("=== PREVIOUS SESSION DIAGNOSTICS ===\n" + prevDiag + "\n=== END DIAGNOSTICS ===");
                ClearDiagLog();
            }
            DiagLog("[GM] Game Awake - new session started");

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

            // Check for saved continue state (from previous session where host removed iframe)
            m_HasContinueState = HasSavedGameState() == 1;
            if (m_HasContinueState)
            {
                string stateJson = GetSavedGameState();
                DiagLog("[GM] Found saved continue state: " + stateJson);
                m_SavedContinueState = JsonUtility.FromJson<ContinueState>(stateJson);
                ClearSavedGameState();
            }
        }

        private bool m_HasContinueState = false;
        private ContinueState m_SavedContinueState = null;

        void UpdateDeathEvent(bool isDead)
        {
            if (isDead)
            {
                // Save the block where the player died BEFORE ragdoll moves the body
                m_HasDeathBlock = false;
                if (TerrainGeneration.TerrainGenerator.Singleton != null)
                {
                    var deathBlock = TerrainGeneration.TerrainGenerator.Singleton.GetCharacterBlock();
                    if (deathBlock != null)
                    {
                        m_DeathBlockPosition = deathBlock.transform.position;
                        m_HasDeathBlock = true;
                        Debug.Log($"[GameManager] Death block saved at position: {m_DeathBlockPosition}");
                    }
                }

                if (AudioManager.Singleton != null)
                {
                    AudioManager.Singleton.PlayGameOverSound();
                }

                // Reset camera timer so it stops and waits the full duration again
                if (Utilities.CameraController.Singleton != null)
                {
                    Utilities.CameraController.Singleton.ResetCamera();
                }

                m_Lives--;
                if (OnLifeChanged != null)
                {
                    OnLifeChanged(m_Lives);
                }

                if (m_Lives > 0)
                {
                    Vector3 respawnPos = GetRespawnPosition();
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
                // Not connected — show leaderboard for 5 seconds then end screen
                if (m_LeaderboardPanel != null)
                {
                    m_LeaderboardPanel.SetActive(true);
                }
                StartCoroutine(ShowEndScreenAfterLeaderboard());
            }
        }

        IEnumerator ShowEndScreenAfterLeaderboard()
        {
            yield return new WaitForSecondsRealtime(5f);
            if (m_LeaderboardPanel != null)
            {
                m_LeaderboardPanel.SetActive(false);
            }
            ShowEndScreen();
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

            // SAVE game state BEFORE requesting transaction
            // (host may remove the iframe - state needs to survive in localStorage)
            var state = new ContinueState
            {
                coins = m_Coin.Value,
                levelCount = m_LevelCount,
                valid = true
            };
            string stateJson = JsonUtility.ToJson(state);
            SaveGameState(stateJson);
            DiagLog("[GM] State saved to localStorage: " + stateJson);

            // Request session continue from Luxodd (shows Continue/End popup)
            DiagLog("[GM] Requesting session continue from Luxodd...");
            LuxoddIntegrationManager.Singleton.RequestSessionContinue(
                () =>
                {
                    // Continue approved — resume the game in-place without exiting.
                    DiagLog("[GM] >>> onAllowed fired - continuing in-place");
                    ClearSavedGameState();
                    ContinueAfterTransaction();
                },
                () =>
                {
                    // Session denied — clear saved state and exit.
                    DiagLog("[GM] >>> onDenied fired - ending session");
                    ClearSavedGameState();
                    LuxoddIntegrationManager.Singleton.EndSessionAndReturnToSystem();
                }
            );
        }

        void ContinueAfterTransaction()
        {
            DiagLog("[GM] ContinueAfterTransaction - starting coroutine");
            StartCoroutine(ContinueAfterTransactionCrt());
        }

        IEnumerator ContinueAfterTransactionCrt()
        {
            DiagLog("[GM] ContinueAfterTransactionCrt STARTED");

            m_GameStarted = true;
            ResumeGame();
            DiagLog("[GM] Game resumed, timeScale=" + Time.timeScale);

            m_Lives = 3;
            if (OnLifeChanged != null)
            {
                OnLifeChanged(m_Lives);
            }

            Vector3 respawnPos;
            if (m_HasDeathBlock)
            {
                float playerZ = m_MainCharacter.transform.position.z;
                respawnPos = new Vector3(m_DeathBlockPosition.x + 10f, m_DeathBlockPosition.y + 12f, playerZ);
                DiagLog($"[GM] Block respawn at: {respawnPos}");
            }
            else
            {
                respawnPos = GetRespawnPosition();
                DiagLog($"[GM] Checkpoint respawn at: {respawnPos}");
            }

            if (TerrainGeneration.TerrainGenerator.Singleton != null)
            {
                TerrainGeneration.TerrainGenerator.Singleton.ResetPathFollowers();
            }

            if (UIManager.Singleton != null)
            {
                var ingameScreen = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
                if (ingameScreen != null)
                {
                    UIManager.Singleton.OpenScreen(ingameScreen);
                }
            }

            if (Utilities.CameraController.Singleton != null)
            {
                Utilities.CameraController.Singleton.fastMove = false;
                var cam = Utilities.CameraController.Singleton.transform;
                cam.position = new Vector3(respawnPos.x, respawnPos.y, cam.position.z);
            }

            DiagLog("[GM] Waiting 1s before respawn...");
            yield return new WaitForSeconds(1f);

            DiagLog("[GM] Respawning character now");
            RespawnMainCharacter(respawnPos);

            // NOW it's safe to clear the saved state — game is alive and running
            ClearSavedGameState();

            if (LuxoddIntegrationManager.Singleton != null)
            {
                LuxoddIntegrationManager.Singleton.TrackLevelBegin(m_LevelCount);
            }
            DiagLog("[GM] ContinueAfterTransactionCrt COMPLETED");
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

            // Check if we should auto-continue from a previous session
            // (host removed the iframe after Continue was pressed, game reloaded)
            if (m_HasContinueState && m_SavedContinueState != null && m_SavedContinueState.valid)
            {
                DiagLog("[GM] AUTO-CONTINUE: Restoring from saved state");
                m_HasContinueState = false;

                // Restore score and level count from saved state
                m_Coin.Value = m_SavedContinueState.coins;
                m_Score = (float)m_SavedContinueState.coins;
                m_LevelCount = m_SavedContinueState.levelCount;
                m_Lives = 3;

                if (OnScoreChanged != null)
                {
                    OnScoreChanged(m_Score, m_HighScore, m_LastScore);
                }
                if (OnLifeChanged != null)
                {
                    OnLifeChanged(m_Lives);
                }

                DiagLog($"[GM] AUTO-CONTINUE: coins={m_Coin.Value} level={m_LevelCount}");

                // Skip start screen — go directly to in-game
                var ingameScreen = UIManager.Singleton.GetUIScreen(UIScreenInfo.IN_GAME_SCREEN);
                if (ingameScreen != null)
                {
                    UIManager.Singleton.OpenScreen(ingameScreen);
                }

                // Start the game immediately
                StartGame();
                DiagLog("[GM] AUTO-CONTINUE: Game started");
                yield break;
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
            m_Checkpoints.Clear();
            foreach (var barrier in m_Barriers)
            {
                if (barrier != null) Destroy(barrier);
            }
            m_Barriers.Clear();
            m_LevelCount = 0; // Reset level count
            m_HasDeathBlock = false;
            ClearSavedGameState(); // Clear any stale continue state
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

        private Vector3 GetRespawnPosition()
        {
            float playerZ = m_MainCharacter.transform.position.z;

            if (m_Checkpoints.Count > 0)
            {
                Vector3 last = m_Checkpoints[m_Checkpoints.Count - 1];
                return new Vector3(last.x, last.y + 10f, playerZ);
            }

            return new Vector3(7.24f, 12.59f, playerZ);
        }

        public void AddBarrier(GameObject barrier)
        {
            m_Barriers.Add(barrier);
        }

        public void SetCheckpoint(Vector3 position)
        {
            m_Checkpoints.Add(position);
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