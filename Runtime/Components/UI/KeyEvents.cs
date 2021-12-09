using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

		public static bool editingInput { get; private set; } = false;
		private static bool resetEditingInput = false;

		public static void Register( KeyEvents keyEvents )
		{
			if( !Utils.IsObjectAlive( keyEvents ) )
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
			while( !KeyBindings.hasLoaded )
				yield return null;

			while( true )
			{
				triggerKeyDownEvents.Clear();
				triggerKeyUpEvents.Clear();

				bool _editingInput = false;
				UnityEngine.UI.Selectable selectable = null;
				if( Utils.IsObjectAlive( EventSystem.current ) && Utils.IsObjectAlive( EventSystem.current.currentSelectedGameObject ) )
					selectable = EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Selectable>();
				
				if( Utils.IsObjectAlive( selectable ) )
				{
					System.Type selectableType = selectable.GetType();
					
					if( ( typeof( TMPro.TMP_InputField ).EqualsOrAssignable( selectableType ) ) )
					{
						TMPro.TMP_InputField inputField = ( TMPro.TMP_InputField ) selectable;
						_editingInput = inputField.gameObject.activeSelf && inputField.enabled && inputField.isFocused;
					}
					else if( typeof( UnityEngine.UI.InputField ).EqualsOrAssignable( selectableType ) )
					{
						UnityEngine.UI.InputField inputField = ( UnityEngine.UI.InputField ) selectable;
						_editingInput = inputField.gameObject.activeSelf && inputField.enabled && inputField.isFocused;
					}
				}

				if( resetEditingInput )
				{
					if( _editingInput )
						editingInput = true;
					else
						editingInput = false;

					resetEditingInput = false;
				}
				else if( editingInput != _editingInput )
				{
					if( _editingInput )
						editingInput = true;
					else
						resetEditingInput = true;
				}

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

	public class KeyBindings
	{
		private const string jsonFileName = "KeyBindings.json";

		public static bool hasLoaded { get; private set; }
		private static bool loading = false;
		private static int saveRequests = 0;

		public static string GetJsonFilePath() =>
			System.IO.Path.Combine( Application.persistentDataPath, jsonFileName );

		private static int enabledCount = 0;
		public static Dictionary<string, KeyCode[]> binds = new Dictionary<string, KeyCode[]>();
		
		public static KeyCode[] GetKeyCodes( string key )
		{
			if( binds.ContainsKey( key ) )
				return new List<KeyCode>( binds[ key ] ).ToArray();
			else
				return new KeyCode[ 0 ];
		}

		public static void SetKeyCodes( string key, KeyCode[] keyCodes )
		{
			if( string.IsNullOrWhiteSpace( key ) )
				return;

			HashSet<KeyCode> hashSet = new HashSet<KeyCode>();
			for( int i = 0, iC = keyCodes.Length; i < iC; i++ )
				if( keyCodes[ i ] != KeyCode.None )
					hashSet.Add( keyCodes[ i ] );

			keyCodes = new List<KeyCode>( hashSet ).ToArray();

			if( keyCodes.Length == 0 )
			{
				if( binds.ContainsKey( key ) )
					binds.Remove( key );
			}
			else
			{
				if( binds.ContainsKey( key ) )
					binds[ key ] = keyCodes;
				else
					binds.Add( key, keyCodes );
			}
		}

		public static void OnEnable()
		{
			if( enabledCount++ == 0 )
			{
				Load();
			}
		}

		public static void OnDisable()
		{
			Save();
		}

		public static void Save()
		{
			saveRequests++;

			if( saveRequests > 1 )
				return;

			_Save();
		}

		private static void _Save()
		{
			string jsonFilePath = GetJsonFilePath();

			Utils.WaitForTask( new Task( () =>
			{
				string jsonFileDirectory = Path.GetDirectoryName( jsonFilePath );

				if( File.Exists( jsonFilePath ) )
					File.Delete( jsonFilePath );

				if( !Directory.Exists( jsonFileDirectory ) )
					Directory.CreateDirectory( jsonFileDirectory );

				if( Directory.Exists( jsonFileDirectory ) && !File.Exists( jsonFilePath ) )
				{
					using( StreamWriter streamWriter = File.CreateText( jsonFilePath ) )
					{
						JsonTextWriter jsonWriter = new JsonTextWriter( streamWriter );
						jsonWriter.Formatting = Formatting.Indented;

						JObject jObj = new JObject();

						List<string> bindKeys = new List<string>( binds.Keys );
						bindKeys.Sort();

						foreach( string bindKey in bindKeys )
						{
							KeyCode[] keyCodes = binds[ bindKey ];

							if( keyCodes.Length > 0 )
							{
								JArray jArray = new JArray();

								foreach( KeyCode keyCode in keyCodes )
									jArray.Add( keyCode.ToString() );

								jObj.Add( bindKey, jArray );
							}
						}

						jObj.WriteTo( jsonWriter );
					}
				}

				saveRequests--;

			} ), () =>
			{
				if( saveRequests > 0 )
					_Save();
			} );
		}

		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.BeforeSceneLoad )]
		public static void Load()
		{
			if( loading )
				return;

			loading = true;

			string jsonFilePath = GetJsonFilePath();

			Utils.WaitForTask( new Task<Dictionary<string,KeyCode[]>>( () =>
			{
				Dictionary<string, KeyCode[]> binds = new Dictionary<string, KeyCode[]>();

				if( File.Exists( jsonFilePath ) )
				{
					using( StreamReader streamReader = new StreamReader( jsonFilePath ) )
					{
						JsonTextReader jsonReader = new JsonTextReader( streamReader );

						JObject jObj = null;

						try
						{
							jObj = ( JObject ) JToken.ReadFrom( jsonReader );
							
							foreach( KeyValuePair<string, JToken> kvp in jObj )
							{
								JArray jArray = ( JArray ) kvp.Value;

								HashSet<KeyCode> hashSet = new HashSet<KeyCode>();
								foreach( JToken jToken in jArray )
								{
									string keyCodeString = jToken.Value<string>();

									if( !string.IsNullOrEmpty( keyCodeString ) )
									{
										KeyCode key;
										if( System.Enum.TryParse( keyCodeString, out key ) && key != KeyCode.None )
											hashSet.Add( key );
									}
								}

								if( hashSet.Count > 0 )
									binds.Add( kvp.Key, new List<KeyCode>( hashSet ).ToArray() );
							}
						}
						catch { }
					}
				}

				hasLoaded = true;
				loading = false;

				return binds;
			} ),
			( Dictionary<string,KeyCode[]> binds ) =>
			{
				foreach( KeyValuePair<string, KeyCode[]> kvp in binds )
				{
					SetKeyCodes( kvp.Key, kvp.Value );
				}
			} );
		}
	}

	public class KeyEvents : MonoBehaviour
	{
		[System.Serializable]
		public class KeyEvent
		{
			public string preferencesKey = string.Empty;

			public KeyCode[] keys;

			public bool invokeDuringInput = false;

			public UnityEvent onDown = new UnityEvent();
			public UnityEvent onUp = new UnityEvent();
		}

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

		public bool OnKeyDown( KeyCode keyCode )
		{
			bool editingInput = KeyEventSingleton.editingInput;
			
			List<KeyEvent> keyEvents = lookup[ keyCode ];
			foreach( KeyEvent keyEvent in keyEvents )
			{
				if( keyEvent.invokeDuringInput || !editingInput )
					keyEvent.onDown.Invoke();
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