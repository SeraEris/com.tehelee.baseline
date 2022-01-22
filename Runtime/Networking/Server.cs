using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using Unity.Collections.LowLevel.Unsafe;

using Open.Nat;

using StringComparison = System.StringComparison;

using Bundle = Tehelee.Baseline.Networking.Packets.Bundle;

namespace Tehelee.Baseline.Networking
{
	public class Server : Shared
	{
		////////////////////////////////
		#region Attributes

		[Range( 1f, 10f )]
		public float pingBroadcastInterval = 5f;

		[Range( 0f, 5f )]
		public float disconnectAndCloseDelay = 1f;

		public enum UsernameDuplicates
		{
			None = 0,
			DifferentCase = 1,
			Any = 2,
		}
		public UsernameDuplicates allowedUsernameDuplicates = UsernameDuplicates.None;

		public enum AdminVisibility
		{
			Everyone = 0,
			AdminsOnly = 1
		}
		public AdminVisibility adminVisibility = AdminVisibility.Everyone;

		public float natReservationTime = 10f;

		public bool useNatPunchThrough = false;

		public HostInfo hostInfo = new HostInfo();

		#endregion

		////////////////////////////////
		#region Members

		private bool closing = false;
		private bool closeInvoked = false;
		private event System.Action closingCallback = null;
		private List<Mapping> natMappings = new List<Mapping>();

		#endregion

		////////////////////////////////
		#region Properties

		public override string networkScopeLabel => "Server";

		public static Singleton<Server> singleton { get; private set; } = new Singleton<Server>();

		public int playerCount { get { return networkConnections.Count; } }

		public byte packetsSentLastFrame = 0;

		private bool _isPrivate = false;
		public bool isPrivate
		{
			get => _isPrivate;
			set
			{
				if( _isPrivate == value )
					return;
				
				if( value )
				{
					approvedInternalIds = new HashSet<int>();
					foreach( NetworkConnection networkConnection in networkConnections )
						approvedInternalIds.Add( networkConnection.InternalId );
				}

				_isPrivate = value;

				if( !_isPrivate )
				{
					approvedInternalIds = null;
					pendingInternalIds.Clear();
				}
			}
		}

		public bool promoteAll { get; private set; }

		private ushort _localHostId = 0;
		public ushort localHostId
		{
			get => _localHostId;
			set
			{
				_localHostId = value;

				if( IsValidId( _localHostId ) )
				{
					AdminPromote( _localHostId );
				}
			}
		}

		#endregion

		////////////////////////////////
		#region Events

		public event System.Action onOpen;
		public event System.Action onClose;

		public event System.Action<NetworkConnection> onClientAdded;
		public event System.Action<NetworkConnection> onClientReady;
		public event System.Action<NetworkConnection> onClientDropped;

		#endregion

		////////////////////////////////
		#region Mono Methods

		protected override void Awake()
		{
			base.Awake();
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			RegisterListener( typeof( Packets.Administration ), OnAdministration );

			RegisterListener( typeof( Packets.Loopback ), OnLoopback );

			RegisterListener( typeof( Packets.Username ), OnUsername );

			if( registerSingleton && object.Equals( null, singleton.instance ) )
				singleton.instance = this;

			if( openOnEnable )
				this.Open();

			_IPingBroadcast = StartCoroutine( IPingBroadcast() );
		}

		protected override void OnDisable()
		{
			if( open )
				this.Close();

			if( object.Equals( this, singleton.instance ) )
				singleton.instance = null;

			DropListener( typeof( Packets.Administration ), OnAdministration );

			DropListener( typeof( Packets.Loopback ), OnLoopback );

			DropListener( typeof( Packets.Username ), OnUsername );

			if( !object.Equals( null, _IPingBroadcast ) )
			{
				StopCoroutine( _IPingBroadcast );
				_IPingBroadcast = null;
			}

			pendingInternalIds.Clear();

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

		////////////////////////////////
		#region Open & Close

		public override void Open()
		{
			if( open )
				return;
			
			if( useNatPunchThrough && natMappings.Count == 0 )
			{
				ApplyPortMappings();

				return;
			}

			clientInfoByNetworkId.Clear();

			adminAuthorizationAttempts.Clear();

			localHostId = 0;
			_isPrivate = string.IsNullOrWhiteSpace( hostInfo.password );
			isPrivate = !_isPrivate;
			promoteAll = string.IsNullOrWhiteSpace( hostInfo.adminPassword );

			base.Open();

			rotatingNetworkId = 0;

			NetworkEndPoint networkEndPoint = NetworkEndPoint.Parse( address ?? string.Empty, port, ( address?.IndexOf( ':' ) ?? -1 ) > -1 ? NetworkFamily.Ipv6 : NetworkFamily.Ipv4 );
			
			int bind = -1;
			for( int i = 0; ( i < 10 ) && ( bind != 0 ); i++ )
				bind = driver.Bind( networkEndPoint );
			
			
			
			if( bind != 0 )
			{
				Debug.LogError( $"Server: Failed to bind to '{address}' on port {port}." );
			}
			else
			{
				driver.Listen();

				if( debug )
					Debug.Log( $"Server: Bound to '{address}' on port {port}." );
			}

			networkConnectionsNative = new NativeList<NetworkConnection>( hostInfo.maxPlayers, Allocator.Persistent );

			onOpen?.Invoke();
		}

		public override void Close()
		{
			if( !open )
				return;

			foreach( NetworkConnection networkConnection in networkConnections )
				driver.Disconnect( networkConnection );

			managedQueue.Clear();

			networkConnections.Clear();
			networkIdsByNetworkConnection.Clear();
			
			networkConnectionsNative.Dispose();

			base.Close();

			if( !object.Equals( null, _IRefreshPortForward ) )
			{
				StopCoroutine( _IRefreshPortForward );
				_IRefreshPortForward = null;
			}

			if( natMappings.Count > 0 )
			{
				foreach( Mapping mapping in natMappings )
					OpenNatWrapper.DeletePortMapping( mapping, null );

				natMappings.Clear();
			}

			closingCallback?.Invoke();
			closingCallback = null;

			onClose?.Invoke();

			closing = false;
			closeInvoked = false;
		}

		public void DisconnectAndClose( string message = null, System.Action callback = null )
		{
			if( closing )
			{
				closingCallback += callback;

				return;
			}

			if( string.IsNullOrWhiteSpace( message ) )
				message = "Server shutting down...";

			Send( new Packets.Administration() { operation = Packets.Administration.Operation.Shutdown, text = message }, true );

			closingCallback += callback;
			closing = true;
		}

		#endregion

		////////////////////////////////
		#region NAT Punch Through

		private void ApplyPortMappings()
		{
			void OnGetPortMappings( List<Mapping> mappings )
			{
				StringBuilder sb = new StringBuilder( $"Current Port Mappings ({mappings.Count}):" );
				foreach( Mapping mapping in mappings )
				{
					sb.AppendLine( $"  {mapping.PrivateIP}:{mapping.PrivatePort} => {mapping.PublicIP}:{mapping.PublicPort}" );
				}
				Debug.Log( sb );
			}
			
			OpenNatWrapper.GetPortMappings( 30000, OnGetPortMappings );
			
			void MapPort( Mapping mapping )
			{
				natMappings.Add( mapping );
				OpenNatWrapper.CreatePortMapping( mapping, OnPortMappingResult );
			}

			void OnFetchIP( IPAddress ipAddress )
			{
				MapPort( GenerateCurrentMapping( ipAddress ) );				
			}
			
			OpenNatWrapper.GetInternalIP( true, OnFetchIP );
			OpenNatWrapper.GetInternalIP( false, OnFetchIP );
			
			OpenNatWrapper.DiscoverDevice( device => device.GetExternalIP( OnFetchIP ) );
		}
		
		private Mapping GenerateCurrentMapping( IPAddress ipAddress ) =>
			new Mapping
			(
				Protocol.Udp,
				ipAddress,
				port,
				port,
				Mathf.RoundToInt
				(
					natReservationTime * 60f
				),
				$"{Application.productName} - {ipAddress}:{port}"
			);

		private void OnPortMappingResult( OpenNatWrapper.PortMappingResult portMappingResult )
		{
			switch( portMappingResult )
			{
				case OpenNatWrapper.PortMappingResult.Success:
					Open();
					_IRefreshPortForward = StartCoroutine( IRefreshPortForward() );
					break;
				case OpenNatWrapper.PortMappingResult.PortInUse:
					if( port == ushort.MaxValue )
						port = 0;
					else
						port++;

					ApplyPortMappings();
					break;
				default:
					if( debug )
						Debug.LogWarning( $"Server unable to use NAT punch-through, reason: {portMappingResult}" );

					natMappings.Clear();
					useNatPunchThrough = false;
					Open();
					break;
			}
		}

		private Coroutine _IRefreshPortForward = null;
		private IEnumerator IRefreshPortForward()
		{
			yield return new WaitForSeconds( Mathf.RoundToInt( natReservationTime * 60f ) );

			int deletedPorts = 0;
			void OnDeletePort()
			{
				deletedPorts++;
			}

			int activePorts = natMappings.Count;
			foreach( Mapping mapping in natMappings )
				OpenNatWrapper.DeletePortMapping( mapping, OnDeletePort );

			while( deletedPorts < activePorts )
				yield return null;
			
			ApplyPortMappings();

			_IRefreshPortForward = null;

			yield break;
		}

		#endregion

		////////////////////////////////
		#region Network Connections Data

		// Used as an auto-incrementing id, checked against networkClients<> for collisions.
		protected ushort rotatingNetworkId = 0;

		// NetworkConnections
		protected NativeList<NetworkConnection> networkConnectionsNative;
		protected List<NetworkConnection> networkConnections = new List<NetworkConnection>();
		public List<NetworkConnection> GetNetworkConnections() => new List<NetworkConnection>( networkConnections );
		public List<NetworkConnection> GetNetworkConnections( IEnumerable<ushort> clientIds )
		{
			List<NetworkConnection> networkConnections = new List<NetworkConnection>();
			foreach( ushort clientId in clientIds )
			{
				NetworkConnection networkConnection = GetNetworkConnection( clientId );
				if( !object.Equals( null, networkConnection ) )
					networkConnections.Add( networkConnection );
			}

			return networkConnections;
		}

		// NetworkConnection => NetworkId
		protected Dictionary<NetworkConnection, ushort> networkIdsByNetworkConnection = new Dictionary<NetworkConnection, ushort>();
		public ushort GetNetworkId( NetworkConnection networkConnection ) => networkIdsByNetworkConnection.ContainsKey( networkConnection ) ? networkIdsByNetworkConnection[ networkConnection ] : ( ushort ) 0;
		
		// NetworkId => ClientInfo( NetworkConnection, JoinTime )
		protected struct ClientInfo
		{
			public ushort networkId;
			public NetworkConnection networkConnection;
			public System.DateTime joinTime;
		}
		protected Dictionary<ushort, ClientInfo> clientInfoByNetworkId = new Dictionary<ushort, ClientInfo>();
		public List<ushort> clientNetworkIds => new List<ushort>( clientInfoByNetworkId.Keys );
		public bool IsValidId( ushort networkId ) => networkId != 0 && clientInfoByNetworkId.ContainsKey( networkId );
		public System.DateTime GetClientJoinTime( ushort clientId ) => IsValidId( clientId ) ? clientInfoByNetworkId[ clientId ].joinTime : default;
		public NetworkConnection GetNetworkConnection( ushort clientId ) => IsValidId( clientId ) ? clientInfoByNetworkId[ clientId ].networkConnection : default;
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
		#region Connections Accept & Cleanup

		protected virtual void AcceptNewConnections()
		{
			NetworkConnection networkConnection;
			while( ( networkConnection = driver.Accept() ) != default(NetworkConnection) )
			{
				if( clientInfoByNetworkId.Count == ushort.MaxValue )
				{
					Debug.LogError( "Server: Could not add additional clients, internal network ids full." );
					break;
				}

				AddConnection( networkConnection );
			}
		}

		protected virtual void CleanupOldConnections()
		{
			for( int i = 0; i < networkConnectionsNative.Length; i++ )
			{
				NetworkConnection networkConnection = networkConnectionsNative[ i ];

				if( !networkConnection.IsCreated || driver.GetConnectionState( networkConnection ) == NetworkConnection.State.Disconnected )
				{
					RemoveConnection( networkConnection, i );
					--i;
				}
			}
		}

		#endregion

		////////////////////////////////
		#region Connection Add & Remove

		private void AddConnection( NetworkConnection networkConnection )
		{
			List<NetworkConnection> otherConnections = new List<NetworkConnection>( networkConnections );
			
			networkConnectionsNative.Add( networkConnection );

			networkConnections.Add( networkConnection );

			onClientAdded?.Invoke( networkConnection );

			unchecked // rotatingNetworkId should overflow to 0
			{
				do
				{
					// This should - at most - only iterate by the max number of players to find an available id.
					// If you're going for thousands of connected clients this might need to be threaded or in a coroutine with frame delays.
					rotatingNetworkId++;
				}
				while( rotatingNetworkId == 0 || IsValidId( rotatingNetworkId ) );
			}

			ushort clientId = rotatingNetworkId;

			clientInfoByNetworkId.Add( clientId, new ClientInfo()
			{
				networkId = clientId,
				networkConnection = networkConnection,
				joinTime = System.DateTime.Now
			} );
			networkIdsByNetworkConnection.Add( networkConnection, clientId );
			
			if( otherConnections.Count > 0 )
				Send( new Packets.Handshake() { networkId = clientId, operation = Packets.Handshake.Operation.CreateOther, targets = otherConnections }, true );
			
			Bundle welcomeBundle = new Bundle();

			welcomeBundle.packets.Add( new Packets.Handshake() { networkId = clientId, operation = Packets.Handshake.Operation.AssignSelf, targets = new List<NetworkConnection>() { networkConnection } } );
			welcomeBundle.packets.Add( new Packets.Ping() { timingsByNetworkId = new Dictionary<ushort, ushort>( pingTimingsByNetworkId ) } );
			welcomeBundle.targets.Add( networkConnection );

			Send( welcomeBundle, true );

			Send( new Packets.ServerInfo( hostInfo ), true );
			
			if( debug )
				Debug.LogWarning( $"Server: Added client {networkConnection.InternalId}" );

			if( isPrivate )
				pendingInternalIds.Add( networkConnection.InternalId, 0 );
			else
				OnClientApproved( clientId );
		}

		private void RemoveConnection( NetworkConnection networkConnection, int nativeIndex = -1 )
		{
			onClientDropped?.Invoke( networkConnection );
			
			int internalId = networkConnection.InternalId;
			
			if( _isPrivate )
			{
				if( pendingInternalIds.ContainsKey( internalId ) )
					pendingInternalIds.Remove( internalId );

				if( approvedInternalIds.Contains( internalId ) )
					approvedInternalIds.Remove( internalId );
			}

			if( nativeIndex < 0 || nativeIndex > networkConnectionsNative.Length )
			{
				
				for( int i = 0, iC = networkConnectionsNative.Length; i < iC; i++ )
				{
					if( networkConnectionsNative[ i ].InternalId == internalId )
					{
						nativeIndex = i;
						break;
					}
				}
			}

			networkConnections.Remove( networkConnection );

			networkConnectionsNative.RemoveAtSwapBack( nativeIndex );

			if( networkIdsByNetworkConnection.ContainsKey( networkConnection ) )
			{
				ushort clientId = networkIdsByNetworkConnection[ networkConnection ];

				if( clientsReady.Contains( clientId ) )
					clientsReady.Remove( clientId );

				if( pingTimingsByNetworkId.ContainsKey( clientId ) )
					pingTimingsByNetworkId.Remove( clientId );

				if( usernamesByNetworkId.ContainsKey( clientId ) )
					usernamesByNetworkId.Remove( clientId );

				Send( new Packets.Handshake() { networkId = clientId, operation = Packets.Handshake.Operation.DestroyOther }, true );

				adminIds.Remove( clientId );
				if( adminAuthorizationAttempts.ContainsKey( clientId ) )
					adminAuthorizationAttempts.Remove( clientId );

				clientInfoByNetworkId.Remove( clientId );
				networkIdsByNetworkConnection.Remove( networkConnection );
			}

			if( debug )
				Debug.LogWarning( "Server: Removed client {networkConnection.InternalId}" );
		}

		#endregion

		////////////////////////////////
		#region Password & Approval

		private Dictionary<int, byte> pendingInternalIds = new Dictionary<int, byte>();
		private HashSet<int> approvedInternalIds = new HashSet<int>();

		private ReadResult OnPassword( NetworkConnection networkConnection, ref PacketReader reader )
		{
			Packets.Password packetPassword = new Packets.Password( ref reader );

			int id = networkConnection.InternalId;
			
			if( !pendingInternalIds.ContainsKey( id ) )
				return ReadResult.Error;

			if( string.Equals( hostInfo.password, packetPassword.password ) )
			{
				approvedInternalIds.Add( id );
				pendingInternalIds.Remove( id );

				OnClientApproved( networkIdsByNetworkConnection[ networkConnection ] );

				return ReadResult.Consumed;
			}
			else
			{
				pendingInternalIds[ id ]++;
				int attempts = pendingInternalIds[ id ];

				if( debug )
					Debug.LogWarning( $"Server: Denied client {id} ( {attempts} / {hostInfo.maxPasswordAttempts} )" );

				if( attempts < hostInfo.maxPasswordAttempts )
				{
					int remainingAttempts = hostInfo.maxPasswordAttempts - attempts;
					Send( new Packets.Password() { password = string.Format("Incorrect password; {0} {1} remaining.", remainingAttempts, remainingAttempts == 1 ? "attempt" : "attempts" ), targets = new List<NetworkConnection>() { networkConnection } }, true );
				}
				else
				{
					AdminBoot( networkIdsByNetworkConnection[ networkConnection ], "Invalid Password", false );
					pendingInternalIds.Remove( id );
				}
				
				return ReadResult.Error;

			}
		}

		private void OnClientApproved( ushort clientId )
		{
			if( clientId == 0 || !clientInfoByNetworkId.ContainsKey( clientId ) )
				return;

			ClientInfo clientInfo = clientInfoByNetworkId[ clientId ];

			Bundle welcomeBundle = new Bundle();

			welcomeBundle.packets.Add( new Packets.Password() { networkId = clientId } );

			foreach( ushort otherId in clientInfoByNetworkId.Keys )
			{
				if( otherId != clientId )
				{
					welcomeBundle.packets.Add( new Packets.Handshake() { networkId = otherId, operation = Packets.Handshake.Operation.CreateOther } );
					welcomeBundle.packets.Add( new Packets.Username() { networkId = otherId, name = usernamesByNetworkId.ContainsKey( otherId ) ? usernamesByNetworkId[ otherId ] : string.Empty } );
				}
			}

			welcomeBundle.targets.Add( clientInfo.networkConnection );
			Send( welcomeBundle, true );

			UpdateClientReady( clientId );

			if( debug )
				Debug.LogWarning( $"Server: Approved client {clientInfo.networkConnection.InternalId}" );
		}

		#endregion

		////////////////////////////////
		#region Internal Read

		private static HashSet<ushort> alwaysAllowedPacketTypes = new HashSet<ushort>()
		{
			Packet.Hash( typeof( Packets.Loopback ) )
		};

		private static ushort passwordPacketHash = Packet.Hash( typeof( Packets.Password ) );

		protected override ReadResult InternalRead( NetworkConnection connection, ref PacketReader reader )
		{
			switch( base.InternalRead( connection, ref reader ) )
			{
				case ReadResult.Consumed:
					return ReadResult.Consumed;
				case ReadResult.Error:
					return ReadResult.Error;

				default:
					break;
			}
			
			ushort packetId = reader.ReadUShort();
			
			// Filter packets until password has been approved
			if( _isPrivate )
			{
				if( alwaysAllowedPacketTypes.Contains( packetId ) )
					return ReadResult.Skipped;

				if( approvedInternalIds.Contains( connection.InternalId ) )
					return ReadResult.Skipped;
				
				if( packetId == passwordPacketHash )
				{
					if( OnPassword( connection, ref reader ) != ReadResult.Consumed )
						return ReadResult.Error;
					else
						return ReadResult.Consumed;
				}

				return ReadResult.Error;
			}

			return ReadResult.Skipped;
		}

		#endregion

		////////////////////////////////
		#region Network Update

		protected override void NetworkUpdate()
		{
			if( !driver.IsCreated )
				return;

			driver.ScheduleUpdate().Complete();

			CleanupOldConnections();

			AcceptNewConnections();

			QueryForEvents();

			SendQueue();

			if( closing && !closeInvoked && packetsSentLastFrame == 0 )
			{
				closeInvoked = true;
				Utils.Delay( disconnectAndCloseDelay, Close );
			}
		}

		#endregion

		////////////////////////////////
		#region Query For Events

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
		#region Send Queue
			
		protected Dictionary<NetworkConnection, LinkedList<Packet>> managedQueue = new Dictionary<NetworkConnection, LinkedList<Packet>>();

		public override void Send( Packet packet, bool reliable = false )
		{
			if( playerCount == 0 || closing )
				return;
			
			base.Send( packet, reliable );
		}

		protected override void SendQueue()
		{
			packetsSentLastFrame = 0;

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
				
				while( packets.Count > 0 )
				{
					packet = packets.First.Value;
					
					int i = driver.BeginSend( pipeline.reliable, target, out DataStreamWriter writer, packet.bytes + 2 );

					// Add Packet identifier
					writer.WriteUShort( packet.id );

					// Apply / write packet data to data stream writer
					packet.Write( ref writer );

					driver.EndSend( writer );

					int errorId = GetReliabilityError( target );
					if( errorId != 0 )
					{
						ReliableUtility.ErrorCodes error = ( ReliableUtility.ErrorCodes ) errorId;

						if( error != ReliableUtility.ErrorCodes.OutgoingQueueIsFull )
							Debug.LogError( $"Reliability Error: {error}" );
						else
							break;
					}
					
					packetsSentLastFrame++;

					packets.RemoveFirst();
				}

				if( packets.Count == 0 && managedQueue[ target ].Count == 0 )
					managedQueue.Remove( target );
			}

			while( packetQueue.unreliable.Count > 0 )
			{
				packet = packetQueue.unreliable.Dequeue();

				List<NetworkConnection> unreliableTargets = packet.targets.Count > 0 ? packet.targets : networkConnections;

				foreach( NetworkConnection connection in unreliableTargets )
				{
					int i = driver.BeginSend( pipeline.unreliable, connection, out DataStreamWriter writer, packet.bytes + 2 );

					writer.WriteUShort( packet.id );

					packet.Write( ref writer );

					driver.EndSend( writer );
				}

				packetsSentLastFrame++;
			}
		}

		#endregion

		////////////////////////////////
		#region Usernames

		public delegate string SanitizeUsername( string name );
		public SanitizeUsername sanitizeUsername = null;

		private ReadResult OnUsername( NetworkConnection networkConnection, ref PacketReader reader )
		{
			Packets.Username username = new Packets.Username( ref reader );

			ushort clientId = GetNetworkId( networkConnection );
			username.networkId = clientId;
			
			string name = username.name.Trim();
			if( string.IsNullOrWhiteSpace( name ) )
			{
				AdminRename( clientId, "Your name cannot be empty." );
				return ReadResult.Consumed;
			}

			if( !object.Equals( null, sanitizeUsername ) )
			{
				name = sanitizeUsername( name );
				if( string.IsNullOrEmpty( name ) )
				{
					AdminRename( clientId, "Your name was not allowed." );
					return ReadResult.Consumed;
				}
			}

			if( allowedUsernameDuplicates != UsernameDuplicates.Any )
			{
				StringComparison stringComparison = ( allowedUsernameDuplicates == UsernameDuplicates.DifferentCase ) ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
				foreach( KeyValuePair<ushort, string> kvp in usernamesByNetworkId )
				{
					if( kvp.Key == clientId )
						continue;

					if( string.Equals( name, kvp.Value, stringComparison ) )
					{
						AdminRename( clientId, "Your name cannot be a duplicate." );
						return ReadResult.Consumed;
					}
				}
			}
			
			SetUsername( clientId, name );

			username.name = name;
			Send( username, true );

			UpdateClientReady( clientId );

			return ReadResult.Consumed;
		}

		#endregion

		////////////////////////////////
		#region Client Ready

		private HashSet<ushort> clientsReady = new HashSet<ushort>();

		public bool IsClientReady( ushort clientId ) => clientsReady.Contains( clientId );
		
		private bool UpdateClientReady( ushort clientId )
		{
			if( clientId == 0 || !clientInfoByNetworkId.ContainsKey( clientId ) )
				return false;
			
			if( clientsReady.Contains( clientId ) )
				return true;
			
			ClientInfo clientInfo = clientInfoByNetworkId[ clientId ];

			if( !EvaluateClientReady( clientInfo.networkConnection, clientId ) )
				return false;

			clientsReady.Add( clientId );

			OnClientReady( clientInfo.networkConnection );
			
			if( debug )
				Debug.LogWarning( "Server: Client {clientInfo.networkConnection.InternalId} is Ready" );

			return true;
		}

		protected virtual bool EvaluateClientReady( NetworkConnection networkConnection, ushort clientId )
		{
			if( _isPrivate && !approvedInternalIds.Contains( networkConnection.InternalId ) )
				return false;
			
			if( !usernamesByNetworkId.ContainsKey( clientId ) )
				return false;
			
			return true;
		}

		protected virtual void OnClientReady( NetworkConnection networkConnection )
		{
			if( promoteAll )
				AdminPromote( networkIdsByNetworkConnection[ networkConnection ] );
			
			if( adminVisibility == AdminVisibility.Everyone )
			{
				Bundle packetBundle = new Bundle();
				foreach( ushort adminId in adminIds )
					packetBundle.packets.Add( new Packets.Administration() { networkId = adminId, operation = Packets.Administration.Operation.Promote } );

				packetBundle.targets.Add( networkConnection );

				Send( packetBundle, true );
			}

			onClientReady?.Invoke( networkConnection );
		}

		#endregion

		////////////////////////////////
		#region Loopback & Ping

		private ReadResult OnLoopback( NetworkConnection connection, ref PacketReader reader )
		{
			Packets.Loopback packetLoopback = new Packets.Loopback( ref reader );

			packetLoopback.targets.Add( connection );
			Send( packetLoopback );

			if( networkIdsByNetworkConnection.ContainsKey( connection ) )
				SetPing( networkIdsByNetworkConnection[ connection ], ( ushort ) Mathf.Max( 1, packetLoopback.averagePingMS ) );

			return ReadResult.Consumed;
		}

		private Coroutine _IPingBroadcast = null;
		private IEnumerator IPingBroadcast()
		{
			WaitForSeconds wait = new WaitForSeconds( pingBroadcastInterval );
			do
			{
				if( pingTimingsByNetworkId.Count > 0 )
				{
					Packets.Ping packetPing = new Packets.Ping() { timingsByNetworkId = new Dictionary<ushort, ushort>( pingTimingsByNetworkId ) };
					Send( packetPing );
				}

				yield return wait;
			}
			while( true );
		}

		#endregion

		////////////////////////////////
		#region Admins

		public List<NetworkConnection> GetAdminNetworkConnections()
		{
			List<NetworkConnection> networkConnections = new List<NetworkConnection>();

			foreach( ushort adminId in adminIds )
				networkConnections.Add( GetNetworkConnection( adminId ) );

			return networkConnections;
		}

		public override bool AdminPromote( ushort clientId )
		{
			if( clientId == 0 || !clientNetworkIds.Contains( clientId ) )
				return false;

			if( !base.AdminPromote( clientId ) )
				return false;

			Packets.Administration packetPromote = new Packets.Administration() { networkId = clientId, operation = Packets.Administration.Operation.Promote };
			if( adminVisibility == AdminVisibility.AdminsOnly )
				packetPromote.targets = GetAdminNetworkConnections();

			Send( packetPromote, true );

			if( adminVisibility == AdminVisibility.AdminsOnly )
			{
				Bundle packetBundle = new Bundle();
				foreach( ushort adminId in adminIds )
					packetBundle.packets.Add( new Packets.Administration() { networkId = adminId, operation = Packets.Administration.Operation.Promote } );

				packetBundle.targets.Add( GetNetworkConnection( clientId ) );

				Send( packetBundle, true );
			}

			return true;
		}

		public override bool AdminDemote( ushort clientId )
		{
			if( clientId == 0 || !adminIds.Contains( clientId ) )
				return false;

			if( clientId == localHostId )
				return false;

			Packets.Administration packetAdministration = new Packets.Administration() { networkId = clientId, operation = Packets.Administration.Operation.Demote };
			if( adminVisibility == AdminVisibility.AdminsOnly )
				packetAdministration.targets = GetAdminNetworkConnections();

			if( !base.AdminDemote( clientId ) )
				return false;

			Send( packetAdministration, true );

			if( ( adminVisibility == AdminVisibility.AdminsOnly ) && ( adminIds.Count > 0 ) )
			{
				Packets.Bundle packetBundle = new Packets.Bundle();
				packetBundle.targets.Add( GetNetworkConnection( clientId ) );
				
				foreach( ushort adminId in adminIds )
					packetBundle.packets.Add( new Packets.Administration() { networkId = adminId, operation = Packets.Administration.Operation.Demote } );
				
				Send( packetBundle, true );
			}

			return true;
		}

		private Dictionary<ushort, List<float>> adminAuthorizationAttempts = new Dictionary<ushort, List<float>>();

		private bool AdminAuthorize( ushort clientId, string password )
		{
			if( !IsValidId( clientId ) )
				return false;

			if( promoteAll )
			{
				if( !adminIds.Contains( clientId ) )
					AdminPromote( clientId );

				return true;
			}

			if( !string.Equals( hostInfo.adminPassword, password ) )
			{
				if( hostInfo.maxAdminAttempts > 0 )
				{
					if( adminAuthorizationAttempts.ContainsKey( clientId ) )
					{
						adminAuthorizationAttempts[ clientId ].Add( Time.time );

						if( hostInfo.adminAttemptsArePerMinute )
						{
							List<float> attemptsInTheLastMinute = new List<float>();
							float threshold = Mathf.Max( 0f, Time.time - 60f );
							foreach( float time in adminAuthorizationAttempts[ clientId ] )
								if( time >= threshold )
									attemptsInTheLastMinute.Add( time );

							adminAuthorizationAttempts[ clientId ] = attemptsInTheLastMinute;
						}

						if( adminAuthorizationAttempts[ clientId ].Count > hostInfo.maxAdminAttempts )
						{
							AdminBoot( clientId, "Too many invalid authorization attempts!" );
						}
					}
					else
					{
						adminAuthorizationAttempts.Add( clientId, new List<float>() { Time.time } );
					}
				}

				return false;
			}

			AdminPromote( clientId );

			return true;
		}

		#endregion

		////////////////////////////////
		#region Administration

		private ReadResult OnAdministration( NetworkConnection networkConnection, ref PacketReader reader )
		{
			Packets.Administration packetAdministration = new Packets.Administration( ref reader );

			ushort clientId = GetNetworkId( networkConnection );

			if( packetAdministration.operation == Packets.Administration.Operation.Authorize )
			{
				AdminAuthorize( clientId, packetAdministration.text );
				
				return ReadResult.Consumed;
			}

			if( !adminIds.Contains( clientId ) )
			{
				Debug.LogError( "Client {clientId} ( {GetUsername( clientId )} ) is attempting to issue admin operations without permissions." );
				return ReadResult.Consumed;
			}
			
			switch( packetAdministration.operation )
			{
				default:
					break;

				case Packets.Administration.Operation.Shutdown:
					AdminShutdown( packetAdministration.text );
					break;

				case Packets.Administration.Operation.Alert:
					AdminAlert( packetAdministration.text, packetAdministration.networkId );
					break;

				case Packets.Administration.Operation.Promote:
					AdminPromote( packetAdministration.networkId );
					break;

				case Packets.Administration.Operation.Demote:
					AdminDemote( packetAdministration.networkId );
					break;

				case Packets.Administration.Operation.Rename:
					AdminRename( packetAdministration.networkId, packetAdministration.text );
					break;

				case Packets.Administration.Operation.Kick:
					AdminBoot( packetAdministration.networkId, packetAdministration.text );
					break;

				case Packets.Administration.Operation.Ban:
					AdminBoot( packetAdministration.networkId, packetAdministration.text, true );
					break;
			}
			
			return ReadResult.Consumed;
		}

		#endregion

		////////////////////////////////
		#region Admin Operations

		public void AdminShutdown( string reason )
		{
			DisconnectAndClose( reason );
		}

		public void AdminAlert( string alert, ushort targetId = 0 )
		{
			Packets.Administration packetAdminAlert = new Packets.Administration()
			{
				operation = Packets.Administration.Operation.Alert,
				text = alert
			};

			if( IsValidId( targetId ) )
				packetAdminAlert.targets.Add( GetNetworkConnection( targetId ) );

			Send( packetAdminAlert, true );
		}

		public void AdminRename( ushort clientId, string reason = null )
		{
			if( !IsValidId( clientId ) )
				return;

			Packets.Administration packetAdminRename = new Packets.Administration() { operation = Packets.Administration.Operation.Rename, text = reason };
			packetAdminRename.targets.Add( GetNetworkConnection( clientId ) );
			
			Send( packetAdminRename, true );
		}

		public void AdminBoot( ushort clientId, string reason, bool isBan = false )
		{
			if( !IsValidId( clientId ) )
				return;

			if( clientId == localHostId )
				return;

			Packets.Administration packetAdminBoot = new Packets.Administration()
			{
				operation = isBan ? Packets.Administration.Operation.Ban : Packets.Administration.Operation.Kick,
				networkId = clientId,
				text = reason
			};
			NetworkConnection targetConnection = GetNetworkConnection( clientId );
			packetAdminBoot.targets.Add( targetConnection );
			
			Send( packetAdminBoot, true );

			Utils.Delay( disconnectAndCloseDelay, () =>
			{
				if( !object.Equals( null, driver ) && driver.IsCreated )
					driver.Disconnect( targetConnection );
			} );
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

			inspectorHeight += lineHeight * 8f + 24f;
			
			inspectorHeight += EditorGUI.GetPropertyHeight( this[ "hostInfo" ], true ) + lineHeight * 0.5f;

			if( Application.isPlaying )
			{
				inspectorHeight += lineHeight * 1.5f;
				int clientCount = server.clientNetworkIds.Count;
				if( clientCount > 0 )
					inspectorHeight += ( 4f + lineHeight ) * clientCount;
			}
			
			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Networking - Server" ) );
			bRect.y += lineHeight * 1.5f;

			EditorGUI.Slider( bRect, this[ "pingBroadcastInterval" ], 1f, 10f, new GUIContent( "Ping Broadcast Interval", "The delay between ping broadcast packets used to update clients of player pings." ) );
			bRect.y += lineHeight + 4f;

			EditorGUI.Slider( bRect, this[ "disconnectAndCloseDelay" ], 0.1f, 5f, new GUIContent( "Disconnect And Close Delay", "How long the server waits to close after sending shutdown packets." ) );
			bRect.y += lineHeight + 4f;

			EditorGUI.PropertyField( bRect, this[ "allowedUsernameDuplicates" ], new GUIContent( "Allowed Username Duplicates" ) );
			bRect.y += lineHeight + 4f;

			EditorGUI.PropertyField( bRect, this[ "adminVisibility" ], new GUIContent( "Admin Visiblity", "Which clients can see the operator flair." ) );
			bRect.y += lineHeight + 4f;

			EditorGUI.Slider( bRect, this[ "natReservationTime" ], 10f, 240f, new GUIContent( "NAT Reservation Time ( minutes )", "How long the NAT port reservation should last in minutes." ) );
			bRect.y += lineHeight + 4f;

			bRect.height = lineHeight * 1.5f;
			EditorUtils.BetterToggleField( bRect, new GUIContent( "NAT Punch-Through", "Uses Open.NAT to negotiate NAT port forwarding." ), this[ "useNatPunchThrough" ] );
			bRect.y += bRect.height + 4f;
			bRect.height = lineHeight;
			
			bRect.height = EditorGUI.GetPropertyHeight( this[ "hostInfo" ], true );
			EditorGUI.PropertyField( bRect, this[ "hostInfo" ], true );
			bRect.y += bRect.height + lineHeight * 0.5f;
			bRect.height = lineHeight;

			if( Application.isPlaying )
			{
				float labelWidth = EditorGUIUtility.labelWidth;

				EditorGUI.LabelField( bRect, new GUIContent( "Connected Players", "The current number of active connections out of the maximum." ) );
				cRect = new Rect( bRect.x + labelWidth, bRect.y, bRect.width - labelWidth, bRect.height );
				cRect.width = ( cRect.width - 45f ) * 0.5f;

				EditorGUI.BeginDisabledGroup( true );
				EditorGUI.IntField( cRect, new GUIContent( string.Empty, "Connected Players" ), server.playerCount );
				EditorGUI.EndDisabledGroup();

				EditorGUI.LabelField( new Rect( cRect.x + cRect.width + 5f, cRect.y, 35f, cRect.height ), new GUIContent( "out of" ) );
				cRect.x += cRect.width + 45f;
				EditorGUI.BeginDisabledGroup( server.open );
				EditorGUI.PropertyField( cRect, this[ this[ "hostInfo" ], "maxPlayers" ], new GUIContent( string.Empty, "Max Players" ) );
				EditorGUI.EndDisabledGroup();
				
				bRect.y += lineHeight;

				List<ushort> clientIds = server.clientNetworkIds;
				int clientCount = clientIds.Count;
				if( clientCount > 0 )
				{
					bRect.y += 4f;

					Rect labelRect = new Rect( bRect.x + 15f, bRect.y, labelWidth - 15f, bRect.height );
					
					bRect.x += labelWidth;
					bRect.width -= labelWidth;

					cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) / 2f, lineHeight );
					Rect dRect = new Rect( cRect.x + cRect.width + 10f, cRect.y, cRect.width, lineHeight );

					foreach( ushort clientId in clientIds )
					{
						EditorUtils.DrawClickCopyLabel( labelRect, "Username", server.GetUsername( clientId ) );
						EditorUtils.DrawClickCopyLabel( cRect, "Network Id", clientId.ToString() );
						EditorUtils.DrawClickCopyLabel( dRect, "Join Time", server.GetClientJoinTime( clientId ).ToString( "T" ).ToLower() );
						
						labelRect.y += lineHeight + 4f;
						cRect.y += lineHeight + 4f;
						dRect.y += lineHeight + 4f;
					}

					bRect.x -= labelWidth;
					bRect.width += labelWidth;

					bRect.y += ( lineHeight * clientCount ) + ( 5f * ( clientCount - 1 ) );
				}
				
				bRect.y += lineHeight * 0.5f;
			}

			rect.y = bRect.y;
		}
	}
#endif
}
