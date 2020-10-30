#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
#endif

namespace Tehelee.Baseline
{
	public enum NetworkScope : int
	{
		Server = 0,
		Client = 1
	}

	public enum Permission
	{
		Kernel		= 0,
		Admin		= 1,
		Moderator	= 2,
		Player		= 3,
		Spectator	= 4
	}

	public enum SoundCategories : byte
	{
		Master		= 0,
		Menu		= 1,
		Music		= 2,
	}
	
	public static class Globals
	{
		public static class Layers
		{
			public static readonly int Default				= ( 1 << 0 );
			public static readonly int TransparentFX		= ( 1 << 1 );
			public static readonly int IgnoreRaycast		= ( 1 << 2 );
			public static readonly int Water				= ( 1 << 4 );
			public static readonly int UI					= ( 1 << 5 );

			public static readonly int PostProcessing		= ( 1 << 8 );
			public static readonly int SkipRenderer			= ( 1 << 9 );
			public static readonly int World				= ( 1 << 10 );

			public static readonly int PlayerServer			= ( 1 << 12 );
			public static readonly int PlayerClient			= ( 1 << 13 );
			public static readonly int PlayerPrediction		= ( 1 << 14 );

			public static readonly int Player = ( PlayerServer | PlayerClient | PlayerPrediction );

			public static readonly int DynamicServer		= ( 1 << 15 );
			public static readonly int DynamicClient		= ( 1 << 16 );
			public static readonly int DynamicPrediction	= ( 1 << 17 );

			public static readonly int Dynamic = ( DynamicServer | DynamicClient | DynamicPrediction );

#if UNITY_EDITOR
			private static Dictionary<string, string> layerAssignments = new Dictionary<string, string>()
			{
				{ "layers.Array.data[8]", "Post Processing" },
				{ "layers.Array.data[9]", "Skip Renderer" },
				{ "layers.Array.data[10]", "World" },

				{ "layers.Array.data[12]", "Player Server" },
				{ "layers.Array.data[13]", "Player Client" },
				{ "layers.Array.data[14]", "Player Prediction" },

				{ "layers.Array.data[15]", "Dynamic Server" },
				{ "layers.Array.data[16]", "Dynamic Client" },
				{ "layers.Array.data[17]", "Dynamic Prediction" }
			};

			[MenuItem( "Tehelee/Setup Layers", priority = 100 )]
			public static void SetupLayers( MenuCommand menuCommand )
			{
				Object[] objects = AssetDatabase.LoadAllAssetsAtPath( "ProjectSettings/TagManager.asset" );
				if( !object.Equals( null, objects ) && objects.Length > 0 )
				{
					SerializedObject tagManager = new SerializedObject( objects[ 0 ] );
					if( !object.Equals( null, tagManager ) )
					{
						SerializedProperty iterator = tagManager.GetIterator();
						bool showChildren = true;
						while( iterator.NextVisible( showChildren ) )
						{
							if( layerAssignments.ContainsKey( iterator.propertyPath ) )
							{
								iterator.stringValue = layerAssignments[ iterator.propertyPath ];
							}
						}
						tagManager.ApplyModifiedProperties();
						EditorUtility.SetDirty( tagManager.targetObject );
						EditorUtils.SaveDirtyAssets();
					}
				}
			}
#endif
		}
	}
}