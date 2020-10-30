using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
	public static class KeyEventSingleton
	{
		private static HashSet<KeyEvents> listeners = new HashSet<KeyEvents>();
		public static int listenersCount => listeners.Count;

		private static Dictionary<KeyCode, HashSet<KeyEvents>> lookup = new Dictionary<KeyCode, HashSet<KeyEvents>>();

		private static Dictionary<KeyEvents, float> registerTime = new Dictionary<KeyEvents, float>();

		private static KeyCode[] allKeys = new KeyCode[ 0 ];
		private static bool[] keyState = new bool[ 0 ];

		public static void Register( KeyEvents keyEvents )
		{
			if( object.Equals( null, keyEvents ) || !keyEvents )
				return;

			listeners.Add( keyEvents );

			registerTime.Add( keyEvents, Time.time );

			if( object.Equals( null, _IKeyWatcher ) )
				_IKeyWatcher = Utils.StartCoroutine( IKeyWatcher() );

			foreach( KeyEvents.KeyEvent keyEvent in keyEvents.keyEvents )
			{
				foreach( KeyCode keyCode in keyEvent.keys )
				{
					if( !lookup.ContainsKey( keyCode ) )
						lookup.Add( keyCode, new HashSet<KeyEvents>() );

					lookup[ keyCode ].Add( keyEvents );
				}
			}

			Dictionary<KeyCode, bool> state = new Dictionary<KeyCode, bool>();
			for( int i = 0, iC = allKeys.Length; i < iC; i++ )
				state.Add( allKeys[ i ], keyState[ i ] );

			allKeys = new List<KeyCode>( lookup.Keys ).ToArray();
			keyState = new bool[ allKeys.Length ];

			for( int i = 0, iC = allKeys.Length; i < iC; i++ )
				if( state.ContainsKey( allKeys[ i ] ) )
					keyState[ i ] = state[ allKeys[ i ] ];
		}

		public static void UnRegister( KeyEvents keyEvents )
		{
			if( object.Equals( null, keyEvents ) || !listeners.Contains( keyEvents ) )
				return;

			listeners.Remove( keyEvents );

			if( registerTime.ContainsKey( keyEvents ) )
				registerTime.Remove( keyEvents );

			if( listeners.Count == 0 )
			{
				Utils.StopCoroutine( _IKeyWatcher );
				_IKeyWatcher = null;

				lookup.Clear();

				allKeys = new KeyCode[ 0 ];
				keyState = new bool[ 0 ];
			}
			else
			{
				List<KeyCode> cleanup = new List<KeyCode>();
				Dictionary<KeyCode, HashSet<KeyEvents>> modified = new Dictionary<KeyCode, HashSet<KeyEvents>>();

				foreach( KeyCode keyCode in lookup.Keys )
				{
					HashSet<KeyEvents> hashSet = lookup[ keyCode ];
					if( hashSet.Contains( keyEvents ) )
						hashSet.Remove( keyEvents );

					if( hashSet.Count == 0 )
						cleanup.Add( keyCode );
					else
						modified.Add( keyCode, hashSet );
				}

				foreach( KeyCode keyCode in modified.Keys )
					if( lookup.ContainsKey( keyCode ) )
						lookup[ keyCode ] = modified[ keyCode ];

				foreach( KeyCode keyCode in cleanup )
					lookup.Remove( keyCode );

				Dictionary<KeyCode, bool> state = new Dictionary<KeyCode, bool>();
				for( int i = 0, iC = allKeys.Length; i < iC; i++ )
					state.Add( allKeys[ i ], keyState[ i ] );

				allKeys = new List<KeyCode>( lookup.Keys ).ToArray();
				keyState = new bool[ allKeys.Length ];

				for( int i = 0, iC = allKeys.Length; i < iC; i++ )
					if( state.ContainsKey( allKeys[ i ] ) )
						keyState[ i ] = state[ allKeys[ i ] ];
			}
		}

		private static Dictionary<KeyCode, List<KeyEvents>> triggerKeyDownEvents = new Dictionary<KeyCode, List<KeyEvents>>();
		private static Dictionary<KeyCode, List<KeyEvents>> triggerKeyUpEvents = new Dictionary<KeyCode, List<KeyEvents>>();

		private static Coroutine _IKeyWatcher = null;
		private static IEnumerator IKeyWatcher()
		{
			while( true )
			{
				triggerKeyDownEvents.Clear();
				triggerKeyUpEvents.Clear();

				bool pressed = false;
				for( int i = 0, iC = allKeys.Length; i < iC; i++ )
				{
					KeyCode keyCode = allKeys[ i ];
					pressed = Input.GetKey( keyCode );

					if( pressed != keyState[ i ] )
					{
						HashSet<KeyEvents> _triggers = new HashSet<KeyEvents>();
						foreach( KeyEvents keyEvents in lookup[ keyCode ] )
							_triggers.Add( keyEvents );

						List<KeyEvents> triggers = new List<KeyEvents>( _triggers );
						triggers.Sort
						(
							( KeyEvents a, KeyEvents b ) =>
							{
								if( object.Equals( null, a ) || object.Equals( null, b ) )
									return 0;
								if( !registerTime.ContainsKey( a ) || !registerTime.ContainsKey( b ) )
									return 0;

								return -registerTime[ a ].CompareTo( registerTime[ b ] );
							}
						);
						
						if( pressed )
							triggerKeyDownEvents.Add( keyCode, triggers );
						else
							triggerKeyUpEvents.Add( keyCode, triggers );

						keyState[ i ] = pressed;
					}
				}

				foreach( KeyCode keyCode in triggerKeyDownEvents.Keys )
					foreach( KeyEvents keyEvents in triggerKeyDownEvents[ keyCode ] )
						if( keyEvents.OnKeyDown( keyCode ) )
							break;

				foreach( KeyCode keyCode in triggerKeyUpEvents.Keys )
					foreach( KeyEvents keyEvents in triggerKeyUpEvents[ keyCode ] )
						if( keyEvents.OnKeyUp( keyCode ) )
							break;

				yield return null;
			}
		}
	}

	public class KeyEvents : MonoBehaviour
	{
		[System.Serializable]
		public class KeyEvent
		{
			public KeyCode[] keys;

			public UnityEvent onDown = new UnityEvent();
			public UnityEvent onUp = new UnityEvent();
		}

		public KeyEvent[] keyEvents = new KeyEvent[ 0 ];

		private Dictionary<KeyCode, List<KeyEvent>> lookup = new Dictionary<KeyCode, List<KeyEvent>>();

		public bool consumeKeyEvents = true;
		
		protected virtual void OnEnable()
		{
			lookup.Clear();
			for( int i = 0, iC = keyEvents.Length; i < iC; i++ )
			{
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
		}

		public bool OnKeyDown( KeyCode keyCode )
		{
			List<KeyEvent> keyEvents = lookup[ keyCode ];
			foreach( KeyEvent keyEvent in keyEvents )
			{
				keyEvent.onDown.Invoke();
			}

			return consumeKeyEvents;
		}

		public bool OnKeyUp( KeyCode keyCode )
		{
			List<KeyEvent> keyEvents = lookup[ keyCode ];
			foreach( KeyEvent keyEvent in keyEvents )
			{
				keyEvent.onUp.Invoke();
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

					if( keyEventKeys[ index ].serializedProperty.isExpanded )
					{
						height += keyEventKeys[ index ].GetHeight() + lineHeight * 0.5f;
					}
					else
					{
						height += lineHeight * 1.5f;
					}

					height += EditorUtils.BetterUnityEventFieldHeight( element.FindPropertyRelative( "onDown" ) );

					height += EditorUtils.BetterUnityEventFieldHeight( element.FindPropertyRelative( "onUp" ) );

					return height;
				},
				( Rect rect, SerializedProperty list, int index, SerializedProperty element, bool isActive, bool isFocussed ) =>
				{
					Rect bRect = new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, rect.height - lineHeight * 0.5f );

					if( index >= keyEventKeys.Length )
						RebuildKeyEventKeys();

					if( keyEventKeys[ index ].serializedProperty.isExpanded )
					{
						bRect.height = keyEventKeys[ index ].GetHeight();

						keyEventKeys[ index ].DoList( bRect );
						bRect.y += bRect.height + lineHeight * 0.5f;
					}
					else
					{
						bRect.height = lineHeight;

						EditorUtils.DrawListHeader( bRect, keyEventKeys[ index ].serializedProperty );

						bRect.y += lineHeight * 1.5f;
					}

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

		public override float inspectorLeadingOffset => lineHeight * 0.5f;

		public override float GetInspectorHeight()
		{
			float height = base.GetInspectorHeight();

			height += lineHeight * 2f;

			if( keyEvents.serializedProperty.isExpanded )
			{
				height += keyEvents.GetHeight();
			}
			else
			{
				height += lineHeight * 1.5f;
			}

			return height;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( bRect, new GUIContent( "Consume Key Events" ), consumeKeyEvents );

			bRect.y += lineHeight * 2f;

			if( keyEvents.serializedProperty.isExpanded )
			{
				bRect.height = keyEvents.GetHeight();

				keyEvents.DoList( bRect );

				bRect.y += bRect.height;
			}
			else
			{
				bRect.height = lineHeight;

				EditorUtils.DrawListHeader( bRect, keyEvents.serializedProperty );

				bRect.y += lineHeight * 1.5f;
			}

			rect.y = bRect.y;
		}
	}
#endif
}