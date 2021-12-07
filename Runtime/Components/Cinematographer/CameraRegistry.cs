using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.Cinematographer
{
	public class CameraRegistry : MonoBehaviour
	{
		////////////////////////////////
		#region Static

		private static Dictionary<string, CameraAnchor> anchorRegistry = new Dictionary<string, CameraAnchor>();
		
		public static bool HasCameraAnchor( string key ) =>
			anchorRegistry.ContainsKey( key.ToLowerInvariant() );

		public static CameraAnchor GetCameraAnchor( string key )
		{
			key = key.ToLowerInvariant();
			return anchorRegistry.ContainsKey( key ) ? anchorRegistry[ key ] : null;
		}

		public static void Register( string key, CameraAnchor cameraAnchor )
		{
			key = key.ToLowerInvariant();
			if( !string.IsNullOrWhiteSpace( key ) && Utils.IsObjectAlive( cameraAnchor ) )
			{
				if( anchorRegistry.ContainsKey( key ) )
					anchorRegistry[ key ] = cameraAnchor;
				else
					anchorRegistry.Add( key, cameraAnchor );
			}
		}

		public static void Drop( string key, CameraAnchor cameraAnchor )
		{
			key = key.ToLowerInvariant();
			if( !string.IsNullOrWhiteSpace( key ) && Utils.IsObjectAlive( cameraAnchor ) )
			{
				if( anchorRegistry.ContainsKey( key ) && object.Equals( anchorRegistry[ key ], cameraAnchor ) )
					anchorRegistry.Remove( key );
			}
		}

		#endregion

		////////////////////////////////
		#region Attributes

		[System.Serializable]
		public class AnchorEntry
		{
			public string key = string.Empty;
			public CameraAnchor cameraAnchor = null;
		}
		public List<AnchorEntry> anchorEntries = new List<AnchorEntry>();

		#endregion

		////////////////////////////////
		#region Mono Methods

		private void OnEnable()
		{
			Register();
		}

		private void OnDisable()
		{
			Drop();
		}

		#endregion

		////////////////////////////////
		#region CameraRegistry

		public void Register()
		{
			foreach( AnchorEntry anchorEntry in anchorEntries )
				if( !object.Equals( null, anchorEntry ) )
					Register( anchorEntry.key, anchorEntry.cameraAnchor );
		}

		public void Drop()
		{
			foreach( AnchorEntry anchorEntry in anchorEntries )
				if( !object.Equals( null, anchorEntry ) )
					Drop( anchorEntry.key, anchorEntry.cameraAnchor );
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( CameraRegistry ) )]
	public class EditorCameraRegistry : EditorUtils.InheritedEditor
	{
		private ReorderableList anchorEntries;

		public override void Setup()
		{
			base.Setup();

			anchorEntries = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "anchorEntries" ),
				( SerializedProperty element ) =>
				{
					return lineHeight * 1.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					SerializedProperty key = element.FindPropertyRelative( "key" );
					SerializedProperty cameraAnchor = element.FindPropertyRelative( "cameraAnchor" );

					Rect bRect = new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight );
					Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight );

					EditorGUI.PropertyField( cRect, key, emptyContent );
					key.ToLower();

					cRect.x += cRect.width + 10f;

					EditorUtils.BetterObjectField( cRect, emptyContent, cameraAnchor, typeof( CameraAnchor ), true );
				}
			);
		}

		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 1.5f + anchorEntries.CalculateCollapsableListHeight();

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "CameraRegistry" ) );
			bRect.y += lineHeight * 1.5f;

			anchorEntries.DrawCollapsableList( ref bRect, new GUIContent( "Camera Anchors" ) );

			rect.y = bRect.y;
		}
	}
#endif
}