using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using System.IO;

using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.DesignData
{
	[CreateAssetMenu( fileName = "Data Collection", menuName = "Design Data Collection", order = 200 )]
	public class Collection : Data
	{
		public List<Data> datas = new List<Data>();

#if UNITY_EDITOR
		public string directory { get { return Path.Combine( Application.dataPath, path, name ); } }
		public string localDirectory { get { return Path.Combine( "Assets", path, name ); } }
#endif

		public HashSet<Data> GetAllDatas()
		{
			HashSet<Data> _datas = new HashSet<Data>();
			foreach( Data data in datas )
			{
				if( object.Equals( null, data ) )
					continue;

				if( data is Collection )
					_datas.AddRange( ( ( Collection ) data ).GetAllDatas() );
				else
					_datas.Add( data );
			}

			return _datas;
		}
	}

#if UNITY_EDITOR

	[CustomEditor( typeof( Collection ) )]
	public class EditorCollection : EditorData
	{
		public override bool drawPath { get { return !object.Equals( null, path ) && !string.IsNullOrEmpty( path.stringValue ) && path.stringValue.Length > 0; } }

		private static GUIStyle _buttonStyle;
		private static GUIStyle buttonStyle
		{
			get
			{
				if( object.Equals( null, _buttonStyle ) )
				{
					_buttonStyle = new GUIStyle( EditorStyles.miniButton );
					_buttonStyle.fixedHeight = lineHeight * 1.5f;
				}

				return _buttonStyle;
			}
		}

		ReorderableList dataList;

		Collection collection;

		private bool folderExists = false;

		public override void Setup()
		{
			base.Setup();

			dataList = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "datas" ),
				( SerializedProperty element ) =>
				{
					return lineHeight * 1.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					EditorUtils.BetterObjectField<Data>( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), EditorUtils.EmptyContent, element );
				}
			);

			collection = ( Collection ) target;

			CheckFolder();
		}
		
		public override float GetInspectorHeight()
		{
			float height = base.GetInspectorHeight();

			height += lineHeight * 1.5f;

			height += dataList.CalculateCollapsableListHeight();

			return height;
		}

		public override float GetPostInspectorHeight()
		{
			return base.GetPostInspectorHeight() + lineHeight * 1.5f;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Data Collection" ) );
			bRect.y += lineHeight * 1.5f;

			dataList.DrawCollapsableList( ref bRect );

			rect.y = bRect.y;
		}

		public override void DrawPostInspector( ref Rect rect )
		{
			base.DrawPostInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight * 1.5f );

			if( !string.IsNullOrEmpty( collection.name ) )
			{
				if( GUI.Button( bRect, new GUIContent( string.Format( folderExists ? "Populate From {0} Folder" : "Create {0} Folder", collection.name ) ), buttonStyle ) )
				{
					if( folderExists )
						PopulateFromFolder( ref collection );
					else
						CreateFolder();

					serializedObject.Update();
				}
			}

			bRect.y += lineHeight * 1.5f;
			rect.y = bRect.y;
		}

		private void CheckFolder()
		{
			if( string.IsNullOrEmpty( collection.name ) )
			{
				folderExists = false;
				return;
			}

			string assetPath = AssetDatabase.GetAssetPath( collection );

			assetPath = assetPath.Substring( 7, Mathf.Max( 0, assetPath.Length - Path.GetFileName( assetPath ).Length - 8 ) );

			if( path.stringValue != assetPath )
			{
				path.stringValue = assetPath;

				serializedObject.ApplyModifiedProperties();
			}

			folderExists = Directory.Exists( collection.directory );
		}

		private void CreateFolder()
		{
			Directory.CreateDirectory( collection.directory );

			CheckFolder();
		}

		private void PopulateFromFolder( ref Collection collection )
		{
			if( !Utils.IsObjectAlive( collection ) )
				return;

			if( !Directory.Exists( collection.directory ) )
			{
				collection.path = string.Empty;
				return;
			}

			string[] assetPaths = Directory.GetFiles( collection.directory, "*.asset" );

			string pathPrefix = Application.dataPath;
			pathPrefix = pathPrefix.Substring( 0, pathPrefix.Length - 7 );

			string collectionPath = AssetDatabase.GetAssetPath( collection );

			collectionPath = collectionPath.Substring( 7, Mathf.Max( 0, collectionPath.Length - Path.GetFileName( collectionPath ).Length - 8 ) );

			if( collection.path != collectionPath )
			{
				collection.path = collectionPath;
			}

			collectionPath = string.Format( "{0}/{1}", collectionPath, collection.name );

			System.Type collectionType = typeof( Collection ), dataType = typeof( Data ), assetType;

			List<Data> data = new List<Data>();

			foreach( string assetPath in assetPaths )
			{
				string _assetPath = assetPath.Substring( pathPrefix.Length + 1, assetPath.Length - pathPrefix.Length - 1 );

				assetType = AssetDatabase.GetMainAssetTypeAtPath( _assetPath );

				if( dataType.IsAssignableFrom( assetType ) )
				{
					if( collectionType.IsAssignableFrom( assetType ) )
					{
						Collection _collection = AssetDatabase.LoadAssetAtPath<Collection>( _assetPath );

						PopulateFromFolder( ref _collection );
					}

					Data _data = AssetDatabase.LoadAssetAtPath<Data>( _assetPath );

					data.Add( _data );

					if( _data.path != collectionPath )
					{
						_data.path = collectionPath;
						EditorUtility.SetDirty( _data );
					}
				}
			}

			collection.datas = data;

			EditorUtility.SetDirty( collection );

			if( collection == this.collection )
			{
				serializedObject.ApplyModifiedProperties();

				AssetDatabase.SaveAssets();

				AssetDatabase.Refresh();
			}
		}
	}

#endif
}