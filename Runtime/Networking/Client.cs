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

		protected override string displayName => "Client";

		////////////////////////////////
		
		public static Singleton<Client> singleton = new Singleton<Client>();

		////////////////////////////////

		protected override void Awake()
		{
			base.Awake();

			singleton.instance = this;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if( openOnEnable )
				Open();
		}

		protected override void OnDisable()
		{
			Close();

			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			if( driver.IsCreated )
				driver.Dispose();

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

		protected NetworkConnection connection;

		private uint reconnectAttempts = 0;

		////////////////////////////////

		public bool isConnected { get; private set; }

		public delegate void OnNetworkEvent();

		public event OnNetworkEvent onConnected;
		public event OnNetworkEvent onDisconnected;

		public bool reattemptFailedConnections = false;

		public bool openOnEnable = false;
		
		////////////////////////////////

		public override void Open()
		{
			RegisterPacketDatas();

			base.Open();

			connection = default( NetworkConnection );

			connection = driver.Connect( NetworkEndPoint.Parse( this.serverAddress ?? string.Empty, this.port ) );

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

		////////////////////////////////

		private void QueryForEvents()
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

		private void SendQueue()
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

		protected override ReadResult InternalRead( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			ushort packetId = reader.ReadUShort( ref context );
			
			if( packetId == Packet.Hash( typeof( Packets.PacketMap ) ) )
			{
				Packets.PacketMap packetMap = new Packets.PacketMap( ref reader, ref context );

				packetHashMap = new PacketHashMap( packetMap.map );

				return ReadResult.Consumed;
			}
			
			return ReadResult.Skipped;
		}

		////////////////////////////////

		private void Update()
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

			if( reattemptFailedConnections && ( connectionState == NetworkConnection.State.Disconnected ) )
			{
				Debug.LogFormat( "Client: Connection to '{0}:{1}' failed; auto-reconnect attempt {2}.", connectionAddress, port, ++reconnectAttempts );
				
				connection = driver.Connect( NetworkEndPoint.Parse( this.serverAddress ?? string.Empty, this.port ) );

				return;
			}

			if( connectionState != NetworkConnection.State.Connected )
				return;

			SendQueue();
		}

		////////////////////////////////

		private Coroutine _IHeartbeat = null;
		private IEnumerator IHeartbeat()
		{
			while( true )
			{
				yield return new WaitForSeconds( 10f );

				if( isConnected )
				{
					Send( new Packet() );
				}
			}
		}

		////////////////////////////////
	}


#if UNITY_EDITOR
	[CustomEditor( typeof( Client ) )]
	public class EditorClient : Editor
	{
		Client client;
		public void OnEnable()
		{
			client = ( Client ) target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			GUILayout.Space( 10f );

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();

			EditorGUI.BeginDisabledGroup( !Application.isPlaying );

			if( client.open )
			{
				if( GUILayout.Button( "Close Connection" ) )
				{
					client.Close();
				}
			}
			else
			{
				if( GUILayout.Button( "Open Connection" ) )
				{
					client.Open();
				}
			}
			
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 10f );

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}