using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline
{
#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	public class RectAspect : MonoBehaviour
	{
		public bool updateInRuntime;
		public bool updateInEdit;

		public bool horizontal;

		public float ratio = 1f;

		private void Update()
		{
#if UNITY_EDITOR
			if( Application.isPlaying )
			{
				if( updateInRuntime )
					PerformLayout();
			}
			else if( updateInEdit)
			{
				PerformLayout();
			}
#else
			if( updateInRuntime )
				PerformLayout();
#endif
		}

		public void PerformLayout()
		{
			RectTransform rectTransform = ( RectTransform ) transform;
			Vector2 size = rectTransform.rect.size;

			if( horizontal )
				rectTransform.sizeDelta = new Vector2
					(
						rectTransform.sizeDelta.x,
						size.x * ratio
					);
			else
				rectTransform.sizeDelta = new Vector2
					(
						size.y * ratio,
						rectTransform.sizeDelta.y
					);
		}
	}
	
#if UNITY_EDITOR
	//[CustomEditor( typeof( RectAspect ) )]
	public class EditorRectAspect : EditorUtils.InheritedEditor
	{
		public override void Setup()
		{
			base.Setup();

			// Setup Inspector Objects
		}

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			// Calc Inspector Height

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			// Draw Inspector GUI

			rect.y = bRect.y;
		}
	}
#endif
}