using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline.Components.UI
{
#if UNITY_EDITOR
	[ExecuteInEditMode]
#endif
	[RequireComponent( typeof( RectTransform ) )]
	public class MirrorRectTransform : MonoBehaviour
	{
		public RectTransform target;

		public bool updateInRuntime = false;

#if UNITY_EDITOR
		public bool updateInEdit = false;
#endif
		protected virtual void Update()
		{
#if UNITY_EDITOR
			if( Application.isPlaying )
			{
				if( !updateInRuntime )
					return;
			}
			else if( !updateInEdit )
			{
				return;
			}
#else
			if( !updateInRuntime )
				return;
#endif
			PerformLayout();
		}

		public void PerformLayout()
		{
			if( object.Equals( null, target ) || !target )
				return;

			RectTransform rectTransform = ( RectTransform ) transform;

			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.sizeDelta = Vector2.zero;
			Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds( rectTransform, target );
			rectTransform.anchoredPosition = bounds.center - bounds.extents;
			rectTransform.sizeDelta = bounds.extents * 2f;
		}
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( MirrorRectTransform ) )]
	public class EditorMirrorRectTransform : EditorUtils.InheritedEditor
	{
		public override float inspectorLeadingOffset => lineHeight * 0.5f;

		public override float GetInspectorHeight()
		{
			return base.GetInspectorHeight() + lineHeight * 3f;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.BetterObjectField( bRect, new GUIContent( "RectTransform" ), this[ "target" ], typeof( RectTransform ), true );

			bRect.y += lineHeight * 1.5f;
			bRect.height = lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, bRect.height );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Runtime" ), this[ "updateInRuntime" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Edit" ), this[ "updateInEdit" ] );

			bRect.y += lineHeight * 1.5f;

			rect.y = bRect.y;
		}
	}
#endif
}