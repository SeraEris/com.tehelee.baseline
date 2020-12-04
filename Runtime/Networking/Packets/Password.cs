using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class Password : Packet
	{
		////////////////////////////////
		
		public override int bytes
		{
			get
			{
				int _bytes = 2; // networkId

				_bytes += GetSafeStringBytes( password );

				return _bytes;
			}
		}

		////////////////////////////////

		public ushort networkId;
		public string password;

		////////////////////////////////

		public Password() : base() { }

		public Password( ref PacketReader reader ) : base( ref reader )
		{
			networkId = reader.ReadUShort();

			password = ReadSafeString( ref reader );
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteUShort( networkId );

			WriteSafeString( ref writer, password );
		}

		////////////////////////////////
	}
}