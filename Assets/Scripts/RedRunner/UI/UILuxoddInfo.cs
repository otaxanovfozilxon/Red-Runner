using UnityEngine;
using TMPro;

namespace RedRunner.UI
{
    public class UILuxoddInfo : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_PlayerNameText;
        [SerializeField] private TextMeshProUGUI m_BalanceText;

        void Update()
        {
            if (LuxoddIntegrationManager.Singleton != null)
            {
                if (m_PlayerNameText != null)
                    m_PlayerNameText.text = LuxoddIntegrationManager.Singleton.PlayerName;
                
                if (m_BalanceText != null)
                    m_BalanceText.text = $"Credits: {LuxoddIntegrationManager.Singleton.PlayerBalance}";
            }
        }
    }
}
