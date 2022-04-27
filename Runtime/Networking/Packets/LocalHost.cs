using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class LocalHost : Packet
	{
		////////////////////////////////

		public override int bytes => 5; // authKey(4) + command(1)

		////////////////////////////////

		public enum Command : byte
		{
			Shutdown		= 0,
			Promote			= 1
		}

		////////////////////////////////

		public int authKey;
		
		public Command command;
		
		////////////////////////////////

		public LocalHost() : base() { }

		public LocalHost( int authKey, Command command, ushort networkId ) : this()
		{
			this.authKey = authKey;
			this.command = command;
		}

		public LocalHost( ref PacketReader reader ) : base( ref reader )
		{
			authKey = reader.ReadInt();
			command = ( Command ) reader.ReadByte();
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteInt( authKey );
			writer.WriteByte( ( byte ) command );
		}

		////////////////////////////////
	}
}