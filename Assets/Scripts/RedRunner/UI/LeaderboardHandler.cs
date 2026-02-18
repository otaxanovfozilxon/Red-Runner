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
                return;
            }

            _webSocketCommandHandler.SendLeaderboardRequestCommand(
                (response) => Populate(response),
                (code, msg) => Debug.LogError($"[Luxodd] Leaderboard Error {code}: {msg}")
            );
        }

        private void Populate(LeaderboardDataResponse response)
        {
            // NULL CHECK: Prevent crash if lists not assigned in Inspector
            if (_playerNameTexts == null || _playerScoreTexts == null)
            {
                Debug.LogWarning("[Leaderboard] Player text lists are null! Cannot populate leaderboard.");
                return;
            }
            
            // Reset all texts first
            for (int i = 0; i < _leaderboardSize; i++)
            {
                if (i < _playerNameTexts.Count) _playerNameTexts[i].text = "";
                if (i < _playerScoreTexts.Count) _playerScoreTexts[i].text = "";
            }

            int index = 0;

            // 1. Show CURRENT PLAYER at slot 0 (live session name + score)
            if (_playerNameTexts.Count > 0)
            {
                string pName = "Guest";
                int pScore = 0;

                if (LuxoddIntegrationManager.Singleton != null)
                {
                    pName = LuxoddIntegrationManager.Singleton.PlayerName;
                }

                if (GameManager.Singleton != null)
                {
                    pScore = (int)GameManager.Singleton.Score;
                }

                _playerNameTexts[0].text = FormatName(pName);
                _playerScoreTexts[0].text = pScore.ToString();
                index = 1;
            }

            // 2. Fill remaining 4 slots from the server leaderboard
            if (response.Leaderboard != null)
            {
                for (int i = 0; i < response.Leaderboard.Count && index < _playerNameTexts.Count && index < _leaderboardSize; i++)
                {
                    var data = response.Leaderboard[i];
                    _playerNameTexts[index].text = FormatName(data.PlayerName);
                    _playerScoreTexts[index].text = data.TotalScore.ToString();
                    index++;
                }
            }
        }

        private string FormatName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Guest";
            if (name.StartsWith("lux-")) return "Guest";
            return name;
        }
    }
}
