using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline.Networking
{
	public class Client : Shared
	{
		////////////////////////////////
		//	Open & Close

		#region OpenClose

		public override void Open()
		{
			failedReconnects = 0;

			base.Open();

			connection = default( NetworkConnection );

			connection = driver.Connect( NetworkEndPoint.Parse( this.address ?? string.Empty, this.port ) );

			_IHeartbeat = StartCoroutine( IHeartbeat() );
		}

		public override void Close()
		{
			if( connection.IsCreated )
			{
				connection.Disconnect( driver );
				connection = default( NetworkConnection );
			}

			if( !object.Equals( null, _IHeartbeat ) )
			{
				StopCoroutine( _IHeartbeat );
				_IHeartbeat = null;
			}

			isConnected = false;

			onDisconnected?.Invoke();

			base.Close();
		}

		#endregion

		////////////////////////////////
		//	NetworkUpdate

		#region NetworkUpdate

		protected override void NetworkUpdate()
		{
			if( !driver.IsCreated )
				return;

			driver.ScheduleUpdate().Complete();

			if( !connection.IsCreated )
				return;

			QueryForEvents();

			// Events *could* result in destruction of these, so now we re-check.
			if( !driver.IsCreated || !connection.IsCreated )
				return;

			NetworkConnection.State connectionState = driver.GetConnectionState( connection );

			switch( connectionState )
			{
				default:
					return;

				case NetworkConnection.State.Disconnected:
					if( failedReconnects < reconnectAttempts )
					{
						Debug.LogFormat( "Client: Connection to '{0}:{1}' failed; auto-reconnect attempt {2}.", address, port, ++failedReconnects );

						connection = driver.Connect( NetworkEndPoint.Parse( this.address ?? string.Empty, this.port ) );
					}

					return;

				case NetworkConnection.State.Connected:
					break;
			}

			SendQueue();
		}

		#endregion

		////////////////////////////////
		//	QueryForEvents

		#region QueryForEvents

		protected override void QueryForEvents()
		{
			DataStreamReader stream;
			NetworkEvent.Type netEventType;
			while( driver.IsCreated && connection.IsCreated && ( netEventType = connection.PopEvent( driver, out stream ) ) != NetworkEvent.Type.Empty )
			{
				if( netEventType == NetworkEvent.Type.Connect )
				{
					if( debug )
						Debug.Log( "Client: Connected to the server." );

					reconnectAttempts = 0;

					isConnected = true;
					hasConnected = true;

					onConnected?.Invoke();
				}
				else if( netEventType == NetworkEvent.Type.Data )
				{
					Read( connection, ref stream );
				}
				else if( netEventType == NetworkEvent.Type.Disconnect )
				{
					if( debug )
						Debug.Log( "Client: Removed from server." );

					Close();
				}
			}
		}

		#endregion

		////////////////////////////////
		//	SendQueue

		#region SendQueue

		public override void Send( Packet packet, bool reliable = false )
		{
			if( !isConnected )
				return;

			base.Send( packet, reliable );
		}

		protected override void SendQueue()
		{
			Packet packet;

			while( packetQueue.reliable.Count > 0 )
			{
				packet = packetQueue.reliable.Dequeue();

				DataStreamWriter writer = new DataStreamWriter( packet.bytes + 2, Allocator.Temp );

				writer.Write( packet.id );

				packet.Write( ref writer );

				connection.Send( driver, pipeline.reliable, writer );

				writer.Dispose();
			}

			while( packetQueue.unreliable.Count > 0 )
			{
				packet = packetQueue.unreliable.Dequeue();

				DataStreamWriter writer = new DataStreamWriter( packet.bytes + 2, Allocator.Temp );
				
				writer.Write( packet.id );

				packet.Write( ref writer );

				connection.Send( driver, pipeline.unreliable, writer );

				writer.Dispose();
			}
		}

		#endregion

		////////////////////////////////
		//	Heartbeat & Loopback

		#region HeartbeatLoopback

		private Queue<float> pingTimings = new Queue<float>();

		public float pingAverage { get; private set; } = 0f;
		public int pingAverageMS { get; private set; } = 0;

		private Coroutine _IHeartbeat = null;
		private IEnumerator IHeartbeat()
		{
			pingTimings.Clear();
			pingAverage = 0f;

			while( true )
			{
				if( isConnected )
				{
					Send( new Packets.PacketLoopback()
					{
						originTime = Time.time
					} );
				}

				yield return new WaitForSeconds( heartbeatInterval );
			}
		}

		private ReadResult OnPacketLoopback( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			Packets.PacketLoopback packetLoopback = new Packets.PacketLoopback( ref reader, ref context );
			
			pingTimings.Enqueue( Time.time - packetLoopback.originTime );

			if( pingTimings.Count > pingAverageQueueSize )
				pingTimings.Dequeue();

			float _pingAverage = 0f;
			foreach( float pingTime in pingTimings )
				_pingAverage += pingTime;

			if( pingTimings.Count > 1 )
				_pingAverage /= pingTimings.Count;

			pingAverage = _pingAverage;
			pingAverageMS = Mathf.RoundToInt( pingAverage * 1000f );

			return ReadResult.Consumed;
		}

		#endregion

		////////////////////////////////
		//	Events

		#region Events

		public event Callback onConnected;
		public event Callback onDisconnected;

		#endregion

		////////////////////////////////
		//	Properties

		#region Properties

		public override string networkScopeLabel => "Client";

		public static Singleton<Client> singleton { get; private set; } = new Singleton<Client>();

		public NetworkConnection connection { get; private set; }

		public bool isConnected { get; private set; }
		private bool hasConnected;

		public int failedReconnects { get; private set; }

		#endregion

		////////////////////////////////
		//	Attributes

		#region Attributes
			
		[Range( 1f, 10f )]
		public float heartbeatInterval = 5f;
		[Range( 1, 10 )]
		public int pingAverageQueueSize = 3;
		[Range( 0, 10 )]
		public int reconnectAttempts = 3;

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
			if( driver.IsCreated && hasConnected )
				driver.Dispose();

			if( object.Equals( singleton.instance, this ) )
				singleton.instance = null;

			base.OnDestroy();
		}

		#endregion
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( Client ) )]
	public class EditorClient : EditorShared
	{
		Client client;

		public override void Setup()
		{
			base.Setup();

			client = ( Client ) target;
		}

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 5f + 8f;

			if( client.isConnected )
				inspectorHeight += lineHeight * 1f + 4f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Networking - Client" ) );
			bRect.y += lineHeight * 1.5f;

			EditorGUI.Slider( bRect, this[ "heartbeatInterval" ], 1f, 10f, new GUIContent( "Heartbeart Interval", "The delay between individual heartbeat packets used for ping updates." ) );
			bRect.y += lineHeight + 4f;

			EditorGUI.IntSlider( bRect, this[ "pingAverageQueueSize" ], 1, 10, new GUIContent( "Ping Averages Count", "The amount of ping results that should be tracked and averaged." ) );
			bRect.y += lineHeight + 4f;

			EditorGUI.IntSlider( bRect, this[ "reconnectAttempts" ], 0, 10, new GUIContent( "Reconnect Attempts", "Amount of times to try reconnecting on unsignaled disconnects." ) );

			if( client.isConnected )
			{
				bRect.y += lineHeight + 4f;

				EditorGUI.BeginDisabledGroup( true );

				EditorGUI.IntField( bRect, new GUIContent( "Average Ping" ), client.pingAverageMS );

				EditorGUI.EndDisabledGroup();
			}

			bRect.y += lineHeight * 1.5f;

			rect.y = bRect.y;
		}
	}
#endif
}