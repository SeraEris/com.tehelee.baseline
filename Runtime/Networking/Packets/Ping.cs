using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class Ping : Packet
	{
		////////////////////////////////

		public override int bytes => 2 + ( 4 * timingsByNetworkId.Count ); // networkId, ping

		////////////////////////////////

		public Dictionary<ushort, ushort> timingsByNetworkId = new Dictionary<ushort, ushort>();

		////////////////////////////////

		public Ping() : base() { }

		public Ping( ref PacketReader reader ) : base( ref reader )
		{
			int count = reader.ReadUShort();
			for( int i = 0; i < count; i++ )
			{
				ushort networkId = reader.ReadUShort();
				ushort ping = reader.ReadUShort();

				timingsByNetworkId.Add( networkId, ping );
			}
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteUShort( ( ushort ) timingsByNetworkId.Count );
			
			foreach( KeyValuePair<ushort,ushort> kvp in timingsByNetworkId )
			{
				writer.WriteUShort( kvp.Key ); // networkId
				writer.WriteUShort( kvp.Value ); // ping
			}
		}

		////////////////////////////////
	}
}