using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
	public class KeyEvents : MonoBehaviour, IKeyEventReceiver
	{
		public KeyEvent[] keyEvents = new KeyEvent[ 0 ];

		private Dictionary<KeyCode, List<KeyEvent>> lookup = new Dictionary<KeyCode, List<KeyEvent>>();

		public bool consumeKeyEvents = true;
		
		protected virtual void OnEnable()
		{
			KeyBindings.OnEnable();

			lookup.Clear();
			for( int i = 0, iC = keyEvents.Length; i < iC; i++ )
			{
				if( !string.IsNullOrWhiteSpace( keyEvents[ i ].preferencesKey ) )
				{
					KeyCode[] keyCodes = KeyBindings.GetKeyCodes( keyEvents[ i ].preferencesKey );
					if( keyCodes.Length > 0 )
						keyEvents[ i ].keys = keyCodes;
					else if( keyEvents[ i ].keys.Length > 0 )
						KeyBindings.SetKeyCodes( keyEvents[ i ].preferencesKey, keyEvents[ i ].keys );
				}

				KeyCode[] keys = keyEvents[ i ].keys;

				for( int j = 0, jC = keys.Length; j < jC; j++ )
				{
					if( !lookup.ContainsKey( keys[ j ] ) )
						lookup.Add( keys[ j ], new List<KeyEvent>() );

					lookup[ keys[ j ] ].Add( keyEvents[ i ] );
				}
			}

			KeyEventSingleton.Register( this );
		}

		protected virtual void OnDisable()
		{
			KeyEventSingleton.UnRegister( this );

			KeyBindings.OnDisable();
		}

		public IList<KeyEvent> GetKeyEvents() => keyEvents;

		public bool OnKeyDown( KeyCode keyCode )
		{
			bool editingInput = KeyEventSingleton.editingInput;
			
			List<KeyEvent> keyEvents = lookup[ keyCode ];
			foreach( KeyEvent keyEvent in keyEvents )
			{
				if( keyEvent.invokeDuringInput || !editingInput )
					keyEvent.InvokeDown( keyCode );
			}

			return consumeKeyEvents;
		}

		public bool OnKeyUp( KeyCode keyCode )
		{
			bool editingInput = KeyEventSingleton.editingInput;
			
			List<KeyEvent> keyEvents = lookup[ keyCode ];
			foreach( KeyEvent keyEvent in keyEvents )
			{
				if( keyEvent.invokeDuringInput || !editingInput )
					keyEvent.InvokeUp( keyCode );
			}

			return consumeKeyEvents;
		}
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( KeyEvents ) )]
	public class EditorKeyEvents : EditorUtils.InheritedEditor
	{
		SerializedProperty consumeKeyEvents;

		ReorderableList keyEvents;

		ReorderableList[] keyEventKeys = new ReorderableList[ 0 ];

		private void RebuildKeyEventKeys()
		{
			if( object.Equals( null, keyEvents ) || object.Equals( null, keyEvents.serializedProperty ) )
			{
				keyEventKeys = new ReorderableList[ 0 ];
				return;
			}

			SerializedProperty _keyEvents = keyEvents.serializedProperty;

			keyEventKeys = new ReorderableList[ _keyEvents.arraySize ];

			for( int i = 0, iC = keyEventKeys.Length; i < iC; i++ )
			{
				keyEventKeys[ i ] = EditorUtils.CreateReorderableList
				(
					_keyEvents.GetArrayElementAtIndex( i ).FindPropertyRelative( "keys" ),
					( SerializedProperty element ) =>
					{
						return lineHeight * 1.5f;
					},
					( Rect rect, SerializedProperty element ) =>
					{
						EditorGUI.PropertyField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), element, new GUIContent() );
					}
				);
			}
		}

		public override void Setup()
		{
			base.Setup();

			consumeKeyEvents = serializedObject.FindProperty( "consumeKeyEvents" );

			keyEvents = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "keyEvents" ),
				( SerializedProperty list, int index, SerializedProperty element ) =>
				{
					float height = lineHeight * 0.5f;

					if( index >= keyEventKeys.Length )
						RebuildKeyEventKeys();

					height += keyEventKeys[ index ].CalculateCollapsableListHeight();

					height += lineHeight * 3.5f;

					height += EditorUtils.BetterUnityEventFieldHeight( element.FindPropertyRelative( "onDown" ) );

					height += EditorUtils.BetterUnityEventFieldHeight( element.FindPropertyRelative( "onUp" ) );

					return height;
				},
				( Rect rect, SerializedProperty list, int index, SerializedProperty element, bool isActive, bool isFocussed ) =>
				{
					Rect bRect = new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, rect.height - lineHeight * 0.5f );

					if( index >= keyEventKeys.Length )
						RebuildKeyEventKeys();

					keyEventKeys[ index ].DrawCollapsableList( ref bRect );

					bRect.height = lineHeight * 1.5f;
					EditorUtils.BetterToggleField( bRect, new GUIContent( "Invoke During Input" ), element.FindPropertyRelative( "invokeDuringInput" ) );
					bRect.y += lineHeight * 2f;

					float labelWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 110f;

					bRect.height = lineHeight;
					EditorGUI.PropertyField( bRect, element.FindPropertyRelative( "preferencesKey" ), new GUIContent( "Preferences Key" ) );
					bRect.y += lineHeight * 1.5f;

					EditorGUIUtility.labelWidth = labelWidth;

					SerializedProperty onDown = element.FindPropertyRelative( "onDown" );
					SerializedProperty onUp = element.FindPropertyRelative( "onUp" );

					bRect.height = EditorUtils.BetterUnityEventFieldHeight( onDown );
					EditorUtils.BetterUnityEventField( bRect, onDown );

					bRect.y += bRect.height;

					bRect.height = EditorUtils.BetterUnityEventFieldHeight( onUp );
					EditorUtils.BetterUnityEventField( bRect, onUp );

					bRect.y += bRect.height;
				},
				( SerializedProperty list, SerializedProperty element ) => { }
			);
		}

		public override float GetInspectorHeight()
		{
			float height = base.GetInspectorHeight();

			height += lineHeight * 3.5f;

			height += keyEvents.CalculateCollapsableListHeight();

			return height;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Key Events", "Provides UnityEvents executed on key input events." ) );
			bRect.y += lineHeight * 1.5f;

			bRect.height = lineHeight * 1.5f;
			EditorUtils.BetterToggleField( bRect, new GUIContent( "Consume Key Events" ), consumeKeyEvents );
			bRect.height = lineHeight;

			bRect.y += lineHeight * 2f;

			keyEvents.DrawCollapsableList( ref bRect );

			rect.y = bRect.y;
		}
	}
#endif
}