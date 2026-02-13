using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedRunner
{

	public class Skeleton : MonoBehaviour
	{

		#region Delegates

		public delegate void ActiveChangedHandler (bool active);

		#endregion

		#region Events

		public event ActiveChangedHandler OnActiveChanged;

		#endregion

		#region Fields

		[Header ("Skeleton")]
		[Space]
		[SerializeField]
		private Rigidbody2D m_Body;
		[SerializeField]
		private Rigidbody2D m_RightFoot;
		[SerializeField]
		private Rigidbody2D m_LeftFoot;
		[SerializeField]
		private Rigidbody2D m_RightHand;
		[SerializeField]
		private Rigidbody2D m_LeftHand;
		[SerializeField]
		private Rigidbody2D m_RightArm;
		[SerializeField]
		private Rigidbody2D m_LeftArm;
		[SerializeField]
		private Transform m_LeftEye;
		[SerializeField]
		private Transform m_RightEye;
		[SerializeField]
		private bool m_IsActive = false;

		#endregion

		#region Properties

		public Rigidbody2D Body { get { return m_Body; } }

		public Rigidbody2D RightFoot { get { return m_RightFoot; } }

		public Rigidbody2D LeftFoot { get { return m_LeftFoot; } }

		public Rigidbody2D RightHand { get { return m_RightHand; } }

		public Rigidbody2D LeftHand { get { return m_LeftHand; } }

		public Rigidbody2D RightArm { get { return m_RightArm; } }

		public Rigidbody2D LeftArm { get { return m_LeftArm; } }

		public Transform LeftEye { get { return m_LeftEye; } }

		public Transform RightEye { get { return m_RightEye; } }

		public bool IsActive { get { return m_IsActive; } }

		#endregion

		// Safety: if a bone flies farther than this from the body, stop simulating it
		private const float MAX_BONE_DISTANCE = 15f;
		// Absolute world bounds: any bone beyond this from origin triggers full shutdown
		private const float MAX_WORLD_DISTANCE = 500f;
		// Position where ragdoll was activated (to detect body itself flying too far)
		private Vector2 m_ActivationPosition;

		#region Public Methods

		public void SetActive (bool active, Vector2 velocity)
		{
			if (m_IsActive != active) {
				m_IsActive = active;
				if (active) {
					// ACTIVATING ragdoll: give ALL bones the player's velocity
					// so they fly in the death direction instead of exploding apart
					m_ActivationPosition = m_Body != null ? m_Body.position : Vector2.zero;
					SetAllBonesVelocity (velocity);
				} else {
					// DEACTIVATING ragdoll: zero all velocities
					SetAllBonesVelocity (Vector2.zero);
				}
				SetAllBonesSimulated (active);
				if (OnActiveChanged != null) {
					OnActiveChanged (active);
				}
			}
		}

		/// <summary>
		/// Force-disable all bone physics without changing IsActive state.
		/// Call before Time.timeScale=0 to prevent Invalid worldAABB errors.
		/// </summary>
		public void ForceStopSimulation ()
		{
			SetAllBonesVelocity (Vector2.zero);
			SetAllBonesSimulated (false);
		}

		#endregion

		#region Private Methods

		void FixedUpdate ()
		{
			if (!m_IsActive || m_Body == null) return;

			// Safety: if the BODY itself flies too far from where ragdoll started,
			// shut down the entire ragdoll (prevents Spike joint pulling body away)
			if (Vector2.Distance (m_Body.position, m_ActivationPosition) > MAX_BONE_DISTANCE ||
				Mathf.Abs (m_Body.position.x) > MAX_WORLD_DISTANCE ||
				Mathf.Abs (m_Body.position.y) > MAX_WORLD_DISTANCE)
			{
				ForceStopSimulation ();
				return;
			}

			// Safety net: disable individual bones that fly too far from body
			Vector2 bodyPos = m_Body.position;
			ClampBone (m_LeftHand, bodyPos);
			ClampBone (m_RightHand, bodyPos);
			ClampBone (m_LeftArm, bodyPos);
			ClampBone (m_RightArm, bodyPos);
			ClampBone (m_LeftFoot, bodyPos);
			ClampBone (m_RightFoot, bodyPos);
		}

		private void ClampBone (Rigidbody2D bone, Vector2 center)
		{
			if (bone == null || !bone.simulated) return;
			if (Vector2.Distance (bone.position, center) > MAX_BONE_DISTANCE)
			{
				bone.linearVelocity = Vector2.zero;
				bone.simulated = false;
			}
		}

		private void SetAllBonesSimulated (bool simulated)
		{
			if (m_Body != null) m_Body.simulated = simulated;
			if (m_RightFoot != null) m_RightFoot.simulated = simulated;
			if (m_LeftFoot != null) m_LeftFoot.simulated = simulated;
			if (m_RightHand != null) m_RightHand.simulated = simulated;
			if (m_LeftHand != null) m_LeftHand.simulated = simulated;
			if (m_RightArm != null) m_RightArm.simulated = simulated;
			if (m_LeftArm != null) m_LeftArm.simulated = simulated;
		}

		private void SetAllBonesVelocity (Vector2 velocity)
		{
			if (m_Body != null) m_Body.linearVelocity = velocity;
			if (m_RightFoot != null) m_RightFoot.linearVelocity = velocity;
			if (m_LeftFoot != null) m_LeftFoot.linearVelocity = velocity;
			if (m_RightHand != null) m_RightHand.linearVelocity = velocity;
			if (m_LeftHand != null) m_LeftHand.linearVelocity = velocity;
			if (m_RightArm != null) m_RightArm.linearVelocity = velocity;
			if (m_LeftArm != null) m_LeftArm.linearVelocity = velocity;
		}

		#endregion

	}

}