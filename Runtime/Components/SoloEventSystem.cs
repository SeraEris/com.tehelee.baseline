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
	public class EditorSoloEventSystem : EditorSolo
	{
		protected override GUIContent label => new GUIContent( "Solo EventSystem" );
		protected override bool suppressed => ( ( SoloEventSystem ) target ).suppressed;
		protected override int count => SoloEventSystem.GetActiveCount();
	}
#endif
}