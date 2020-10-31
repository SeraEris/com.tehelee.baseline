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

#if UNITY_EDITOR
			private static Dictionary<string, string> layerAssignments = new Dictionary<string, string>()
			{
				{ "layers.Array.data[8]", "Post Processing" },
				{ "layers.Array.data[9]", "Skip Renderer" }
			};

			[MenuItem( "Tehelee/Baseline/Setup Layer Assignments", priority = 100 )]
			public static void SetupLayers( MenuCommand menuCommand )
			{
				Object[] objects = AssetDatabase.LoadAllAssetsAtPath( "ProjectSettings/TagManager.asset" );
				if( !object.Equals( null, objects ) && objects.Length > 0 )
				{
					

					SerializedObject tagManager = new SerializedObject( objects[ 0 ] );
					if( !object.Equals( null, tagManager ) )
					{
						bool differenceFound = false;
						int changed = 0;

						SerializedProperty iterator = tagManager.GetIterator();
						bool showChildren = true;
						while( iterator.NextVisible( showChildren ) )
						{
							string propertyPath = iterator.propertyPath;

							if( layerAssignments.ContainsKey( propertyPath ) )
							{
								string oldLayerName = iterator.stringValue;
								string newLayerName = layerAssignments[ propertyPath ];

								if( string.Equals( oldLayerName, newLayerName ) )
									continue;

								differenceFound = true;

								int layerIndex = -1;

								int layerIndexOpen = propertyPath.LastIndexOf( '[' ) + 1;
								int layerIndexClose = propertyPath.LastIndexOf( ']' );

								if( layerIndexOpen > -1 && layerIndexClose > layerIndexOpen )
									int.TryParse( propertyPath.Substring( layerIndexOpen, layerIndexClose - layerIndexOpen ), out layerIndex );
								
								if( string.IsNullOrEmpty( oldLayerName ) )
								{
									iterator.stringValue = newLayerName;
									changed++;
									Debug.LogFormat( "Updated layer #{0} to '{1}'.", layerIndex, newLayerName );
								}
								else if
								(
									EditorUtility.DisplayDialog
									(
										string.Format( "Update Layer #{0}?", layerIndex ),
										string.Format( "Current: {0}\nTarget: {1}", oldLayerName, newLayerName ),
										"Update",
										"Skip"
									)
								)
								{
									iterator.stringValue = layerAssignments[ propertyPath ];
									changed++;
									Debug.LogFormat( "Updated layer #{0} from '{1}' to '{2}'.", layerIndex, oldLayerName, newLayerName );
								}
							}
						}

						if( changed > 0 )
						{
							if( UnityEditor.VersionControl.Provider.enabled )
								UnityEditor.VersionControl.Provider.Checkout( objects[ 0 ], UnityEditor.VersionControl.CheckoutMode.Asset ).Wait();

							tagManager.ApplyModifiedProperties();
							EditorUtility.SetDirty( tagManager.targetObject );
							EditorUtils.SaveDirtyAssets();

							Debug.LogFormat( "Updated 'ProjectSettings/TagManager.asset' with {0} layer assignments.", changed );
						}

						if( !differenceFound )
							EditorUtility.DisplayDialog( "Layers Already Assigned!", "Layers already match global definitions.", "Cool", "Alright" );
					}
				}
			}
#endif
		}
	}
}