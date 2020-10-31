using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

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
		//	Open & Close

		#region OpenClose
		
		public override void Open()
		{
			base.Open();

			if( driver.Bind( NetworkEndPoint.Parse( this.address ?? string.Empty, this.port ) ) != 0 )
			{
				Debug.LogErrorFormat( "Server: Failed to bind to '{0}' on port {1}.", this.address, port );
			}
			else
			{
				driver.Listen();

				if( debug )
					Debug.LogFormat( "Server: Bound to '{0}' on port {1}.", this.address, port );
			}

			networkConnectionsNative = new NativeList<NetworkConnection>( ( int )playerLimit, Allocator.Persistent );
		}

		public override void Close()
		{
			networkConnectionsNative.Dispose();

			base.Close();
		}

		#endregion

		////////////////////////////////
		//	Network Connections

		#region NetworkConnections

		protected List<NetworkConnection> networkConnections = new List<NetworkConnection>();
		public List<NetworkConnection> GetNetworkConnections() => new List<NetworkConnection>( networkConnections );

		protected NativeList<NetworkConnection> networkConnectionsNative;

		protected virtual void AcceptNewConnections()
		{
			NetworkConnection networkConnection;
			while( ( networkConnection = driver.Accept() ) != default(NetworkConnection) )
			{
				networkConnectionsNative.Add( networkConnection );

				networkConnections.Add( networkConnection );
				
				onClientAdded?.Invoke( networkConnection );

				if( debug )
					Debug.LogWarningFormat( "Server: Added client {0}", networkConnection.InternalId );
			}
		}

		protected virtual void CleanupOldConnections()
		{
			for( int i = 0; i < networkConnectionsNative.Length; i++ )
			{
				NetworkConnection networkConnection = networkConnectionsNative[ i ];

				if( !networkConnection.IsCreated || driver.GetConnectionState( networkConnection ) == NetworkConnection.State.Disconnected )
				{
					if( debug )
						Debug.LogWarningFormat( "Server: Removed client {0}", networkConnection.InternalId );

					onClientDropped?.Invoke( networkConnection );

					networkConnections.Remove( networkConnection );

					networkConnectionsNative.RemoveAtSwapBack( i );
					--i;
				}
			}
		}

		public NetworkConnection GetConnectionFromInternalId( int internalId )
		{
			if( internalId >= 0 && internalId < networkConnectionsNative.Length )
			{
				return networkConnectionsNative[ internalId ];
			}

			return default;
		}

		#endregion

		////////////////////////////////
		//	Network Update

		#region NetworkUpdate

		protected override void NetworkUpdate()
		{
			if( !driver.IsCreated )
				return;

			driver.ScheduleUpdate().Complete();

			CleanupOldConnections();

			AcceptNewConnections();

			QueryForEvents();

			SendQueue();
		}

		#endregion

		////////////////////////////////
		//	Query For Events

		#region QueryForEvents

		protected override void QueryForEvents()
		{
			DataStreamReader stream;
			for( int i = 0; i < networkConnectionsNative.Length; i++ )
			{
				NetworkConnection networkConnection = networkConnectionsNative[ i ];

				if( !networkConnection.IsCreated )
					continue;

				NetworkEvent.Type netEventType;
				while( ( netEventType = driver.PopEventForConnection( networkConnection, out stream ) ) != NetworkEvent.Type.Empty )
				{
					if( netEventType == NetworkEvent.Type.Data )
					{
						Read( networkConnection, ref stream );
					}
				}
			}
		}

		#endregion

		////////////////////////////////
		//	Send Queue

		#region SendQueue
			
		protected Dictionary<NetworkConnection, LinkedList<Packet>> managedQueue = new Dictionary<NetworkConnection, LinkedList<Packet>>();

		public override void Send( Packet packet, bool reliable = false )
		{
			if( playerCount == 0 )
				return;

			base.Send( packet, reliable );
		}

		protected override void SendQueue()
		{
			Packet packet;

			List<NetworkConnection> targets = new List<NetworkConnection>();

			while( packetQueue.reliable.Count > 0 )
			{
				packet = packetQueue.reliable.Dequeue();

				targets.AddRange( packet.targets );
				packet.targets.Clear();

				if( targets.Count == 0 )
					targets.AddRange( networkConnections );

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

						if( error != ReliableUtility.ErrorCodes.OutgoingQueueIsFull )
							Debug.LogErrorFormat( "Reliability Error: {0}", error );
						else
							break;
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
				foreach( NetworkConnection connection in networkConnectionsNative )
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

		private unsafe int GetReliabilityError( NetworkConnection connection )
		{
			NativeSlice<byte> readProcessingBuffer = default;
			NativeSlice<byte> writeProcessingBuffer = default;
			NativeSlice<byte> sharedBuffer = default;

			driver.GetPipelineBuffers( pipeline.reliable, 4, connection, ref readProcessingBuffer, ref writeProcessingBuffer, ref sharedBuffer );

			ReliableUtility.SharedContext* unsafePointer = ( ReliableUtility.SharedContext* ) sharedBuffer.GetUnsafePtr();

			if( unsafePointer->errorCode != 0 )
			{
				int errorId = ( int ) unsafePointer->errorCode;
				return errorId;
			}
			else
			{
				return 0;
			}
		}

		#endregion

		////////////////////////////////
		//	Heartbeat Loopback

		#region HeartbeatLoopback
		
		private ReadResult OnPacketLoopback( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			Packets.PacketLoopback packetLoopback = new Packets.PacketLoopback( ref reader, ref context );
			packetLoopback.targets.Add( connection );

			Send( packetLoopback );

			return ReadResult.Consumed;
		}

		#endregion
		
		////////////////////////////////
		//	Events

		#region Events

		public event Callback<NetworkConnection> onClientAdded;
		public event Callback<NetworkConnection> onClientDropped;

		#endregion

		////////////////////////////////
		//	Properties

		#region Properties

		public override string networkScopeLabel => "Server";

		public static Singleton<Server> singleton { get; private set; } = new Singleton<Server>();

		public int playerCount { get { return networkConnections.Count; } }

		#endregion

		////////////////////////////////
		//	Attributes

		#region Attributes

		[Min( 1 )]
		public int playerLimit = 32;

		#endregion

		////////////////////////////////
		//	Mono Methods

		#region MonoMethods

		protected override void Awake()
		{
			base.Awake();

			singleton.instance = this;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			RegisterListener( typeof( Packets.PacketLoopback ), OnPacketLoopback );

			if( openOnEnable )
				this.Open();
		}

		protected override void OnDisable()
		{
			if( open )
				this.Close();

			DropListener( typeof( Packets.PacketLoopback ), OnPacketLoopback );

			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			if( driver.IsCreated )
				driver.Dispose();

			if( networkConnectionsNative.IsCreated )
				networkConnectionsNative.Dispose();

			if( object.Equals( singleton.instance, this ) )
				singleton.instance = null;

			base.OnDestroy();
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( Server ) )]
	public class EditorServer : EditorShared
	{
		Server server;

		public override void Setup()
		{
			base.Setup();

			server = ( Server ) target;
		}

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 3f;
			
			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Networking - Server" ) );
			bRect.y += lineHeight * 1.5f;
			
			if( Application.isPlaying )
			{
				EditorGUI.LabelField( bRect, new GUIContent( "Connected Players", "The current number of active connections out of the maximum." ) );
				cRect = new Rect( bRect.x + EditorGUIUtility.labelWidth, bRect.y, bRect.width - EditorGUIUtility.labelWidth, bRect.height );
				cRect.width = ( cRect.width - 45f ) * 0.5f;

				EditorGUI.BeginDisabledGroup( true );
				EditorGUI.IntField( cRect, new GUIContent( string.Empty, "Connected Players" ), server.playerCount );
				EditorGUI.EndDisabledGroup();

				EditorGUI.LabelField( new Rect( cRect.x + cRect.width + 5f, cRect.y, 35f, cRect.height ), new GUIContent( "out of" ) );
				cRect.x += cRect.width + 45f;
				EditorGUI.BeginDisabledGroup( server.open );
				EditorGUI.PropertyField( cRect, this[ "playerLimit" ], new GUIContent( string.Empty, "Max Players" ) );
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUI.PropertyField( bRect, this[ "playerLimit" ], new GUIContent( "Player Limit", "The maximum number of active connections the server will support." ) );
			}

			this[ "playerLimit" ].Min( 1 );

			bRect.y += lineHeight * 1.5f;

			rect.y = bRect.y;
		}
	}
#endif
}
