using System.Collections.Generic;
using UnityEngine;

using Type = System.Type;

namespace Tehelee.Baseline.DesignData
{
	public static class Lookup
	{
		private class Manifest
		{
			private List<Data> loaded = new List<Data>();
			
			private Dictionary<Type, HashSet<int>> typeHashes = new Dictionary<Type, HashSet<int>>();
			private Dictionary<int, int> hashToIndex = new Dictionary<int, int>();
			private Dictionary<string, int> pathToIndex = new Dictionary<string, int>();

			public void Import( Data data, int depth = 0 )
			{
				if( !Utils.IsObjectAlive( data ) )
					return;

				Type type = data.GetType();
				int hash = data.GetDataHash();
				string path = data.GetFullPath();

				int index = -1;

				if( pathToIndex.ContainsKey( path ) )
					index = pathToIndex[ path ];

				if( index < 0 )
				{
					loaded.Add( data );
					index = loaded.Count - 1;
				}
				else
				{
					loaded[ index ] = data;
				}
				
				pathToIndex.InsertOrReplace( path, index );
				hashToIndex.InsertOrReplace( hash, index );
				
				ImportByType( type, index );
				
				if( data is Collection collection )
					foreach( Data _data in collection.datas )
						Import( _data, depth + 1 );
			}

			private void ImportByType( Type type, int index )
			{
				if( object.Equals( null, type ) )
					return;
				
				HashSet<int> hashes = typeHashes.ContainsKey( type ) ? typeHashes[ type ] ?? new HashSet<int>() : new HashSet<int>();
				hashes.Add( index );
				
				typeHashes.InsertOrReplace( type, hashes );

				if( type != typeof( Data ) )
					ImportByType( type.BaseType, index );
			}

			public T Find<T>( string path ) where T : Data
			{
				if( !pathToIndex.ContainsKey( path ) )
					return null;
				
				int index = pathToIndex[ path ];
				if( index < 0 || index >= loaded.Count )
				{
					Debug.LogError
					(
						$"DesignData.Lookup path points to invalid index.\n" +
						$"Path: {path}\n" +
						$"Index: {index}"
					);
					return null;
				}

				Data data = loaded[ index ];

				switch( data )
				{
					case null:
						return null;
					case T castData:
						return castData;
					default:
						Debug.LogError
						(
							$"DesignData.Lookup cannot cast {data.GetType()} as {typeof(T)}\n" +
							$"Hash: {data.GetDataHash()}\n" +
							$"Path: {data.GetFullPath()}"
						);

						return null;
				}
			}
			
			public T Find<T>( int hash ) where T : Data
			{
				if( !hashToIndex.ContainsKey( hash ) )
					return null;
				
				int index = hashToIndex[ hash ];
				if( index < 0 || index >= loaded.Count )
				{
					Debug.LogError
					(
						$"DesignData.Lookup hash points to invalid index.\n" +
						$"Hash: {hash}\n" +
						$"Index: {index}"
					);
					return null;
				}

				Data data = loaded[ index ];

				switch( data )
				{
					case null:
						return null;
					case T castData:
						return castData;
					default:
						Debug.LogError
						(
							$"DesignData.Lookup cannot cast {data.GetType()} as {typeof(T)}\n" +
							$"Hash: {data.GetDataHash()}\n" +
							$"Path: {data.GetFullPath()}"
						);

						return null;
				}
			}

			public List<T> GetList<T>() where T : Data
			{
				Type type = typeof( T );
				List<T> list = new List<T>();

				if( typeHashes.ContainsKey( type ) )
				{
					foreach( int index in typeHashes[ type ] )
					{
						if( index >= 0 && index < loaded.Count )
						{
							Data data = loaded[ index ];
							if( data is T castData )
								list.Add( castData  );
						}
					}
				}

				return list;
			}
		}

		private static Manifest manifest = null;
		
		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.AfterAssembliesLoaded )]
		private static void Init()
		{
			manifest = new Manifest();
			
			Collection[] collections = Resources.LoadAll<Collection>( string.Empty );
			foreach( Collection collection in collections )
				manifest.Import( collection );
		}

		public static T Find<T>( string path ) where T : Data
		{
			if( object.Equals( null, manifest ) )
			{
				Debug.LogError( "DesignData.Lookup invoked with NULL manifest." );
				return null;
			}

			return manifest.Find<T>( path );
		}

		public static T Find<T>( int hash ) where T : Data
		{
			if( object.Equals( null, manifest ) )
			{
				Debug.LogError( "DesignData.Lookup invoked with NULL manifest." );
				return null;
			}

			return manifest.Find<T>( hash );
		}

		public static List<T> GetList<T>() where T : Data
		{
			if( object.Equals( null, manifest ) )
			{
				Debug.LogError( "DesignData.Lookup invoked with NULL manifest." );
				return new List<T>();
			}

			return manifest.GetList<T>();
		}
	}
}