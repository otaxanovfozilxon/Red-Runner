using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedRunner.TerrainGeneration
{

	public abstract class Block : MonoBehaviour
	{

		[SerializeField]
		protected float m_Width;
		[SerializeField]
		protected float m_Probability = 1f;
		protected RedRunner.Utilities.PathFollower[] m_CachedPathFollowers;

		public virtual float Width {
			get {
				return m_Width;
			}
			set {
				m_Width = value;
			}
		}

		public virtual float Probability {
			get {
				return m_Probability;
			}
		}

		public virtual void OnRemove (TerrainGenerator generator)
		{
			
		}


		public virtual void PreGenerate (TerrainGenerator generator)
		{
			
		}

		public virtual void PostGenerate (TerrainGenerator generator)
		{
			
		}

		public virtual void Reset ()
		{
			// Cache PathFollower references to avoid GetComponentsInChildren every reset
			if ( m_CachedPathFollowers == null )
				m_CachedPathFollowers = GetComponentsInChildren<RedRunner.Utilities.PathFollower> ();
			for (int i = 0; i < m_CachedPathFollowers.Length; i++) {
				m_CachedPathFollowers [i].Reset ();
			}
		}

	}

}