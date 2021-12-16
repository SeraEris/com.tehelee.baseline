using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline
{
	[System.Serializable]
	public class SerializedScene : ISerializationCallbackReceiver
	{
		[SerializeField] private Object sceneAsset;
		[SerializeField] private string _scenePath = "";

		public const string indexAssets = "Assets";
		public const string indexUnity = ".unity";

		public string scenePath => _scenePath;
		
		public static implicit operator string( SerializedScene sceneField ) => sceneField.scenePath;
		
		public void OnBeforeSerialize()
		{
#if UNITY_EDITOR
			if( !object.Equals( null, sceneAsset ) )
			{
				string assetPath = AssetDatabase.GetAssetPath( sceneAsset );
				if( !string.IsNullOrWhiteSpace( assetPath ) )
				{
					int assetsIndex = assetPath.IndexOf( indexAssets, System.StringComparison.Ordinal ) + indexAssets.Length + 1;
					int extensionIndex = assetPath.LastIndexOf( indexUnity, System.StringComparison.Ordinal );
					assetPath = assetPath.Substring( assetsIndex, extensionIndex - assetsIndex );
					_scenePath = assetPath;
				}
			}
#endif
		}

		public void OnAfterDeserialize() {}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer( typeof( SerializedScene ) )]
	public class SerializedScenePropertyDrawer : PropertyDrawer
	{
		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
			EditorGUI.BeginProperty( position, GUIContent.none, property );
			
			SerializedProperty sceneAsset = property.FindPropertyRelative( "sceneAsset" );
			SerializedProperty scenePath = property.FindPropertyRelative( "_scenePath" );
			
			position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID( FocusType.Passive ), label );
			if( !object.Equals( null, sceneAsset ) )
			{
				EditorGUI.BeginChangeCheck();
				Object value = EditorGUI.ObjectField( position, sceneAsset.objectReferenceValue, typeof( SceneAsset ), false );
				if( EditorGUI.EndChangeCheck() )
				{
					sceneAsset.objectReferenceValue = value;
					if( sceneAsset.objectReferenceValue != null )
					{
						string assetPath = AssetDatabase.GetAssetPath( sceneAsset.objectReferenceValue );
						int assetsIndex = assetPath.IndexOf( SerializedScene.indexAssets, System.StringComparison.Ordinal ) + SerializedScene.indexAssets.Length + 1;
						int extensionIndex = assetPath.LastIndexOf( SerializedScene.indexUnity, System.StringComparison.Ordinal );
						assetPath = assetPath.Substring( assetsIndex, extensionIndex - assetsIndex );
						scenePath.stringValue = assetPath;
					}
				}
			}
			EditorGUI.EndProperty();
		}
	}
#endif
}