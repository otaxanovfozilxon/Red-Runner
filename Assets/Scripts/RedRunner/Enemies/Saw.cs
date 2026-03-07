using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Characters;

namespace RedRunner.Enemies
{

	public class Saw : Enemy
	{

		[SerializeField]
		private Collider2D m_Collider2D;
		[SerializeField]
		private Transform targetRotation;
		[SerializeField]
		private float m_Speed = 1f;
		[SerializeField]
		private bool m_RotateClockwise = false;
		[SerializeField]
		private AudioClip m_DefaultSound;
		[SerializeField]
		private AudioClip m_SawingSound;
		[SerializeField]
		private AudioSource m_AudioSource;

		private Collider2D m_TriggerCollider;

		public override Collider2D Collider2D {
			get {
				return m_Collider2D;
			}
		}

		void Start ()
		{
			if (targetRotation == null) {
				targetRotation = transform;
			}

			// Replace any PolygonCollider2D with a CircleCollider2D at runtime
			var polyCollider = GetComponent<PolygonCollider2D>();
			if (polyCollider != null)
			{
				// Calculate radius from the polygon bounds
				float radius = Mathf.Max(polyCollider.bounds.extents.x, polyCollider.bounds.extents.y);
				Vector2 offset = polyCollider.offset;
				Destroy(polyCollider);

				var circle = gameObject.AddComponent<CircleCollider2D>();
				circle.radius = radius;
				circle.offset = offset;
				m_Collider2D = circle;
			}

			// If m_Collider2D is missing, grab whatever collider exists
			if (m_Collider2D == null)
			{
				m_Collider2D = GetComponent<Collider2D>();
			}

			// Create a trigger collider for instant kill detection
			if (m_Collider2D is CircleCollider2D circleCol)
			{
				var trigger = gameObject.AddComponent<CircleCollider2D>();
				trigger.radius = circleCol.radius * 0.9f;
				trigger.offset = circleCol.offset;
				trigger.isTrigger = true;
				m_TriggerCollider = trigger;
			}
		}

		void Update ()
		{
			Vector3 rotation = targetRotation.rotation.eulerAngles;
			if (!m_RotateClockwise) {
				rotation.z += m_Speed;
			} else {
				rotation.z -= m_Speed;
			}
			targetRotation.rotation = Quaternion.Euler (rotation);
		}

		void OnTriggerEnter2D (Collider2D collider)
		{
			Character character = collider.GetComponent<Character> ();
			if (character != null) {
				Kill (character);
			}
		}

		void OnCollisionEnter2D (Collision2D collision2D)
		{
			Character character = collision2D.collider.GetComponent<Character> ();
			if (character != null) {
				Kill (character);
			}
		}

		void OnCollisionStay2D (Collision2D collision2D)
		{
			if (collision2D.collider.CompareTag ("Player")) {
				if (m_AudioSource.clip != m_SawingSound) {
					m_AudioSource.clip = m_SawingSound;
				} else if (!m_AudioSource.isPlaying) {
					m_AudioSource.Play ();
				}
			}
		}

		void OnCollisionExit2D (Collision2D collision2D)
		{
			if (collision2D.collider.CompareTag ("Player")) {
				if (m_AudioSource.clip != m_DefaultSound) {
					m_AudioSource.clip = m_DefaultSound;
				}
				m_AudioSource.Play ();
			}
		}

		public override void Kill (Character target)
		{
			target.Die (true);
		}

	}

}
