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

            // Spawn an invisible barrier at the checkpoint to prevent backward movement
            var barrier = new GameObject("CheckpointBarrier");
            barrier.transform.position = new Vector3(transform.position.x - 5f, transform.position.y, transform.position.z);
            var col = barrier.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 50f);
            GameManager.Singleton.AddBarrier(barrier);

            if (UI.ScreenFadeController.Instance != null)
            {
                UI.ScreenFadeController.Instance.PlayCheckpointFade();
            }
        }
    }
}
