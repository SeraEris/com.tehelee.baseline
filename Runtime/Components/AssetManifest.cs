using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components
{
	[System.Serializable]
	[CreateAssetMenu( fileName = "AssetManifest", menuName = "Asset Manifest", order = 200 )]
	public class AssetManifest : ScriptableObject
	{
		public List<ObjectLookup.ObjectReference> entries = new List<ObjectLookup.ObjectReference>();

		public T Lookup<T>( string key ) where T : Object
		{
			if( string.IsNullOrEmpty( key ) )
				return null;
			
			foreach( ObjectLookup.ObjectReference entry in entries )
			{
				if( string.Equals( key, entry.key ) )
					return ( T ) entry.reference;
			}

			return null;
		}
	}
#if UNITY_EDITOR
	[CustomEditor( typeof( AssetManifest ) )]
	public class EditorAssetManifest : EditorUtils.InheritedEditor
	{
		private ReorderableList entries;
		
		public override void Setup()
		{
			base.Setup();
			
			entries = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "entries" ),
				( SerializedProperty element ) =>
					EditorGUI.GetPropertyHeight( element ) + lineHeight * 0.5f,
				( Rect rect, SerializedProperty element ) =>
				{
					EditorGUI.PropertyField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, EditorGUI.GetPropertyHeight( element ) ), element, true );
				}
			);
		}

		public override float GetInspectorHeight() => base.GetInspectorHeight() + entries.GetHeight() + lineHeight * 1.5f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Asset Reference" ) );
			bRect.y += lineHeight * 1.5f;
			
			bRect.height = entries.GetHeight();
			entries.DoList( bRect );

			bRect.y += bRect.height;
			
			rect.y = bRect.y;
		}
	}
#endif
}