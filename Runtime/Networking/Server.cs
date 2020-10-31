using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using Unity.Collections.LowLevel.Unsafe;

using PacketBundle= Tehelee.Baseline.Networking.Packets.PacketBundle;

namespace Tehelee.Baseline.Networking
{
	public class Server : Shared
	{
		////////////////////////////////

		protected override string displayName => "Server";

		////////////////////////////////

		public static Singleton<Server> singleton = new Singleton<Server>();
		
		////////////////////////////////

		protected override void Awake()
		{
			base.Awake();

			singleton.instance = this;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			
			RegisterListener( typeof( Packets.PacketLoopback ), OnPacketLoopback );

			Open();
		}

		protected override void OnDisable()
		{
			Close();

			DropListener( typeof( Packets.PacketLoopback ), OnPacketLoopback );

			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			if( driver.IsCreated )
				driver.Dispose();

			if( connections.IsCreated )
				connections.Dispose();

			if( object.Equals( singleton.instance, this ) )
				singleton.instance = null;

			base.OnDestroy();
		}

		////////////////////////////////

		public static string connectionAddress = null;
		public static ushort connectionPort = 0;

		public override string GetConnectionAddress()
		{
			return connectionAddress;
		}

		public override ushort GetConnectionPort()
		{
			return connectionPort;
		}

		////////////////////////////////

		protected NativeList<NetworkConnection> connections;

		protected List<NetworkConnection> _connections = new List<NetworkConnection>();

		public byte playerLimit = 32;

		public byte playerCount { get { return (byte)connections.Length; } }

		protected Dictionary<NetworkConnection, LinkedList<Packet>> managedQueue = new Dictionary<NetworkConnection, LinkedList<Packet>>();

		public bool debugPacketWrite = false;

		////////////////////////////////
		
		public delegate void OnNetworkConnection( NetworkConnection networkConnection );

		public event OnNetworkConnection onClientAdded;
		public event OnNetworkConnection onClientDropped;

		////////////////////////////////
		
		private Packets.PacketMap cachedPacketMap = null;

		public void RebuildPacketMap()
		{
			Packets.PacketMap packetMap = new Packets.PacketMap();
			
			packetMap.map = Packet.GetRegisteredPacketHashes().ToArray();

			packetHashMap = new PacketHashMap( packetMap.map );

			cachedPacketMap = packetMap;

			if( open )
			{
				Send( packetMap, true );
			}
		}

		////////////////////////////////

		public override void Open()
		{
			RebuildPacketMap();

			base.Open();
			
			if( driver.Bind( NetworkEndPoint.Parse( this.serverAddress ?? string.Empty, this.port ) ) != 0 )
			{
				Debug.LogErrorFormat( "Server: Failed to bind to '{0}' on port {1}.", this.serverAddress, port );
			}
			else
			{
				driver.Listen();

				if( debug )
					Debug.LogFormat( "Server: Bound to '{0}' on port {1}.", this.serverAddress, port );
			}

			connections = new NativeList<NetworkConnection>( ( int )playerLimit, Allocator.Persistent );
		}

		public override void Close()
		{
			connections.Dispose();

			base.Close();
		}

		public override void Send( Packet packet, bool reliable = false )
		{
			if( playerCount == 0 )
				return;

			base.Send( packet, reliable );
		}

		////////////////////////////////

		private void CleanupOldConnections()
		{
			for( int i = 0; i < connections.Length; i++ )
			{
				if( !connections[ i ].IsCreated || driver.GetConnectionState( connections[ i ] ) == NetworkConnection.State.Disconnected )
				{
					if( debug )
						Debug.LogWarningFormat( "Server: Removed client {0}", connections[ i ].InternalId );

					onClientDropped?.Invoke( connections[ i ] );

					_connections.Remove( connections[ i ] );

					connections.RemoveAtSwapBack( i );
					--i;
				}
			}
		}

		private void AcceptNewConnections()
		{
			NetworkConnection networkConnection;
			while( ( networkConnection = driver.Accept() ) != default(NetworkConnection) )
			{
				connections.Add( networkConnection );

				_connections.Add( networkConnection );

				cachedPacketMap.targets.Clear();
				cachedPacketMap.targets.Add( networkConnection );

				Send( cachedPacketMap, true );

				onClientAdded?.Invoke( networkConnection );

				if( debug )
					Debug.LogWarningFormat( "Server: Added client {0}", networkConnection.InternalId );
			}
		}

		private void QueryForEvents()
		{
			DataStreamReader stream;
			for( int i = 0; i < connections.Length; i++ )
			{
				if( !connections[ i ].IsCreated )
					continue;

				NetworkEvent.Type netEventType;
				while( ( netEventType = driver.PopEventForConnection( connections[ i ], out stream ) ) != NetworkEvent.Type.Empty )
				{
					if( netEventType == NetworkEvent.Type.Data )
					{
						Read( connections[ i ], ref stream );
					}
				}
			}
		}

		////////////////////////////////

		private unsafe int GetReliabilityError( NetworkConnection connection )
		{
			NativeSlice<byte> readProcessingBuffer = default;
			NativeSlice<byte> writeProcessingBuffer = default;
			NativeSlice<byte> sharedBuffer  = default;

			driver.GetPipelineBuffers( pipeline.reliable, 4, connection, ref readProcessingBuffer, ref writeProcessingBuffer, ref sharedBuffer );
			
			ReliableUtility.SharedContext* unsafePointer = ( ReliableUtility.SharedContext* ) sharedBuffer.GetUnsafePtr();

			if ( unsafePointer->errorCode != 0 )
			{
				int errorId = ( int ) unsafePointer->errorCode;
				return errorId;
			}
			else
			{
				return 0;
			}
		}

		private void SendQueue()
		{
			Packet packet;

			List<NetworkConnection> targets = new List<NetworkConnection>();

			while( packetQueue.reliable.Count > 0 )
			{
				packet = packetQueue.reliable.Dequeue();

				targets.AddRange( packet.targets );
				packet.targets.Clear();

				if( targets.Count == 0 )
					targets.AddRange( _connections );

				foreach( NetworkConnection target in targets )
				{
					if( !managedQueue.ContainsKey( target ) )
						managedQueue.Add( target, new LinkedList<Packet>() );

					managedQueue[ target ].AddLast( packet );
				}

				targets.Clear();
			}

			targets.AddRange( managedQueue.Keys );
			foreach( NetworkConnection target in targets )
			{
				LinkedList<Packet> packets = managedQueue[ target ];

				if( packets.Count > 1 )
				{
					PacketBundle packetBundle = new PacketBundle();

					if( debugPacketWrite )
					{
						foreach( Packet _packet in packets )
						{
							Debug.LogErrorFormat( "[{0}]: {1}", _packet.id, _packet.GetType() );

							if( _packet.GetType() == typeof( PacketBundle ) )
							{
								PacketBundle _packetBundle = ( PacketBundle ) _packet;

								foreach( Packet __packet in _packetBundle.packets )
								{
									Debug.LogErrorFormat( "\t[{0}]: {1}", __packet.id, __packet.GetType() );
								}
							}
						}
					}

					packetBundle.packets.AddRange( packets );

					packets.Clear();

					packets.AddFirst( packetBundle );
				}

				while( packets.Count > 0 )
				{
					packet = packets.First.Value;

					DataStreamWriter writer = new DataStreamWriter( packet.bytes + 2, Allocator.Temp );

					// Add Packet identifier
					writer.Write( packet.id );

					// Apply / write packet data to data stream writer
					packet.Write( ref writer );

					driver.Send( pipeline.reliable, target, writer );
					int errorId = GetReliabilityError( target );
					if( errorId != 0 )
					{
						ReliableUtility.ErrorCodes error = ( ReliableUtility.ErrorCodes ) errorId;
						if( error == ReliableUtility.ErrorCodes.OutgoingQueueIsFull )
						{
							/*
							if( debug )
								Debug.LogWarningFormat( "Server.SendReliable( {0} ): Outgoing reliable queue is full, delaying...", target );
							*/
							break;
						}
						else
						{
							Debug.LogErrorFormat( "Reliability Error: {0}", error );
						}
					}

					writer.Dispose();

					packets.RemoveFirst();
				}

				if( packets.Count == 0 && managedQueue[ target ].Count == 0 )
					managedQueue.Remove( target );
			}
			
			List<DataStreamWriter> sendAllUnreliable = new List<DataStreamWriter>();

			while( packetQueue.unreliable.Count > 0 )
			{
				packet = packetQueue.unreliable.Dequeue();

				DataStreamWriter writer = new DataStreamWriter( packet.bytes + 2, Allocator.Temp );

				writer.Write( packet.id );

				packet.Write( ref writer );

				if( packet.targets.Count > 0 )
				{
					foreach( NetworkConnection connection in packet.targets )
					{
						driver.Send( pipeline.unreliable, connection, writer );
					}

					writer.Dispose();
				}
				else
				{
					sendAllUnreliable.Add( writer );
				}
			}

			if( sendAllUnreliable.Count > 0 )
			{
				foreach( NetworkConnection connection in connections )
				{
					foreach( DataStreamWriter writer in sendAllUnreliable )
					{
						driver.Send( pipeline.unreliable, connection, writer );
					}
				}
				
				foreach( DataStreamWriter writer in sendAllUnreliable )
					writer.Dispose();
			}
		}

		////////////////////////////////

		public NetworkConnection GetConnection( int internalId )
		{
			if( internalId >= 0 && internalId < connections.Length )
			{
				return connections[ internalId ];
			}

			return default( NetworkConnection );
		}

		////////////////////////////////

		private void Update()
		{
			if( !driver.IsCreated )
				return;

			driver.ScheduleUpdate().Complete();

			CleanupOldConnections();

			AcceptNewConnections();

			QueryForEvents();

			SendQueue();
		}

		////////////////////////////////
		
		private ReadResult OnPacketLoopback( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			Packets.PacketLoopback packetLoopback = new Packets.PacketLoopback( ref reader, ref context );
			packetLoopback.targets.Add( connection );

			Send( packetLoopback );

			return ReadResult.Consumed;
		}

		////////////////////////////////
	}
}
