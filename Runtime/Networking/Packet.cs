using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace Tehelee.Baseline.Networking
{
	[System.Serializable]
	public abstract class Packet
	{
		////////////////////////////////
		//	Packet
		//
		//	Override:
		//		bytes { get }
		//			Used to marshal packets from server to client; this should match what you're writing.
		//		Write( ref DataStreamWriter writer );
		//			Used to write data values to the write stream to be sent to targets.
		//		<Packet>( ref DataStreamReader reader, ref DataStreamReader.Context context );
		//			Used to re-construct this packet from the read stream on targets.

		#region Packet

		public ushort id { get { return Hash( this.GetType() ); } }

		public virtual int bytes { get { return -1; } }

		public List<NetworkConnection> targets = new List<NetworkConnection>();

		public virtual void Write( ref DataStreamWriter writer ) { }

		public Packet() { }

		public Packet( ref DataStreamReader reader, ref DataStreamReader.Context context ) { }

		#endregion

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
	}
}