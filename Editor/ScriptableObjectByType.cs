using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Tehelee.Baseline
{
	public class ScriptableObjectByType : EditorWindow
	{
		private static ScriptableObjectByType _instance = null;

		public static ScriptableObjectByType instance
		{
			get
			{
				if( !object.Equals( null, _instance) && !_instance )
				{
					_instance = null;
				}

				if( object.Equals( null, _instance ) )
				{
					if( EditorWindow.HasOpenInstances<ScriptableObjectByType>() )
					{
						_instance = EditorWindow.GetWindow<ScriptableObjectByType>();
					}
					
					if( object.Equals( null, _instance ) )
					{
						_instance = EditorWindow.CreateWindow<ScriptableObjectByType>();

						_instance.titleContent = new GUIContent( "ScriptableObject<T>" );

						float lineHeight = EditorGUIUtility.singleLineHeight;

						_instance.minSize = new Vector2( 200f, lineHeight * 2f );
						_instance.maxSize = new Vector2( 2000f, lineHeight * 2f );
					}
				}

				return _instance;
			}
		}

		[MenuItem( "Assets/Create/ScriptableObject<T>", false, 200 )]
		public static void Create( MenuCommand menuCommand )
		{
			destinationPath = AssetDatabase.GetAssetPath( Selection.activeObject );
			
			instance.Show();
		}

		private static GUIStyle _warningStyle = null;
		private static GUIStyle warningStyle
		{
			get
			{
				if( object.Equals( null, _warningStyle ) )
				{
					_warningStyle = new GUIStyle( "CN EntryWarnIconSmall" );
				}

				return _warningStyle;
			}
		}

		public static string destinationPath;

		public static bool validated;

		public static string typeFullName;

		private static System.Type GetType( string fullName )
		{
			System.Type type = null;

			System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

			foreach( System.Reflection.Assembly assembly in assemblies )
			{
				System.Type[] types = assembly.GetTypes();

				foreach( System.Type _type in types )
				{
					if( string.Equals( _type.FullName, typeFullName ) )
					{
						type = _type;
						break;
					}
				}

				if( !object.Equals( null, type ) )
				{
					break;
				}
			}

			return type;
		}

		public void OnGUI()
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;

			Rect rect = EditorGUILayout.GetControlRect( GUILayout.ExpandHeight( true ) );

			Rect boundsRect = new Rect( rect.x + 5f, rect.y + lineHeight * 0.5f, rect.width - 10f, lineHeight );

			Rect bRect = new Rect( boundsRect.x, boundsRect.y, boundsRect.width - 80f, lineHeight );
			
			Color backgroundColor = GUI.backgroundColor;
			Color contentColor = GUI.contentColor;

			if( !validated )
			{
				bRect = new Rect( bRect.x + 25f, bRect.y, bRect.width - 25f, lineHeight );

				GUI.contentColor = new Color( 1f, 0.75f, 0.25f, 1f );
			}

			EditorGUI.BeginChangeCheck();

			typeFullName = EditorGUI.TextField( bRect, new GUIContent(), typeFullName );

			if( EditorGUI.EndChangeCheck() )
			{
				if( string.IsNullOrWhiteSpace( typeFullName ) )
				{
					validated = false;
				}
				else
				{
					string _typeFullName = typeFullName.Trim();
					
					System.Type type = GetType( _typeFullName );

					validated = ( !object.Equals( null, type ) && typeof( ScriptableObject ).IsAssignableFrom( type ) );
				}
			}

			if( !validated )
			{
				GUI.contentColor = contentColor;

				Rect cRect = new Rect( boundsRect.x, boundsRect.y + 2f, 15f, lineHeight );

				EditorGUI.LabelField( cRect, new GUIContent(), warningStyle );
			}

			GUI.backgroundColor = Color.clear;
			GUI.contentColor = Color.clear;

			MonoScript monoScript = EditorUtils.BetterObjectField<MonoScript>( bRect, new GUIContent(), ( MonoScript ) null );

			if( !object.Equals( null, monoScript ) )
			{
				System.Type monoType = monoScript.GetClass();

				if( typeof( ScriptableObject ).IsAssignableFrom( monoType ) )
				{
					typeFullName = monoType.FullName;
					validated = true;
				}
			}

			GUI.backgroundColor = backgroundColor;
			GUI.contentColor = contentColor;

			EditorGUI.BeginDisabledGroup( !validated );

			if( EditorUtils.BetterButton( new Rect( boundsRect.x + boundsRect.width - 70f, boundsRect.y - lineHeight * 0.25f, 70f, lineHeight * 1.5f ), new GUIContent( "Create" ) ) && validated )
			{
				System.Type type = GetType( typeFullName );

				if( !object.Equals( null, type ) )
				{
					ScriptableObject scriptableObject = ScriptableObject.CreateInstance( type );
					
					AssetDatabase.CreateAsset( scriptableObject, AssetDatabase.GenerateUniqueAssetPath( System.IO.Path.Combine( destinationPath, string.Format( "{0}.asset", type.Name ) ) ) );

					typeFullName = string.Empty;

					validated = false;

					destinationPath = string.Empty;

					AssetDatabase.Refresh();
				}
				
				instance.Close();
			}

			EditorGUI.EndDisabledGroup();
		}
	}
}