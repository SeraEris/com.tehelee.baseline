using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Tehelee.Baseline.Components.UI
{
	public class EnablerEvents : MonoBehaviour
	{
		public UnityEvent onEnable = new UnityEvent();
		public UnityEvent onDisable = new UnityEvent();

		public void OnEnable()
		{
			onEnable.Invoke();
		}

		public void OnDisable()
		{
			onDisable.Invoke();
		}
	}
}