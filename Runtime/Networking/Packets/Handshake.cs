using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class Handshake : Packet
	{
		////////////////////////////////
		
		public override int bytes { get { return 3; } }

		////////////////////////////////

		public enum Operation : byte
		{
			AssignSelf		= 0,
			CreateOther		= 1,
			DestroyOther	= 2
		}

		////////////////////////////////

		public ushort networkId;

		public Operation operation;

		////////////////////////////////

		public Handshake() : base() { }

		public Handshake( ref PacketReader reader ) : base( ref reader )
		{
			networkId = reader.ReadUShort();
			operation = ( Operation ) reader.ReadByte();
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteUShort( networkId );
			writer.WriteByte( ( byte ) operation );
		}

		////////////////////////////////
	}
}