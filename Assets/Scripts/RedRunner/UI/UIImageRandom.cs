using UnityEngine;
using UnityEngine.UI;

namespace RedRunner.UI
{

	public class UIImageRandom : MonoBehaviour
	{

		[SerializeField]
		protected Image m_ColorImage;
		[SerializeField]
		protected Image m_PatternImage;

		private static readonly Color[] s_Colors = new Color[]
		{
			new Color(1f, 0.6f, 0.796f, 1f),       // Pink    #FF99CB
			new Color(0.055f, 0.937f, 0.098f, 1f),  // Green   #0EEF19
			new Color(0.957f, 0.482f, 0.125f, 1f),  // Orange  #F47B20
			Color.white                               // White   #FFFFFF
		};

		private static int s_LastColorIndex = -1;

		protected virtual void Start()
		{
			Randomize();
		}

		/// <summary>
		/// Pick a random color (different from the last one) and apply it.
		/// Does NOT touch the Pattern image sprite — leave that to the Inspector.
		/// </summary>
		public void Randomize()
		{
			if (m_ColorImage != null)
			{
				int index;
				do
				{
					index = Random.Range(0, s_Colors.Length);
				}
				while (index == s_LastColorIndex && s_Colors.Length > 1);

				s_LastColorIndex = index;
				m_ColorImage.color = s_Colors[index];
			}

			// Ensure the pattern image is fully visible (white with full alpha)
			if (m_PatternImage != null)
			{
				m_PatternImage.color = Color.white;
			}
		}

	}

}
