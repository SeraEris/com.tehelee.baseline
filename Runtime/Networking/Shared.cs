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

namespace Tehelee.Baseline.Networking
{
	public class Shared : MonoBehaviour
	{
		////////////////////////////////
		//	Packet Datas

		#region PacketDatas

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

		#endregion

		////////////////////////////////
		//	Pipeline

		#region Pipeline

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

		#endregion

		////////////////////////////////
		//	Open & Close

		#region OpenClose

		public virtual void Open()
		{
			if( open )
				return;
			
			Packet.Register( typeof( Packets.PacketBundle ) );
			Packet.Register( typeof( Packets.PacketLoopback ) );

			RegisterPacketDatas();

			Dictionary<string, string> args = new Dictionary<string, string>();
			args.Add( "networkAddress", null );
			args.Add( "networkPort", null );

			Utils.GetArgsFromDictionary( ref args );

			if( !string.IsNullOrEmpty( args[ "networkAddress" ] ) )
				this.address = args[ "networkAddress" ];

			this.address = this.address ?? LoopbackAddress;

			if( args[ "networkPort" ] != null )
			{
				ushort port;
				if( ushort.TryParse( args[ "networkPort" ], out port ) )
					this.port = port;
			}

			driver = new UdpNetworkDriver( new ReliableUtility.Parameters { WindowSize = networkParameters.maxPacketCount }, new NetworkConfigParameter { connectTimeoutMS = networkParameters.connectTimeoutMS, disconnectTimeoutMS = networkParameters.disconnectTimeoutMS, maxConnectAttempts = networkParameters.maxConnectAttempts }, ( SimulatorUtility.Parameters ) networkParameters );
			
			pipeline = new Pipeline( driver );

			open = true;

			if( debug )
				Debug.LogFormat( "{0}.Open() {1} on {2} with {3}", networkScopeLabel, this.address, this.port, networkParameters.ToString() );
		}

		public virtual void Close()
		{
			if( !open )
				return;

			open = false;

			pipeline = default;

			driver.Dispose();

			UnregisterPacketDatas();

			if( debug )
				Debug.LogFormat( "{0}.Close()", networkScopeLabel );
		}

		#endregion

		////////////////////////////////
		//	Send & Recieve

		#region SendRecieve

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

			if( packetMonitors.ContainsKey( packet.id ) && packetMonitors[ packet.id ] != null )
			{
				Dictionary<int, SendMonitor> listeners = packetMonitors[ packet.id ];

				List<int> keys = new List<int>( listeners.Keys );
				keys.Sort();

				for( int i = 0; i < keys.Count; i++ )
				{
					int key = keys[ i ];
					SendMonitor spyListener = listeners[ key ];

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
				Debug.LogWarningFormat( "{0}.Write( {1} ) using {2} channel.{3}", networkScopeLabel, Packet.LookupType( packet.id ).FullName, reliable ? "verified" : "fast", packet.targets.Count > 0 ? string.Format( " Sent only to {0} connections.", packet.targets.Count ) : string.Empty );
		}

		protected void Read( NetworkConnection connection, ref DataStreamReader reader )
		{
			DataStreamReader.Context context = default( DataStreamReader.Context );

			Read( connection, ref reader, ref context );
		}

		protected virtual ReadResult InternalRead( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context ) => ReadResult.Skipped;

		protected void Read( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			DataStreamReader.Context beginningContext = ReaderContextFactory.Clone( context );

			ushort packetId = reader.ReadUShort( ref context );
			
			DataStreamReader.Context internalReadContext = ReaderContextFactory.Clone( beginningContext );
			ReadResult internalReadResult = InternalRead( connection, ref reader, ref internalReadContext );
			if( internalReadResult == ReadResult.Consumed || internalReadResult == ReadResult.Error )
			{
				context = internalReadContext;

				if( debug )
					Debug.LogFormat( "{0}.InternalRead( {1} ).", networkScopeLabel, Packet.LookupType( packetId ).FullName );

				return;
			}
			
			if( packetId == Packet.Hash( typeof( Packets.PacketBundle ) ) )
			{
				int count = reader.ReadInt( ref context );

				if( debug )
					Debug.LogFormat( "{0}.ReadPacketBundle() with {1} packets.", networkScopeLabel, count );

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
					Debug.LogFormat( "{0}.Read( {1} ) with {2} listeners.", networkScopeLabel, Packet.LookupType( packetId ).FullName, listenerCount );

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
										Debug.LogFormat( "{0}.Read( {1} ) processed by listener {2}.", networkScopeLabel, Packet.LookupType( packetId ).FullName, i );
									processedContext = iterationContext;
									processed = true;
									break;
								case ReadResult.Consumed:
									if( debug )
										Debug.LogFormat( "{0}.Read( {1} ) consumed by listener {2}.", networkScopeLabel, Packet.LookupType( packetId ).FullName, i );
									context = iterationContext;
									return;
								case ReadResult.Error:
									Debug.LogErrorFormat( "{0}.Read( {1} ) encountered an error on listener {2}.{3}", networkScopeLabel, Packet.LookupType( packetId ).FullName, i, readHandlerErrorMessage != null ? string.Format( "\nError Message: {0}", readHandlerErrorMessage ) : string.Empty );
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
						Debug.LogWarningFormat( "{0}.Read( {1} ) [ {2} ] skipped; no associated listeners.", networkScopeLabel, Packet.LookupType( packetId )?.FullName, packetId );
					else
						Debug.LogWarningFormat( "{0}.Read( {1} ) failed to be consumed by one of the {2} listeners.", networkScopeLabel, Packet.LookupType( packetId ).FullName, listenerCount );
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
					Debug.LogErrorFormat( "{0}.Read( {1} ) failed to create a fall-through listener!", networkScopeLabel, packetType );
				}
			}
		}

		public string readHandlerErrorMessage = null;

		#endregion

		////////////////////////////////
		//	Processing

		#region Processing

		protected virtual void NetworkUpdate() { }

		protected virtual void QueryForEvents() { }
		protected virtual void SendQueue() { }

		#endregion

		////////////////////////////////
		//	Listeners

		#region Listeners

		public enum ReadResult : byte
		{
			Skipped,    // Not utilized by this listener
			Processed,  // Used by this listener, but non exclusively.
			Consumed,   // Used by this listener, and stops all further listener checks.
			Error       // A problem was encountered, store information into readHandlerErrorMessage
		}

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

		#endregion

		////////////////////////////////
		//	Monitors

		#region Monitors

		public delegate void SendMonitor( Packet packet, bool reliable );

		Dictionary<ushort, Dictionary<int, SendMonitor>> packetMonitors = new Dictionary<ushort, Dictionary<int, SendMonitor>>();
		
		public void RegisterMonitor( System.Type packetType, SendMonitor handler, int priority = 0 )
		{
			RegisterMonitor( Packet.Hash( packetType ), handler, priority );
		}

		public void RegisterMonitor( ushort packetId, SendMonitor handler, int priority = 0 )
		{
			if( !packetMonitors.ContainsKey( packetId ) )
			{
				packetMonitors.Add( packetId, new Dictionary<int, SendMonitor>() );
			}

			Dictionary<int, SendMonitor> listeners = packetMonitors[ packetId ];

			while( listeners.ContainsKey( priority ) )
				priority++;

			packetMonitors[ packetId ].Add( priority, handler );
		}

		public void DropMonitor( System.Type packetType, SendMonitor handler )
		{
			DropMonitor( Packet.Hash( packetType ), handler );
		}

		public void DropMonitor( ushort packetId, SendMonitor handler )
		{
			if( packetMonitors.ContainsKey( packetId ) && packetMonitors[ packetId ].ContainsValue( handler ) )
			{
				Dictionary<int, SendMonitor> listeners = packetMonitors[ packetId ];
				List<int> keys = new List<int>( listeners.Keys );
				for( int i = 0; i < keys.Count; i++ )
				{
					int k = keys[ i ];
					if( listeners[ k ] == handler )
					{
						packetMonitors[ packetId ].Remove( k );
						break;
					}
				}

				if( packetMonitors[ packetId ].Count == 0 )
					packetMonitors.Remove( packetId );
			}
		}

		#endregion
		
		////////////////////////////////
		//	Properties

		#region Properties

		public virtual string networkScopeLabel { get { return "Shared"; } }

		public bool open { get; private set; }

		#endregion

		////////////////////////////////
		//	Members

		#region Members

		public Pipeline pipeline;

		public UdpNetworkDriver driver;

		#endregion

		////////////////////////////////
		//	Attributes

		#region Attributes

		public const string LoopbackAddress = "127.0.0.1";

		public string address = LoopbackAddress;
		public ushort port = 16448;
		
		public bool openOnEnable = false;
		public bool debug = false;
		
		public NetworkParamters networkParameters = new NetworkParamters();

		#endregion

		////////////////////////////////
		//	Mono Methods

		#region MonoMethods

		protected virtual void Awake() { }

		protected virtual void OnEnable() { }

		protected virtual void OnDisable() { }

		protected virtual void OnDestroy() { }

		protected virtual void Update()
		{
			if( open )
			{
				NetworkUpdate();
			}
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( Shared ) )]
	public class EditorShared : EditorUtils.InheritedEditor
	{
		Shared shared;

		ReorderableList packetDatas;

		public override void Setup()
		{
			shared = ( Shared ) target;

			base.Setup();

			packetDatas = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "packetDatas" ),
				( SerializedProperty element ) =>
				{
					return lineHeight * 1.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					EditorUtils.BetterObjectField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), new GUIContent(), element, typeof( DesignData.PacketData ) );
				}
			);
		}

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 1.5f;

			if( packetDatas.GetExpanded() )
				inspectorHeight += packetDatas.GetHeight();
			else
				inspectorHeight += lineHeight * 1.5f;

			inspectorHeight += lineHeight * 3.5f;

			inspectorHeight += EditorGUI.GetPropertyHeight( this[ "networkParameters" ], true ) + lineHeight * 0.5f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Networking - Shared" ) );
			bRect.y += lineHeight * 1.5f;

			if( packetDatas.GetExpanded() )
			{
				bRect.height = packetDatas.GetHeight();
				packetDatas.DoList( bRect );
			}
			EditorUtils.DrawListHeader( new Rect( bRect.x, bRect.y, bRect.width, lineHeight ), packetDatas.serializedProperty );
			bRect.y += bRect.height + lineHeight * 0.5f;
			bRect.height = lineHeight;

			cRect = new Rect( bRect.x, bRect.y, bRect.width - 80f, lineHeight );
			EditorGUI.PropertyField( cRect, this[ "address" ], new GUIContent( "Address & Port" ) );
			cRect = new Rect( bRect.x + bRect.width - 75f, bRect.y, 75f, lineHeight );
			EditorGUI.PropertyField( cRect, this[ "port" ], new GUIContent( string.Empty, "Port" ) );
			bRect.y += lineHeight * 1.5f;

			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Auto-Open on Enable", string.Format( "The {0} will immediately attempt to connect on component enable.", shared.networkScopeLabel.ToLower() ) ), this[ "openOnEnable" ] );
			cRect.x += cRect.width + 10f;
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Debug" ), this[ "debug" ] );
			bRect.y += lineHeight * 2f;

			bRect.height = EditorGUI.GetPropertyHeight( this[ "networkParameters" ], true );
			EditorGUI.PropertyField( bRect, this[ "networkParameters" ], true );
			bRect.y += bRect.height + lineHeight * 0.5f;
			bRect.height = lineHeight;

			rect.y = bRect.y;
		}

		public override float inspectorPostInspectorOffset => 0f;

		public override float GetPostInspectorHeight()
		{
			return base.GetPostInspectorHeight() + lineHeight * 3.5f;
		}

		public override void DrawPostInspector( ref Rect rect )
		{
			base.DrawPostInspector( ref rect );

			Rect cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Network - State" ) );
			bRect.y += lineHeight * 1.5f;
			
			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );
			EditorGUI.BeginDisabledGroup( true );
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Is Open" ), shared.open );
			EditorGUI.EndDisabledGroup();
			cRect.x += cRect.width + 10f;
			EditorGUI.BeginDisabledGroup( !Application.isPlaying );
			if( EditorUtils.BetterButton( cRect, new GUIContent( !Application.isPlaying ? "Unavailable In Edit Mode" : shared.open ? "Close" : "Open" ) ) )
			{
				if( shared.open )
					shared.Close();
				else
					shared.Open();
			}
			EditorGUI.EndDisabledGroup();
			bRect.y += lineHeight * 2f;
			rect.y = bRect.y;
		}
	}
#endif
}
