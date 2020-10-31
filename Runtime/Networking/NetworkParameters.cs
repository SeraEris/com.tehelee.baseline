using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

namespace Tehelee.Baseline
{
	[System.Serializable]
	public class NetworkParamters
	{
		////////////////////////////////
		//	Members

		#region Members

		public int connectTimeoutMS = 1000; // If no response recieved for this long, couldn't connect.
		public int maxConnectAttempts = 30; // Amount of times to retry the connection ( connection duration = connectTimeoutMS * maxConnectAttempts )
		public int disconnectTimeoutMS = 15000; // If no packets are recieved for this long, assume disconnect

		public int maxPacketSize = NetworkParameterConstants.MTU; // Limited to MTU
		public int maxPacketCount = 32; // Max in-flight packets ( up to 32 )

		public int packetDelayMS = 0; // Amount to delay packet sends by
		public int packetJitterMS = 0; // Added / Subtracted from packetDelayMS

		public int packetDropPercent = 0; // 0 - 100 chance to drop packets
		public int packetDropInterval = 0; // fixed drop rate, 5 = every 5th is dropped

		#endregion

		////////////////////////////////
		//	SimulatorUtility.Parameters

		#region SimulatorUtilityParameters

		public static implicit operator SimulatorUtility.Parameters( NetworkParamters networkParamters ) => new SimulatorUtility.Parameters()
		{
			MaxPacketCount = networkParamters.maxPacketCount,
			MaxPacketSize = networkParamters.maxPacketSize,
			PacketDelayMs = networkParamters.packetDelayMS,
			PacketJitterMs = networkParamters.packetJitterMS,
			PacketDropInterval = networkParamters.packetDropInterval,
			PacketDropPercentage = networkParamters.packetDropPercent
		};

		#endregion

		////////////////////////////////
		//	ToString()

		#region ToString

		public override string ToString() => string.Format
		(
			"NetworkParamters:\n  connectTimeoutMS: {0}\n  maxConnectAttempts: {1}\n  disconnectTimeoutMS: {2}\n  maxPacketSize: {3} / 1400 (MTU)\n  maxPacketCount: {4} / 32\n  packetDelayMS: {5} +- {6} (packetJitterMS)\n  packetDropPercent: {7}%\n  packetDropInterval: {8}",
			connectTimeoutMS, maxConnectAttempts, disconnectTimeoutMS,
			maxPacketSize, maxPacketCount,
			packetDelayMS, packetJitterMS,
			packetDropPercent, packetDropInterval
		);

		#endregion
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer( typeof( NetworkParamters ) )]
	public class NetworkParametersPropertyDrawer : EditorUtils.InheritedPropertyDrawer
	{
		public override LabelMode labelMode => LabelMode.Foldout;

		public override float offsetFoldoutGUI => 4f;

		public override float CalculatePropertyHeight( ref SerializedProperty property ) => base.CalculatePropertyHeight( ref property ) + lineHeight * 6f + 20f;

		public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
		{
			base.DrawGUI( ref rect, ref property );

			Rect cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			////////////////////////////////
			//	Serialized Properties

			SerializedProperty connectTimeoutMS = property.FindPropertyRelative( "connectTimeoutMS" );
			SerializedProperty maxConnectAttempts = property.FindPropertyRelative( "maxConnectAttempts" );
			SerializedProperty disconnectTimeoutMS = property.FindPropertyRelative( "disconnectTimeoutMS" );

			SerializedProperty maxPacketSize = property.FindPropertyRelative( "maxPacketSize" );
			SerializedProperty maxPacketCount = property.FindPropertyRelative( "maxPacketCount" );

			SerializedProperty packetDelayMS = property.FindPropertyRelative( "packetDelayMS" );
			SerializedProperty packetJitterMS = property.FindPropertyRelative( "packetJitterMS" );

			SerializedProperty packetDropPercent = property.FindPropertyRelative( "packetDropPercent" );
			SerializedProperty packetDropInterval = property.FindPropertyRelative( "packetDropInterval" );

			////////////////////////////////
			//	Connection

			cRect = new Rect( bRect.x, bRect.y, bRect.width - 100f, bRect.height );
			EditorGUI.PropertyField( cRect, connectTimeoutMS, new GUIContent( "Connection Timeout MS" ) );

			EditorGUIUtility.labelWidth = 15f;
			cRect = new Rect( bRect.x + bRect.width - 90f, bRect.y, 90f, bRect.height );
			EditorGUI.PropertyField( cRect, maxConnectAttempts, new GUIContent( "x", "Max Connection Attempts" ) );
			EditorGUIUtility.labelWidth = labelWidth;
			bRect.y += lineHeight + 4f;

			EditorGUI.PropertyField( bRect, disconnectTimeoutMS, new GUIContent( "Disconnect Timeout MS" ) );
			bRect.y += lineHeight + 4f;

			////////////////////////////////
			//	Packet Size & Count

			cRect = new Rect( bRect.x, bRect.y, bRect.width - 100f, bRect.height );
			EditorGUI.IntSlider( cRect, maxPacketSize, 1, NetworkParameterConstants.MTU, new GUIContent( "Max Packet Size", "Measured in bytes" ) );
			EditorGUIUtility.labelWidth = 15f;
			cRect = new Rect( bRect.x + bRect.width - 90f, bRect.y, 90f, bRect.height );
			EditorGUI.PropertyField( cRect, maxPacketCount, new GUIContent( "x", "Max Packet Count" ) );
			maxPacketCount.Clamp( 1, 32 );
			EditorGUIUtility.labelWidth = labelWidth;
			bRect.y += lineHeight + 4f;

			////////////////////////////////
			//	Packet Delay & Jitter

			cRect = new Rect( bRect.x, bRect.y, bRect.width - 100f, bRect.height );
			EditorGUI.PropertyField( cRect, packetDelayMS, new GUIContent( "Packet Delay MS" ) );
			packetDelayMS.Min( 0 );
			EditorGUIUtility.labelWidth = 20f;
			cRect = new Rect( bRect.x + bRect.width - 95f, bRect.y, 95f, bRect.height );
			EditorGUI.PropertyField( cRect, packetJitterMS, new GUIContent( "+-", "Packet Jitter MS" ) );
			packetJitterMS.Min( 0 );
			EditorGUIUtility.labelWidth = labelWidth;
			bRect.y += lineHeight + 4f;

			////////////////////////////////
			//	Packet Drop & Interval

			EditorGUI.IntSlider( bRect, packetDropPercent, 0, 100, new GUIContent( "Packet Drop Percentage" ) );
			bRect.y += lineHeight + 4f;

			EditorGUI.PropertyField( bRect, packetDropInterval, new GUIContent( "Packet Drop Interval" ) );
			packetDropInterval.Min( 0 );
			bRect.y += lineHeight;

			rect.y = bRect.y;
		}
	}
#endif
}