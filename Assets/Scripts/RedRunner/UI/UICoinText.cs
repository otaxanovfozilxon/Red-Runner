using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Collectables;
using System;

namespace RedRunner.UI
{
	public class UICoinText : UIText
	{
		[SerializeField]
		protected string m_CoinTextFormat = "x {0}";

		protected override void Awake ()
		{
			base.Awake ();
		}

		protected bool m_Initialized = false;

		protected override void Start()
		{
			InitConnection ();
		}

		protected virtual void Update ()
		{
			if (!m_Initialized)
			{
				InitConnection ();
			}
		}

		private void InitConnection ()
		{
			var gm = GameManager.Singleton ?? FindFirstObjectByType<GameManager>();
			if (gm == null)
			{
				return;
			}

			gm.m_Coin.AddEventAndFire(UpdateCoinsText, this);
			m_Initialized = true;
		}

		private void UpdateCoinsText(int newCoinValue)
		{
			var animator = GetComponent<Animator>();
			if (animator != null)
			{
				animator.SetTrigger("Collect");
			}
			text = string.Format(m_CoinTextFormat, newCoinValue);
		}
	}
}
