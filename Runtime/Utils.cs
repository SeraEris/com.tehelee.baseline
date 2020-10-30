using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

#if( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
using System.Runtime.InteropServices;
#endif

namespace Tehelee.Baseline
{
	public delegate void Callback();

	public delegate void Callback<T>( T context );

	public delegate void Callback<T, T2>( T context, T2 context2 );

	// This is a static utility class for method defines used in multiple locations

	public static class Utils
	{
		////////////////
		// DelayHelper
		////////////////

		#region DelayHelper

		private static DelaySlave _delaySlave = null;
		private static DelaySlave delaySlave
		{
			get
			{
				if( !_delaySlave )
				{
					_delaySlave = new GameObject( "[Delay Slave]", typeof( DelaySlave ) ).GetComponent<DelaySlave>();
					_delaySlave.gameObject.hideFlags = HideFlags.HideAndDontSave;
					GameObject.DontDestroyOnLoad( _delaySlave.gameObject );
				}

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
			return delaySlave.StartCoroutine( routine );
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

		////////////////
		// Hashing
		////////////////

		#region Hasing

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
		// Program Arguments
		////////////////////////

		#region Args

		public static void GetArgsFromDictionary( ref Dictionary<string, string> args )
		{
#if( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
			string[] _args = System.Environment.GetCommandLineArgs();
			List<string> keys = new List<string>( args.Keys );
			for( int i = 0; i < _args.Length; i++ )
			{
				foreach( string key in keys )
				{
					if( string.Format( "-{0}", key ).Equals( _args[ i ] ) )
					{
						args[ key ] = _args[ ++i ];
					}
				}
			}
#endif
		}

		#endregion

		////////////////////////
		// Windows Helpers
		////////////////////////

		#region WindowsHelpers

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
		// Debug Helpers
		////////////////////////

		#region DebugHelpers

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

		////////////////
		// HashSet Helpers
		////////////////

		#region HashSet

		public static void AddRange<T>( this HashSet<T> hashSet, IEnumerable<T> range )
		{
			foreach( T add in range )
			{
				if( !object.Equals( null, add ) )
					hashSet.Add( add );
			}
		}

		#endregion

		////////////////
		// List Helpers
		////////////////

		#region List

		public static void Shuffle<T>( this IList<T> list )
		{
			T value;
			for( int i = list.Count - 1, key; i > 1; i-- )
			{
				key = Random.Range( 0, i + 1 );
				value = list[ key ];
				list[ key ] = list[ i ];
				list[ i ] = value;
			}
		}

		#endregion

		////////////////
		// String Helpers
		////////////////

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

			string s, output = "";
			string[] split = input.Split( splitSpace, System.StringSplitOptions.RemoveEmptyEntries );

			for( int i = 0, c = split.Length; i < c; i++ )
			{
				s = split[ i ];

				output += char.ToUpper( s[ 0 ] ) + s.Substring( 1 ).ToLower();

				if( underscoreSpaces && ( i < ( c - 1 ) ) )
					output += "_";
			}

			return output;
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
		// Component Helpers
		////////////////////////

		#region ComponentHelpers

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
		// Sound Helpers
		////////////////////////

		#region SoundHelpers

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
		// Rigidbody Helpers
		////////////////////////

		#region RigidbodyHelpers

		public static void RotateRigidbody( Rigidbody rigidbody, Vector3 from, Vector3 to, float power = 4f, float maxAngular = 12f )
		{
			float angle = Vector3.Angle( from, to );

			angle = 1f - Mathf.Pow( 1f - ( angle / 180f ), power );

			Vector3 rotCross = Vector3.Cross( from, to );
			float rotTheta = Mathf.Asin( rotCross.magnitude );
			Vector3 rotDelta = rotCross.normalized * ( rotTheta / Time.fixedDeltaTime );
			Quaternion rotInertia = rigidbody.transform.rotation * rigidbody.inertiaTensorRotation;

			Vector3 torque = rotInertia * Vector3.Scale( rigidbody.inertiaTensor, ( Quaternion.Inverse( rotInertia ) * rotDelta ) ) * angle;

			if( torque.magnitude > maxAngular )
				torque = torque.normalized * maxAngular;

			rigidbody.AddTorque( torque, ForceMode.Impulse );
		}

		#endregion

		////////////////////////
		// Vector3 Helpers
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

	public class CallbackSingleton<T> where T : class
	{
		private List<System.Action<T>> setupCallbacks = new List<System.Action<T>>();

		public void OnSetup( System.Action<T> callback )
		{
			if( !object.Equals( null, _value ) )
			{
				callback.Invoke( _value );
			}

			setupCallbacks.Add( callback );
		}

		private T _value = null;
		public T value
		{
			get { return _value; }
			set
			{
				_value = value;

				if( !object.Equals( null, value ) )
				{
					List<System.Action<T>> callbacks = new List<System.Action<T>>();

					foreach( System.Action<T> callback in setupCallbacks )
					{
						if( !object.Equals( null, callback ) )
						{
							callback.Invoke( _value );

							callbacks.Add( callback );
						}
					}

					setupCallbacks = callbacks;
				}
			}
		}

		public static implicit operator T( CallbackSingleton<T> callbackSingleton ) { return callbackSingleton._value; }
	}
}