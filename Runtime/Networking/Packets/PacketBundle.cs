using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

using Tehelee.Baseline.Networking;

namespace Tehelee.Baseline.Networking.Packets
{
	public class PacketBundle : Packet
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

		public PacketBundle() : base() { }

		public PacketBundle( ref DataStreamReader reader, ref DataStreamReader.Context context ) : base( ref reader, ref context ) { }

		public override void Write( ref DataStreamWriter writer )
		{
			writer.Write( packets.Count );

			foreach( Packet packet in packets )
			{
				writer.Write( packet.id );

				packet.Write( ref writer );
			}
		}

		////////////////////////////////
	}
}