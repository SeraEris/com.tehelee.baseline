using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline.DesignData
{
	//[CreateAssetMenu( fileName = "Data", menuName = "Design Data/Data", order = 200 )]
	public class Data : ScriptableObject
	{
		public string path;
		public string GetFullPath() => $"{path}/{name}";

		[SerializeField]
		private string _displayName;
		public string displayName => string.IsNullOrEmpty( _displayName ) ? name : _displayName;

		public string description;

		private int _dataHash = 0;
		public int dataHash
		{
			get
			{
				if( _dataHash == 0 )
					_dataHash = GetFullPath().GetHashCode();
				
				return _dataHash;
			}
		}

		public int GetDataHash() => dataHash;

		private static Dictionary<Data, HashSet<Object>> dataReservations = new Dictionary<Data, HashSet<Object>>();
		
		public static void ReserveData( Object obj, Data data )
		{
			if( data is Collection )
			{
				foreach( Data _data in ( ( Collection ) data ).GetAllDatas() )
					ReserveData( obj, _data );
				return;
			}

			if( dataReservations.ContainsKey( data ) )
				dataReservations[ data ].Add( obj );
			else
				dataReservations.Add( data, new HashSet<Object>() { obj } );
		}

		public static void ReleaseData( Object obj, Data data )
		{
			if( data is Collection )
			{
				foreach( Data _data in ( ( Collection ) data ).GetAllDatas() )
					ReleaseData( obj, _data );
				return;
			}

			if( dataReservations.ContainsKey( data ) )
			{
				HashSet<Object> reservations = dataReservations[ data ];
				reservations.Remove( obj );
				if( reservations.Count == 0 )
					dataReservations.Remove( data );
				else
					dataReservations[ data ] = reservations;
			}
		}

		public static List<T> FindDatasOfType<T>() where T : Data
		{
			System.Type compare = typeof( T );
			List<T> datas = new List<T>();
			foreach( Data data in dataReservations.Keys )
			{
				System.Type type = data.GetType();
				if( type == compare || compare.IsAssignableFrom( type ) )
					datas.Add( ( T ) data );
			}
			return datas;
		}
	}

#if UNITY_EDITOR

	[CustomEditor( typeof( Data ) )]
	public class EditorData : EditorUtils.InheritedEditor
	{
		public virtual bool drawPath { get { return true; } }
		public virtual bool drawHash { get { return true; } }
		public virtual bool drawTypes { get { return true; } }
		
		protected SerializedProperty path;

		protected SerializedProperty displayName;
		protected SerializedProperty description;

		Data data;

		protected string populatedTypes;

		protected bool internalData { get; private set; }

		

		private static GUIStyle _styleRichTextArea;
		protected static GUIStyle styleRichTextArea
		{
			get
			{
				if( object.Equals( null, _styleRichTextArea ) )
				{
					_styleRichTextArea = new GUIStyle( EditorStyles.textArea );
					_styleRichTextArea.richText = true;
				}

				return _styleRichTextArea;
			}
		}

		public override bool saveAssetsOnDisable => true;

		public override void Setup()
		{
			base.Setup();
			
			path = serializedObject.FindProperty( "path" );
			displayName = serializedObject.FindProperty( "_displayName" );
			description = serializedObject.FindProperty( "description" );

			data = ( Data ) target;

			internalData = AssetDatabase.GetAssetPath( data ).StartsWith( "Assets/Data/" );

			populatedTypes = "";
			System.Type typeData = typeof( Data ), typeCurrent = target.GetType();

			while( typeData.IsAssignableFrom( typeCurrent ) || typeData == typeCurrent )
			{
				populatedTypes = populatedTypes.Length > 0 ? string.Format( "{0}, {1}", typeCurrent.Name, populatedTypes ) : typeCurrent.Name;
				typeCurrent = typeCurrent.BaseType;
			}
		}

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 1.5f;
			
			int i = 0;

			if( drawPath )
				i++;

			if( drawHash )
				i++;

			if( drawTypes )
				i++;

			inspectorHeight += lineHeight * 1.5f * i;

			inspectorHeight += lineHeight * 4.5f + 4f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Color contentColor = GUI.contentColor;

			EditorGUIUtility.labelWidth = 50f;

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Data" ) );
			bRect.y += lineHeight * 1.5f;

			if( drawPath )
			{
				if( internalData )
				{
					GUI.contentColor = new Color( 1f, 1f, 1f, 0.5f );

					Rect cRect = EditorGUI.PrefixLabel( bRect, new GUIContent( "Path" ) );

					EditorGUI.SelectableLabel( cRect, path.stringValue, EditorStyles.textField );

					GUI.contentColor = contentColor;
				}
				else
				{
					EditorGUI.PropertyField( bRect, path, new GUIContent( "Path" ) );
				}

				bRect.y += lineHeight * 1.5f;
			}
			
			GUI.contentColor = new Color( 1f, 1f, 1f, 0.5f );

			if( drawHash )
			{
				Rect cRect = EditorGUI.PrefixLabel( bRect, new GUIContent( "Hash" ) );

				cRect.width = ( cRect.width - 10f ) * 0.5f;

				float width = rect.width - EditorGUIUtility.labelWidth;
				EditorGUI.SelectableLabel( cRect, $"0x{data.dataHash:X}", EditorStyles.textField );

				cRect.x += cRect.width + 10f;

				EditorGUI.SelectableLabel( cRect, data.dataHash.ToString(), EditorStyles.textField );

				bRect.y += lineHeight * 1.5f;
			}

			if( drawTypes )
			{
				Rect cRect = EditorGUI.PrefixLabel( bRect, new GUIContent( "Types" ) );

				EditorGUI.SelectableLabel( cRect, populatedTypes, EditorStyles.textField );

				bRect.y += lineHeight * 1.5f;
			}

			GUI.contentColor = contentColor;

			EditorGUIUtility.labelWidth = labelWidth;

			EditorUtils.BetterTextField( bRect, new GUIContent( "Display Name" ), displayName );
			bRect.y += lineHeight + 4f;

			bRect.height = lineHeight * 3f;
			EditorUtils.BetterTextArea( bRect, new GUIContent( "Description" ), description );
			bRect.y += bRect.height + lineHeight * 0.5f;
			bRect.height = lineHeight;

			rect.y = bRect.y;
		}
	}

#endif
}