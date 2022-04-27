using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
using System.Runtime.InteropServices;
#endif

namespace Tehelee.Baseline
{
	// This is a static utility class for method defines used in multiple locations

	public static class Utils
	{
		
		////////////////////////
		#region Quitting

		private enum QuitState
		{
			Running,
			Redirected,
			Exitable
		}
		private static QuitState quitState = QuitState.Running;
		
		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.AfterAssembliesLoaded )]
		private static void RegisterWantsToQuit()
		{
			bool quitRedirect()
			{
				switch( quitState )
				{
					default:
						StartCoroutine( IHandleQuit() );
						quitState = QuitState.Redirected;
						return false;
					case QuitState.Redirected:
						Debug.LogWarning( "Application.Quit() invoked multiple times." );
						return false;
					case QuitState.Exitable:
						return true;
				}
			}
			
			quitState = QuitState.Running;
			Application.wantsToQuit += quitRedirect;
		}

		private static HashSet<GetCoroutine> InvokeOnQuit = new HashSet<GetCoroutine>();

		public delegate IEnumerator GetCoroutine();
		
		public static void AddQuitCoroutine( GetCoroutine coroutine )
		{
			if( coroutine != null )
				InvokeOnQuit.Add( coroutine );
		}

		public static void RemoveQuitCoroutine( GetCoroutine coroutine )
		{
			if( coroutine != null )
				InvokeOnQuit.Remove( coroutine );
		}

		private static IEnumerator IHandleQuit()
		{
			foreach( GetCoroutine invoke in InvokeOnQuit )
			{
				IEnumerator coroutine = invoke?.Invoke();
				if( coroutine != null )
					yield return StartCoroutine( coroutine );
			}

			quitState = QuitState.Exitable;

			Application.Quit();

			yield break;
		}
		
		#endregion
		
		////////////////////////
		#region DelayHelper

		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.AfterAssembliesLoaded )]
		private static void OnAssembliesLoaded()
		{
			IsShuttingDown = false;	
			Application.quitting += OnShutdown;
			cts = new CancellationTokenSource();
			#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			#endif
		}
		
		#if UNITY_EDITOR
		private static void OnPlayModeStateChanged( PlayModeStateChange playModeStateChange )
		{
			if( playModeStateChange == PlayModeStateChange.EnteredEditMode )
			{
				IsShuttingDown = false;
			}
		}
		#endif
		
		private static void OnShutdown()
		{
			Application.quitting -= OnShutdown;
			
			if( IsObjectAlive( delaySlave ) )
				delaySlave.StopAllCoroutines();
			
			cts.Cancel();
			
			IsShuttingDown = true;
		}

		public static bool IsShuttingDown { get; internal set; }

		private static System.Threading.CancellationTokenSource cts;

		private static List<IEnumerator> pendingCoroutines = new List<IEnumerator>();

		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.AfterSceneLoad )]
		private static void SpawnDelaySlave()
		{
			_delaySlave = new GameObject( "[Delay Slave]", typeof( DelaySlave ) ).GetComponent<DelaySlave>();
			_delaySlave.gameObject.hideFlags = HideFlags.HideAndDontSave;
			GameObject.DontDestroyOnLoad( _delaySlave.gameObject );

			foreach( IEnumerator pendingCoroutine in pendingCoroutines )
				if( !object.Equals( null, pendingCoroutine ) )
					_delaySlave.StartCoroutine( pendingCoroutine );
		}

		private static DelaySlave _delaySlave = null;
		private static DelaySlave delaySlave
		{
			get
			{
				if( !Utils.IsObjectAlive( _delaySlave ) )
					SpawnDelaySlave();

				return _delaySlave;
			}
		}

		public static void Delay( float delay, System.Action action )
		{
			if( object.Equals( null, action ) )
				return;

			delaySlave.Delay( delay, action );
		}

		public static Coroutine StartCoroutine( IEnumerator routine )
		{
			return Utils.IsObjectAlive( delaySlave ) ? delaySlave.StartCoroutine( routine ) : null;
		}

		public static void StopCoroutine( Coroutine coroutine )
		{
			if( object.Equals( null, coroutine ) )
				return;
			if( object.Equals( null, _delaySlave ) )
				return;

			_delaySlave.StopCoroutine( coroutine );
		}

		#endregion

		////////////////////////
		#region Hashing

		private static readonly ushort CrcPolynomial = 0xA001;

		private static ushort[] _CrcTable = null;
		private static ushort[] CrcTable
		{
			get
			{
				if( _CrcTable == null )
				{
					ushort[] table = new ushort[ 256 ];

					ushort value;
					ushort temp;

					for( ushort i = 0; i < 256; ++i )
					{
						value = 0;
						temp = i;
						for( byte j = 0; j < 8; j++ )
						{
							if( ( ( value ^ temp ) & 0x0001 ) != 0 )
							{
								value = ( ushort ) ( ( value >> 1 ) ^ CrcPolynomial );
							}
							else
							{
								value >>= 1;
							}
							temp >>= 1;
						}
						table[ i ] = value;
					}

					_CrcTable = table;
				}

				return _CrcTable;
			}
		}

		public static ushort HashCRC( string str )
		{
			byte[] bytes = Encoding.UTF8.GetBytes( str );

			ushort[] table = CrcTable;

			ushort crc = 0xFFF;
			for( int i = 0; i < bytes.Length; ++i )
			{
				byte index = ( byte ) ( crc ^ bytes[ i ] );
				crc = ( ushort ) ( ( crc >> 8 ) ^ table[ index ] );
			}

			return crc;
		}

		public static ulong HashSDBM( string str )
		{
			ulong hash = 0;

			for( ulong i = 0, c = ( ulong ) str.Length; i < c; i++ )
			{
				hash = i + ( hash << 6 ) + ( hash << 16 ) - hash;
			}

			return hash;
		}

		#endregion

		////////////////////////
		#region Launch Arguments

		public static string GetArg( string key )
		{
			if( string.IsNullOrEmpty( key ) )
				return null;
			
#if( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
			string[] _args = System.Environment.GetCommandLineArgs();
			for( int i = 0; i < _args.Length; i++ )
			{
				if( $"-{key}".Equals( _args[ i ] ) )
				{
					return _args[ ++i ];
				}
			}
#endif
			return null;
		}

		public static Dictionary<string, string> GetArgs( params string[] keys )
		{
			Dictionary<string, string> lookup = new Dictionary<string, string>();
			
			if( object.Equals( null, keys ) || keys.Length == 0 )
				return lookup;
			
			foreach( string key in keys )
				if( !string.IsNullOrEmpty( key ) && !lookup.ContainsKey( key ) )
					lookup.Add( key, null );

			GetArgsFromDictionary( ref lookup );

			return lookup;
		}

		public static void GetArgsFromDictionary( ref Dictionary<string, string> args )
		{
#if( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
			string[] _args = System.Environment.GetCommandLineArgs();
			List<string> keys = new List<string>( args.Keys );
			HashSet<string> missing = new HashSet<string>( args.Keys );
			for( int i = 0; i < _args.Length; i++ )
			{
				foreach( string key in keys )
				{
					if( string.Format( "-{0}", key ).Equals( _args[ i ] ) )
					{
						int valId = i + 1;
						string val = _args.Length == valId ? string.Empty : _args[ valId ];
						args[ key ] = val.StartsWith( "-" ) ? string.Empty : val;
						missing.Remove( key );
						break;
					}
				}
			}

			foreach( string key in missing )
				args[ key ] = null;
#endif
		}

		#endregion

		////////////////////////
		#region Windows 

#if( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
		[return: MarshalAs( UnmanagedType.Bool )]
		[DllImport( "user32.dll", EntryPoint = "SetCursorPos" )]
		public static extern bool SetCursorPos( int x, int y );

		[System.Serializable]
		public struct WinPoint
		{
			public int x;
			public int y;

			public override bool Equals( object obj )
			{
				if( !( obj is WinPoint ) )
					return false;

				WinPoint point = ( WinPoint ) obj;
				return ( this == point );
			}

			public override int GetHashCode()
			{
				return ( x.GetHashCode() ^ y.GetHashCode() );
			}

			public static bool operator ==( WinPoint a, WinPoint b )
			{
				return ( ( a.x == b.x ) && ( a.y == b.y ) );
			}

			public static bool operator !=( WinPoint a, WinPoint b )
			{
				return ( ( a.x != b.x ) || ( a.y != b.y ) );
			}

			public static implicit operator Vector2( WinPoint point )
			{
				return new Vector2( point.x, point.y );
			}

			public static implicit operator WinPoint( Vector2 vector )
			{
				return new WinPoint() { x = Mathf.RoundToInt( vector.x ), y = Mathf.RoundToInt( vector.y ) };
			}
		}

		[DllImport( "user32.dll", EntryPoint = "GetCursorPos" )]
		public static extern bool GetCursorPos( out WinPoint point );

		[DllImport( "user32.dll", EntryPoint = "GetActiveWindow" )]
		public static extern System.IntPtr GetActiveWindow();

		[DllImport( "user32.dll", EntryPoint = "FindWindow" )]
		public static extern System.IntPtr FindWindow( System.String className, System.String windowName );

		[StructLayout( LayoutKind.Sequential )]
		public struct WinRect
		{
			public int Left, Top, Right, Bottom;
		}

		[StructLayout( LayoutKind.Sequential )]
		public struct WinWindoInfo
		{
			public uint cbSize;
			public WinRect rcWindow;
			public WinRect rcClient;
			public uint dwStyle;
			public uint dwExStyle;
			public uint dwWindowStatus;
			public uint cxWindowBorders;
			public uint cyWindowBorders;
			public ushort atomWindowType;
			public ushort wCreatorVersion;

			public WinWindoInfo( System.Boolean? filler ) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
			{
				cbSize = ( System.UInt32 ) ( Marshal.SizeOf( typeof( WinWindoInfo ) ) );
			}

		}

		[return: MarshalAs( UnmanagedType.Bool )]
		[DllImport( "user32.dll", SetLastError = true )]
		public static extern bool GetWindowInfo( System.IntPtr hwnd, ref WinWindoInfo pwi );

		public static System.IntPtr GetWindowHandle()
		{
			return GetActiveWindow();
		}
#else
		public struct WINDOWINFO
		{
			public WINDOWINFO( bool? filler ) : this() { }
		}

		public static bool SetCursorPos( int x, int y ) => false;

		public static System.IntPtr GetActiveWindow() => default;

		public static System.IntPtr FindWindow( System.String className, System.String windowName ) => default;

		public static bool GetWindowInfo( System.IntPtr hwnd, ref WINDOWINFO pwi ) { pwi = new WINDOWINFO( null ); return false; }

		public static System.IntPtr GetWindowHandle() => default;
#endif

		#endregion

		////////////////////////
		#region Debug

		public static void DebugPoint( Vector3 point, Color color = default( Color ), float size = 0.1f, float time = 0f )
		{
			if( time == 0f )
				time = Time.deltaTime * 1.1f;

			if( color == default( Color ) )
				color = Color.cyan;

			size *= 0.5f;

			Debug.DrawLine( point + Vector3.right * -size, point + Vector3.right * size, color, time );
			Debug.DrawLine( point + Vector3.forward * -size, point + Vector3.forward * size, color, time );
			Debug.DrawLine( point + Vector3.up * -size, point + Vector3.up * size, color, time );
		}
		
		#endregion

		////////////////////////
		#region HashSet

		public static void AddRange<T>( this HashSet<T> hashSet, IEnumerable<T> range )
		{
			foreach( T add in range )
			{
				if( !object.Equals( null, add ) )
					hashSet.Add( add );
			}
		}

		public static List<T> ToList<T>( this HashSet<T> hashSet ) => new List<T>( hashSet );
		public static T[] ToArray<T>( this HashSet<T> hashSet ) => new List<T>( hashSet ).ToArray();

		#endregion

		////////////////////////
		#region List

		public static void Shuffle<T>( this IList<T> list )
		{
			if( list.Count <= 1 )
				return;
			
			T value;
			for( int i = list.Count - 1, key; i > 1; i-- )
			{
				key = Random.Range( 0, i + 1 );
				value = list[ key ];
				list[ key ] = list[ i ];
				list[ i ] = value;
			}
		}

		public static void RemoveEmptyEntries<T>( this IList<T> list ) where T : class
		{
			List<T> _list = new List<T>( list );
			list.Clear();
			foreach( T entry in _list )
				if( !object.Equals( null, entry ) )
					list.Add( entry );
		}
		
		public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize=30)  
		{        
			for (int i = 0; i < locations.Count; i += nSize) 
			{ 
				yield return locations.GetRange(i, Mathf.Min(nSize, locations.Count - i)); 
			}  
		}

		public static T GetFirst<T>( this IList<T> list, bool returnNulls = false ) where T : class
		{
			if( list.Count == 0 )
				return null;

			foreach( T entry in list )
			{
				if( object.Equals( null, entry ) && !returnNulls )
					continue;
				
				return entry;
			}

			return null;
		}
		
		public static T GetLast<T>( this IList<T> list, bool returnNulls = false ) where T : class
		{
			int count = list.Count;
			
			if( count == 0 )
				return null;

			for( int i = count - 1; i >= 0; i-- )
			{
				T entry = list[ i ];
				
				if( object.Equals( null, entry ) && !returnNulls )
					continue;
				
				return entry;
			}

			return null;
		}
		
		public static T GetRandom<T>( this IList<T> list, bool returnNulls = false ) where T : class
		{
			if( list.Count == 0 )
				return null;

			List<T> _list = new List<T>( list );
			_list.Shuffle();

			return _list.GetFirst( returnNulls );
		}

		#endregion

		////////////////////////
		#region Dictionary

		public static void InsertOrReplace<K,V>( this IDictionary<K,V> dictionary, K key, V value )
		{
			if( dictionary.ContainsKey( key ) )
				dictionary[ key ] = value;
			else
				dictionary.Add( key, value );
		}
		
		public static T MergeLeft<T, K, V>( this T me, params IDictionary<K, V>[] others ) where T : IDictionary<K, V>, new()
		{
			T newMap = new T();
			List<IDictionary<K, V>> list = new List<IDictionary<K, V>>();
			list.Add( me );
			list.AddRange( others );

			foreach( IDictionary<K, V> src in list )
			{
				foreach( KeyValuePair<K, V> p in src )
				{
					newMap[ p.Key ] = p.Value;
				}
			}
			return newMap;
		}

		#endregion

		////////////////////////
		#region StringHelpers

		private static char[] splitSpace = new char[] { ' ', '_' };

		/// <summary>
		/// PascalCase
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string ToPascalCase( string input, bool underscoreSpaces = false )
		{
			if( string.IsNullOrEmpty( input ) )
				return string.Empty;

			if( input.Length <= 3 )
				return input.ToUpper();

			Regex invalidCharsRgx = new Regex( "[^_a-zA-Z0-9]" );
			Regex whiteSpace = new Regex( @"(?<=\s)" );
			Regex startsWithLowerCaseChar = new Regex( "^[a-z]" );
			Regex firstCharFollowedByUpperCasesOnly = new Regex( "(?<=[A-Z])[A-Z0-9]+$" );
			Regex lowerCaseNextToNumber = new Regex( "(?<=[0-9])[a-z]" );
			Regex upperCaseInside = new Regex( "(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))" );

			// replace white spaces with undescore, then replace all invalid chars with empty string
			var pascalCase = invalidCharsRgx.Replace( whiteSpace.Replace( input, "_" ), string.Empty )
				// split by underscores
				.Split( new char[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries )
				// set first letter to uppercase
				.Select( w => startsWithLowerCaseChar.Replace( w, m => m.Value.ToUpper() ) )
				// replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
				.Select( w => firstCharFollowedByUpperCasesOnly.Replace( w, m => m.Value.ToLower() ) )
				// set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
				.Select( w => lowerCaseNextToNumber.Replace( w, m => m.Value.ToUpper() ) )
				// lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
				.Select( w => upperCaseInside.Replace( w, m => m.Value.ToLower() ) );

			return string.Concat( pascalCase );
		}

		/// <summary>
		/// camelCase
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string ToCamelCase( string input, bool underscoreSpaces = false )
		{
			if( string.IsNullOrEmpty( input ) )
				return string.Empty;

			string s, output = "";
			string[] split = input.Split( splitSpace, System.StringSplitOptions.RemoveEmptyEntries );
			bool first = true;

			for( int i = 0, c = split.Length; i < c; i++ )
			{
				s = split[ i ];

				output += ( first ? char.ToLower( s[ 0 ] ) : char.ToUpper( s[ 0 ] ) ) + s.Substring( 1 ).ToLower();

				if( underscoreSpaces && ( i < ( c - 1 ) ) )
					output += "_";

				first = false;
			}

			return output;
		}

		public static string SpaceCondensedString( string input )
		{
			if( string.IsNullOrEmpty( input ) )
				return string.Empty;

			string output = input.Length > 0 ? input[ 0 ].ToString() : string.Empty;
			for( int i = 1, c = input.Length; i < c; i++ )
			{
				if( char.IsUpper( input[ i ] ) )
					output += ' ';
				output += input[ i ];
			}

			return output;
		}

		public static string FormatVariableName( string variableName )
		{
			return SpaceCondensedString( ToPascalCase( SpaceCondensedString( variableName ) ) );
		}

		public static string GetFileTimeString( System.DateTime dateTime ) =>
			$"{dateTime:MM}-{dateTime:dd}-{dateTime:yyyy}_{dateTime:HH}-{dateTime:mm}-{dateTime:ss}_{dateTime:FFFFFFF}";

		public static string ArrayToString<T>( T[] array, bool newLines = false, bool number = false, sbyte precision = -1 )
		{
			string s = "";
			int i, c = array.Length - 1;

			if( c <= 0 )
				return s;

			string decimalFormat = null, formatString = null;

			bool hasPrecision = ( precision >= 0 );

			if( precision == 0 )
			{
				decimalFormat = "0";
			}
			else if( precision > 0 )
			{
				decimalFormat = "0.0";
				for( int p = 1; p < precision; p++ )
					decimalFormat += "0";
			}

			if( number )
			{
				if( newLines )
				{
					formatString = hasPrecision ? "[{0}]: {1:" + decimalFormat + "}\n" : "[{0}]: {1}\n";
					for( i = 0; i < c; i++ )
						s += string.Format( formatString, i, array[ i ].ToString() );
				}
				else
				{
					formatString = hasPrecision ? "[{0}]: {1:" + decimalFormat + "}, " : "[{0}]: {1}, ";
					for( i = 0; i < c; i++ )
						s += string.Format( formatString, i, array[ i ].ToString() );
				}
			}
			else
			{
				if( newLines )
				{
					formatString = hasPrecision ? "{0:" + decimalFormat + "}\n" : "{0}\n";
					for( i = 0; i < c; i++ )
						s += string.Format( formatString, array[ i ].ToString() );
				}
				else
				{
					formatString = hasPrecision ? "{0:" + decimalFormat + "}, " : "{0}, ";
					for( i = 0; i < c; i++ )
						s += string.Format( formatString, array[ i ].ToString() );
				}
			}

			i++;

			if( i < array.Length )
			{
				if( number )
				{
					s += string.Format( "[{0}]: {1}", i, array[ i ].ToString() );
				}
				else
				{
					s += array[ i ].ToString();
				}
			}

			return s;
		}

		public static string GetDecimalPrecisionFormat( byte precision )
		{
			string decimalFormat = precision == 0 ? "0" : "0.0";
			for( int p = 1; p < precision; p++ )
				decimalFormat += "0";

			return decimalFormat;
		}

		#endregion

		////////////////////////
		#region Safe Parse

		// 8-bit

		public static sbyte SafeParse( string value, sbyte defaultValue = 0 )
		{
			sbyte _value = defaultValue;
			sbyte.TryParse( value, out _value );
			return _value;
		}

		public static byte SafeParse( string value, byte defaultValue = 0 )
		{
			byte _value = defaultValue;
			byte.TryParse( value, out _value );
			return _value;
		}

		// 16-bit

		public static short SafeParse( string value, short defaultValue = 0 )
		{
			short _value = defaultValue;
			short.TryParse( value, out _value );
			return _value;
		}

		public static ushort SafeParse( string value, ushort defaultValue = 0 )
		{
			ushort _value = defaultValue;
			ushort.TryParse( value, out _value );
			return _value;
		}

		// 32-bit

		public static uint SafeParse( string value, uint defaultValue = 0 )
		{
			uint _value = defaultValue;
			uint.TryParse( value, out _value );
			return _value;
		}

		public static int SafeParse( string value, int defaultValue = 0 )
		{
			int _value = defaultValue;
			int.TryParse( value, out _value );
			return _value;
		}

		// 64-bit

		public static long SafeParse( string value, long defaultValue = 0 )
		{
			long _value = defaultValue;
			long.TryParse( value, out _value );
			return _value;
		}

		public static ulong SafeParse( string value, ulong defaultValue = 0 )
		{
			ulong _value = defaultValue;
			ulong.TryParse( value, out _value );
			return _value;
		}

		#endregion

		////////////////////////
		#region Component

		public static void DestroyEditorSafe( Object obj )
		{
			if( !IsObjectAlive( obj ) )
				return;

#if UNITY_EDITOR
			if( Application.isPlaying )
				Object.Destroy( obj );
			else
				Object.DestroyImmediate( obj );
#else
			Object.Destroy( obj );
#endif
		}

		public static T[] FindInScene<T>( UnityEngine.SceneManagement.Scene scene ) where T : Component
		{
			if( !scene.isLoaded )
				return new T[ 0 ];

			GameObject[] rootObjects = scene.GetRootGameObjects();
			List<T> foundObjects = new List<T>();
			foreach( GameObject rootObject in rootObjects )
			{
				foundObjects.AddRange( rootObject.GetComponentsInChildren<T>() );
			}

			return foundObjects.ToArray();
		}

#endregion

		////////////////////////
		#region Clone Serialized Fields

		public static void CloneSerializedFields( object copy, object paste ) =>
			JsonUtility.FromJsonOverwrite( JsonUtility.ToJson( copy ), paste );
		
		#endregion

		////////////////////////
		#region Unity Object

		// Shortcut to check if an object is null or if it's been destroyed.
		public static bool IsObjectAlive( Object obj )
		{
			if( object.Equals( null, obj ) )
				return false;

			return obj;
		}

		#endregion

		////////////////////////
		#region Sound

		private static Stack<AudioSource> AudioPool = new Stack<AudioSource>();

		private static void PoolAudio()
		{
			if( AudioPool.Count == 0 )
			{
				for( int i = 0; i < 2; i++ )
				{
					GameObject gameObject = new GameObject( "[AudioSource]", typeof( AudioSource ) );
					gameObject.hideFlags = HideFlags.HideAndDontSave;
					GameObject.DontDestroyOnLoad( gameObject );

					AudioPool.Push( gameObject.GetComponent<AudioSource>() );
				}
			}
		}

		public static void PlayAudio( Vector3 position, AudioClip audioClip, float volume = 1f )
		{
			if( object.Equals( null, audioClip ) )
				return;

			PoolAudio();

			AudioSource audioSource = AudioPool.Pop();

			audioSource.volume = volume;

			audioSource.clip = audioClip;

			audioSource.Play();

			Utils.Delay( audioClip.length, () => { AudioPool.Push( audioSource ); } );
		}

		public static void PlayAudio( Vector3 position, AudioClip audioClip, AudioParameters audioParameters = default( AudioParameters ) )
		{
			if( object.Equals( null, audioClip ) )
				return;

			PoolAudio();

			AudioSource audioSource = AudioPool.Pop();

			audioParameters.Apply( audioSource );

			audioSource.clip = audioClip;

			audioSource.Play();

			Utils.Delay( audioClip.length * ( 1f / audioSource.pitch ), () => { AudioPool.Push( audioSource ); } );
		}

		#endregion

		////////////////////////
		#region Rigidbody
		
		public static void RotateRigidbody( Rigidbody rigidbody, Vector3 from, Vector3 to, float power = 1f, float maxAngular = 12f, bool useMass = false )
		{
			float angle = Vector3.Angle( from, to );

			if( power != 1f )
				angle = 1f - Mathf.Pow( 1f - ( angle / 180f ), power );

			Vector3 rotCross = Vector3.Cross( from, to );
			float rotTheta = Mathf.Asin( rotCross.magnitude );
			Vector3 rotDelta = rotCross.normalized * ( rotTheta / Time.fixedDeltaTime );

			Vector3 torque;
			
			if( useMass )
			{
				Quaternion rotInertia = rigidbody.transform.rotation * rigidbody.inertiaTensorRotation;
				torque = rotInertia * Vector3.Scale( rigidbody.inertiaTensor, ( Quaternion.Inverse( rotInertia ) * rotDelta ) ) * angle;
			}
			else
			{
				torque = rotDelta - rigidbody.angularVelocity;
			}
			
			if( torque.magnitude > maxAngular )
				torque = torque.normalized * maxAngular;

			rigidbody.AddTorque( torque - rigidbody.angularVelocity, useMass ? ForceMode.Impulse : ForceMode.VelocityChange );
		}

		#endregion

		////////////////////////
		#region Rounding
		
		public static float Round( this float a, string format = "0.0" )
		{
			float b = a;
			format = "{0:" + format + "}";
			float.TryParse( string.Format( format, b ), out b );

			return b;
		}
		
		public static Vector2 Round( this Vector2 a, string format = "0.0" )
		{
			Vector2 b = a;
			format = "{0:" + format + "}";
			float.TryParse( string.Format( format, b.x ), out b.x );
			float.TryParse( string.Format( format, b.y ), out b.y );

			return b;
		}

		public static Vector3 Round( this Vector3 a, string format = "0.0" )
		{
			Vector3 b = a;
			format = "{0:" + format + "}";
			float.TryParse( string.Format( format, b.x ), out b.x );
			float.TryParse( string.Format( format, b.y ), out b.y );
			float.TryParse( string.Format( format, b.z ), out b.z );

			return b;
		}
		
		#endregion

		////////////////////////
		#region Vector3

		public static Vector3 Multiply( this Vector3 a, Vector3 b )
		{
			return new Vector3
			(
				a.x * b.x,
				a.y * b.y,
				a.z * b.z
			);
		}

		public static Vector3 Divide( this Vector3 a, Vector3 b )
		{
			Vector3 c = new Vector3
			(
				a.x / ( b.x == 0f ? 1f : b.x ),
				a.y / ( b.y == 0f ? 1f : b.y ),
				a.z / ( b.z == 0f ? 1f : b.z )
			);
			c = new Vector3
			(
				float.IsNaN( c.x ) ? 0f : c.x,
				float.IsNaN( c.y ) ? 0f : c.y,
				float.IsNaN( c.z ) ? 0f : c.z
			);
			return c;
		}

		public static Vector3 Clamp( this Vector3 a, float max ) =>
			a.magnitude > max ? a.normalized * max : a;

		#endregion
		
		////////////////////////
		#region Quaternion

		public static Vector2 GetLookVector( this Quaternion rotation )
		{
			Vector2 look = Vector2.zero;
			look.x = Vector3.SignedAngle( Vector3.forward, Vector3.ProjectOnPlane( rotation * Vector3.forward, Vector3.up ), Vector3.up );
			Quaternion pitch = Quaternion.Inverse( Quaternion.AngleAxis( look.x, Vector3.up ) ) * rotation;
			look.y = Vector3.SignedAngle( Vector3.forward, Vector3.ProjectOnPlane( pitch * Vector3.forward, Vector3.right ), Vector3.right );
			
			return look;
		}
		#endregion

		////////////////////////
		#region NaN

		public static bool IsNaN( this Vector2 a ) => float.IsNaN( a.x ) || float.IsNaN( a.y );
		public static bool IsNaN( this Vector3 a ) => float.IsNaN( a.x ) || float.IsNaN( a.y ) || float.IsNaN( a.z );
		public static bool IsNaN( this Vector4 a ) => float.IsNaN( a.x ) || float.IsNaN( a.y ) || float.IsNaN( a.z ) || float.IsNaN( a.w );
		public static bool IsNaN( this Quaternion a ) => float.IsNaN( a.x ) || float.IsNaN( a.y ) || float.IsNaN( a.z ) || float.IsNaN( a.w );
		
		public static bool IsInfinity( this Vector2 a ) => float.IsInfinity( a.x ) || float.IsInfinity( a.y );
		public static bool IsInfinity( this Vector3 a ) => float.IsInfinity( a.x ) || float.IsInfinity( a.y ) || float.IsInfinity( a.z );
		public static bool IsInfinity( this Vector4 a ) => float.IsInfinity( a.x ) || float.IsInfinity( a.y ) || float.IsInfinity( a.z ) || float.IsInfinity( a.w );
		public static bool IsInfinity( this Quaternion a ) => float.IsInfinity( a.x ) || float.IsInfinity( a.y ) || float.IsInfinity( a.z ) || float.IsInfinity( a.w );
		
		#endregion

		////////////////////////
		#region Clipboard

		private static TextEditor _clipboardTextEditor = null;
		private static TextEditor clipboardTextEditor
		{
			get
			{
				if( object.Equals( null, _clipboardTextEditor ) )
				{
					_clipboardTextEditor = new TextEditor();
				}
				return _clipboardTextEditor;
			}
		}

		public static string Clipboard
		{
			get
			{
				clipboardTextEditor.Paste();
				return clipboardTextEditor.text;
			}
			set
			{
				clipboardTextEditor.text = value;
				clipboardTextEditor.OnFocus();
				clipboardTextEditor.Copy();
				clipboardTextEditor.text = string.Empty;
			}
		}

		#endregion

		////////////////////////
		#region Threading
		
		public static void WaitForTask( Task task, System.Action callback = null )
		{
			if( IsShuttingDown )
			{
				task.Wait( 5000 );
				callback?.Invoke();
			}
			else if( !Utils.IsObjectAlive( delaySlave ) )
			{
				pendingCoroutines.Add( IWaitForTask( task, callback ) );
			}
			else
			{
				StartCoroutine( IWaitForTask( task, callback ) );
			}
		}

		private static IEnumerator IWaitForTask( Task task, System.Action callback = null )
		{
			if( !task.IsCompleted )
				if( task.Status == TaskStatus.Created )
					task.Start();
			do
				yield return null;
			while( !task.IsCompleted );

			callback?.Invoke();

			yield break;
		}

		public static void WaitForTask<T>( Task<T> task, System.Action<T> callback = null )
		{
			if( IsShuttingDown )
			{
				task.Wait( 5000 );
				callback?.Invoke( default );
			}
			else if( !Utils.IsObjectAlive( delaySlave ) )
			{
				pendingCoroutines.Add( IWaitForTask<T>( task, callback ) );
			}
			else
			{
				StartCoroutine( IWaitForTask<T>( task, callback ) );
			}
		}

		private static IEnumerator IWaitForTask<T>( Task<T> task, System.Action<T> callback = null )
		{
			if( !task.IsCompleted )
				if( task.Status == TaskStatus.Created )
					task.Start();
			do
				yield return null;
			while( !task.IsCompleted );

			callback?.Invoke( task.Result );

			yield break;
		}

#endregion

		////////////////////////
		#region Types

		private static Dictionary<string, System.Type> typeDictionary = new Dictionary<string, System.Type>();

		public static void ClearTypeCache()
		{
			typeDictionary.Clear();
		}

		private static void PopulateTypeCache()
		{
			if( object.Equals( null, typeDictionary ) )
				typeDictionary = new Dictionary<string, System.Type>();

			if( typeDictionary.Count == 0 )
			{
				foreach( System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies() )
				{
					foreach( System.Type type in assembly.GetTypes() )
					{
						string fullName = type.FullName;
						if( !typeDictionary.ContainsKey( fullName ) )
							typeDictionary.Add( fullName, type );
					}
				}
			}
		}

		public static System.Type FindType( string typeFullName )
		{
			PopulateTypeCache();

			return typeDictionary.ContainsKey( typeFullName ) ? typeDictionary[ typeFullName ] : null;
		}

		public static List<System.Type> FindSubTypes<T>( string fullNameStartsWith )
		{
			PopulateTypeCache();

			List<System.Type> types = new List<System.Type>();

			foreach( KeyValuePair<string, System.Type> kvp in typeDictionary )
				if( kvp.Key.StartsWith( fullNameStartsWith ) && typeof( T ).IsAssignableFrom( kvp.Value ) )
					types.Add( kvp.Value );

			return types;
		}

		public static bool EqualsOrAssignable( this System.Type type, System.Type assignable ) =>
			( ( type == assignable ) || type.IsAssignableFrom( assignable ) );

		#endregion

		////////////////////////
		#region Scene

		public static List<T> FindAllObjectsOfType<T>( this UnityEngine.SceneManagement.Scene scene ) where T : Component
		{
			List<T> list = new List<T>();
			
			if( scene.IsValid() && ( scene.isLoaded || !Application.isPlaying ) )
			{
				foreach( GameObject gameObject in scene.GetRootGameObjects() )
					if( Utils.IsObjectAlive( gameObject ) )
						list.AddRange( gameObject.GetComponentsInChildren<T>() );
			}

			return list;
		}

#endregion

		////////////////////////
		#region Bezier Curve

		// A = startPoint.position;
		// B = controlPointStart.position;
		// C = controlPointEnd.position;
		// D = endPoint.position;

		// DeCasteljausAlgorithm
		public static Vector3 SampleBezierCurve( Vector3 start, Vector3 startControl, Vector3 end, Vector3 endControl, float t )
		{
			//Linear interpolation = lerp = (1 - t) * a + t * b
			//Could use Vector3.Lerp(a, b, t)

			//To make it faster
			float oneMinusT = 1f - t;

			//Layer 1
			Vector3 q = oneMinusT * start + t * startControl;
			Vector3 r = oneMinusT * startControl + t * endControl;
			Vector3 s = oneMinusT * endControl + t * end;

			//Layer 2
			Vector3 p = oneMinusT * q + t * r;
			Vector3 u = oneMinusT * r + t * s;

			//Final interpolated position
			Vector3 v = oneMinusT * p + t * u;

			return v;
		}

		public static Vector3 GetBezierNormal( Vector3 start, Vector3 startControl, Vector3 end, Vector3 endControl, float t, float offset = 0.1f )
		{
			Vector3 previous = SampleBezierCurve( start, startControl, end, endControl, Mathf.Clamp( t - offset, 0f, 1f ) );
			Vector3 current = SampleBezierCurve( start, startControl, end, endControl, Mathf.Clamp( t, 0f, 1f ) );
			Vector3 next = SampleBezierCurve( start, startControl, end, endControl, Mathf.Clamp( t + offset, 0f, 1f ) );

			Vector3 forward = next - current;
			Vector3 back = current - previous;

			return ( forward + back ) * 0.5f;
		}

		public static float EstimateBezierLength( Vector3 start, Vector3 startControl, Vector3 end, Vector3 endControl, int samples = 10 )
		{
			float length = 0f;
			Vector3 lastPoint = start;
			for( int i = 0; i < samples; i++ )
			{
				Vector3 point = SampleBezierCurve( start, startControl, end, endControl, ( i + 1 ) / ( float ) samples );
				length += ( point - lastPoint ).magnitude;
				lastPoint = point;
			}

			return length;
		}

		public static void DrawBezierGizmo( Vector3 start, Vector3 startControl, Vector3 end, Vector3 endControl, float distance = 0.25f, byte limit = 0 )
		{
			float length = EstimateBezierLength( start, startControl, end, endControl );
			int desiredCount = length <= distance ? 1 : Mathf.FloorToInt( length / distance );
			if( limit > 0)
				desiredCount = Mathf.Min( limit, desiredCount );

			float step = 1f / desiredCount;

			for( int i = 0; i < desiredCount; i++ )
			{
				Vector3 s = SampleBezierCurve( start, startControl, end, endControl, i * step );
				Vector3 e = SampleBezierCurve( start, startControl, end, endControl, ( i + 1 ) * step );
				Gizmos.DrawLine( s, e );
			}

			Color color = Gizmos.color;

			Gizmos.DrawSphere( start, distance * 0.5f );
			Gizmos.DrawSphere( end, distance * 0.5f );
			
			Gizmos.color = new Color( color.r, color.g, color.b, color.a * 0.5f );
			Gizmos.DrawLine( start, startControl );
			Gizmos.DrawLine( end, endControl );

			if( color.r == color.g && color.g == color.b )
				Gizmos.color = Color.yellow;
			else
				Gizmos.color = new Color( color.b, color.r, color.g, color.a);
			Gizmos.DrawSphere( startControl, distance * 0.5f );
			Gizmos.DrawSphere( endControl, distance * 0.5f );

			Gizmos.color = color;
		}

		#endregion
	}

	public class AudioParameters
	{
		public float volume = 1f;
		public float pitch = 1f;
		public float panStereo = 0f;
		public float dopplerLevel = 1f;
		public float maxDistance = 100f;
		public float minDistance = 0.1f;
		public bool linearFalloff = false;

		public void Apply( AudioSource audioSource )
		{
			audioSource.volume = volume;
			audioSource.pitch = pitch;
			audioSource.panStereo = panStereo;
			audioSource.dopplerLevel = dopplerLevel;
			audioSource.maxDistance = maxDistance;
			audioSource.minDistance = minDistance;
			audioSource.rolloffMode = linearFalloff ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
		}
	}

	public class DelaySlave : MonoBehaviour
	{
		public void Delay( float delay, System.Action action ) { StartCoroutine( IDelay( delay, action ) ); }

		public IEnumerator IDelay( float delay, System.Action action )
		{
			yield return new WaitForSeconds( delay );

			if( !object.Equals( null, action ) )
				action();

			yield break;
		}
	}
}