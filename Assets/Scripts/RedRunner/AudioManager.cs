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
		protected AudioClip m_MenuMusicClip;

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
		[SerializeField]
		protected AudioClip m_GameOverSound;

		#endregion

#if UNITY_WEBGL && !UNITY_EDITOR
		[System.Runtime.InteropServices.DllImport("__Internal")]
		private static extern void WebGLPatchAudioPitch ();
#endif

		#region MonoBehaviour Messages

		void Awake ()
		{
			m_Singleton = this;

#if UNITY_WEBGL && !UNITY_EDITOR
			// Patch HTMLMediaElement.playbackRate BEFORE any audio plays.
			// Unity's WebGL audio bridge computes pitch = AudioSource.pitch * Time.timeScale.
			// At timeScale=0 this produces ~1e-6, which browsers reject with NotSupportedError,
			// corrupting the entire audio context and killing all subsequent audio.
			WebGLPatchAudioPitch ();
#endif

			// Spread audio pre-warming across frames to avoid freezing WebGL on startup
			StartCoroutine ( PreWarmAllClipsAsync () );
		}

		/// <summary>
		/// Pre-warm all audio clips one per frame by briefly playing at zero volume.
		/// In WebGL, the first play of each clip triggers decompression which freezes the game.
		/// Spreading across frames prevents a multi-second freeze.
		/// </summary>
		IEnumerator PreWarmAllClipsAsync ()
		{
			// Wait one frame so other Awake() methods finish first
			yield return null;

			// Pre-warm sound effect clips — use each AudioSource at least once
			// to activate its WebGL audio channel
			PreWarmClip ( m_SoundAudioSource, m_CoinSound );
			yield return null;
			PreWarmClip ( m_CoinAudioSource, m_ChestSound );
			yield return null;
			PreWarmClip ( m_DieAudioSource, m_WaterSplashSound );
			yield return null;
			PreWarmClip ( m_SoundAudioSource, m_SpikeSound );
			yield return null;
			PreWarmClip ( m_SoundAudioSource, m_JumpSound );
			yield return null;
			PreWarmClip ( m_MaceSlamAudioSource, m_MaceSlamSound );
			yield return null;
			PreWarmClip ( m_UIAudioSource, m_ButtonClickSound );
			yield return null;
			PreWarmClip ( m_MusicAudioSource, m_MenuMusicClip );
			yield return null;
			PreWarmClip ( m_DieAudioSource, m_GameOverSound );
			yield return null;

			if ( m_GroundedSounds != null )
			{
				foreach ( var clip in m_GroundedSounds )
				{
					PreWarmClip ( m_SoundAudioSource, clip );
					yield return null;
				}
			}
			if ( m_FootstepSounds != null )
			{
				foreach ( var clip in m_FootstepSounds )
				{
					PreWarmClip ( m_SoundAudioSource, clip );
					yield return null;
				}
			}

			// Don't call PlayMenuMusic() here — it will start when the StartScreen opens.
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

		[Header ("Music Settings")]
		[SerializeField]
		[Range (0f, 1f)]
		protected float m_MenuMusicVolume = 0.8f;

		public void PlayMenuMusic ()
		{
			if ( m_MusicAudioSource == null || m_MenuMusicClip == null ) return;
			m_MusicAudioSource.ignoreListenerPause = true;
			m_MusicAudioSource.clip = m_MenuMusicClip;
			m_MusicAudioSource.loop = true;
			m_MusicAudioSource.volume = m_MenuMusicVolume;
			m_MusicAudioSource.Play ();
		}

		public void StopMenuMusic ()
		{
			if ( m_MusicAudioSource == null ) return;
			m_MusicAudioSource.Stop ();
			m_MusicAudioSource.ignoreListenerPause = false;
		}

		public void PlayGameOverSound ()
		{
			PlaySoundOn (m_SoundAudioSource, m_GameOverSound);
		}

		public void PlaySoundAt (AudioClip clip, Vector3 position, float volume)
		{
			AudioSource.PlayClipAtPoint (clip, position, volume);
		}

		public void PlaySoundOn (AudioSource audio, AudioClip clip)
		{
			if ( audio == null || clip == null ) return;
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