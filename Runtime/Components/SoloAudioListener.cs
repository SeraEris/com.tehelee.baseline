using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline
{
	[RequireComponent( typeof( AudioListener ) )]
	public class SoloAudioListener : Solo<AudioListener>
	{
		protected override bool GetSuppressed() => !target.enabled;
		protected override void SetSuppressed( bool suppressed ) => target.enabled = !suppressed;
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( SoloAudioListener ) )]
	public class EditorSoloAudioListener : EditorSolo
	{
		protected override GUIContent label => new GUIContent( "Solo AudioListener" );
		protected override bool suppressed => ( ( SoloAudioListener ) target ).suppressed;
		protected override int count => SoloAudioListener.GetActiveCount();
	}
#endif
}