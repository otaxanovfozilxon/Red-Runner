using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using Luxodd.Game.Scripts.Game.Leaderboard;

namespace RedRunner.UI
{
    public class LeaderboardHandler : MonoBehaviour
    {
        [Header("Network")]
        [SerializeField] private WebSocketCommandHandler _webSocketCommandHandler;
        [SerializeField] private WebSocketService _webSocketService;

        [Header("Leaderboard UI")]
        [SerializeField] private List<TextMeshProUGUI> _playerNameTexts;
        [SerializeField] private List<TextMeshProUGUI> _playerScoreTexts;
        [SerializeField] private int _leaderboardSize = 5;

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_webSocketCommandHandler == null)
            {
                Debug.LogWarning("[Luxodd] WebSocketCommandHandler is missing on LeaderboardHandler!");
                PopulateOffline();
                return;
            }

            _webSocketCommandHandler.SendLeaderboardRequestCommand(
                (response) => Populate(response),
                (code, msg) =>
                {
                    Debug.LogError($"[Luxodd] Leaderboard Error {code}: {msg}");
                    PopulateOffline();
                }
            );
        }

        private void Populate(LeaderboardDataResponse response)
        {
            if (_playerNameTexts == null || _playerScoreTexts == null)
            {
                Debug.LogWarning("[Leaderboard] Player text lists are null! Cannot populate leaderboard.");
                return;
            }

            ClearSlots();

            // Slot 0: current player with session coin count
            int currentCoins = 0;
            if (GameManager.Singleton != null)
            {
                currentCoins = GameManager.Singleton.m_Coin.Value;
            }
            string currentName = "You";
            if (LuxoddIntegrationManager.Singleton != null)
            {
                currentName = FormatName(LuxoddIntegrationManager.Singleton.PlayerName);
            }
            if (_playerNameTexts.Count > 0)
            {
                _playerNameTexts[0].text = currentName;
                _playerScoreTexts[0].text = currentCoins.ToString();
            }

            // Slots 1+: other players from server leaderboard (skip current user)
            int index = 1;
            if (response.Leaderboard != null)
            {
                // Get current user's rank to skip their duplicate entry
                int currentUserRank = -1;
                if (response.CurrentUserData != null)
                {
                    currentUserRank = response.CurrentUserData.Rank;
                }

                for (int i = 0; i < response.Leaderboard.Count && index < _leaderboardSize && index < _playerNameTexts.Count; i++)
                {
                    var data = response.Leaderboard[i];

                    // Skip current user's entry from the server list
                    if (currentUserRank >= 0 && data.Rank == currentUserRank)
                        continue;

                    _playerNameTexts[index].text = FormatName(data.PlayerName);
                    _playerScoreTexts[index].text = data.TotalScore.ToString();
                    index++;
                }
            }
        }

        private void PopulateOffline()
        {
            if (_playerNameTexts == null || _playerScoreTexts == null) return;

            ClearSlots();

            int currentCoins = 0;
            if (GameManager.Singleton != null)
            {
                currentCoins = GameManager.Singleton.m_Coin.Value;
            }
            string currentName = "You";
            if (LuxoddIntegrationManager.Singleton != null)
            {
                currentName = FormatName(LuxoddIntegrationManager.Singleton.PlayerName);
            }
            if (_playerNameTexts.Count > 0)
            {
                _playerNameTexts[0].text = currentName;
                _playerScoreTexts[0].text = currentCoins.ToString();
            }
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _leaderboardSize; i++)
            {
                if (i < _playerNameTexts.Count) _playerNameTexts[i].text = "";
                if (i < _playerScoreTexts.Count) _playerScoreTexts[i].text = "";
            }
        }

        private string FormatName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Guest";
            if (name.StartsWith("lux-", StringComparison.OrdinalIgnoreCase)) return "Guest";
            return name;
        }
    }
}
