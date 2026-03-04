using System;
using System.Runtime.InteropServices;
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
        private bool m_ContinueCallbackProcessed = false;
        private bool m_ContinueAllowed = false;

        public string PlayerName => m_PlayerName;
        public float PlayerBalance => m_PlayerBalance;
        public bool IsConnected => m_IsConnected;
        public bool SessionActive => m_SessionActive;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AutoEndSession_Start(int seconds);

        [DllImport("__Internal")]
        private static extern void AutoEndSession_Cancel();

        [DllImport("__Internal")]
        private static extern void DiagLog(string msg);
#else
        private static void DiagLog(string msg) { Debug.Log(msg); }
#endif

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
                    onSuccess?.Invoke();
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

        public void RequestSessionContinue(Action onAllowed, Action onDenied)
        {
            if (!m_IsConnected || m_WebSocketService == null)
            {
                DiagLog("[Luxodd] Not connected - calling onAllowed directly");
                onAllowed?.Invoke();
                return;
            }

            m_ContinueCallbackProcessed = false;
            m_ContinueAllowed = false;
            DiagLog("[Luxodd] Requesting session continue...");

            m_WebSocketService.SendSessionOptionContinue((action) =>
            {
                DiagLog($"[Luxodd] Session continue callback: action={action} enum={(int)action}");

                // Guard: prevent double-firing
                if (m_ContinueCallbackProcessed)
                {
                    DiagLog($"[Luxodd] DUPLICATE callback ignored: {action}");
                    return;
                }
                m_ContinueCallbackProcessed = true;

                // Cancel the auto-end timer
#if UNITY_WEBGL && !UNITY_EDITOR
                AutoEndSession_Cancel();
#endif

                if (action == SessionOptionAction.Continue)
                {
                    m_ContinueAllowed = true;
                    DiagLog("[Luxodd] CONTINUE ALLOWED - calling onAllowed");
                    m_SessionActive = true;
                    RefreshBalance();
                    onAllowed?.Invoke();
                    DiagLog("[Luxodd] onAllowed completed");
                }
                else
                {
                    DiagLog($"[Luxodd] DENIED: action={action} - calling onDenied");
                    onDenied?.Invoke();
                }
            });

            // Start 30s auto-end timer in the browser
            // Needs to be >10s because host PIN verification takes ~11s
#if UNITY_WEBGL && !UNITY_EDITOR
            AutoEndSession_Start(30);
#endif
            DiagLog("[Luxodd] 30s browser auto-end timer started");
        }

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
            });
        }

        public void EndSessionAndReturnToSystem()
        {
            if (!m_IsConnected || m_WebSocketService == null) return;

            DiagLog("[Luxodd] Ending session, returning to system...");
            m_SessionActive = false;
            m_WebSocketService.BackToSystem();
        }

        /// <summary>
        /// Send session_end after Continue was approved.
        /// The host closes the popup and reloads the game.
        /// Game state is preserved in localStorage for auto-continue on reload.
        /// </summary>
        public void EndSessionForReload()
        {
            if (!m_IsConnected || m_WebSocketService == null) return;

            DiagLog("[Luxodd] Ending session for reload (continue via save/restore)...");
            m_SessionActive = false;
            m_WebSocketService.BackToSystem();
        }

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
