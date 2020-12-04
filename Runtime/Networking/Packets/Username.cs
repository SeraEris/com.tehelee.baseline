using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class Username : Packet
	{
		////////////////////////////////
		
		public override int bytes
		{
			get
			{
				int _bytes = 2; // networkId

				_bytes += GetSafeStringBytes( name );

				return _bytes;
			}
		}

		////////////////////////////////

		public ushort networkId;
		public string name;

		////////////////////////////////

		public Username() : base() { }

		public Username( ref PacketReader reader ) : base( ref reader )
		{
			networkId = reader.ReadUShort();

			name = ReadSafeString( ref reader );
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteUShort( networkId );

			WriteSafeString( ref writer, name );
		}

		////////////////////////////////
	}
}