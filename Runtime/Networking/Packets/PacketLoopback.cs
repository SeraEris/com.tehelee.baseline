using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class PacketLoopback : Packet
	{
		////////////////////////////////
		
		public override int bytes { get { return 4; } }

		////////////////////////////////

		public float originTime;

		////////////////////////////////

		public PacketLoopback() : base() { }

		public PacketLoopback( ref DataStreamReader reader, ref DataStreamReader.Context context ) : base( ref reader, ref context )
		{
			originTime = reader.ReadFloat( ref context );
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.Write( originTime );
		}

		////////////////////////////////
	}
}