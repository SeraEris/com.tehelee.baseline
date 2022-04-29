using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tehelee.Baseline
{
	[System.Serializable]
	public class KeyEvent
	{
		public string preferencesKey = string.Empty;

		public KeyCode[] keys;

		public bool invokeDuringInput = false;

		public UnityEvent onDown = new UnityEvent();
		public UnityEvent onUp = new UnityEvent();

		public delegate void OnKey( KeyCode keyCode );
		public event OnKey onKeyDown;
		public event OnKey onKeyUp;

		public void InvokeDown( KeyCode keyCode )
		{
			onDown?.Invoke();
			onKeyDown?.Invoke( keyCode );
		}

		public void InvokeUp( KeyCode keyCode )
		{
			onUp?.Invoke();
			onKeyUp?.Invoke( keyCode );
		}
	}
	
	public interface IKeyEventReceiver
	{
		IList<KeyEvent> GetKeyEvents();

		bool OnKeyDown( KeyCode keyCode );
		bool OnKeyUp( KeyCode keyCode );
	}
	
    public static class KeyEventSingleton
	{
		private static HashSet<IKeyEventReceiver> listeners = new HashSet<IKeyEventReceiver>();
		public static int listenersCount => listeners.Count;

		private static Dictionary<KeyCode, HashSet<IKeyEventReceiver>> lookup = new Dictionary<KeyCode, HashSet<IKeyEventReceiver>>();

		private static Dictionary<IKeyEventReceiver, float> registerTime = new Dictionary<IKeyEventReceiver, float>();

		private static KeyCode[] allKeys = new KeyCode[ 0 ];
		private static bool[] keyState = new bool[ 0 ];
		
		public delegate void OnEditingInputChanged( bool editingInput );
		public static event OnEditingInputChanged onEditingInputChanged;

		private static bool _editingInput = false;
		public static bool editingInput
		{
			get => _editingInput;
			private set
			{
				if( _editingInput != value )
				{
					_editingInput = value;
					onEditingInputChanged?.Invoke( _editingInput );
				}
			}
		}
		
		private static bool resetEditingInput = false;

		public static void Register( IKeyEventReceiver keyEvents )
		{
			if( keyEvents == null )
				return;

			listeners.Add( keyEvents );

			registerTime.Add( keyEvents, Time.time );

			if( object.Equals( null, _IKeyWatcher ) )
				_IKeyWatcher = Utils.StartCoroutine( IKeyWatcher() );

			foreach( KeyEvent keyEvent in keyEvents.GetKeyEvents() )
			{
				foreach( KeyCode keyCode in keyEvent.keys )
				{
					if( !lookup.ContainsKey( keyCode ) )
						lookup.Add( keyCode, new HashSet<IKeyEventReceiver>() );

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

		public static void UnRegister( IKeyEventReceiver keyEvents )
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
				Dictionary<KeyCode, HashSet<IKeyEventReceiver>> modified = new Dictionary<KeyCode, HashSet<IKeyEventReceiver>>();

				foreach( KeyCode keyCode in lookup.Keys )
				{
					HashSet<IKeyEventReceiver> hashSet = lookup[ keyCode ];
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

		private static Dictionary<KeyCode, List<IKeyEventReceiver>> triggerKeyDownEvents = new Dictionary<KeyCode, List<IKeyEventReceiver>>();
		private static Dictionary<KeyCode, List<IKeyEventReceiver>> triggerKeyUpEvents = new Dictionary<KeyCode, List<IKeyEventReceiver>>();
		
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
						HashSet<IKeyEventReceiver> _triggers = new HashSet<IKeyEventReceiver>();
						foreach( IKeyEventReceiver keyEvents in lookup[ keyCode ] )
							_triggers.Add( keyEvents );

						List<IKeyEventReceiver> triggers = new List<IKeyEventReceiver>( _triggers );
						triggers.Sort
						(
							( IKeyEventReceiver a, IKeyEventReceiver b ) =>
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
					foreach( IKeyEventReceiver keyEvents in triggerKeyDownEvents[ keyCode ] )
						if( keyEvents.OnKeyDown( keyCode ) )
							break;

				foreach( KeyCode keyCode in triggerKeyUpEvents.Keys )
					foreach( IKeyEventReceiver keyEvents in triggerKeyUpEvents[ keyCode ] )
						if( keyEvents.OnKeyUp( keyCode ) )
							break;

				yield return null;
			}
		}
	}
}
