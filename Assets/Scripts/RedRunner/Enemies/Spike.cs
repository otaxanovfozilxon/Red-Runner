using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Characters;

namespace RedRunner.Enemies
{

	public class Spike : Enemy
	{

		[SerializeField]
		private Collider2D m_Collider2D;
		[SerializeField]
		private FixedJoint2D m_FixedJoint2D;

		public override Collider2D Collider2D {
			get {
				return m_Collider2D;
			}
		}

		void Awake ()
		{
			GameManager.OnReset += Reset;
		}

		void OnDestroy ()
		{
			GameManager.OnReset -= Reset;
		}

		void Reset ()
		{
			// Disconnect joint on reset — leaving it connected to the skeleton Body
			// can pull bones to extreme positions causing Invalid worldAABB errors
			if (m_FixedJoint2D != null)
			{
				m_FixedJoint2D.connectedBody = null;
			}
		}

		void OnCollisionStay2D (Collision2D collision2D)
		{
			Character character = collision2D.collider.GetComponent<Character> ();
			if (character && !character.IsDead.Value) {
				bool isTop = false;
				for (int i = 0; i < collision2D.contacts.Length; i++) {
					if (!isTop) {
						isTop = collision2D.contacts [i].normal.y < -0.7f && collision2D.contacts [i].normal.y >= -1f;
					} else {
						break;
					}
				}
				if (isTop) {
					Kill (character);
				}
			}
		}

		public override void Kill (Character target)
		{
			target.Die (true);
			m_FixedJoint2D.connectedBody = target.GetComponent<Skeleton> ().Body;
			AudioManager.Singleton.PlaySpikeSound (transform.position);
		}

	}

}
