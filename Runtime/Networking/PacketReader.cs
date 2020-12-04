using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Tehelee.Baseline.Networking
{
	// *Shakes fist at DataStreamReader changes*
	//
	// This class is a necessary buffer on the Unity.Networking.Transport.DataStreamReader
	// It provides a mutable read index versus the now private read index of the DataStreamReader.Context

	unsafe public class PacketReader
	{
		private NativeArray<byte> data = new NativeArray<byte>();
		public int readIndex = 0;

		public int length { get; private set; }

		byte* dataPtr;

		public PacketReader() { }

		public PacketReader( NativeArray<byte> data, ushort readIndex = 0 )
		{
			this.data = data;
			this.readIndex = readIndex;

			this.length = data.Length;
			this.dataPtr = ( byte* ) data.GetUnsafeReadOnlyPtr();
		}

		public void ReadBytes( byte* data, int length )
		{
			if( readIndex + length > this.length )
			{
                UnsafeUtility.MemClear(data, length);
                return;
			}
			UnsafeUtility.MemCpy( data, dataPtr + readIndex, length );
			readIndex += length;
		}
		
		public void ReadBytes( NativeArray<byte> array )
		{
			ReadBytes( ( byte* ) array.GetUnsafePtr(), array.Length );
		}

		public byte ReadByte()
		{
			byte data;
			ReadBytes( ( byte* ) &data, sizeof( byte ) );
			return data;
		}

		public short ReadShort()
		{
			short data;
			ReadBytes( ( byte* ) &data, sizeof( short ) );
			return data;
		}

		public ushort ReadUShort()
		{
			ushort data;
			ReadBytes( ( byte* ) &data, sizeof( ushort ) );
			return data;
		}

		public int ReadInt()
		{
			int data;
			ReadBytes( ( byte* ) &data, sizeof( int ) );
			return data;
		}

		public uint ReadUInt()
		{
			uint data;
			ReadBytes( ( byte* ) &data, sizeof( uint ) );
			return data;
		}

		public ulong ReadULong()
		{
			ulong data;
			ReadBytes( ( byte* ) &data, sizeof( ulong ) );
			return data;
		}

		public float ReadFloat() => ReadInt();
	}
}