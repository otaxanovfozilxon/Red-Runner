using System;
using System.Collections;
using System.Collections.Generic;
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
            get
            {
                return m_Singleton;
            }
        }

        [Header("Luxodd Services")]
        [SerializeField] private WebSocketService _webSocketService;
        [SerializeField] private HealthStatusCheckService _healthStatusCheckService;
        [SerializeField] private WebSocketCommandHandler _webSocketCommandHandler;

        [Header("Debug")]
        [SerializeField] private bool _isDebugMode = true;

        // Player Data
        public string PlayerName { get; private set; }
        public float PlayerBalance { get; private set; }

        void Awake()
        {
            if (m_Singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            m_Singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Auto-wire dependencies if not assigned
            if (_webSocketService == null) _webSocketService = FindObjectOfType<WebSocketService>();
            if (_healthStatusCheckService == null) _healthStatusCheckService = FindObjectOfType<HealthStatusCheckService>();
            if (_webSocketCommandHandler == null) _webSocketCommandHandler = FindObjectOfType<WebSocketCommandHandler>();

            if (_webSocketService == null)
            {
                LogError("WebSocketService not found! Please ensure the Luxodd UnityPluginPrefab is added to the scene.");
                return;
            }

            ConnectToLuxoddServer();
        }

        #region Connection & Setup

        public void ConnectToLuxoddServer()
        {
            if (_webSocketService == null)
            {
                LogError("WebSocketService is not assigned!");
                return;
            }

            Log("Connecting to Luxodd Server...");
            _webSocketService.ConnectToServer(
                OnConnectedSuccess,
                OnConnectedFailure
            );
        }

        private void OnConnectedSuccess()
        {
            Log("Connected to server successfully!");
            
            // Activate Health Check
            if (_healthStatusCheckService != null)
            {
                _healthStatusCheckService.Activate();
                Log("Health Status Check Activated.");
            }

            // Fetch Initial Data
            RequestPlayerProfile();
            RequestUserBalance();
        }

        private void OnConnectedFailure()
        {
            LogError("Connecting to server failed!");
        }

        #endregion

        #region Player Data

        public void RequestPlayerProfile()
        {
            if (_webSocketCommandHandler == null) return;

            _webSocketCommandHandler.SendProfileRequestCommand(
                (name) => {
                    PlayerName = name;
                    Log($"Player Profile Received: {name}");
                },
                (code, msg) => LogError($"Player Profile Request Failed: {code}: {msg}")
            );
        }

        public void RequestUserBalance()
        {
            if (_webSocketCommandHandler == null) return;

            _webSocketCommandHandler.SendUserBalanceRequestCommand(
                (balance) => {
                    PlayerBalance = balance;
                    Log($"User Balance Received: {balance}");
                },
                (code, msg) => LogError($"User Balance Request Failed: {code}: {msg}")
            );
        }

        #endregion

        #region Game Events

        public void TrackLevelStart(int levelNumber)
        {
            if (_webSocketCommandHandler == null) return;

            Log($"Tracking Level Start: {levelNumber}");
            _webSocketCommandHandler.SendLevelBeginRequestCommand(
                levelNumber,
                () => Log("Level Begin Request success"),
                (code, msg) => LogError($"Level Begin Request Failure: {code}: {msg}")
            );
        }

        public void TrackLevelEnd(int levelNumber, int score)
        {
            if (_webSocketCommandHandler == null) return;

            Log($"Tracking Level End: {levelNumber}, Score: {score}");
            _webSocketCommandHandler.SendLevelEndRequestCommand(
                levelNumber,
                score,
                () => {
                    Log("Level End Request success");
                    // Optionally fetch updated leaderboard after submitting score
                    FetchLeaderboard(); 
                },
                (code, msg) => LogError($"Level End Request Failure: {code}: {msg}")
            );
        }

        #endregion

        #region Leaderboard

        public void FetchLeaderboard()
        {
            if (_webSocketCommandHandler == null) return;

            _webSocketCommandHandler.SendLeaderboardRequestCommand(
                (response) => {
                    Log($"Leaderboard Fetched. Current Rank: {response.CurrentUserData.Rank}");
                    // Here you could fire an event or update UI with response.Leaderboard data
                },
                (code, msg) => LogError($"Leaderboard Request Failure: {code}: {msg}")
            );
        }

        #endregion

        #region Helpers

        private void Log(string message)
        {
            if (_isDebugMode) Debug.Log($"[LuxoddIntegration] {message}");
        }

        private void LogError(string message)
        {
            if (_isDebugMode) Debug.LogError($"[LuxoddIntegration] {message}");
        }

        #endregion
    }
}
