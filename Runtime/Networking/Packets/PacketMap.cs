using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

using Tehelee.Baseline.Networking;

namespace Tehelee.Baseline.Networking.Packets
{
	public class PacketMap : Packet
	{
		////////////////////////////////
		
		public override int bytes { get { return map.Length * 2 + 2; } }

		////////////////////////////////

		public ushort[] map;

		////////////////////////////////

		public PacketMap() : base() { }

		public PacketMap( ref DataStreamReader reader, ref DataStreamReader.Context context ) : base( ref reader, ref context )
		{
			map = new ushort[ reader.ReadUShort( ref context ) ];

			for( int i = 0, iC = map.Length; i < iC; i++ )
			{
				map[ i ] = reader.ReadUShort( ref context );
			}
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.Write( ( ushort ) map.Length );

			for( int i = 0, iC = map.Length; i < iC; i++ )
			{
				writer.Write( map[ i ] );
			}
		}

		////////////////////////////////
	}
}