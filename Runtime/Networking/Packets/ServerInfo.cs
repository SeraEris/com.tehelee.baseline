using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking.Packets
{
	public class ServerInfo : Packet
	{
		////////////////////////////////

		public override int bytes => 3 + GetSafeStringBytes( name, 100 ) + GetSafeStringBytes( tags, 200 ) + GetSafeStringBytes( description, 300 );

		////////////////////////////////
		
		public ushort maxPlayers;

		public bool isPrivate;

		public string name;
		public string tags;

		public string description;

		////////////////////////////////

		public HostInfo GetHostInfo()
		{
			HostInfo hostInfo = new HostInfo();

			hostInfo.maxPlayers = maxPlayers;
			hostInfo.name = name;
			hostInfo.tagsFlat = tags;
			hostInfo.description = description;

			if( isPrivate )
				hostInfo.password = "*";

			return hostInfo;
		}

		////////////////////////////////

		public ServerInfo() : base() { }

		public ServerInfo( HostInfo hostInfo ) : base()
		{
			maxPlayers = ( ushort ) Mathf.Clamp( hostInfo.maxPlayers, 1, ushort.MaxValue );

			isPrivate = !string.IsNullOrWhiteSpace( hostInfo.password );

			name = hostInfo.name;
			tags = hostInfo.tagsFlat;
			description = hostInfo.description;
		}

		public ServerInfo( ref PacketReader reader ) : base( ref reader )
		{
			maxPlayers = reader.ReadUShort();

			isPrivate = reader.ReadByte() != 0;

			name = ReadSafeString( ref reader );
			tags = ReadSafeString( ref reader );
			description = ReadSafeString( ref reader );
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.WriteUShort( maxPlayers );

			writer.WriteByte( ( byte ) ( isPrivate ? 1 : 0 ) );

			WriteSafeString( ref writer, name, 100 );
			WriteSafeString( ref writer, tags, 200 );
			WriteSafeString( ref writer, description, 300 );
		}

		////////////////////////////////
	}
}