using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class Loopback : Packet
	{
		////////////////////////////////
		
		public override int bytes { get { return 6; } }

		////////////////////////////////
		
		public float originTime;

		public ushort averagePingMS;

		////////////////////////////////

		public Loopback() : base() { }

		public Loopback( ref PacketReader reader ) : base( ref reader )
		{
			originTime = reader.ReadFloat();

			averagePingMS = reader.ReadUShort();
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteFloat( originTime );

			writer.WriteUShort( averagePingMS );
		}

		////////////////////////////////
	}
}