using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
	[CustomEditor( typeof( EnablerEvents ) )]
	public class EditorEnablerEvents : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 1.5f;

			inspectorHeight += EditorUtils.BetterUnityEventFieldHeight( this[ "onEnable" ] );
			inspectorHeight += EditorUtils.BetterUnityEventFieldHeight( this[ "onDisable" ] );

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );
			
			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Enabler Events", "Provides UnityEvents invoked with OnEnable and OnDisable." ) );
			bRect.y += lineHeight * 1.5f;

			bRect.height = EditorUtils.BetterUnityEventFieldHeight( this[ "onEnable" ] );
			EditorUtils.BetterUnityEventField( bRect, this[ "onEnable" ] );
			bRect.y += bRect.height;
			
			bRect.height = EditorUtils.BetterUnityEventFieldHeight( this[ "onDisable" ] );
			EditorUtils.BetterUnityEventField( bRect, this[ "onDisable" ] );
			bRect.y += bRect.height;

			rect.y = bRect.y;
		}
	}
#endif
}