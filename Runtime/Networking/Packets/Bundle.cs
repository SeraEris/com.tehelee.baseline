using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

using Tehelee.Baseline.Networking;

namespace Tehelee.Baseline.Networking.Packets
{
	public class Bundle : Packet
	{
		////////////////////////////////
		
		public override int bytes
		{
			get
			{
				int bytes = 4;

				foreach( Packet packet in packets )
					bytes += ( packet.bytes + 2 );

				return bytes;
			}
		}

		////////////////////////////////

		public List<Packet> packets = new List<Packet>();

		////////////////////////////////

		public Bundle() : base() { }

		public Bundle( ref PacketReader reader ) : base( ref reader ) { }

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteUShort( ( ushort ) packets.Count );

			foreach( Packet packet in packets )
			{
				writer.WriteUShort( packet.id );

				packet.Write( ref writer );
			}
		}

		////////////////////////////////
	}
}