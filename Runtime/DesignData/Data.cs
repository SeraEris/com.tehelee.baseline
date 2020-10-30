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

		[System.NonSerialized]
		private ushort _dataHash = 0;
		public ushort dataHash
		{
			get
			{
				if( _dataHash == 0 )
				{
					_dataHash = GetDataHash();
				}

				return _dataHash;
			}
		}
		

		public ushort GetDataHash()
		{
			return Utils.HashCRC( string.Format( "[{0}] {1}/{2}", this.GetType().FullName, path, this.name ) );
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

		Data data;

		protected string populatedTypes;

		protected bool internalData { get; private set; }

		public override bool saveAssetsOnDisable => true;

		public override void Setup()
		{
			base.Setup();
			
			path = serializedObject.FindProperty( "path" );

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

		public override float inspectorLeadingOffset => lineHeight * 0.5f;

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			int i = 0;

			if( drawPath )
				i++;

			if( drawHash )
				i++;

			if( drawTypes )
				i++;

			inspectorHeight += lineHeight * i;

			inspectorHeight += lineHeight * Mathf.Max( 0, ( i - 1 ) ) * 0.5f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Color contentColor = GUI.contentColor;

			EditorGUIUtility.labelWidth = 50f;

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

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
				EditorGUI.SelectableLabel( cRect, string.Format( "0x{0:X}", data.GetDataHash() ), EditorStyles.textField );

				cRect.x += cRect.width + 10f;

				EditorGUI.SelectableLabel( cRect, data.GetDataHash().ToString(), EditorStyles.textField );

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

			if( drawPath || drawHash || drawTypes )
			{
				rect.y = bRect.y - lineHeight * 0.5f;
			}
		}
	}

#endif
}