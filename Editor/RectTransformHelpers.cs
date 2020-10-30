using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

namespace Tehelee.Baseline
{
	public static class RectTransformHelpers
	{
		private static System.Type rectTransformType = typeof( RectTransform );

		[MenuItem( "CONTEXT/RectTransform/Reset Rect" )]
		public static void Reset()
		{
			foreach( GameObject gameObject in Selection.gameObjects )
			{
				if( !rectTransformType.IsAssignableFrom( gameObject.transform.GetType() ) )
					continue;

				RectTransform rectTransform = ( RectTransform ) gameObject.transform;

				rectTransform.sizeDelta = Vector2.zero;

				rectTransform.anchoredPosition = Vector2.zero;

				rectTransform.anchorMax = rectTransform.anchorMin = Vector2.one * 0.5f;
			}
		}

		[MenuItem( "CONTEXT/RectTransform/Fill Rect" )]
		public static void Fill()
		{
			foreach( GameObject gameObject in Selection.gameObjects )
			{
				if( !rectTransformType.IsAssignableFrom( gameObject.transform.GetType() ) )
					continue;

				RectTransform rectTransform = ( RectTransform ) gameObject.transform;

				rectTransform.sizeDelta = Vector2.zero;

				rectTransform.anchoredPosition = Vector2.zero;

				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
			}
		}
	}
}