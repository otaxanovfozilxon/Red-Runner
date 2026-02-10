using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RedRunner.UI
{
    public class UILifeManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_HeartPrefab;
        [SerializeField]
        private Transform m_Container;
        [SerializeField]
        private List<GameObject> m_Hearts;

        void Start()
        {
            GameManager.OnLifeChanged += UpdateHearts;
            if (GameManager.Singleton != null)
            {
                UpdateHearts(GameManager.Singleton.Lives);
            }
        }

        void OnDestroy()
        {
            GameManager.OnLifeChanged -= UpdateHearts;
        }

        void CreateHeart()
        {
            if (m_HeartPrefab && m_Container)
            {
                GameObject updatedHeart = Instantiate(m_HeartPrefab, m_Container);
                updatedHeart.transform.localScale = Vector3.one; // Ensure scale is correct
                m_Hearts.Add(updatedHeart);
            }
            else
            {
                Debug.LogWarning("Heart Prefab or Container is missing in UILifeManager.");
            }
        }

        void UpdateHearts(int lives)
        {
            // NULL CHECK: Initialize hearts list if not already initialized
            if (m_Hearts == null)
            {
                m_Hearts = new List<GameObject>();
            }
            
            // Ensure we have enough hearts visual
            int safetyCounter = 0;
            while (m_Hearts.Count < lives)
            {
                CreateHeart();
                safetyCounter++;
                if (safetyCounter > 100)
                {
                    Debug.LogError("Infinite loop detected in UILifeManager (Adding Hearts). Breaking loop.");
                    break;
                }
            }
            safetyCounter = 0;
            while (m_Hearts.Count > lives)
            {
                if (m_Hearts.Count > 0)
                {
                    // Remove the last one (Rightmost typically in Horizontal Group)
                    GameObject heart = m_Hearts[m_Hearts.Count - 1];
                    m_Hearts.RemoveAt(m_Hearts.Count - 1);
                    Destroy(heart);
                }
                else
                {
                    break;
                }
                safetyCounter++;
                if (safetyCounter > 100)
                {
                    break;
                }
            }
        }
    }
}
