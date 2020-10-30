using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace Tehelee.Baseline.Networking
{
	[System.Serializable]
	public class Packet
	{
		////////////////////////////////
		//	HashToType

		#region HashToType

		private static Dictionary<ushort, System.Type> HashToType = new Dictionary<ushort, System.Type>();
		private static Dictionary<System.Type, ushort> TypeToHash = new Dictionary<System.Type, ushort>();
		
		public static List<System.Type> GetRegisteredPacketTypes()
		{
			return new List<System.Type>( TypeToHash.Keys );
		}

		public static List<ushort> GetRegisteredPacketHashes()
		{
			return new List<ushort>( HashToType.Keys );
		}

		public static System.Type LookupType( ushort hash )
		{
			if( !HashToType.ContainsKey( hash ) )
				return null;

			return HashToType[ hash ];
		}

		public void Register()
		{
			HashToType.Add( this.id, this.GetType() );
		}

		public static void Register( System.Type packetType )
		{
			ushort hash = Hash( packetType );
			if( !HashToType.ContainsKey( hash ) )
				HashToType.Add( hash, packetType );
		}

		public static void Unregister( System.Type packetType )
		{
			if( TypeToHash.ContainsKey( packetType ) )
			{
				ushort hash = TypeToHash[ packetType ];
				if( HashToType.ContainsKey( hash ) )
					HashToType.Remove( hash );
				TypeToHash.Remove( packetType );
			}
			else
			{
				ushort hash = Utils.HashCRC( packetType.FullName );
				if( HashToType.ContainsKey( hash ) )
					HashToType.Remove( hash );
			}
		}

		public static ushort Hash( System.Type packetType )
		{
			if( object.Equals( null, packetType ) )
				return 0;

			if( !TypeToHash.ContainsKey( packetType ) )
			{
				ushort hash = Utils.HashCRC( packetType.FullName );
				
				TypeToHash.Add( packetType, hash );

				return hash;
			}

			return TypeToHash[ packetType ];
		}

		#endregion
		
		////////////////////////////////
		//	Transport Helpers

		#region TransportHelpers

		public static void WriteFloatSafe( ref DataStreamWriter writer, float value )
		{
			writer.Write( ( float.IsNaN( value ) || float.IsInfinity( value ) ) ? 0f : value );
		}

		private static float[] precisionCompress = new[] { 10f, 100f, 1000f, 10000f };
		private static float[] precisionUncompress = new[] { 0.1f, 0.01f, 0.001f, 0.0001f };

		public static int GetCompressedFloatBytes() => 2;

		public static void WriteCompressedFloat( ref DataStreamWriter writer, float value, byte precision )
		{
			float _value = ( float.IsNaN( value ) || float.IsInfinity( value ) ) ? 0f : value;

			writer.Write( ( short ) Mathf.RoundToInt( _value * precisionCompress[ precision ] ) );
		}

		public static float ReadCompressedFloat( ref DataStreamReader reader, ref DataStreamReader.Context context, byte precision )
		{
			return reader.ReadShort( ref context ) * precisionUncompress[ precision ];
		}

		public static int GetSafeStringBytes( string str )
		{
			int bytes = 4;

			if( !string.IsNullOrEmpty( str ) )
				bytes += str.Length * 2;

			return bytes;
		}

		public static void WriteSafeString( ref DataStreamWriter writer, string str )
		{
			string _str = string.IsNullOrEmpty( str ) ? string.Empty : str;

			writer.Write( _str.Length );

			for( int i = 0, iC = _str.Length; i < iC; i++ )
				writer.Write( ( ushort ) _str[ i ] );
		}

		public static string ReadSafeString( ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			char[] _str = new char[ reader.ReadInt( ref context ) ];

			for( int i = 0, iC = _str.Length; i < iC; i++ )
				_str[ i ] = ( char ) reader.ReadUShort( ref context );

			return new string( _str );
		}

		#endregion

		////////////////////////////////
		//	Packet

		#region Packet

		public ushort id { get { return Hash( this.GetType() ); } }
		
		public virtual int bytes { get { return 0; } }

		public List<NetworkConnection> targets = new List<NetworkConnection>();

		public virtual void Write( ref DataStreamWriter writer ) { }

		public Packet() { }

		public Packet( ref DataStreamReader reader, ref DataStreamReader.Context context ) { }

		#endregion
	}

	public interface ITransportObject
	{
		int GetBytes( params object[] parameters );
		void Read( ref DataStreamReader reader, ref DataStreamReader.Context context, params object[] parameters );
		void Write( ref DataStreamWriter writer, params object[] parameters );
	}
	
	public enum ReadResult : byte
	{
		Skipped,	// Not utilized by this listener
		Processed,	// Used by this listener, but non exclusively.
		Consumed,	// Used by this listener, and stops all further listener checks.
		Error		// A problem was encountered, store information into readHandlerErrorMessage
	}

	public static class ReaderContextFactory
	{
		static System.Reflection.FieldInfo m_ReadByteIndex;
		static System.Reflection.FieldInfo m_BitIndex;
		static System.Reflection.FieldInfo m_BitBuffer;

		static ReaderContextFactory()
		{
			System.Type contextType = typeof( DataStreamReader.Context );

			m_ReadByteIndex = contextType.GetField( "m_ReadByteIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
			m_BitIndex = contextType.GetField( "m_BitIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
			m_BitBuffer = contextType.GetField( "m_BitBuffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
		}

		public static DataStreamReader.Context Clone( DataStreamReader.Context context )
		{
			// Due to boxing, this must be defined as an object, then cast to type on return
			object _context = new DataStreamReader.Context();
			
			m_ReadByteIndex.SetValue( _context, m_ReadByteIndex.GetValue( context ) );
			m_BitIndex.SetValue( _context, m_BitIndex.GetValue( context ) );
			m_BitBuffer.SetValue( _context, m_BitBuffer.GetValue( context ) );
			
			return ( DataStreamReader.Context ) _context;
		}
	}

	public class Shared : MonoBehaviour
	{
		////////////////////////////////

		protected virtual string displayName { get { return "Shared"; } }

		////////////////////////////////
		//	PacketHashMap

		#region PacketHashMap

		public class PacketHashMap
		{
			public PacketHashMap( ushort[] packetMap )
			{
				mappedHashes = packetMap;
				hashMapLookup.Clear();

				for( int i = 0, iC = packetMap.Length; i < iC; i++ )
				{
					hashMapLookup.Add( packetMap[ i ], ( ushort ) i );
				}
			}

			private ushort[] mappedHashes = new ushort[ 0 ];
			private Dictionary<ushort, ushort> hashMapLookup = new Dictionary<ushort, ushort>();

			public int MapIndexFromHash( ushort hash )
			{
				if( !hashMapLookup.ContainsKey( hash ) )
					return -1;

				return hashMapLookup[ hash ];
			}

			public ushort HashFromMapIndex( ushort index )
			{
				if( index < 0 || index > mappedHashes.Length - 1 )
					return 0;

				return mappedHashes[ index ];
			}
		}

		public PacketHashMap packetHashMap;

		#endregion

		////////////////////////////////
		
		public List<DesignData.PacketData> packetDatas = new List<DesignData.PacketData>();

		protected virtual void RegisterPacketDatas()
		{
			foreach( DesignData.PacketData packetData in packetDatas )
			{
				LoadPacketData( packetData, true );
			}
		}

		protected virtual void UnregisterPacketDatas()
		{
			foreach( DesignData.PacketData packetData in packetDatas )
			{
				UnloadPacketData( packetData, true );
			}

			packetDatas.Clear();
		}

		public void LoadPacketData( DesignData.PacketData packetData, bool skipAdd = false )
		{
			string[] packetTypeNames = packetData.packetTypeNames;
			foreach( string packetTypeName in packetTypeNames )
			{
				System.Type packetType = System.Type.GetType( packetTypeName );

				if( !object.Equals( null, packetType ) )
					Packet.Register( packetType );
			}

			if( !skipAdd && !packetDatas.Contains( packetData ) )
				packetDatas.Add( packetData );
		}

		public void UnloadPacketData( DesignData.PacketData packetData, bool skipRemove = false )
		{
			if( packetDatas.Contains( packetData ) )
			{
				string[] packetTypeNames = packetData.packetTypeNames;
				foreach( string packetTypeName in packetTypeNames )
				{
					System.Type packetType = System.Type.GetType( packetTypeName );

					if( !object.Equals( null, packetType ) )
						Packet.Unregister( packetType );
				}

				if( !skipRemove )
					packetDatas.Remove( packetData );
			}
		}

		////////////////////////////////

		protected virtual void Awake()
		{

		}

		protected virtual void OnEnable()
		{
			Packet.Register( typeof( Packet ) );
			Packet.Register( typeof( Packets.PacketMap ) );
			Packet.Register( typeof( Packets.PacketBundle ) );

			RegisterPacketDatas();
		}

		protected virtual void OnDisable()
		{
			UnregisterPacketDatas();
		}

		protected virtual void OnDestroy()
		{

		}

		////////////////////////////////

		[System.Serializable]
		protected struct PacketQueue
		{
			public Queue<Packet> reliable;
			public Queue<Packet> unreliable;
		}

		protected PacketQueue packetQueue = new PacketQueue() { reliable = new Queue<Packet>(), unreliable = new Queue<Packet>() };

		public virtual void Send( Packet packet, bool reliable = false )
		{
			if( !open )
				return;

			if( null == packet )
				return;

			if( packetSpies.ContainsKey( packet.id ) && packetSpies[ packet.id ] != null )
			{
				Dictionary<int, SpyListener> listeners = packetSpies[ packet.id ];

				List<int> keys = new List<int>( listeners.Keys );
				keys.Sort();

				for( int i = 0; i < keys.Count; i++ )
				{
					int key = keys[ i ];
					SpyListener spyListener = listeners[ key ];

					if( spyListener != null )
					{
						spyListener.Invoke( packet, reliable );
					}
				}
			}

			if( reliable )
				packetQueue.reliable.Enqueue( packet );
			else
				packetQueue.unreliable.Enqueue( packet );

			if( debug )
				Debug.LogWarningFormat( "{0}.Write( {1} ) using {2} channel.{3}", displayName, Packet.LookupType( packet.id ).FullName, reliable ? "verified" : "fast", packet.targets.Count > 0 ? string.Format( " Sent only to {0} connections.", packet.targets.Count ) : string.Empty );
		}

		protected void Read( NetworkConnection connection, ref DataStreamReader reader )
		{
			DataStreamReader.Context context = default( DataStreamReader.Context );

			Read( connection, ref reader, ref context );
		}

		protected virtual ReadResult InternalRead( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context ) { return ReadResult.Skipped; }

		protected void Read( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			DataStreamReader.Context beginningContext = ReaderContextFactory.Clone( context );

			ushort packetId = reader.ReadUShort( ref context );

			if( packetId == Packet.Hash( typeof( Packet ) ) )
			{
				return;
			}
			
			DataStreamReader.Context internalReadContext = ReaderContextFactory.Clone( beginningContext );
			ReadResult internalReadResult = InternalRead( connection, ref reader, ref internalReadContext );
			if( internalReadResult == ReadResult.Consumed || internalReadResult == ReadResult.Error )
			{
				context = internalReadContext;

				if( debug )
					Debug.LogFormat( "{0}.InternalRead( {1} ).", displayName, Packet.LookupType( packetId ).FullName );

				return;
			}
			
			if( packetId == Packet.Hash( typeof( Packets.PacketBundle ) ) )
			{
				int count = reader.ReadInt( ref context );

				if( debug )
					Debug.LogFormat( "{0}.ReadPacketBundle() with {1} packets.", displayName, count );

				for( int i = 0; i < count; i++ )
				{
					Read( connection, ref reader, ref context );
				}

				return;
			}
			
			bool processed = false;
			int listenerCount = 0;

			if( packetListeners.ContainsKey( packetId ) && packetListeners[ packetId ] != null )
			{
				listenerCount = packetListeners[ packetId ].Count;

				if( debug )
					Debug.LogFormat( "{0}.Read( {1} ) with {2} listeners.", displayName, Packet.LookupType( packetId ).FullName, listenerCount );

				Dictionary<int, ReadHandler> listeners = packetListeners[ packetId ];
				List<int> keys = new List<int>( listeners.Keys );
				keys.Sort();
				
				DataStreamReader.Context iterationContext, processedContext = context;
				for( int i = 0; i < keys.Count; i++ )
				{
					int key = keys[ i ];
					ReadHandler readHandler = listeners[ key ];

					iterationContext = ReaderContextFactory.Clone( context );

					if( readHandler != null )
					{
						ReadResult result = readHandler( connection, ref reader, ref iterationContext );
						if( result != ReadResult.Skipped )
						{
							switch( result )
							{
								case ReadResult.Processed:
									if( debug )
										Debug.LogFormat( "{0}.Read( {1} ) processed by listener {2}.", displayName, Packet.LookupType( packetId ).FullName, i );
									processedContext = iterationContext;
									processed = true;
									break;
								case ReadResult.Consumed:
									if( debug )
										Debug.LogFormat( "{0}.Read( {1} ) consumed by listener {2}.", displayName, Packet.LookupType( packetId ).FullName, i );
									context = iterationContext;
									return;
								case ReadResult.Error:
									Debug.LogErrorFormat( "{0}.Read( {1} ) encountered an error on listener {2}.{3}", displayName, Packet.LookupType( packetId ).FullName, i, readHandlerErrorMessage != null ? string.Format( "\nError Message: {0}", readHandlerErrorMessage ) : string.Empty );
									return;
							}
						}
					}
				}

				if( processed )
					context = processedContext;

				return;
			}

			if( !processed )
			{
				if( debug )
				{
					if( listenerCount == 0 )
						Debug.LogWarningFormat( "{0}.Read( {1} ) [ {2} ] skipped; no associated listeners.", displayName, Packet.LookupType( packetId )?.FullName, packetId );
					else
						Debug.LogWarningFormat( "{0}.Read( {1} ) failed to be consumed by one of the {2} listeners.", displayName, Packet.LookupType( packetId ).FullName, listenerCount );
				}

				System.Type packetType = Packet.LookupType( packetId );

				if( !object.Equals( null, packetType ) )
				{
					System.Reflection.ConstructorInfo packetConstructor = packetType.GetConstructor( new[] { typeof( DataStreamReader ).MakeByRefType(), typeof( DataStreamReader.Context ).MakeByRefType() } );

					object[] constructorParamters = new object[] { reader, context };

					Packet packet = ( Packet ) packetConstructor.Invoke( constructorParamters );

					context = ( DataStreamReader.Context ) constructorParamters[ 1 ];
				}
				else
				{
					Debug.LogErrorFormat( "{0}.Read( {1} ) failed to create a fall-through listener!", displayName, packetType );
				}
			}
		}

		public string readHandlerErrorMessage = null;
		
		////////////////////////////////

		public delegate ReadResult ReadHandler( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context );

		Dictionary<ushort, Dictionary<int,ReadHandler>> packetListeners = new Dictionary<ushort, Dictionary<int, ReadHandler>>();
		
		public void RegisterListener( System.Type packetType, ReadHandler handler, int priority = 0 )
		{
			RegisterListener( Packet.Hash( packetType ), handler, priority );
		}

		public void RegisterListener( ushort packetId, ReadHandler handler, int priority = 0 )
		{
			if( packetId == 0 )
				return;

			if( !packetListeners.ContainsKey( packetId ) )
			{
				packetListeners.Add( packetId, new Dictionary<int, ReadHandler>() );
			}

			Dictionary<int, ReadHandler> listeners = packetListeners[ packetId ];

			while( listeners.ContainsKey( priority ) )
				priority++;

			packetListeners[ packetId ].Add( priority, handler );
		}

		public void DropListener( System.Type packetType, ReadHandler handler )
		{
			DropListener( Packet.Hash( packetType ), handler );
		}

		public void DropListener( ushort packetId, ReadHandler handler )
		{
			if( packetListeners.ContainsKey( packetId ) && packetListeners[ packetId ].ContainsValue( handler ) )
			{
				Dictionary<int, ReadHandler> listeners = packetListeners[ packetId ];
				List<int> keys = new List<int>( listeners.Keys );
				for( int i = 0; i < keys.Count; i++ )
				{
					int k = keys[ i ];
					if( listeners[ k ] == handler )
					{
						packetListeners[ packetId ].Remove( k );
						break;
					}
				}

				if( packetListeners[ packetId ].Count == 0 )
					packetListeners.Remove( packetId );
			}
		}

		////////////////////////////////

		public delegate void SpyListener( Packet packet, bool reliable );

		Dictionary<ushort, Dictionary<int, SpyListener>> packetSpies = new Dictionary<ushort, Dictionary<int, SpyListener>>();
		
		public void RegisterSpy( System.Type packetType, SpyListener handler, int priority = 0 )
		{
			RegisterSpy( Packet.Hash( packetType ), handler, priority );
		}

		public void RegisterSpy( ushort packetId, SpyListener handler, int priority = 0 )
		{
			if( !packetSpies.ContainsKey( packetId ) )
			{
				packetSpies.Add( packetId, new Dictionary<int, SpyListener>() );
			}

			Dictionary<int, SpyListener> listeners = packetSpies[ packetId ];

			while( listeners.ContainsKey( priority ) )
				priority++;

			packetSpies[ packetId ].Add( priority, handler );
		}

		public void DropSpy( System.Type packetType, SpyListener handler )
		{
			DropSpy( Packet.Hash( packetType ), handler );
		}

		public void DropSpy( ushort packetId, SpyListener handler )
		{
			if( packetSpies.ContainsKey( packetId ) && packetSpies[ packetId ].ContainsValue( handler ) )
			{
				Dictionary<int, SpyListener> listeners = packetSpies[ packetId ];
				List<int> keys = new List<int>( listeners.Keys );
				for( int i = 0; i < keys.Count; i++ )
				{
					int k = keys[ i ];
					if( listeners[ k ] == handler )
					{
						packetSpies[ packetId ].Remove( k );
						break;
					}
				}

				if( packetSpies[ packetId ].Count == 0 )
					packetSpies.Remove( packetId );
			}
		}

		////////////////////////////////

		public UdpNetworkDriver driver;

		[System.Serializable]
		public struct Pipeline
		{
			public NetworkPipeline reliable;
			public NetworkPipeline unreliable;

			public Pipeline( UdpNetworkDriver driver )
			{
				reliable = driver.CreatePipeline( typeof( ReliableSequencedPipelineStage ), typeof( SimulatorPipelineStage ) );
				unreliable = driver.CreatePipeline( typeof( UnreliableSequencedPipelineStage ), typeof( SimulatorPipelineStage ) );
			}

			public NetworkPipeline this[ bool reliable ] { get { return reliable ? this.reliable : this.unreliable; } }
		}

		public Pipeline pipeline { get; private set; }

		public const string LoopbackAddress = "127.0.0.1";
		public string serverAddress = LoopbackAddress;

		public ushort port = 16448;

		public bool open { get; private set; }

		public bool debug = false;

		public int disconnectTimeoutMS = 15000;

		public int packetDelayMS = 0;

		[Range(0,100)]
		public int packetDropPercentage;

		////////////////////////////////

		public virtual string GetConnectionAddress() { return null; }
		public virtual ushort GetConnectionPort() { return 0; }

		public virtual void Open()
		{
			if( open )
				return;
			
			Dictionary<string, string> args = new Dictionary<string, string>();
			args.Add( "serverAddress", null );
			args.Add( "serverPort", null );

			Utils.GetArgsFromDictionary( ref args );

			if( !string.IsNullOrEmpty( args[ "serverAddress" ] ) )
				this.serverAddress = args[ "serverAddress" ];

			this.serverAddress = this.serverAddress ?? LoopbackAddress;

			if( args[ "serverPort" ] != null )
			{
				ushort port;
				if( ushort.TryParse( args[ "serverPort" ], out port ) )
					this.port = port;
			}

			string connectionAddress = GetConnectionAddress();
			

			if( !string.IsNullOrWhiteSpace( connectionAddress ) )
			{
				this.serverAddress = connectionAddress;
				
				ushort connectionPort = GetConnectionPort();

				if( connectionPort > 0 )
					this.port = connectionPort;
			}

			driver = new UdpNetworkDriver( new ReliableUtility.Parameters { WindowSize = 32 }, new NetworkConfigParameter { connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS, disconnectTimeoutMS = disconnectTimeoutMS, maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts }, new SimulatorUtility.Parameters { MaxPacketSize = 256, MaxPacketCount = 30, PacketDelayMs = packetDelayMS, PacketDropPercentage = packetDropPercentage } );

			pipeline = new Pipeline( driver );
			
			open = true;

			if( debug )
				Debug.LogFormat( "{0}.Open()", displayName );
		}

		public virtual void Close()
		{
			if( !open )
				return;

			open = false;

			pipeline = default( Pipeline );

			driver.Dispose();

			if( debug )
				Debug.LogFormat( "{0}.Close()", displayName );
		}

		////////////////////////////////
	}
}
