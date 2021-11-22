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
		//		<Packet>( ref PacketReader reader );
		//			Used to re-construct this packet from the read stream on targets.

		#region Packet

		public ushort id { get { return Hash( this.GetType() ); } }

		public virtual int bytes { get { return -1; } }

		public List<NetworkConnection> targets = new List<NetworkConnection>();

		public virtual void Write( ref DataStreamWriter writer ) { }

		public Packet() { }

		public Packet( ref PacketReader reader ) { }

		#endregion

		////////////////////////////////
		//	Byte Limits

		#region ByteLimits

		public static readonly ushort maxBytes = NetworkParameterConstants.MTU - 16;
		public const int bytesSafeString = 2;

		#endregion

		////////////////////////////////
		//	HashToType

		#region HashToType

		private static Dictionary<ushort, System.Type> HashToType = new Dictionary<ushort, System.Type>();
		private static Dictionary<System.Type, ushort> TypeToHash = new Dictionary<System.Type, ushort>();
		private static Dictionary<ushort, HashSet<Shared>> RegistrySources = new Dictionary<ushort, HashSet<Shared>>();

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

		public static void Register( Shared source, System.Type packetType )
		{
			ushort hash = Hash( packetType );
			if( !HashToType.ContainsKey( hash ) )
			{
				HashToType.Add( hash, packetType );
				RegistrySources.Add( hash, new HashSet<Shared>() { source } );
			}
			else
			{
				RegistrySources[ hash ].Add( source );
			}
		}

		public static void Unregister( Shared source, System.Type packetType )
		{
			ushort hash = ( TypeToHash.ContainsKey( packetType ) ) ? TypeToHash[ packetType ] : Utils.HashCRC( packetType.FullName );
			
			if( HashToType.ContainsKey( hash ) )
			{

				if( RegistrySources[ hash ].Contains( source ) )
					RegistrySources[ hash ].Remove( source );

				if( RegistrySources[ hash ].Count == 0 )
				{
					HashToType.Remove( hash );
					TypeToHash.Remove( packetType );
					RegistrySources.Remove( hash );
				}
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

		public static bool IsValid( ushort packetId ) => HashToType.ContainsKey( packetId );

		#endregion

		////////////////////////////////
		//	Transport Helpers

		#region TransportHelpers

		public static void WriteBool( ref DataStreamWriter writer, bool value ) =>
			writer.WriteByte( ( byte ) ( value ? 1 : 0 ) );

		public static bool ReadBool( ref PacketReader reader ) =>
			reader.ReadByte() != 0;

		private static bool ValidateFloat( float value ) =>
			!float.IsNaN( value ) && !float.IsInfinity( value );
		
		public static void WriteFloatSafe( ref DataStreamWriter writer, float value )
		{
			writer.WriteFloat( ValidateFloat( value ) ? value : 0f );
		}

		private static float[] precisionCompress = new[] { 10f, 100f, 1000f, 10000f };
		private static float[] precisionUncompress = new[] { 0.1f, 0.01f, 0.001f, 0.0001f };

		public static int GetCompressedFloatBytes() => 2;

		public static void WriteCompressedFloat( ref DataStreamWriter writer, float value, byte precision )
		{
			float _value = ( float.IsNaN( value ) || float.IsInfinity( value ) ) ? 0f : value;

			writer.WriteShort( ( short ) Mathf.RoundToInt( _value * precisionCompress[ precision ] ) );
		}

		public static float ReadCompressedFloat( ref PacketReader reader, byte precision )
		{
			return reader.ReadShort() * precisionUncompress[ precision ];
		}

		public static int GetSafeStringBytes( string str, int truncate = 0 )
		{
			int bytes = bytesSafeString;

			string _str = string.IsNullOrEmpty( str ) ? string.Empty : str;

			if( truncate > 0 )
				_str = _str.Substring( 0, Mathf.Min( _str.Length, truncate ) );

			if( !string.IsNullOrEmpty( _str ) )
				bytes += _str.Length * 2;

			return bytes;
		}

		public static void WriteSafeString( ref DataStreamWriter writer, string str, int truncate = 0 )
		{
			string _str = string.IsNullOrEmpty( str ) ? string.Empty : str;

			if( truncate > 0 )
				_str = _str.Substring( 0, Mathf.Min( _str.Length, truncate ) );

			int stringLength = Mathf.Min( ushort.MaxValue, _str.Length );
			writer.WriteUShort( ( ushort ) stringLength );

			for( int i = 0, iC = stringLength; i < iC; i++ )
				writer.WriteUShort( _str[ i ] );
		}

		public static string ReadSafeString( ref PacketReader reader )
		{
			char[] _str = new char[ reader.ReadUShort() ];

			for( int i = 0, iC = _str.Length; i < iC; i++ )
				_str[ i ] = ( char ) reader.ReadUShort();

			return new string( _str );
		}

		public static void WriteVector3( ref DataStreamWriter writer, Vector3 vector )
		{
			if( !ValidateFloat( vector.x ) || !ValidateFloat( vector.y ) || !ValidateFloat( vector.z ) )
				vector = Vector3.zero;
			
			WriteFloatSafe( ref writer, vector.x );
			WriteFloatSafe( ref writer, vector.y );
			WriteFloatSafe( ref writer, vector.z );
		}

		public static Vector3 ReadVector3( ref PacketReader reader ) =>
			new Vector3( reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat() );
		
		public static void WriteQuaternion( ref DataStreamWriter writer, Quaternion quaternion )
		{
			if( !ValidateFloat( quaternion.x ) || !ValidateFloat( quaternion.y ) || !ValidateFloat( quaternion.z ) || !ValidateFloat( quaternion.w ) )
				quaternion = Quaternion.identity;
			
			WriteFloatSafe( ref writer, quaternion.x );
			WriteFloatSafe( ref writer, quaternion.y );
			WriteFloatSafe( ref writer, quaternion.z );
			WriteFloatSafe( ref writer, quaternion.w );
		}

		public static Quaternion ReadQuaternion( ref PacketReader reader ) =>
			new Quaternion( reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat() );

		#endregion
	}
}