using System;
using UnityEngine;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using Luxodd.Game.Scripts.Game.Leaderboard;

namespace RedRunner
{
    public class LuxoddIntegrationManager : MonoBehaviour
    {
        private static LuxoddIntegrationManager m_Singleton;

        public static LuxoddIntegrationManager Singleton
        {
            get => m_Singleton;
        }

        [Header("Luxodd Services")]
        [SerializeField] private WebSocketService m_WebSocketService;
        [SerializeField] private HealthStatusCheckService m_HealthStatusCheckService;
        [SerializeField] private WebSocketCommandHandler m_WebSocketCommandHandler;

        private string m_PlayerName = "Guest";
        private float m_PlayerBalance = 0f;
        private bool m_IsConnected = false;
        private bool m_SessionActive = false;

        public string PlayerName => m_PlayerName;
        public float PlayerBalance => m_PlayerBalance;
        public bool IsConnected => m_IsConnected;
        public bool SessionActive => m_SessionActive;

        void Awake()
        {
            if (m_Singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            m_Singleton = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }

        void Start()
        {
            Connect();
        }

        public void Connect()
        {
            if (m_WebSocketService == null)
            {
                Debug.LogWarning("[Luxodd] WebSocketService is missing!");
                return;
            }

            m_WebSocketService.ConnectToServer(
                () => {
                    Debug.Log("[Luxodd] Connected to server successfully!");
                    m_IsConnected = true;
                    if (m_HealthStatusCheckService != null)
                    {
                        m_HealthStatusCheckService.Activate();
                    }
                    FetchPlayerData();
                },
                () => {
                    Debug.LogError("[Luxodd] Connecting to server failed!");
                    m_IsConnected = false;
                }
            );
        }

        private void FetchPlayerData()
        {
            if (m_WebSocketCommandHandler == null)
            {
                Debug.LogWarning("[Luxodd] WebSocketCommandHandler is null!");
                return;
            }
            
            m_WebSocketCommandHandler.SendProfileRequestCommand(
                (name) => {
                    m_PlayerName = name;
                    Debug.Log($"[Luxodd] Player Profile: {name}");
                },
                (code, msg) => Debug.LogError($"[Luxodd] Profile Error {code}: {msg}")
            );

            m_WebSocketCommandHandler.SendUserBalanceRequestCommand(
                (balance) => {
                    m_PlayerBalance = balance;
                    Debug.Log($"[Luxodd] Balance: {balance}");
                },
                (code, msg) => Debug.LogError($"[Luxodd] Balance Error {code}: {msg}")
            );
        }

        public void TrackLevelBegin(int levelNumber)
        {
            if (!m_IsConnected || m_WebSocketCommandHandler == null) return;
            m_WebSocketCommandHandler.SendLevelBeginRequestCommand(levelNumber,
                () => Debug.Log($"[Luxodd] Level {levelNumber} Begin Tracked"),
                (code, msg) => Debug.LogError($"[Luxodd] Level Begin Error {code}: {msg}")
            );
        }

        public void TrackLevelEnd(int levelNumber, int score, Action onSuccess = null)
        {
            if (!m_IsConnected)
            {
                onSuccess?.Invoke();
                return;
            }
            if (m_WebSocketCommandHandler == null)
            {
                Debug.LogWarning("[Luxodd] WebSocketCommandHandler is null!");
                onSuccess?.Invoke();
                return;
            }
            m_WebSocketCommandHandler.SendLevelEndRequestCommand(levelNumber, score,
                () => {
                    Debug.Log($"[Luxodd] Level {levelNumber} End Tracked. Score: {score}");
                    onSuccess?.Invoke();
                },
                (code, msg) => {
                    Debug.LogError($"[Luxodd] Level End Error {code}: {msg}");
                    onSuccess?.Invoke(); // Proceed to leaderboard even on error
                }
            );
        }

        public void RequestLeaderboard(Action<LeaderboardDataResponse> onSuccess)
        {
            if (!m_IsConnected || m_WebSocketCommandHandler == null) return;
            m_WebSocketCommandHandler.SendLeaderboardRequestCommand(
                onSuccess,
                (code, msg) => Debug.LogError($"[Luxodd] Leaderboard Error {code}: {msg}")
            );
        }

        /// <summary>
        /// Request to continue the current game session (keep score, checkpoint, restore lives).
        /// Uses SendSessionOptionContinue per Luxodd docs - system handles billing automatically.
        /// </summary>
        public void RequestSessionContinue(Action onAllowed, Action onDenied)
        {
            if (!m_IsConnected || m_WebSocketService == null)
            {
                onAllowed?.Invoke();
                return;
            }

            Debug.Log("[Luxodd] Requesting session continue...");
            m_WebSocketService.SendSessionOptionContinue((action) =>
            {
                Debug.Log($"[Luxodd] Session continue response: {action}");
                if (action == SessionOptionAction.Continue)
                {
                    m_SessionActive = true;
                    RefreshBalance();
                    onAllowed?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[Luxodd] Session continue denied: {action}");
                    onDenied?.Invoke();
                }
            });
        }

        /// <summary>
        /// Request to restart the game as a new session (fresh start).
        /// Uses SendSessionOptionRestart per Luxodd docs - system handles billing automatically.
        /// Note: Restart does not return a callback on success (system creates new session).
        /// </summary>
        public void RequestSessionRestart(Action onDenied)
        {
            if (!m_IsConnected || m_WebSocketService == null) return;

            Debug.Log("[Luxodd] Requesting session restart...");
            m_WebSocketService.SendSessionOptionRestart((action) =>
            {
                Debug.Log($"[Luxodd] Session restart response: {action}");
                if (action == SessionOptionAction.End)
                {
                    Debug.LogWarning($"[Luxodd] Session restart denied: {action}");
                    onDenied?.Invoke();
                }
                // If Restart: system handles creating new session automatically
            });
        }

        /// <summary>
        /// End the session and return to the arcade game selection screen.
        /// Uses BackToSystem() per Luxodd docs instead of SendSessionOptionEnd.
        /// </summary>
        public void EndSessionAndReturnToSystem()
        {
            if (!m_IsConnected || m_WebSocketService == null) return;

            Debug.Log("[Luxodd] Ending session, returning to system...");
            m_SessionActive = false;
            m_WebSocketService.BackToSystem();
        }

        /// <summary>
        /// Refresh the player's balance from the server.
        /// </summary>
        public void RefreshBalance()
        {
            if (!m_IsConnected || m_WebSocketCommandHandler == null) return;

            m_WebSocketCommandHandler.SendUserBalanceRequestCommand(
                (balance) =>
                {
                    m_PlayerBalance = balance;
                    Debug.Log($"[Luxodd] Balance updated: {balance}");
                },
                (code, msg) => Debug.LogError($"[Luxodd] Balance refresh error {code}: {msg}")
            );
        }
    }
}
