using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
    public class MultiMessage : Packet
	{
		////////////////////////////////
		
		private const int bytesOverhead = 22; // networkId (2), postTime (8), editTime (8), currentParts (2), totalParts (2)
		
		public override int bytes
		{
			get
			{
				int b = bytesOverhead;

				b += GetSafeStringBytes( message );

				return b;
			}
		}

		////////////////////////////////

		public ushort networkId;

		public DateTime postTime;
		public DateTime editTime;

		public ushort currentPart;
		public ushort totalParts;

		public string message;

		////////////////////////////////
		
		private static readonly int characterLimit = Mathf.FloorToInt( 0.5f * ( Packet.maxBytes - ( bytesOverhead + bytesSafeString ) ) );

		public static List<MultiMessage> ConstructMultiMessages( ushort networkId, DateTime postTime, DateTime editTime, string message )
		{
			if( object.Equals( null, message ) )
				message = string.Empty;

			List<MultiMessage> multiMessages = new List<MultiMessage>();

			int characterTotal = message.Length;
			
			ushort totalParts = ( ushort ) Mathf.Max( 1, Mathf.CeilToInt( characterTotal / ( float ) characterLimit ) );

			for( ushort i = 0; i < totalParts; i++ )
			{
				MultiMessage multiMessage = new MultiMessage();
				multiMessage.networkId = networkId;
				multiMessage.postTime = postTime;
				multiMessage.editTime = editTime;
				multiMessage.currentPart = i;
				multiMessage.totalParts = totalParts;
				int messageOffset = i * characterLimit;
				multiMessage.message = message.Substring( messageOffset, Mathf.Min( characterLimit, ( characterTotal - messageOffset ) ) );

				multiMessages.Add( multiMessage );
			}

			return multiMessages;
		}

		////////////////////////////////

		public MultiMessage() : base() { }

		public MultiMessage( ref PacketReader reader ) : base( ref reader )
		{
			networkId = reader.ReadUShort();

			postTime = new DateTime( ( long ) reader.ReadULong() );
			editTime = new DateTime( ( long ) reader.ReadULong() );

			currentPart = reader.ReadUShort();
			totalParts = reader.ReadUShort();

			message = ReadSafeString( ref reader );
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteUShort( networkId );

			writer.WriteULong( ( ulong ) postTime.Ticks );
			writer.WriteULong( ( ulong ) editTime.Ticks );
			
			writer.WriteUShort( currentPart );
			writer.WriteUShort( totalParts );

			WriteSafeString( ref writer, message );
		}

		////////////////////////////////
	}
}
