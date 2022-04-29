using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Tehelee.Baseline
{
    public class KeyBindings
	{
		private const string jsonFileName = "KeyBindings.json";

		public static bool hasLoaded { get; private set; }
		private static bool loading = false;
		private static int saveRequests = 0;

		public static string GetJsonFilePath() =>
			System.IO.Path.Combine( Application.persistentDataPath, jsonFileName );

		private static int enabledCount = 0;
		public static Dictionary<string, KeyCode[]> binds = new Dictionary<string, KeyCode[]>();
		
		public static KeyCode[] GetKeyCodes( string key )
		{
			if( binds.ContainsKey( key ) )
				return new List<KeyCode>( binds[ key ] ).ToArray();
			else
				return new KeyCode[ 0 ];
		}

		public static void SetKeyCodes( string key, KeyCode[] keyCodes )
		{
			if( string.IsNullOrWhiteSpace( key ) )
				return;

			HashSet<KeyCode> hashSet = new HashSet<KeyCode>();
			for( int i = 0, iC = keyCodes.Length; i < iC; i++ )
				if( keyCodes[ i ] != KeyCode.None )
					hashSet.Add( keyCodes[ i ] );

			keyCodes = new List<KeyCode>( hashSet ).ToArray();

			if( keyCodes.Length == 0 )
			{
				if( binds.ContainsKey( key ) )
					binds.Remove( key );
			}
			else
			{
				if( binds.ContainsKey( key ) )
					binds[ key ] = keyCodes;
				else
					binds.Add( key, keyCodes );
			}
		}

		public static void OnEnable()
		{
			if( enabledCount++ == 0 )
			{
				Load();
			}
		}

		public static void OnDisable()
		{
			Save();
		}

		public static void Save()
		{
			saveRequests++;

			if( saveRequests > 1 )
				return;

			_Save();
		}

		private static void _Save()
		{
			string jsonFilePath = GetJsonFilePath();

			string jsonFileDirectory = Path.GetDirectoryName( jsonFilePath );

			if( File.Exists( jsonFilePath ) )
				File.Delete( jsonFilePath );

			if( !Directory.Exists( jsonFileDirectory ) )
				Directory.CreateDirectory( jsonFileDirectory );

			if( Directory.Exists( jsonFileDirectory ) && !File.Exists( jsonFilePath ) )
			{
				using( StreamWriter streamWriter = File.CreateText( jsonFilePath ) )
				{
					JsonTextWriter jsonWriter = new JsonTextWriter( streamWriter );
					jsonWriter.Formatting = Formatting.Indented;

					JObject jObj = new JObject();

					List<string> bindKeys = new List<string>( binds.Keys );
					bindKeys.Sort();

					foreach( string bindKey in bindKeys )
					{
						KeyCode[] keyCodes = binds[ bindKey ];

						if( keyCodes.Length > 0 )
						{
							JArray jArray = new JArray();

							foreach( KeyCode keyCode in keyCodes )
								jArray.Add( keyCode.ToString() );

							jObj.Add( bindKey, jArray );
						}
					}

					jObj.WriteTo( jsonWriter );
				}
			}

			saveRequests = 0;
		}

		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.BeforeSceneLoad )]
		public static void Load()
		{
			if( loading )
				return;

			loading = true;

			string jsonFilePath = GetJsonFilePath();

			Utils.WaitForTask( new Task<Dictionary<string,KeyCode[]>>( () =>
			{
				Dictionary<string, KeyCode[]> binds = new Dictionary<string, KeyCode[]>();

				if( File.Exists( jsonFilePath ) )
				{
					using( StreamReader streamReader = new StreamReader( jsonFilePath ) )
					{
						JsonTextReader jsonReader = new JsonTextReader( streamReader );

						JObject jObj = null;

						try
						{
							jObj = ( JObject ) JToken.ReadFrom( jsonReader );
							
							foreach( KeyValuePair<string, JToken> kvp in jObj )
							{
								JArray jArray = ( JArray ) kvp.Value;

								HashSet<KeyCode> hashSet = new HashSet<KeyCode>();
								foreach( JToken jToken in jArray )
								{
									string keyCodeString = jToken.Value<string>();

									if( !string.IsNullOrEmpty( keyCodeString ) )
									{
										KeyCode key;
										if( System.Enum.TryParse( keyCodeString, out key ) && key != KeyCode.None )
											hashSet.Add( key );
									}
								}

								if( hashSet.Count > 0 )
									binds.Add( kvp.Key, new List<KeyCode>( hashSet ).ToArray() );
							}
						}
						catch { }
					}
				}

				hasLoaded = true;
				loading = false;

				return binds;
			} ),
			( Dictionary<string,KeyCode[]> binds ) =>
			{
				foreach( KeyValuePair<string, KeyCode[]> kvp in binds )
				{
					SetKeyCodes( kvp.Key, kvp.Value );
				}
			} );
		}
	}
}
