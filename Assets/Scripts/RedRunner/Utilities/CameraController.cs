using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedRunner.Characters;

namespace RedRunner.Utilities
{

	public class CameraController : MonoBehaviour
	{

		public delegate void ParallaxCameraDelegate ( Vector3 deltaMovement );

		public ParallaxCameraDelegate onCameraTranslate;

		private static CameraController m_Singleton;

		public static CameraController Singleton
		{
			get
			{
				return m_Singleton;
			}
		}

		[SerializeField]
		private Camera m_Camera;
		[SerializeField]
		private Transform m_Followee;
		[SerializeField]
		private float m_MinY = 0f;
		[SerializeField]
		private float m_MinX = 0f;
		[SerializeField]
		private CameraControl m_ShakeControl;
		[SerializeField]
		private float m_FastMoveSpeed = 10f;
		[SerializeField]
		private float m_Speed = 1f;
		
		[Header("Independent Movement")]
		[SerializeField]
		[Tooltip("Speed at which the camera moves along the X-axis independently.")]
		private float m_IndependentMoveSpeed = 5.0f;
		[SerializeField]
		[Tooltip("Time in seconds before the camera starts moving independently.")]
		private float m_StartIndependentMoveDelay = 60.0f;
		[SerializeField]
		[Tooltip("Maximum X-distance allowed before the player dies.")]
		private float m_MaxDistanceBeforeDeath = 20.0f;

		private bool m_FastMove = false;
		private Vector3 m_OldPosition;
		private float m_TimeElapsed = 0f;
		private bool m_IsIndependentMoving = false;
		private Character m_Character;

		public bool fastMove
		{
			get
			{
				return m_FastMove;
			}
			set
			{
				m_FastMove = value;
			}
		}

		void Awake ()
		{
			m_Singleton = this;
			m_ShakeControl = GetComponent<CameraControl> ();
			if (m_Followee != null)
			{
				m_Character = m_Followee.GetComponent<Character>();
			}
		}

		void Start ()
		{
			m_OldPosition = transform.position;
		}

		void OnEnable()
		{
			GameManager.OnReset += ResetCamera;
		}

		void OnDisable()
		{
			GameManager.OnReset -= ResetCamera;
		}

		void ResetCamera()
		{
			m_TimeElapsed = 0f;
			m_IsIndependentMoving = false;
		}

		void Update ()
		{
			// Update Timer
			if (!m_IsIndependentMoving)
			{
				m_TimeElapsed += Time.deltaTime;
				if (m_TimeElapsed >= m_StartIndependentMoveDelay)
				{
					m_IsIndependentMoving = true;
				}
			}

//			if (!m_ShakeControl.IsShaking) {
			Follow ();
//			}
			if ( transform.position != m_OldPosition )
			{
				if ( onCameraTranslate != null )
				{
					Vector3 delta = m_OldPosition - transform.position;
					onCameraTranslate ( delta );
				}
				m_OldPosition = transform.position;
			}
		}

		public void Follow ()
		{
			float speed = m_Speed;
			if ( m_FastMove )
			{
				speed = m_FastMoveSpeed;
			}
			Vector3 cameraPosition = transform.position;
			Vector3 targetPosition = m_Followee.position;

			if (m_IsIndependentMoving)
			{
				// Independent X Movement
				cameraPosition.x += m_IndependentMoveSpeed * Time.deltaTime;

				// Distance Check & Death
				float distanceX = cameraPosition.x - m_Followee.position.x;
				if (distanceX > m_MaxDistanceBeforeDeath)
				{
					if (m_Character != null && !m_Character.IsDead.Value)
					{
						m_Character.Die();
					}
				}

				// Tracking Y Movement (Sync with Player)
				float targetY = targetPosition.y;
				if ( targetPosition.y - m_Camera.orthographicSize <= m_MinY )
				{
					targetY = m_MinY + m_Camera.orthographicSize;
				}
				
				// Smoothly move Y towards target
				cameraPosition.y = Mathf.MoveTowards(cameraPosition.y, targetY, speed);
				
				// Apply new position
				transform.position = cameraPosition;
			}
			else
			{
				// Original Standard Follow Logic
				if ( targetPosition.x - m_Camera.orthographicSize * m_Camera.aspect > m_MinX )
				{
					cameraPosition.x = targetPosition.x;
				}
				else
				{
					cameraPosition.x = m_MinX + m_Camera.orthographicSize * m_Camera.aspect;
				}
				
				if ( targetPosition.y - m_Camera.orthographicSize > m_MinY )
				{
					cameraPosition.y = targetPosition.y;
				}
				else
				{
					cameraPosition.y = m_MinY + m_Camera.orthographicSize;
				}
				
				transform.position = Vector3.MoveTowards ( transform.position, cameraPosition, speed );
				
				if ( transform.position == targetPosition && m_FastMove )
				{
					m_FastMove = false;
				}
			}
		}

	}

}
