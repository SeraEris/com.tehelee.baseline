using Unity.Networking.Transport;

namespace Tehelee.Baseline.Networking
{
	////////////////////////////////////////////////
	//	DataStreamReader.Context Clone Factory
	//		Due to changes made in Unity Transport, the DataStreamReader.Context can no longer have it's read byte offset externally.
	//		As this is a requirement for existing packet infastructure, this factory uses cached reflection to facilitate the functionality.

	public static class ReaderContextFactory
	{
		////////////////////////////////
		//	Static

		#region Static
		
		static System.Reflection.FieldInfo m_ReadByteIndex;
		static System.Reflection.FieldInfo m_BitIndex;
		static System.Reflection.FieldInfo m_BitBuffer;

		static ReaderContextFactory()
		{
			System.Type contextType = typeof( DataStreamReader.Context );

			m_ReadByteIndex = contextType.GetField( "m_ReadByteIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
			m_BitIndex = contextType.GetField( "m_BitIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
			m_BitBuffer = contextType.GetField( "m_BitBuffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
		}

		#endregion

		////////////////////////////////
		//	Clone Context

		#region CloneContext

		public static DataStreamReader.Context Clone( DataStreamReader.Context context )
		{
			// Due to boxing, this must be defined as an object, then cast to type on return
			object _context = new DataStreamReader.Context();

			m_ReadByteIndex.SetValue( _context, m_ReadByteIndex.GetValue( context ) );
			m_BitIndex.SetValue( _context, m_BitIndex.GetValue( context ) );
			m_BitBuffer.SetValue( _context, m_BitBuffer.GetValue( context ) );

			return ( DataStreamReader.Context ) _context;
		}

		#endregion

		////////////////////////////////
		//	Extension Properties
		//		Implemented for exposure, not recommended usage

		#region ExtensionProperties

		public static int GetReadByteIndex( this DataStreamReader.Context context ) => ( int ) m_ReadByteIndex.GetValue( context );
		public static void SetReadByteIndex( this DataStreamReader.Context context, int readByteIndex ) => m_ReadByteIndex.SetValue( context, readByteIndex );

		public static int GetBitIndex( this DataStreamReader.Context context ) => ( int ) m_BitIndex.GetValue( context );
		public static void SetBitIndex( this DataStreamReader.Context context, int bitIndex ) => m_BitIndex.SetValue( context, bitIndex );

		public static int GetBitBuffer( this DataStreamReader.Context context ) => ( int ) m_BitBuffer.GetValue( context );
		public static void SetBitBuffer( this DataStreamReader.Context context, int bitBuffer ) => m_BitBuffer.SetValue( context, bitBuffer );

		#endregion

		////////////////////////////////
	}
}