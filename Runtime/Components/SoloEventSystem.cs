using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline
{
	[RequireComponent( typeof( EventSystem ) )]
	public class SoloEventSystem : Solo<EventSystem>
	{
		protected override bool GetSuppressed() => !target.enabled;
		protected override void SetSuppressed( bool suppressed ) => target.enabled = !suppressed;
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( SoloEventSystem ) )]
	public class EditorSoloEventSystem : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight() + lineHeight * 1.5f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "SoloEventSystem" ) );
			bRect.y += lineHeight * 1.5f;

			rect.y = bRect.y;
		}
	}
#endif
}