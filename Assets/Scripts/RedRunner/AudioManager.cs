using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedRunner
{

	public class AudioManager : MonoBehaviour
	{

		#region Singleton

		private static AudioManager m_Singleton;

		public static AudioManager Singleton {
			get {
				return m_Singleton;
			}
		}

		#endregion

		#region Fields

		[Header ("Audio Sources")]
		[Space]
		[SerializeField]
		protected AudioSource m_MusicAudioSource;
		[SerializeField]
		protected AudioSource m_SoundAudioSource;
		[SerializeField]
		protected AudioSource m_CoinAudioSource;
		[SerializeField]
		protected AudioSource m_DieAudioSource;
		[SerializeField]
		protected AudioSource m_MaceSlamAudioSource;
		[SerializeField]
		protected AudioSource m_UIAudioSource;

		[Header ("Music Clips")]
		[Space]
		[SerializeField]
		protected AudioClip m_MusicClip;

		[Header ("Sound Clips")]
		[Space]
		[SerializeField]
		protected AudioClip m_CoinSound;
		[SerializeField]
		protected AudioClip m_ChestSound;
		[SerializeField]
		protected AudioClip m_WaterSplashSound;
		[SerializeField]
		protected AudioClip m_SpikeSound;
		[SerializeField]
		protected AudioClip[] m_GroundedSounds;
		[SerializeField]
		protected AudioClip m_JumpSound;
		[SerializeField]
		protected AudioClip[] m_FootstepSounds;
		[SerializeField]
		protected AudioClip m_MaceSlamSound;
		[SerializeField]
		protected AudioClip m_ButtonClickSound;

		#endregion

		#region MonoBehaviour Messages

		void Awake ()
		{
			m_Singleton = this;
			PreWarmAllClips ();
			PlayMusic ();
		}

		/// <summary>
		/// Pre-warm all audio clips by briefly playing them at zero volume.
		/// In WebGL, the first play of each clip triggers decompression which freezes the game.
		/// </summary>
		void PreWarmAllClips ()
		{
			PreWarmClip ( m_SoundAudioSource, m_CoinSound );
			PreWarmClip ( m_SoundAudioSource, m_ChestSound );
			PreWarmClip ( m_SoundAudioSource, m_WaterSplashSound );
			PreWarmClip ( m_SoundAudioSource, m_SpikeSound );
			PreWarmClip ( m_SoundAudioSource, m_JumpSound );
			PreWarmClip ( m_SoundAudioSource, m_MaceSlamSound );
			PreWarmClip ( m_SoundAudioSource, m_ButtonClickSound );
			if ( m_GroundedSounds != null )
			{
				foreach ( var clip in m_GroundedSounds )
					PreWarmClip ( m_SoundAudioSource, clip );
			}
			if ( m_FootstepSounds != null )
			{
				foreach ( var clip in m_FootstepSounds )
					PreWarmClip ( m_SoundAudioSource, clip );
			}
		}

		void PreWarmClip ( AudioSource source, AudioClip clip )
		{
			if ( source == null || clip == null ) return;
			float origVol = source.volume;
			var origClip = source.clip;
			source.volume = 0f;
			source.clip = clip;
			source.Play ();
			source.Stop ();
			source.clip = origClip;
			source.volume = origVol;
		}

		#endregion

		#region Methods

		public void PlayMusic ()
		{
			m_MusicAudioSource.clip = m_MusicClip;
			m_MusicAudioSource.Play ();
		}

		public void PlaySoundAt (AudioClip clip, Vector3 position, float volume)
		{
			AudioSource.PlayClipAtPoint (clip, position, volume);
		}

		public void PlaySoundOn (AudioSource audio, AudioClip clip)
		{
			audio.clip = clip;
			audio.Play ();
		}

		public void PlayChestSound (Vector3 position)
		{
			PlaySoundOn (m_CoinAudioSource, m_ChestSound);
		}

		public void PlayCoinSound (Vector3 position)
		{
			PlaySoundOn (m_CoinAudioSource, m_CoinSound);
		}

		public void PlayWaterSplashSound (Vector3 position)
		{
			PlaySoundOn (m_DieAudioSource, m_WaterSplashSound);
		}

		public void PlayMaceSlamSound (Vector3 position)
		{
			PlaySoundOn (m_MaceSlamAudioSource, m_MaceSlamSound);
		}

		public void PlaySpikeSound (Vector3 position)
		{
			PlaySoundOn (m_DieAudioSource, m_SpikeSound);
		}

		public void PlayGroundedSound (AudioSource audio)
		{
			if (m_GroundedSounds.Length > 0) {
				PlaySoundOn (audio, GetRandomClip (m_GroundedSounds));
			}
		}

		public void PlayJumpSound (AudioSource audio)
		{
			PlaySoundOn (audio, m_JumpSound);
		}

		public void PlayFootstepSound (AudioSource audio)
		{
			if (m_FootstepSounds.Length > 0) {
				PlaySoundOn (audio, GetRandomClip (m_FootstepSounds));
			}
		}

		public void PlayFootstepSound (AudioSource audio, ref int index)
		{
			if (m_FootstepSounds.Length > 0) {
				PlaySoundOn (audio, m_FootstepSounds [index]);
				if (index < m_FootstepSounds.Length - 1) {
					index++;
				} else {
					index = 0;
				}
			}
		}

		public void PlayClickSound ()
		{
			PlaySoundOn (m_UIAudioSource, m_ButtonClickSound);
		}

		public AudioClip GetRandomClip (AudioClip[] clips)
		{
			if (clips.Length > 0) {
				return clips [Random.Range (0, clips.Length)];
			}
			return null;
		}

		#endregion

	}

}