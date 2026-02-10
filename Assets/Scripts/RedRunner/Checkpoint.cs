using UnityEngine;

namespace RedRunner
{
    public class Checkpoint : MonoBehaviour
    {
        private bool m_Activated = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_Activated) return;

            var character = collision.GetComponent<Characters.Character>();
            if (character == null) return;

            m_Activated = true;

            GameManager.Singleton.SetCheckpoint(transform.position);

            if (UI.ScreenFadeController.Instance != null)
            {
                UI.ScreenFadeController.Instance.PlayCheckpointFade();
            }
        }
    }
}
