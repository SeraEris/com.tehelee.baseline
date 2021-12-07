using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace Tehelee.Baseline
{
	public class PlayerConfig
	{
		////////////////////////////////
		#region Static

		public delegate void ConfigCallback( PlayerConfig config );
		
		private static PlayerConfig mainConfig = null;
		private static ConfigCallback mainConfigCallbacks;

		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.BeforeSceneLoad )]
		private static void LoadMainConfig()
		{
			if( !object.Equals( null, mainConfig ) )
				return;

			LoadConfig( "Config", ( PlayerConfig config ) =>
			{
				mainConfig = config;

				mainConfigCallbacks?.Invoke( mainConfig );
				mainConfigCallbacks = null;
			} );
		}

		public static void GetMainConfig( ConfigCallback callback )
		{
			if( object.Equals( null, mainConfig ) )
				mainConfigCallbacks += callback;
			else
				callback?.Invoke( mainConfig );
		}

		private static string GetFilePath( string configName ) =>
			string.Format( "{0}.cfg", Path.Combine( Application.persistentDataPath, Path.GetFileNameWithoutExtension( configName ) ) );

		public static void LoadConfig( string configName, ConfigCallback callback )
		{
			string filePath = GetFilePath( configName );

			Utils.WaitForTask( new Task<PlayerConfig>( () =>
			{
				PlayerConfig playerConfig = new PlayerConfig( configName );
				
				if( File.Exists( filePath ) )
				{
					using( StreamReader streamReader = new StreamReader( filePath ) )
					{
						JsonTextReader jsonReader = new JsonTextReader( streamReader );

						JObject jObj = null;

						try
						{
							jObj = ( JObject ) JToken.ReadFrom( jsonReader );
							playerConfig.Load( ref jObj );
						}
						catch { }
					}
				}

				return playerConfig;
			} ),
			( PlayerConfig config ) =>
			{
				callback?.Invoke( config );
			} );
		}

		public static void SaveConfig( PlayerConfig playerConfig )
		{
			if( object.Equals( null, playerConfig ) || string.IsNullOrWhiteSpace( playerConfig.name ) )
				return;

			playerConfig = new PlayerConfig( playerConfig );

			string filePath = GetFilePath( playerConfig.name );

			Utils.WaitForTask( new Task<string>( () =>
			{
				string failureMessage = string.Format( "Could not save PlayerConfig ( '{0}' ) at:\n  {1}", playerConfig.name, filePath );

				if( File.Exists( filePath ) )
				{
					try
					{
						File.Delete( filePath );
					}
					catch { }
				}

				if( File.Exists( filePath ) )
				{
					return string.Format( "{0}\n  File could not be overwritten.", failureMessage );
				}

				string directoryPath = Path.GetDirectoryName( filePath );

				if( !Directory.Exists( directoryPath ) )
				{
					try
					{
						Directory.CreateDirectory( directoryPath );
					}
					catch { }
				}

				if( !Directory.Exists( directoryPath ) )
				{
					return string.Format( "{0}\n  Directory could not be created.", failureMessage );
				}

				using( StreamWriter streamWriter = File.CreateText( filePath ) )
				{
					JsonTextWriter jsonWriter = new JsonTextWriter( streamWriter );
					jsonWriter.Formatting = Formatting.Indented;

					JObject jObj = new JObject();

					playerConfig.Save( ref jObj );

					jObj.WriteTo( jsonWriter );
				}

				return null;
			} ),
			( string error ) =>
			{
				if( !string.IsNullOrEmpty( error ) )
					Debug.LogError( error );
			} );
		}

		#endregion

		////////////////////////////////
		#region Properties

		public string name { get; private set; } = null;

		#endregion

		////////////////////////////////
		#region Members

		Dictionary<string, string> strings = new Dictionary<string, string>();
		Dictionary<string, double> doubles = new Dictionary<string, double>();
		Dictionary<string, long> longs = new Dictionary<string, long>();
		Dictionary<string, bool> bools = new Dictionary<string, bool>();

		#endregion

		////////////////////////////////
		#region Constructor

		private PlayerConfig() { }

		private PlayerConfig( string configName )
		{
			this.name = configName;
		}

		private PlayerConfig( PlayerConfig playerConfig )
		{
			this.name = playerConfig.name;

			foreach( KeyValuePair<string, string> kvp in playerConfig.strings )
				strings.Add( kvp.Key, kvp.Value );

			foreach( KeyValuePair<string, double> kvp in playerConfig.doubles )
				doubles.Add( kvp.Key, kvp.Value );

			foreach( KeyValuePair<string, long> kvp in playerConfig.longs )
				longs.Add( kvp.Key, kvp.Value );

			foreach( KeyValuePair<string, bool> kvp in playerConfig.bools )
				bools.Add( kvp.Key, kvp.Value );
		}

		#endregion

		////////////////////////////////
		#region Get-Set

			////////////////
			// String

		public string Get( string key, string defaultValue )
		{
			if( strings.ContainsKey( key ) )
				return strings[ key ];

			strings.Add( key, defaultValue );
			return defaultValue;
		}

		public void Set( string key, string value )
		{
			if( strings.ContainsKey( key ) )
				strings[ key ] = value;
			else
				strings.Add( key, value );
		}

			////////////////
			// Double

		public double Get( string key, double defaultValue )
		{
			if( doubles.ContainsKey( key ) )
				return doubles[ key ];

			doubles.Add( key, defaultValue );
			return defaultValue;
		}

		public void Set( string key, double value )
		{
			if( doubles.ContainsKey( key ) )
				doubles[ key ] = value;
			else
				doubles.Add( key, value );
		}

			////////////////
			// Float

		public float Get( string key, float defaultValue )
		{
			double value = Get( key, ( double ) defaultValue );
			if( value < float.MinValue )
				value = float.MinValue;
			else if( value > float.MaxValue )
				value = float.MaxValue;

			return ( float ) value;
		}

		public void Set( string key, float value ) =>
			Set( key, ( double ) value );

			////////////////
			// Long

		public long Get( string key, long defaultValue )
		{
			if( longs.ContainsKey( key ) )
				return longs[ key ];

			longs.Add( key, defaultValue );
			return defaultValue;
		}

		public void Set( string key, long value )
		{
			if( longs.ContainsKey( key ) )
				longs[ key ] = value;
			else
				longs.Add( key, value );
		}

			////////////////
			// Int

		public int Get( string key, int defaultValue )
		{
			long value = Get( key, ( long ) defaultValue );
			if( value < int.MinValue )
				value = int.MinValue;
			else if( value > int.MaxValue )
				value = int.MaxValue;

			return ( int ) value;
		}

		public void Set( string key, int value ) =>
			Set( key, ( long ) value );

			////////////////
			// Bool

		public bool Get( string key, bool defaultValue )
		{
			if( bools.ContainsKey( key ) )
				return bools[ key ];

			bools.Add( key, defaultValue );
			return defaultValue;
		}

		public void Set( string key, bool value )
		{
			if( bools.ContainsKey( key ) )
				bools[ key ] = value;
			else
				bools.Add( key, value );
		}

		#endregion

		////////////////////////////////
		#region Save-Load

		public void Save() =>
			SaveConfig( this );

		private struct SortedKVP
		{
			public string key;
			public JValue value;
		}

		private void Save( ref JObject jObj )
		{
			List<SortedKVP> sortedKVPs = new List<SortedKVP>();

			foreach( string key in strings.Keys )
				if( !string.IsNullOrWhiteSpace( key ) )
					sortedKVPs.Add( new SortedKVP()
					{
						key = key,
						value = new JValue( strings[ key ] )
					} );

			foreach( string key in doubles.Keys )
				if( !string.IsNullOrWhiteSpace( key ) )
					sortedKVPs.Add( new SortedKVP()
					{
						key = key,
						value = new JValue( doubles[ key ] )
					} );

			foreach( string key in longs.Keys )
				if( !string.IsNullOrWhiteSpace( key ) )
					sortedKVPs.Add( new SortedKVP()
					{
						key = key,
						value = new JValue( longs[ key ] )
					} );

			foreach( string key in bools.Keys )
				if( !string.IsNullOrWhiteSpace( key ) )
					sortedKVPs.Add( new SortedKVP()
					{
						key = key,
						value = new JValue( bools[ key ] )
					} );

			sortedKVPs.Sort( ( SortedKVP a, SortedKVP b ) =>
				a.key.CompareTo( b.key ) );

			foreach( SortedKVP sortedKVP in sortedKVPs )
				jObj.Add( sortedKVP.key, sortedKVP.value );
		}

		private void Load( ref JObject jObj )
		{
			strings.Clear();
			doubles.Clear();
			longs.Clear();
			bools.Clear();

			foreach( KeyValuePair<string, JToken> kvp in jObj )
			{
				switch( kvp.Value.Type )
				{
					case JTokenType.String:
						strings.Add( kvp.Key, kvp.Value.Value<string>() );
						break;

					case JTokenType.Float:
						doubles.Add( kvp.Key, kvp.Value.Value<double>() );
						break;

					case JTokenType.Integer:
						longs.Add( kvp.Key, kvp.Value.Value<long>() );
						break;

					case JTokenType.Boolean:
						bools.Add( kvp.Key, kvp.Value.Value<bool>() );
						break;
				}
			}
		}

		#endregion
	}
}