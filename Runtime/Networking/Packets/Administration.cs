using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class Administration : Packet
	{
		////////////////////////////////
		
		private const int bytesOverhead = 3; // operation (1), networkId (2)
		
		public override int bytes { get { return bytesOverhead + GetSafeStringBytes( text, characterLimit ); } }

		////////////////////////////////

		public enum Operation : byte
		{
			Authorize	= 0,
			Shutdown	= 1,
			Alert		= 2,
			Promote		= 3,
			Demote		= 4,
			Rename		= 5,
			Kick		= 6,
			Ban			= 7
		}

		public Operation operation;

		public ushort networkId;

		public string text;

		////////////////////////////////

		private static readonly int characterLimit = Mathf.FloorToInt( 0.5f * ( Packet.maxBytes - ( bytesOverhead + bytesSafeString ) ) );
		
		////////////////////////////////

		public Administration() : base() { }

		public Administration( ref PacketReader reader ) : base( ref reader )
		{
			operation = ( Operation ) reader.ReadByte();

			networkId = reader.ReadUShort();

			text = ReadSafeString( ref reader );
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteByte( ( byte ) operation );

			writer.WriteUShort( networkId );

			WriteSafeString( ref writer, text, characterLimit );
		}

		////////////////////////////////
	}
}