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

        public string PlayerName => m_PlayerName;
        public float PlayerBalance => m_PlayerBalance;
        public bool IsConnected => m_IsConnected;

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
    }
}
