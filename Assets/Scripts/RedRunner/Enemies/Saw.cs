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
		private float m_BaseColliderRadius = 1.1f;

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

			// Big saws get a larger collider
			float colliderRadius = gameObject.name.StartsWith("Saw 512 With Circle")
				? 2.5f
				: m_BaseColliderRadius;

			// Remove PolygonCollider2D if present and replace with CircleCollider2D
			var polyCollider = GetComponent<PolygonCollider2D>();
			if (polyCollider != null)
			{
				Vector2 offset = polyCollider.offset;
				Destroy(polyCollider);

				var circle = gameObject.AddComponent<CircleCollider2D>();
				circle.radius = colliderRadius;
				circle.offset = offset;
				m_Collider2D = circle;
			}

			// If m_Collider2D is missing, grab whatever collider exists
			if (m_Collider2D == null)
			{
				m_Collider2D = GetComponent<Collider2D>();
			}

			// Ensure the collider uses the configured radius
			if (m_Collider2D is CircleCollider2D mainCircle)
			{
				mainCircle.radius = colliderRadius;
			}

			// Create a trigger collider for instant kill detection
			if (m_Collider2D is CircleCollider2D circleCol)
			{
				var trigger = gameObject.AddComponent<CircleCollider2D>();
				trigger.radius = colliderRadius * 0.9f;
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
