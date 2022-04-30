#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UnityEditor.VersionControl;
using UnityEditor.Build.Reporting;

namespace Tehelee.Baseline
{
	public static class EditorUtils
	{
		////////////////////////////////
		#region General

		public static Rect GetSingleLineRect( float padding = 0f )
		{
			return EditorGUILayout.GetControlRect( GUILayout.Height( EditorGUIUtility.singleLineHeight * ( 1f + padding ) ) );
		}

		private static GUIContent _EmptyContent;
		public static GUIContent EmptyContent
		{
			get
			{
				if( object.Equals( null, _EmptyContent ) )
				{
					_EmptyContent = new GUIContent();
				}

				return _EmptyContent;
			}
		}

		public static bool IsEmptyContent( GUIContent guiContent ) => ( object.Equals( null, guiContent ) || string.IsNullOrWhiteSpace( guiContent.text ) );

		public static void RevealInFinder( string path )
		{
			if( string.IsNullOrWhiteSpace( path ) )
				return;

			path = System.IO.Path.GetFullPath( path );
			string fileName = string.Empty;
			foreach( string file in System.IO.Directory.EnumerateFiles( path ) )
				fileName = file;

			if( !string.IsNullOrEmpty( fileName ) )
				EditorUtility.RevealInFinder( System.IO.Path.Combine( path, fileName ) );
			else
				EditorUtility.RevealInFinder( path );
		}

		#endregion

		////////////////////////////////
		#region ReorderableList

		public delegate float ReorderableElementHeightExpanded( SerializedProperty list, int index, SerializedProperty element );
		public delegate float ReorderableElementHeight( SerializedProperty element );
		public delegate void ReorderableElementGUIExpanded( Rect rect, SerializedProperty list, int index, SerializedProperty element, bool isActive, bool isFocussed );
		public delegate void ReorderableElementGUI( Rect rect, SerializedProperty element, int index, bool isActive, bool isFocussed );
		public delegate void ReorderableElementGUIShort( Rect rect, SerializedProperty element );
		public delegate void ReorderableAddElement( SerializedProperty list, SerializedProperty element );

		/*
			= EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "_PROPERTY_" ),
				( SerializedProperty element ) =>
				{
					return EditorGUI.GetPropertyHeight( element, true ) + lineHeight * 0.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					Rect bRect = new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, rect.height - lineHeight * 0.5f );

					EditorGUI.PropertyField( bRect, element, true );
				},
				( SerializedProperty list, SerializedProperty element ) => { }
			);
		*/

		/*
			= EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "_PROPERTY_" ),
				( SerializedProperty element ) =>
				{
					return lineHeight * 1.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					EditorUtils.BetterObjectField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), new GUIContent(), element, typeof( T ) );
				}
			);
		*/

		private static void DrawReorderableListHeader( SerializedProperty property, Rect rect )
		{
			EditorGUI.LabelField( rect, property.displayName );

			if( GUI.Button( new Rect( rect.x - 6f, rect.y - 1f, rect.width + 12f, rect.height + 2f ), new GUIContent(), GUIStyle.none ) )
				property.isExpanded = !property.isExpanded;
		}

		public static ReorderableList CreateReorderableList( SerializedProperty property, ReorderableElementHeightExpanded reorderableElementHeight = null, ReorderableElementGUIExpanded reorderableElementGUI = null, ReorderableAddElement reorderableAddElement = null )
		{
			ReorderableList list = new ReorderableList( property.serializedObject, property );

			list.drawHeaderCallback = ( Rect rect ) => DrawReorderableListHeader( property, rect );

			list.elementHeightCallback = ( int index ) =>
			{
				if( object.Equals( null, reorderableElementHeight ) )
					return EditorGUIUtility.singleLineHeight;

				return reorderableElementHeight( property, index, property.GetArrayElementAtIndex( index ) );
			};

			list.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
			{
				if( object.Equals( null, reorderableElementGUI ) )
				{
					EditorGUI.PropertyField( rect, property.GetArrayElementAtIndex( index ) );
				}
				else
				{
					reorderableElementGUI( rect, property, index, property.GetArrayElementAtIndex( index ), isActive, isFocused );
				}
			};

			list.onAddCallback = ( ReorderableList reorderableList ) =>
			{
				SerializedProperty _list = reorderableList.serializedProperty;
				int arraySize = _list.arraySize;

				_list.InsertArrayElementAtIndex( arraySize );

				SerializedProperty element = _list.GetArrayElementAtIndex( arraySize );

				if( !object.Equals( null, reorderableAddElement ) )
				{
					reorderableAddElement( _list, element );
				}
			};

			return list;
		}

		public static ReorderableList CreateReorderableList( SerializedProperty property, ReorderableElementHeight reorderableElementHeight = null, ReorderableElementGUI reorderableElementGUI = null, ReorderableAddElement reorderableAddElement = null )
		{
			ReorderableList list = new ReorderableList( property.serializedObject, property );

			list.drawHeaderCallback = ( Rect rect ) => DrawReorderableListHeader( property, rect );

			list.elementHeightCallback = ( int index ) =>
			{
				if( object.Equals( null, reorderableElementHeight ) )
					return EditorGUIUtility.singleLineHeight;

				return reorderableElementHeight( property.GetArrayElementAtIndex( index ) );
			};

			list.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
			{
				if( object.Equals( null, reorderableElementGUI ) )
				{
					EditorGUI.PropertyField( rect, property.GetArrayElementAtIndex( index ) );
				}
				else
				{
					reorderableElementGUI( rect, property.GetArrayElementAtIndex( index ), index, isActive, isFocused );
				}
			};

			list.onAddCallback = ( ReorderableList reorderableList ) =>
			{
				SerializedProperty _list = reorderableList.serializedProperty;
				int arraySize = _list.arraySize;

				_list.InsertArrayElementAtIndex( arraySize );

				SerializedProperty element = _list.GetArrayElementAtIndex( arraySize );

				if( !object.Equals( null, reorderableAddElement ) )
				{
					reorderableAddElement( _list, element );
				}
			};

			return list;
		}

		public static ReorderableList CreateReorderableList( SerializedProperty property, ReorderableElementHeight reorderableElementHeight = null, ReorderableElementGUIShort reorderableElementGUI = null, ReorderableAddElement reorderableAddElement = null )
		{
			ReorderableList list = new ReorderableList( property.serializedObject, property );

			list.drawHeaderCallback = ( Rect rect ) => DrawReorderableListHeader( property, rect );

			list.elementHeightCallback = ( int index ) =>
			{
				if( object.Equals( null, reorderableElementHeight ) )
					return EditorGUIUtility.singleLineHeight;

				return reorderableElementHeight( property.GetArrayElementAtIndex( index ) );
			};

			list.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
			{
				if( object.Equals( null, reorderableElementGUI ) )
				{
					EditorGUI.PropertyField( rect, property.GetArrayElementAtIndex( index ) );
				}
				else
				{
					reorderableElementGUI( rect, property.GetArrayElementAtIndex( index ) );
				}
			};

			list.onAddCallback = ( ReorderableList reorderableList ) =>
			{
				SerializedProperty _list = reorderableList.serializedProperty;
				int arraySize = _list.arraySize;

				_list.InsertArrayElementAtIndex( arraySize );

				SerializedProperty element = _list.GetArrayElementAtIndex( arraySize );

				if( !object.Equals( null, reorderableAddElement ) )
				{
					reorderableAddElement( _list, element );
				}
			};

			return list;
		}

		private static GUIStyle _listHeaderStyle = null;
		private static GUIStyle listHeaderStyle
		{
			get
			{
				if( object.Equals( null, _listHeaderStyle ) )
				{
					_listHeaderStyle = new GUIStyle( EditorStyles.label );
					_listHeaderStyle.contentOffset = new Vector2( 6f, 0f );
				}
				return _listHeaderStyle;
			}
		}

		public static void DrawListHeader( Rect rect, SerializedProperty listProperty )
		{
			DrawListHeader( rect, new GUIContent( listProperty.displayName ), listProperty );
		}

		public static void DrawListHeader( Rect rect, GUIContent label, SerializedProperty listProperty, int arraySize = -1 )
		{
			GUIContent _label;
			if( arraySize < 0 && listProperty.isArray )
			{
				_label = new GUIContent
				(
					string.Format
					(
						listProperty.isExpanded ? "{0}" : "{0} ({1})",
						label.text,
						listProperty.arraySize == 0 ? "..." : string.Format( " {0} ", listProperty.arraySize )
					),
					label.image,
					label.tooltip
				);
			}
			else
			{
				_label = new GUIContent
				(
					string.Format( listProperty.isExpanded ? "{0}" : ( arraySize < 0 ? "{0} (...)" : "{0} ( {1} )" ), label.text, arraySize ),
					label.image,
					label.tooltip
				);
			}

			if( DrawListHeader( rect, _label ) )
				listProperty.isExpanded = !listProperty.isExpanded;
		}

		public static bool DrawListHeader( Rect rect, GUIContent label )
		{
			Rect bRect = new Rect( rect.x, rect.y, rect.width, rect.height + 2f );
			EditorGUI.DrawRect( bRect, new Color( 0.141f, 0.141f, 0.141f, 1f ) );
			EditorGUI.DrawRect( new Rect( bRect.x + 1f, bRect.y + 1f, bRect.width - 2f, bRect.height - 2f ), new Color( 0.208f, 0.208f, 0.208f, 1f ) );

			return GUI.Button( bRect, label, listHeaderStyle );
		}
		
		public static bool IsListExpanded( this ReorderableList reorderableList ) =>
			reorderableList.serializedProperty.isExpanded;

		public static void SetListExpanded( this ReorderableList reorderableList, bool expanded ) =>
			reorderableList.serializedProperty.isExpanded = expanded;

		public static float CalculateCollapsableListHeight( this ReorderableList reorderableList, float endSpacing = -1f )
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			if( endSpacing < 0f )
				endSpacing = lineHeight * 0.5f;

			if( reorderableList.IsListExpanded() )
				return reorderableList.GetHeight() + endSpacing;
			else
				return lineHeight + endSpacing;
		}
		
		public static void DrawCollapsableList( this ReorderableList reorderableList, ref Rect rect, GUIContent guiContent = null, float endSpacing = -1f )
		{
			if( object.Equals( null, reorderableList ) || object.Equals( null, reorderableList.serializedProperty ) )
				return;
			
			float lineHeight = EditorGUIUtility.singleLineHeight;
			if( endSpacing < 0f )
				endSpacing = lineHeight * 0.5f;

			Rect headerRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			if( reorderableList.IsListExpanded() )
			{
				float oldHeight = rect.height;
				rect.height = reorderableList.GetHeight();
				reorderableList.DoList( rect );
				rect.y += rect.height + endSpacing;
				rect.height = oldHeight;
			}
			else
			{
				rect.y += lineHeight + endSpacing;
			}

			if( object.Equals( null, guiContent ) )
			{
				guiContent = new GUIContent( reorderableList.serializedProperty.displayName );
			}

			DrawListHeader( headerRect, guiContent, reorderableList.serializedProperty );

			return;
		}

		#endregion

		////////////////////////////////
		#region BetterObjectField

		public static Object BetterObjectField( Rect rect, GUIContent label, Object obj, System.Type type, bool allowSceneObjects = false )
		{
			return EditorGUI.ObjectField( rect, label, obj, type, allowSceneObjects );
		}

		public static T BetterObjectField<T>( Rect rect, GUIContent label, Object obj, bool allowSceneObjects = false ) where T : Object
		{
			return ( T ) BetterObjectField( rect, label, obj, typeof( T ), allowSceneObjects );
		}

		public static void BetterObjectField( Rect rect, GUIContent label, SerializedProperty property, System.Type type, bool allowSceneObjects = false )
		{
			Object obj = BetterObjectField( rect, label, property.objectReferenceValue, type, allowSceneObjects );

			if( obj != property.objectReferenceValue )
			{
				property.objectReferenceValue = obj;

				GUI.changed = true;
			}
		}

		public static void BetterObjectField<T>( Rect rect, GUIContent label, SerializedProperty property, bool allowSceneObjects = false ) where T : Object
		{
			BetterObjectField( rect, label, property, typeof( T ), allowSceneObjects );
		}

		public static Object BetterObjectField( GUIContent label, Object obj, System.Type type, bool allowSceneObjects = false )
		{
			return EditorGUI.ObjectField( GetSingleLineRect(), label, obj, type, allowSceneObjects );
		}

		public static T BetterObjectField<T>( GUIContent label, Object obj, bool allowSceneObjects = false ) where T : Object
		{
			return ( T ) BetterObjectField( GetSingleLineRect(), label, obj, typeof( T ), allowSceneObjects );
		}

		public static void BetterObjectField( GUIContent label, SerializedProperty property, System.Type type, bool allowSceneObjects = false )
		{
			BetterObjectField( GetSingleLineRect(), label, property.objectReferenceValue, type, allowSceneObjects );
		}

		public static void BetterObjectField<T>( GUIContent label, SerializedProperty property, bool allowSceneObjects = false ) where T : Object
		{
			BetterObjectField<T>( GetSingleLineRect(), label, property, allowSceneObjects );
		}

		#endregion

		////////////////////////////////
		#region BetterToggleField

		private static GUIStyle _betterToggleStyle;
		private static GUIStyle betterToggleStyle
		{
			get
			{
				if( object.Equals( null, _betterToggleStyle ) )
				{
					_betterToggleStyle = new GUIStyle( EditorStyles.helpBox );
					_betterToggleStyle.stretchHeight = true;
				}

				return _betterToggleStyle;
			}
		}

		private static GUIStyle _betterToggleStyleSlim;
		private static GUIStyle betterToggleStyleSlim
		{
			get
			{
				if( object.Equals( null, _betterToggleStyleSlim ) )
				{
					_betterToggleStyleSlim = new GUIStyle( EditorStyles.helpBox );
					_betterToggleStyleSlim.fixedHeight = EditorGUIUtility.singleLineHeight * 1f;
				}

				return _betterToggleStyleSlim;
			}
		}

		public static bool BetterToggleField( GUIContent label, bool value )
		{
			return BetterToggleField( EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f ), label, value );
		}

		public static void BetterToggleField( GUIContent label, SerializedProperty property )
		{
			BetterToggleField( EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f ), label, property );
		}

		public static void BetterToggleField( Rect rect, GUIContent label, SerializedProperty property )
		{
			property.boolValue = BetterToggleField( rect, label, property.boolValue );
		}

		public static bool BetterToggleField( Rect rect, GUIContent label, bool value )
		{
			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = rect.width - 30f;

			bool slim = rect.height <= EditorGUIUtility.singleLineHeight;

			if( GUI.Button( rect, new GUIContent( "", label.tooltip ), slim ? betterToggleStyleSlim : betterToggleStyle ) )
			{
				value = !value;
				GUI.changed = true;
			}

			float lineHeight = EditorGUIUtility.singleLineHeight;

			Rect toggleRect = new Rect( rect.x + 5f, rect.y + ( rect.height - lineHeight ) * 0.5f, rect.width - 10f, lineHeight );

			bool toggle = EditorGUI.Toggle( toggleRect, label, value );

			EditorGUIUtility.labelWidth = labelWidth;

			return toggle;
		}

		#endregion

		////////////////////////////////
		#region ToggleRockerField

		private static GUIContent ToggleRockerFieldContentFalse = new GUIContent( "False" );
		private static GUIContent ToggleRockerFieldContentTrue = new GUIContent( "True" );

		private static Color[] ToggleRockerFieldSelectedBackgroundColor = new[]
		{
			Color.Lerp( Color.Lerp( Color.red, Color.yellow, 0.5f ), Color.white, 0.5f ),
			Color.Lerp( Color.Lerp( Color.cyan, Color.green, 0.5f ), Color.white, 0.5f )
		};

		public static bool ToggleRockerField( GUIContent label, bool value, GUIContent contentFalse = null, GUIContent contentTrue = null )
		{
			return ToggleRockerField( EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f ), label, value, contentFalse, contentTrue );
		}

		public static void ToggleRockerField( GUIContent label, SerializedProperty property, GUIContent contentFalse = null, GUIContent contentTrue = null )
		{
			ToggleRockerField( EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f ), label, property, contentFalse, contentTrue );
		}

		public static void ToggleRockerField( Rect rect, GUIContent label, SerializedProperty property, GUIContent contentFalse = null, GUIContent contentTrue = null )
		{
			property.boolValue = ToggleRockerField( rect, label, property.boolValue, contentFalse, contentTrue );
		}

		public static bool ToggleRockerField( Rect rect, GUIContent label, bool value, GUIContent contentFalse = null, GUIContent contentTrue = null )
		{
			if( object.Equals( null, contentFalse ) )
				contentFalse = ToggleRockerFieldContentFalse;

			if( object.Equals( null, contentTrue ) )
				contentTrue = ToggleRockerFieldContentTrue;

			bool toggle = value;

			Rect bRect = new Rect( rect.x, rect.y, rect.width * 0.5f, rect.height );

			Color backgroundColor = GUI.backgroundColor;

			if( value )
			{
				if( GUI.Button( bRect, contentFalse, EditorStyles.miniButtonLeft ) )
				{
					toggle = false;
				}
			}
			else
			{
				GUI.backgroundColor = ToggleRockerFieldSelectedBackgroundColor[ 0 ];
				EditorGUI.LabelField( bRect, contentFalse, EditorStyles.miniButtonLeft );
			}

			GUI.backgroundColor = backgroundColor;
			bRect.x += bRect.width;

			if( !value )
			{
				if( GUI.Button( bRect, contentTrue, EditorStyles.miniButtonRight ) )
				{
					toggle = true;
				}
			}
			else
			{
				GUI.backgroundColor = ToggleRockerFieldSelectedBackgroundColor[ 1 ];
				EditorGUI.LabelField( bRect, contentTrue, EditorStyles.miniButtonRight );
			}

			GUI.backgroundColor = backgroundColor;

			return toggle;
		}

		#endregion

		////////////////////////////////
		#region BetterButton

		private static GUIStyle _betterButtonStyle;
		private static GUIStyle betterButtonStyle
		{
			get
			{
				if( object.Equals( null, _betterButtonStyle ) )
				{
					_betterButtonStyle = new GUIStyle( EditorStyles.miniButton );
					_betterButtonStyle.fixedHeight = EditorGUIUtility.singleLineHeight * 1.5f;
				}

				return _betterButtonStyle;
			}
		}

		public static bool BetterButton( GUIContent label, bool repeat = false )
		{
			return BetterButton( EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f ), label, repeat );
		}

		public static bool BetterButton( Rect rect, GUIContent label, bool repeat = false )
		{
			if( repeat )
				return GUI.RepeatButton( rect, label, betterButtonStyle );
			else
				return GUI.Button( rect, label, betterButtonStyle );
		}

		#endregion

		////////////////////////////////
		#region BetterTextField

		private static GUIStyle _styleRichTextField;
		private static GUIStyle styleRichTextField
		{
			get
			{
				if( object.Equals( null, _styleRichTextField ) )
				{
					_styleRichTextField = new GUIStyle( EditorStyles.textField );
					_styleRichTextField.richText = true;
				}

				return _styleRichTextField;
			}
		}

		private static GUIStyle _styleRichTextLabel;
		private static GUIStyle styleRichTextLabel
		{
			get
			{
				if( object.Equals( null, _styleRichTextLabel ) )
				{
					_styleRichTextLabel = new GUIStyle( EditorStyles.label );
					_styleRichTextLabel.fontStyle = FontStyle.Italic;
					_styleRichTextLabel.alignment = TextAnchor.UpperLeft;
					_styleRichTextLabel.contentOffset = new Vector2( 3f, 1f );
				}

				return _styleRichTextLabel;
			}
		}

		public static void BetterTextField( GUIContent label, SerializedProperty serializedProperty, bool infieldLabel = true )
		{
			Rect rect = EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f );
			rect.height = EditorGUIUtility.singleLineHeight;

			BetterTextField( rect, label, serializedProperty, infieldLabel );
		}

		public static void BetterTextField( Rect rect, GUIContent label, SerializedProperty serializedProperty, bool infieldLabel = true )
		{
			string input = serializedProperty.stringValue;
			string output = BetterTextField( rect, label, input, infieldLabel );
			if( !string.Equals( input, output ) )
				serializedProperty.stringValue = output;
		}

		public static string BetterTextField( Rect rect, GUIContent label, string input, bool infieldLabel = true )
		{
			if( object.Equals( null, label ) )
				label = EmptyContent;

			GUIContent _label = new GUIContent( label );
			if( infieldLabel )
				_label.text = string.Empty;

			string controlName = string.Format
			(
				"BetterTextField.{0}.{1}.{2}",
				string.IsNullOrEmpty( label.text ) ? "NULL" : label.text,
				string.IsNullOrEmpty( label.tooltip ) ? "NULL" : label.tooltip,
				string.IsNullOrEmpty( input ) ? "NULL" : input
			);
			GUI.SetNextControlName( controlName );
			string output = EditorGUI.TextField( rect, _label, input, string.Equals( controlName, GUI.GetNameOfFocusedControl() ) ? EditorStyles.textField : styleRichTextField );

			if( infieldLabel && string.IsNullOrEmpty( output ) )
			{
				Color contentColor = GUI.contentColor;
				GUI.contentColor = new Color( 1f, 1f, 1f, 0.5f );
				EditorGUI.LabelField( rect, label, styleRichTextLabel );
				GUI.contentColor = contentColor;
			}

			return output;
		}

		#endregion

		////////////////////////////////
		#region BetterTextArea

		private static GUIStyle _styleRichTextArea;
		private static GUIStyle styleRichTextArea
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

		public static void BetterTextArea( GUIContent label, SerializedProperty serializedProperty, bool infieldLabel = true )
		{
			Rect rect = EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 3.5f );
			rect.height = EditorGUIUtility.singleLineHeight * 3f;

			BetterTextArea( rect, label, serializedProperty, infieldLabel );
		}

		public static void BetterTextArea( Rect rect, GUIContent label, SerializedProperty serializedProperty, bool infieldLabel = true )
		{
			string input = serializedProperty.stringValue;
			string output = BetterTextArea( rect, label, input, infieldLabel );
			if( !string.Equals( input, output ) )
				serializedProperty.stringValue = output;
		}

		public static string BetterTextArea( Rect rect, GUIContent label, string input, bool infieldLabel = true )
		{
			if( object.Equals( null, label ) )
				label = EmptyContent;

			GUIContent _label = new GUIContent( label );
			if( infieldLabel )
				_label.text = string.Empty;

			string controlName = string.Format
			(
				"BetterTextArea.{0}.{1}.{2}",
				string.IsNullOrEmpty( label.text ) ? "NULL" : label.text,
				string.IsNullOrEmpty( label.tooltip ) ? "NULL" : label.tooltip,
				string.IsNullOrEmpty( input ) ? "NULL" : input
			);
			GUI.SetNextControlName( controlName );
			string output = EditorGUI.TextField( rect, _label, input, string.Equals( controlName, GUI.GetNameOfFocusedControl() ) ? EditorStyles.textField : styleRichTextArea );

			if( infieldLabel && string.IsNullOrEmpty( output ) )
			{
				Color contentColor = GUI.contentColor;
				GUI.contentColor = new Color( 1f, 1f, 1f, 0.5f );
				EditorGUI.LabelField( rect, label, styleRichTextLabel );
				GUI.contentColor = contentColor;
			}

			return output;
		}

		#endregion

		////////////////////////////////
		#region BetterSceneField

		public static string BetterSceneField( GUIContent label, string path )
		{
			return BetterSceneField( EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f ), label, path );
		}

		public static void BetterSceneField( GUIContent label, SerializedProperty property )
		{
			BetterSceneField( EditorUtils.GetLayoutRect( EditorGUIUtility.singleLineHeight * 1.5f ), label, property );
		}

		public static void BetterSceneField( Rect rect, GUIContent label, SerializedProperty property )
		{
			property.stringValue = BetterSceneField( rect, label, property.stringValue );
		}

		public static string BetterSceneField( Rect rect, GUIContent label, string path )
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;

			string _scenePath = path;

			SceneAsset sceneAsset = string.IsNullOrWhiteSpace( _scenePath ) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>( string.Format( "Assets/{0}.unity", _scenePath ) );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			SceneAsset _sceneAsset = EditorUtils.BetterObjectField<SceneAsset>( bRect, label == default ? new GUIContent() : label, sceneAsset );

			if( _sceneAsset != sceneAsset )
			{
				sceneAsset = _sceneAsset;

				if( !Utils.IsObjectAlive( sceneAsset ) )
				{
					_scenePath = string.Empty;
				}
				else
				{
					_scenePath = AssetDatabase.GetAssetPath( sceneAsset ).Substring( 7 );
					_scenePath = _scenePath.Substring( 0, _scenePath.Length - 6 );
				}
			}

			return _scenePath;
		}

		#endregion

		////////////////////////////////
		#region BetterUnityEventField

		private static UnityEventDrawer StaticUnityEventDrawer = new UnityEventDrawer();

		public static float BetterUnityEventFieldHeight( SerializedProperty serializedProperty, float endSpacing = -1f )
		{
			if( endSpacing < 0f )
				endSpacing = EditorGUIUtility.singleLineHeight * 0.5f;

			if( serializedProperty.isExpanded )
			{
				return StaticUnityEventDrawer.GetPropertyHeight( serializedProperty, EmptyContent ) + endSpacing;
			}
			else
			{
				return EditorGUIUtility.singleLineHeight + endSpacing;
			}
		}

		public static void BetterUnityEventField( ref Rect rect, SerializedProperty serializedProperty, float endSpacing = -1f )
		{
			float oldHeight = rect.height;
			rect.height = BetterUnityEventFieldHeight( serializedProperty, endSpacing );
			BetterUnityEventField( rect, serializedProperty, endSpacing );
			rect.y += rect.height;
			rect.height = oldHeight;
		}

		public static void BetterUnityEventField( Rect rect, SerializedProperty serializedProperty, float endSpacing = -1f )
		{
			if( endSpacing < 0f )
				endSpacing = EditorGUIUtility.singleLineHeight * 0.5f;

			if( serializedProperty.isExpanded )
				StaticUnityEventDrawer.OnGUI( rect, serializedProperty, new GUIContent( serializedProperty.displayName ) );

			DrawListHeader( new Rect( rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight ), new GUIContent( serializedProperty.displayName ), serializedProperty, serializedProperty.FindPropertyRelative( "m_PersistentCalls.m_Calls" ).arraySize );
		}

		#endregion

		////////////////////////////////
		#region Foldout

		public static bool Foldout( Rect rect, bool foldout, GUIContent guiContent, GUIStyle style = default( GUIStyle ) )
		{
			if( style == default( GUIStyle ) )
				style = EditorStyles.label;

			int indentLevel = EditorGUI.indentLevel;

			EditorGUI.indentLevel = 0;

			float indentOffset = indentLevel * 15f;

			bool _foldout = EditorGUI.Foldout( new Rect( rect.x + indentOffset, rect.y, 0f, rect.height ), foldout, new GUIContent(), true );

			float labelWidth = EditorStyles.label.CalcSize( guiContent ).x + EditorStyles.label.contentOffset.x + 20f;

			Rect buttonRect = new Rect( rect.x + indentOffset, rect.y, rect.width - indentOffset, rect.height );
			if( !foldout )
				buttonRect.width = Mathf.Min( labelWidth, buttonRect.width );

			if( GUI.Button( buttonRect, guiContent, style ) )
			{
				_foldout = !foldout;
			}

			EditorGUI.indentLevel = indentLevel;

			return _foldout;
		}

		public static bool Foldout( bool foldout, GUIContent guiContent, GUIStyle style = default( GUIStyle ) )
		{
			return Foldout( EditorGUILayout.GetControlRect( GUILayout.Height( EditorGUIUtility.singleLineHeight ) ), foldout, guiContent, style );
		}

		#endregion

		////////////////////////////////
		#region Divider

		private static readonly Color DividerLabel = new Color( 1f, 1f, 1f, 0.75f );
		private static readonly Color DividerColor = new Color( 0f, 0f, 0f, 0.25f );
		private static readonly Color DividerUnderscore = new Color( 0f, 0f, 0f, 0.125f );
		public static void DrawDivider( Rect rect, GUIContent guiContent = null )
		{
			Rect bRect = new Rect( rect.x - 15f, rect.y, rect.width + 15f, rect.height );

			float textWidth = 0f;

			if( !string.IsNullOrWhiteSpace( guiContent.text ) )
			{
				textWidth = EditorStyles.boldLabel.CalcSize( guiContent ).x;
			}
			
			Rect cRect = new Rect( bRect.x, bRect.y + bRect.height * 0.5f - 1f, bRect.width, 2f );
			
			EditorGUI.DrawRect( new Rect( cRect.x, cRect.y, 20f, cRect.height ), DividerColor );

			EditorGUI.DrawRect( new Rect( cRect.x, cRect.y + cRect.height + 2f, 20f, 1f ), DividerUnderscore );

			if( textWidth > 0f )
			{
				float lineHeight = EditorGUIUtility.singleLineHeight;
				Color editorColor = GUI.color;
				GUI.color = DividerLabel;
				EditorGUI.LabelField( new Rect( bRect.x + 25f, bRect.y + bRect.height * 0.5f - lineHeight * 0.5f, textWidth, lineHeight ), new GUIContent( guiContent.text ), EditorStyles.boldLabel );
				GUI.color = editorColor;
				cRect.x += textWidth + 30f;
				cRect.width -= textWidth + 30f;
			}

			EditorGUI.DrawRect( cRect, DividerColor );

			EditorGUI.DrawRect( new Rect( cRect.x, cRect.y + cRect.height + 2f, cRect.width, 1f ), DividerUnderscore );

			if( !string.IsNullOrWhiteSpace( guiContent.tooltip ) )
				EditorGUI.LabelField( rect, new GUIContent( string.Empty, guiContent.tooltip ), GUIStyle.none );
		}

		#endregion

		////////////////////////////////
		#region BeginDisableGroup Toggle

		public static Rect BeginDisabledGroupToggle( Rect rect, SerializedProperty booleanProperty, GUIContent guiContent = null, bool midlineToggle = false, bool toggleBeforeProperty = true, bool invertBoolean = false )
		{
			float labelWidth = EditorGUIUtility.labelWidth;
			float toggleWidth = 20f;

			if( !object.Equals( null, guiContent ) )
			{
				if( string.IsNullOrWhiteSpace( guiContent.text ) )
				{
					guiContent.text = string.Empty;
				}
				else
				{
					Vector2 guiContentSize = EditorStyles.label.CalcSize( guiContent );
					toggleWidth = guiContentSize.x + 25f;
					EditorGUIUtility.labelWidth = guiContentSize.x + 5f;
					
				}
			}
			else
			{
				guiContent = new GUIContent();
			}

			Rect bRect = new Rect( rect.x + ( toggleBeforeProperty ? 0f : rect.width - toggleWidth ), rect.y, toggleWidth, midlineToggle ? rect.height : EditorGUIUtility.singleLineHeight );
			EditorGUI.PropertyField( bRect, booleanProperty, guiContent );
			
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUI.BeginDisabledGroup( invertBoolean ? booleanProperty.boolValue : !booleanProperty.boolValue );

			return new Rect( rect.x + ( toggleBeforeProperty ? toggleWidth + 5f : 0f ), rect.y, rect.width - ( toggleWidth + 5f ), rect.height );
		}

		#endregion

		////////////////////////////////
		#region MaskField

		private static Dictionary<System.Type, string[]> CachedEnumNames = new Dictionary<System.Type, string[]>();

		public static bool MaskField( Rect rect, GUIContent label, SerializedProperty maskProperty, System.Type enumType )
		{
			if( !CachedEnumNames.ContainsKey( enumType ) )
				CachedEnumNames.Add( enumType, System.Enum.GetNames( enumType ) );

			for( int i = 0, iC = CachedEnumNames[ enumType ].Length; i < iC; i++ )
				CachedEnumNames[ enumType ][ i ] = CachedEnumNames[ enumType ][ i ].Replace( '_', ' ' );

			int mask = maskProperty.intValue;

			int _mask = EditorGUI.MaskField( rect, label, mask, CachedEnumNames[ enumType ] );

			bool changed = ( _mask != mask );

			if( changed )
				maskProperty.intValue = _mask;

			return changed;
		}

		public static bool MaskField( GUIContent label, SerializedProperty maskProperty, System.Type enumType )
		{
			if( !CachedEnumNames.ContainsKey( enumType ) )
				CachedEnumNames.Add( enumType, System.Enum.GetNames( enumType ) );

			for( int i = 0, iC = CachedEnumNames[ enumType ].Length; i < iC; i++ )
				CachedEnumNames[ enumType ][ i ] = CachedEnumNames[ enumType ][ i ].Replace( '_', ' ' );

			int mask = maskProperty.intValue;

			int _mask = EditorGUILayout.MaskField( label, mask, CachedEnumNames[ enumType ] );

			bool changed = ( _mask != mask );

			if( changed )
				maskProperty.intValue = _mask;

			return changed;
		}

		#endregion

		////////////////////////////////
		#region LayerMask Field

		public static void LayerMaskField( Rect rect, GUIContent guiContent, SerializedProperty layerMaskProperty )
		{
			layerMaskProperty.intValue = EditorGUI.MaskField( rect, guiContent, layerMaskProperty.intValue, InternalEditorUtility.layers );
		}

		#endregion

		////////////////////////////////
		#region SnapSlider

		public static float snapSliderFraction = 1f;

		public static void DrawSnapSlider( SerializedProperty property, GUIContent label, float minLimit, float maxLimit, bool hideValue = false )
		{
			DrawSnapSlider( EditorUtils.GetLayoutRect(), property, label, minLimit, maxLimit, hideValue );
		}

		public static void DrawSnapSlider( Rect rect, SerializedProperty property, GUIContent label, float minLimit, float maxLimit, bool hideValue = false )
		{
			property.floatValue = DrawSnapSlider( rect, property.floatValue, label, minLimit, maxLimit, hideValue );
		}

		public static float DrawSnapSlider( Rect rect, float value, GUIContent label, float minLimit, float maxLimit, bool hideValue = false )
		{
			float _value;

			if( hideValue )
			{
				Rect bRect;
				if( !string.IsNullOrEmpty( label.text ) || !object.Equals( null, label.image ) )
					bRect = EditorGUI.PrefixLabel( rect, label );
				else
					bRect = rect;

				_value = GUI.HorizontalSlider( bRect, value, minLimit, maxLimit );
			}
			else
			{
				_value = EditorGUI.Slider( rect, label, value, minLimit, maxLimit );
			}
			//Control is bad! Ctrl+S to save will cause anything using this to snap...
			if( value != _value )
			{
				float fraction = snapSliderFraction == 0f ? 1f : 1f / Mathf.Abs( snapSliderFraction );

				_value = Event.current.control ? Mathf.Round( _value * fraction ) / fraction : _value;
			}
			return _value;
		}

		#endregion
		
		////////////////////////////////
		#region FloatFieldDraggable

		public static float FloatFieldDraggable( Rect position, Rect dragHotZone, float value, GUIStyle style = null )
		{
			if( style == null )
				style = EditorStyles.numberField;

			int controlID = GUIUtility.GetControlID( "EditorTextField".GetHashCode(), FocusType.Keyboard, position );
			System.Type editorGUIType = typeof( EditorGUI );

			System.Type RecycledTextEditorType = Assembly.GetAssembly( editorGUIType ).GetType( "UnityEditor.EditorGUI+RecycledTextEditor" );
			System.Type[] argumentTypes = new System.Type[] { RecycledTextEditorType, typeof( Rect ), typeof( Rect ), typeof( int ), typeof( float ), typeof( string ), typeof( GUIStyle ), typeof( bool ) };
			MethodInfo doFloatFieldMethod = editorGUIType.GetMethod( "DoFloatField", BindingFlags.NonPublic | BindingFlags.Static, null, argumentTypes, null );

			FieldInfo fieldInfo = editorGUIType.GetField( "s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static );
			object recycledEditor = fieldInfo.GetValue( null );

			object[] parameters = new object[] { recycledEditor, position, dragHotZone, controlID, value, "g7", style, true };

			return ( float ) doFloatFieldMethod.Invoke( null, parameters );
		}

		#endregion

		////////////////////////////////
		#region Vector2Field
		
		public static void Vector2Field( Rect rect, GUIContent guiContent, SerializedProperty serializedProperty, bool disableX = false, bool disableY = false )
		{
			Vector2 vector = serializedProperty.vector2Value;
			
			Vector2 _vector = Vector2Field( rect, guiContent, vector, disableX, disableY );

			if( ( _vector.x != vector.x ) || ( _vector.y != vector.y ) )
				serializedProperty.vector2Value = _vector;
		}

		public static Vector2 Vector2Field( Rect rect, GUIContent guiContent, Vector2 vector, bool disableX = false, bool disableY = false )
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			Rect bRect = new Rect( rect.x, rect.y + ( rect.height - lineHeight ) * 0.5f, rect.width, lineHeight );

			if( !IsEmptyContent( guiContent ) )
				bRect = EditorGUI.PrefixLabel( bRect, guiContent );

			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 15f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight );

			EditorGUI.BeginDisabledGroup( disableX );
			float x = EditorGUI.FloatField( cRect, new GUIContent( "X" ), vector.x );
			EditorGUI.EndDisabledGroup();
			cRect.x += cRect.width + 10f;

			EditorGUI.BeginDisabledGroup( disableY );
			float y = EditorGUI.FloatField( cRect, new GUIContent( "Y" ), vector.y );
			EditorGUI.EndDisabledGroup();
			bRect.y += lineHeight;

			EditorGUIUtility.labelWidth = labelWidth;

			return new Vector2( x, y );
		}

		public static void Vector2IntField( Rect rect, GUIContent guiContent, SerializedProperty serializedProperty, bool disableX = false, bool disableY = false )
		{
			Vector2Int vector = serializedProperty.vector2IntValue;

			Vector2Int _vector = Vector2IntField( rect, guiContent, vector, disableX, disableY );

			if( ( _vector.x != vector.x ) || ( _vector.y != vector.y ) )
				serializedProperty.vector2IntValue = _vector;
		}

		public static Vector2Int Vector2IntField( Rect rect, GUIContent guiContent, Vector2Int vector, bool disableX = false, bool disableY = false )
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			Rect bRect = new Rect( rect.x, rect.y + ( rect.height - lineHeight ) * 0.5f, rect.width, lineHeight );

			if( !IsEmptyContent( guiContent ) )
				bRect = EditorGUI.PrefixLabel( bRect, guiContent );

			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 15f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight );

			EditorGUI.BeginDisabledGroup( disableX );
			int x = EditorGUI.IntField( cRect, new GUIContent( "X" ), vector.x );
			EditorGUI.EndDisabledGroup();
			cRect.x += cRect.width + 10f;

			EditorGUI.BeginDisabledGroup( disableY );
			int y = EditorGUI.IntField( cRect, new GUIContent( "Y" ), vector.y );
			EditorGUI.EndDisabledGroup();
			bRect.y += lineHeight;

			EditorGUIUtility.labelWidth = labelWidth;

			return new Vector2Int( x, y );
		}

		#endregion

		////////////////////////////////
		#region ClickCopyLabel

		public static void DrawClickCopyLabel( Rect rect, string title, string text, bool showLabel = false ) =>
			DrawClickCopyLabel( rect, new GUIContent( title ), text, showLabel );
		
		public static void DrawClickCopyLabel( Rect rect, GUIContent title, string text, bool showLabel = false )
		{
			if( object.Equals( null, title ) )
				title = EmptyContent;

			Rect bRect = new Rect( rect );
			if( showLabel )
			{
				float labelWidth = bRect.width * 0.5f;
				EditorGUI.LabelField( new Rect( bRect.x, bRect.y, labelWidth, bRect.height ), title );
				bRect.x += labelWidth;
				bRect.width -= labelWidth;
			}
			
			EditorGUI.LabelField( bRect, text, EditorStyles.textField );
			string tooltip, message;
			if( string.IsNullOrEmpty( title.text ) )
			{
				tooltip = $"Copy '{text}'";
				message = $"Copied '{text}' to system clipboard.";
			}
			else
			{
				tooltip = $"Copy {title.text}";
				message = $"Copied {title.text} ( '{text}' ) to system clipboard.";
			}
				
			if( GUI.Button( rect, new GUIContent( string.Empty, tooltip ), GUIStyle.none ) )
			{
				EditorGUIUtility.systemCopyBuffer = text;
				Debug.Log( message );
			}
		}
		
		#endregion

		////////////////////////////////
		#region BetterBackground
		
		private static GUIStyle _betterBackgroundStyle;
		private static GUIStyle betterBackgroundStyle
		{
			get
			{
				if( object.Equals( null, _betterBackgroundStyle ) )
				{
					_betterBackgroundStyle = new GUIStyle( EditorStyles.helpBox );
					_betterBackgroundStyle.stretchHeight = true;
				}

				return _betterBackgroundStyle;
			}
		}

		public static Rect DrawBetterBackground( Rect rect, GUIContent label )
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;

			EditorGUI.LabelField( rect, EmptyContent, betterBackgroundStyle );

			Rect bRect;
			if( !string.IsNullOrEmpty( label.text ) )
			{
				bRect = new Rect( rect.x, rect.y - 2f, rect.width, lineHeight );
				EditorGUI.LabelField( bRect, label, EditorStyles.miniBoldLabel );
			}

			bRect = new Rect( rect.x + 5f, rect.y, rect.width - 10f, Mathf.Max( lineHeight, rect.height - lineHeight ) );
			bRect.y += ( rect.height - bRect.height ) * 0.5f;

			return bRect;
		}
		
		#endregion

		////////////////////////////////
		#region Assets

		public static bool IsCheckedOut( Object target, string assetPath = null )
		{
			if( string.IsNullOrEmpty( assetPath ) )
				assetPath = AssetDatabase.GetAssetPath( target );

			EditorUtility.SetDirty( target );

			if( Provider.isActive && ( Provider.onlineState == OnlineState.Online ) )
			{
				Asset asset = Provider.GetAssetByPath( assetPath );

				if( asset == null )
				{
					return false;
				}

				Task statusTask = Provider.Status( asset );
				statusTask.Wait();
				asset = statusTask.assetList[ 0 ];

				return ( asset.IsState( Asset.States.CheckedOutLocal ) || asset.IsState( Asset.States.AddedLocal ) );
			}

			return false;
		}

		public static void DirtyAndCheckout( Object target, string assetPath = null, bool saveAsset = false )
		{
			if( string.IsNullOrEmpty( assetPath ) )
				assetPath = AssetDatabase.GetAssetPath( target );

			EditorUtility.SetDirty( target );

			if( Provider.isActive && ( Provider.onlineState == OnlineState.Online ) )
			{
				Asset asset = Provider.GetAssetByPath( assetPath );

				Task statusTask = Provider.Status( asset );
				statusTask.Wait();
				asset = statusTask.assetList[ 0 ];

				if( !asset.IsState( Asset.States.CheckedOutLocal ) )
				{
					if( !asset.IsState( Asset.States.AddedLocal ) )
					{
						Provider.Checkout( asset, CheckoutMode.Both ).Wait();
					}
				}
			}

			if( saveAsset )
				SaveDirtyAssets();
		}

		public static void SaveDirtyAssets()
		{
			AssetDatabase.SaveAssets();

			AssetDatabase.Refresh();
		}

		public static List<T> FindAllAssetsOfType<T>() where T : Object
		{
			string typeName = typeof( T ).FullName;

			string[] assetGUIDs = AssetDatabase.FindAssets( string.Format( "t: {0}", typeName ) );

			List<T> loadedObjects = new List<T>();

			T asset = null;
			string assetPath = null;

			foreach( string assetGUID in assetGUIDs )
			{
				if( string.IsNullOrEmpty( assetGUID ) )
					continue;

				assetPath = AssetDatabase.GUIDToAssetPath( assetGUID );

				if( string.IsNullOrEmpty( assetPath ) )
					continue;

				asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );

				if( object.Equals( null, asset ) )
					continue;

				loadedObjects.Add( asset );
			}

			return loadedObjects;
		}

		#endregion

		////////////////////////////////
		#region SerializedProperty

		public static int GetIndex( this SerializedProperty prop )
		{
			return GetPropertyIndexFromPath( prop.propertyPath );
		}

		public static int GetPropertyIndexFromPath( string path )
		{
			int open = path.LastIndexOf( '[' ) + 1;
			int close = path.IndexOf( ']', open );
			string _index = path.Substring( open, close - open );

			int index;
			return int.TryParse( _index, out index ) ? index : -1;
		}

		public static void Clamp( this SerializedProperty prop, int min, int max )
		{
			int value = prop.intValue;
			if( value < min )
				prop.intValue = min;
			else if( value > max )
				prop.intValue = max;
		}

		public static void Clamp( this SerializedProperty prop, float min, float max )
		{
			float value = prop.floatValue;
			if( value < min )
				prop.floatValue = min;
			else if( value > max )
				prop.floatValue = max;
		}

		public static void Clamp( this SerializedProperty prop, Vector2Int min, Vector2Int max )
		{
			Vector2Int vector = prop.vector2IntValue;
			vector.x = Mathf.Clamp( vector.x, min.x, max.x );
			vector.y = Mathf.Clamp( vector.y, min.y, max.y );

			if( prop.vector2IntValue != vector )
				prop.vector2IntValue = vector;
		}

		public static void Clamp( this SerializedProperty prop, Vector2 min, Vector2 max )
		{
			Vector2 vector = prop.vector2Value;
			vector.x = Mathf.Clamp( vector.x, min.x, max.x );
			vector.y = Mathf.Clamp( vector.y, min.y, max.y );

			if( prop.vector2Value != vector )
				prop.vector2Value = vector;
		}

		public static void Clamp( this SerializedProperty prop, Vector3Int min, Vector3Int max )
		{
			Vector3Int vector = prop.vector3IntValue;
			vector.x = Mathf.Clamp( vector.x, min.x, max.x );
			vector.y = Mathf.Clamp( vector.y, min.y, max.y );
			vector.z = Mathf.Clamp( vector.z, min.z, max.z );

			if( prop.vector3IntValue != vector )
				prop.vector3IntValue = vector;
		}

		public static void Clamp( this SerializedProperty prop, Vector3 min, Vector3 max )
		{
			Vector3 vector = prop.vector3Value;
			vector.x = Mathf.Clamp( vector.x, min.x, max.x );
			vector.y = Mathf.Clamp( vector.y, min.y, max.y );
			vector.z = Mathf.Clamp( vector.z, min.z, max.z );

			if( prop.vector3Value != vector )
				prop.vector3Value = vector;
		}

		public static void ClampMinimum( this SerializedProperty prop, int min )
		{
			if( prop.intValue < min )
				prop.intValue = min;
		}

		public static void ClampMinimum( this SerializedProperty prop, float min )
		{
			if( prop.floatValue < min )
				prop.floatValue = min;
		}

		public static void ClampMinimum( this SerializedProperty prop, Vector2Int min, bool ignoreX = false, bool ignoreY = false )
		{
			Vector2Int vector = prop.vector2IntValue;
			if( !ignoreX )
				vector.x = Mathf.Max( vector.x, min.x );
			if( !ignoreY )
				vector.y = Mathf.Max( vector.y, min.y );

			if( prop.vector2IntValue != vector )
				prop.vector2IntValue = vector;
		}

		public static void ClampMinimum( this SerializedProperty prop, Vector2 min, bool ignoreX = false, bool ignoreY = false )
		{
			Vector2 vector = prop.vector2Value;
			if( !ignoreX )
				vector.x = Mathf.Max( vector.x, min.x );
			if( !ignoreY )
				vector.y = Mathf.Max( vector.y, min.y );

			if( prop.vector2Value != vector )
				prop.vector2Value = vector;
		}

		public static void ClampMinimum( this SerializedProperty prop, Vector3Int min, bool ignoreX = false, bool ignoreY = false, bool ignoreZ = false )
		{
			Vector3Int vector = prop.vector3IntValue;
			if( !ignoreX )
				vector.x = Mathf.Max( vector.x, min.x );
			if( !ignoreY )
				vector.y = Mathf.Max( vector.y, min.y );
			if( !ignoreZ )
				vector.z = Mathf.Max( vector.z, min.z );

			if( prop.vector3IntValue != vector )
				prop.vector3IntValue = vector;
		}

		public static void ClampMinimum( this SerializedProperty prop, Vector3 min, bool ignoreX = false, bool ignoreY = false, bool ignoreZ = false )
		{
			Vector3 vector = prop.vector3Value;
			if( !ignoreX )
				vector.x = Mathf.Max( vector.x, min.x );
			if( !ignoreY )
				vector.y = Mathf.Max( vector.y, min.y );
			if( !ignoreZ )
				vector.z = Mathf.Max( vector.z, min.z );

			if( prop.vector3Value != vector )
				prop.vector3Value = vector;
		}

		public static void ClampMaximum( this SerializedProperty prop, int max )
		{
			if( prop.intValue > max )
				prop.intValue = max;
		}

		public static void ClampMaximum( this SerializedProperty prop, float max )
		{
			if( prop.floatValue > max )
				prop.floatValue = max;
		}

		public static void ClampMaximum( this SerializedProperty prop, Vector2Int max, bool ignoreX = false, bool ignoreY = false )
		{
			Vector2Int vector = prop.vector2IntValue;
			if( !ignoreX )
				vector.x = Mathf.Min( vector.x, max.x );
			if( !ignoreY )
				vector.y = Mathf.Min( vector.y, max.y );

			if( prop.vector2IntValue != vector )
				prop.vector2IntValue = vector;
		}

		public static void ClampMaximum( this SerializedProperty prop, Vector2 max, bool ignoreX = false, bool ignoreY = false )
		{
			Vector2 vector = prop.vector2Value;
			if( !ignoreX )
				vector.x = Mathf.Min( vector.x, max.x );
			if( !ignoreY )
				vector.y = Mathf.Min( vector.y, max.y );

			if( prop.vector2Value != vector )
				prop.vector2Value = vector;
		}

		public static void ClampMaximum( this SerializedProperty prop, Vector3Int max, bool ignoreX = false, bool ignoreY = false, bool ignoreZ = false )
		{
			Vector3Int vector = prop.vector3IntValue;
			if( !ignoreX )
				vector.x = Mathf.Min( vector.x, max.x );
			if( !ignoreY )
				vector.y = Mathf.Min( vector.y, max.y );
			if( !ignoreZ )
				vector.z = Mathf.Min( vector.z, max.z );

			if( prop.vector3IntValue != vector )
				prop.vector3IntValue = vector;
		}

		public static void ClampMaximum( this SerializedProperty prop, Vector3 max, bool ignoreX = false, bool ignoreY = false, bool ignoreZ = false )
		{
			Vector3 vector = prop.vector3Value;
			if( !ignoreX )
				vector.x = Mathf.Min( vector.x, max.x );
			if( !ignoreY )
				vector.y = Mathf.Min( vector.y, max.y );
			if( !ignoreZ )
				vector.z = Mathf.Min( vector.z, max.z );

			if( prop.vector3Value != vector )
				prop.vector3Value = vector;
		}

		public static void Normalize( this SerializedProperty prop )
		{
			switch( prop.propertyType )
			{
				case SerializedPropertyType.Vector2:

					Vector2 vector2 = prop.vector2Value;
					vector2.Normalize();

					if( prop.vector2Value != vector2 )
						prop.vector2Value = vector2;

					break;

				case SerializedPropertyType.Vector3:

					Vector3 vector3 = prop.vector3Value;
					vector3.Normalize();

					if( prop.vector3Value != vector3 )
						prop.vector3Value = vector3;

					break;

				case SerializedPropertyType.Vector4:

					Vector4 vector4 = prop.vector4Value;
					vector4.Normalize();

					if( prop.vector4Value != vector4 )
						prop.vector4Value = vector4;

					break;
			}
		}

		public static void Truncate( this SerializedProperty prop, int characterLimit )
		{
			if( prop.stringValue.Length > characterLimit )
				prop.stringValue = prop.stringValue.Substring( 0, characterLimit );
		}

		public static void ToLower( this SerializedProperty prop ) => prop.stringValue = prop.stringValue.ToLowerInvariant();
		public static void ToUpper( this SerializedProperty prop ) => prop.stringValue = prop.stringValue.ToUpperInvariant();

		public static SerializedProperty GetParent( this SerializedProperty property )
		{
			string propertyPath = property.propertyPath;
			int lastBracket = propertyPath.LastIndexOf( '[' );
			int lastPeriod = propertyPath.LastIndexOf( '.' );
			if( lastBracket < 0 )
			{
				if( lastPeriod >= 0 )
				{
					propertyPath = propertyPath.Substring( 0, lastPeriod );
				}
			}
			else
			{
				if( lastPeriod < 0 )
				{
					propertyPath = propertyPath.Substring( 0, lastBracket - 5 );
				}
				else
				{
					propertyPath = propertyPath.Substring( 0, lastPeriod < lastBracket ? lastBracket - 5 : lastPeriod );
				}
			}
			return property.serializedObject.FindProperty( propertyPath );
		}

		public static object GetValue( this SerializedProperty prop )
		{
			if( prop == null ) return null;

			string path = prop.propertyPath.Replace( ".Array.data[", "[" );
			object obj = prop.serializedObject.targetObject;
			string[] elements = path.Split( '.' );
			foreach( string element in elements )
			{
				if( element.Contains( "[" ) )
				{
					string elementName = element.Substring( 0, element.IndexOf( "[" ) );
					obj = GetValueWithReflection( obj, elementName, GetPropertyIndexFromPath( element ) );
				}
				else
				{
					obj = GetValueWithReflection( obj, element );
				}
			}
			return obj;
		}

		public static void SetValue( this SerializedProperty prop, object value )
		{
			string path = prop.propertyPath.Replace( ".Array.data[", "[" );
			object obj = prop.serializedObject.targetObject;
			List<string> elements = new List<string>( path.Split( '.' ) );
			elements.RemoveAt( elements.Count - 1 );
			foreach( string element in elements )
			{
				if( element.Contains( "[" ) )
				{
					string elementName = element.Substring( 0, element.IndexOf( "[" ) );
					obj = GetValueWithReflection( obj, elementName, GetPropertyIndexFromPath( element ) );
				}
				else
				{
					obj = GetValueWithReflection( obj, element );
				}
			}

			if( object.Equals( null, obj ) ) return;

			try
			{
				string element = elements.Count == 0 ? string.Empty : elements[ elements.Count - 1 ];

				if( element.Contains( "[" ) )
				{
					System.Type tp = obj.GetType();
					string elementName = element.Substring( 0, element.IndexOf( "[" ) );
					int index = GetPropertyIndexFromPath( element );
					FieldInfo field = tp.GetField( elementName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					IList arr = field.GetValue( obj ) as IList;
					arr[ index ] = value;
				}
				else
				{
					System.Type tp = obj.GetType();
					FieldInfo field = tp.GetField( element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					if( !object.Equals( null, field ) )
					{
						field.SetValue( obj, value );
					}
				}
			}
			catch
			{
				return;
			}
		}

		private static object GetValueWithReflection( object source, string name )
		{
			if( object.Equals( null, source ) )
				return null;

			System.Type type = source.GetType();

			while( !object.Equals( null, type ) )
			{
				FieldInfo f = type.GetField( name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				if( !object.Equals( null, f ) )
					return f.GetValue( source );

				PropertyInfo p = type.GetProperty( name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase );
				if( !object.Equals( null, p ) )
					return p.GetValue( source, null );

				type = type.BaseType;
			}
			return null;
		}

		private static object GetValueWithReflection( object source, string name, int index )
		{
			IEnumerable enumerable = GetValueWithReflection( source, name ) as IEnumerable;

			if( object.Equals( null, enumerable ) )
				return null;

			IEnumerator enm = enumerable.GetEnumerator();

			for( int i = 0; i <= index; i++ )
			{
				if( !enm.MoveNext() )
					return null;
			}

			return enm.Current;
		}

		public static void ResetValue( this SerializedProperty property, bool children = false )
		{
			if( children && property.hasChildren )
			{
				string end = property.GetEndProperty().propertyPath;
				SerializedProperty next = property;

				while( !object.Equals( null, next ) )
				{
					if( next.propertyPath == end )
						break;

					next.ResetValue();

					if( !next.Next( true ) )
						break;
				}
			}

			if( object.Equals( null, property ) || string.IsNullOrEmpty( property.propertyPath ) )
				return;

			switch( property.propertyType )
			{
				default:
					break;
				//case SerializedPropertyType.Generic:
				//break;
				case SerializedPropertyType.Integer:
					property.intValue = 0;
					break;
				case SerializedPropertyType.Boolean:
					property.boolValue = false;
					break;
				case SerializedPropertyType.Float:
					property.floatValue = 0f;
					break;
				case SerializedPropertyType.String:
					property.stringValue = string.Empty;
					break;
				case SerializedPropertyType.Color:
					property.colorValue = Color.clear;
					break;
				case SerializedPropertyType.ObjectReference:
					property.objectReferenceValue = null;
					break;
				case SerializedPropertyType.LayerMask:
					property.intValue = 0;
					break;
				case SerializedPropertyType.Enum:
					property.enumValueIndex = 0;
					break;
				case SerializedPropertyType.Vector2:
					property.vector2Value = Vector2.zero;
					break;
				case SerializedPropertyType.Vector3:
					property.vector3Value = Vector3.zero;
					break;
				case SerializedPropertyType.Vector4:
					property.vector4Value = Vector4.zero;
					break;
				case SerializedPropertyType.Rect:
					property.rectValue = Rect.zero;
					break;
				case SerializedPropertyType.ArraySize:
					property.arraySize = 0;
					break;
				//case SerializedPropertyType.Character:
				//break;
				case SerializedPropertyType.AnimationCurve:
					property.animationCurveValue = new AnimationCurve();
					break;
				case SerializedPropertyType.Bounds:
					property.boundsValue = new Bounds();
					break;
				//case SerializedPropertyType.Gradient:
				//break;
				case SerializedPropertyType.Quaternion:
					property.quaternionValue = Quaternion.identity;
					break;
				case SerializedPropertyType.ExposedReference:
					property.objectReferenceValue = null;
					break;
				//case SerializedPropertyType.FixedBufferSize:
				//break;
				case SerializedPropertyType.Vector2Int:
					property.vector2IntValue = Vector2Int.zero;
					break;
				case SerializedPropertyType.Vector3Int:
					property.vector3IntValue = Vector3Int.zero;
					break;
				case SerializedPropertyType.RectInt:
					property.rectIntValue = new RectInt();
					break;
				case SerializedPropertyType.BoundsInt:
					property.boundsIntValue = new BoundsInt();
					break;
				case SerializedPropertyType.ManagedReference:
					property.objectReferenceValue = null;
					break;
			}
		}

		#endregion

		////////////////////////////////
		#region InheritedEditor

		public class InheritedEditor : Editor
		{
			public static float lineHeight;
			public static float labelWidth;

			public static GUIContent emptyContent;

			static InheritedEditor()
			{
				emptyContent = new GUIContent();
			}

			public virtual float inspectorLeadingOffset => 0f;
			public virtual float inspectorPostInspectorOffset => 0f;
			public virtual float inspectorTrailingOffset => 0f;

			public virtual void Setup() { }
			public virtual void Cleanup() { }
			public virtual void DrawInspectorLayout() { }
			public virtual void DrawPostInspectorLayout() { }

			public virtual void DrawInspector( ref Rect rect ) { }
			public virtual void DrawPostInspector( ref Rect rect ) { }

			public virtual bool saveAssetsOnDisable => false;

			public bool multiTarget = false;

			public Dictionary<string, SerializedProperty> propertyLookup = new Dictionary<string, SerializedProperty>();

			public Dictionary<SerializedProperty, Dictionary<string, SerializedProperty>> subPropertyLookup = new Dictionary<SerializedProperty, Dictionary<string, SerializedProperty>>();

			public SerializedProperty this[ string propertyPath ]
			{
				get
				{
					if( string.IsNullOrEmpty( propertyPath ) )
						return null;

					if( !propertyLookup.ContainsKey( propertyPath ) )
					{
						SerializedProperty property = serializedObject.FindProperty( propertyPath );

						propertyLookup.Add( propertyPath, property );

						return property;
					}

					return propertyLookup[ propertyPath ];
				}
			}

			public SerializedProperty this[ SerializedProperty property, string propertyPath ]
			{
				get
				{
					if( object.Equals( null, property ) )
						return null;

					if( string.IsNullOrEmpty( propertyPath ) )
						return null;

					if( !subPropertyLookup.ContainsKey( property ) )
					{
						subPropertyLookup.Add( property, new Dictionary<string, SerializedProperty>() );

						SerializedProperty subProperty = property.FindPropertyRelative( propertyPath );

						subPropertyLookup[ property ].Add( propertyPath, subProperty );

						return subProperty;
					}
					else
					{
						if( !subPropertyLookup[ property ].ContainsKey( propertyPath ) )
						{
							SerializedProperty subProperty = property.FindPropertyRelative( propertyPath );

							subPropertyLookup[ property ].Add( propertyPath, subProperty );

							return subProperty;
						}
						else
						{
							return subPropertyLookup[ property ][ propertyPath ];
						}
					}
				}
			}

			public virtual void OnEnable()
			{
				labelWidth = EditorGUIUtility.labelWidth;
				lineHeight = EditorGUIUtility.singleLineHeight;

				Setup();
			}

			public virtual void OnDisable()
			{
				Cleanup();

				propertyLookup.Clear();

				if( needsSave && saveAssetsOnDisable && !EditorApplication.isCompiling && !EditorApplication.isUpdating )
				{
					AssetDatabase.SaveAssets();
				}
			}

			private bool needsSave;
			private float lastSave = 0f;

			public virtual float GetEditorHeight() { return GetInspectorHeight() + inspectorPostInspectorOffset + GetPostInspectorHeight(); }

			public virtual float GetInspectorHeight() { return 0f; }

			public virtual float GetPostInspectorHeight() { return 0f; }

			public override void OnInspectorGUI()
			{
				multiTarget = ( targets.Length > 1 );

				GUILayout.Space( inspectorLeadingOffset );

				float rectHeight = GetEditorHeight();

				if( rectHeight <= 0f )
				{
					DrawEditorLayout();
				}
				else
				{
					DrawEditor( EditorGUILayout.GetControlRect( GUILayout.Height( rectHeight ) ) );
				}

				GUILayout.Space( inspectorTrailingOffset );
			}

			public void DrawEditorLayout()
			{
				serializedObject.Update();

				EditorGUI.BeginChangeCheck();

				DrawInspectorLayout();

				GUILayout.Space( inspectorPostInspectorOffset );

				DrawPostInspectorLayout();

				bool changed = EditorGUI.EndChangeCheck();

				serializedObject.ApplyModifiedProperties();

				needsSave = needsSave || changed;

				if( needsSave && ( lastSave < 0f || ( Time.time - lastSave > 30f ) ) )
				{
					AssetDatabase.SaveAssets();
					needsSave = false;
					lastSave = Time.time;
				}
			}

			public void DrawEditor( Rect rect )
			{
				Rect bRect = new Rect( rect.x, rect.y, rect.width, GetInspectorHeight() );

				serializedObject.Update();

				EditorGUI.BeginChangeCheck();

				DrawInspector( ref bRect );
				
				bRect.height = GetPostInspectorHeight();

				if( bRect.height > 0f )
				{
					bRect.y += inspectorPostInspectorOffset;

					DrawPostInspector( ref bRect );
				}

				bool changed = EditorGUI.EndChangeCheck();

				serializedObject.ApplyModifiedProperties();

				needsSave = needsSave || changed;

				if( needsSave && ( lastSave < 0f || ( Time.time - lastSave > 30f ) ) )
				{
					AssetDatabase.SaveAssets();
					needsSave = false;
					lastSave = Time.time;
				}
			}
		}

		#endregion

		////////////////////////////////
		#region InheritedPropertyDrawer

		public class InheritedPropertyDrawer : PropertyDrawer
		{
			public static float lineHeight = 0f;
			public static float labelWidth = 0f;
			public static int indentLevel = 0;

			////////////////////////////////

			public enum LabelMode
			{
				Hidden,
				Inline,
				Header,
				Foldout
			}

			public virtual LabelMode labelMode { get { return LabelMode.Hidden; } }

			public virtual float CalculatePropertyHeight( ref SerializedProperty property )
			{
				lineHeight = EditorGUIUtility.singleLineHeight;
				labelWidth = EditorGUIUtility.labelWidth;
				indentLevel = EditorGUI.indentLevel;

				return 0f;
			}

			public virtual float offsetHeader { get { return 0f; } }
			public virtual float offsetFooterPreGUI { get { return 0f; } }
			public virtual float offsetFooterGUI { get { return 0f; } }
			public virtual float offsetFooterPostGUI { get { return 0f; } }
			public virtual float offsetFoldoutGUI { get { return lineHeight * 0.5f; } }

			public virtual float indentPreGUI { get { return 0f; } }
			public virtual float indentGUI { get { return 0f; } }
			public virtual float indentPostGUI { get { return 0f; } }

			public virtual GUIStyle headerGUIStyle { get { return EditorStyles.boldLabel; } }
			public virtual GUIStyle foldoutGUIStyle { get { return EditorStyles.label; } }

			public virtual void DrawPreGUI( ref Rect rect, ref SerializedProperty property ) { }
			public virtual void DrawGUI( ref Rect rect, ref SerializedProperty property ) { }
			public virtual void DrawPostGUI( ref Rect rect, ref SerializedProperty property ) { }

			public virtual void ExecutePostGUI( ref SerializedProperty property ) { }

			////////////////////////////////

			private static Dictionary<string, ReorderableList> _listCache = null;
			protected static Dictionary<string, ReorderableList> listCache
			{
				get
				{
					if( object.Equals( null, _listCache ) )
					{
						_listCache = new Dictionary<string, ReorderableList>();
					}

					return _listCache;
				}
			}

			public delegate void AddElementCallback( SerializedProperty property );

			protected static ReorderableList GetList( SerializedProperty property, AddElementCallback addElementCallback = null, ReorderableElementHeightExpanded elementHeightCallback = null, ReorderableElementGUIExpanded drawElementCallback = null )
			{
				if( object.Equals( null, property ) )
					return null;

				if( !listCache.ContainsKey( property.propertyPath ) )
				{
					ReorderableList reorderableList = EditorUtils.CreateReorderableList
					(
						property,
						object.Equals( null, elementHeightCallback ) ? new ReorderableElementHeightExpanded( ( SerializedProperty list, int index, SerializedProperty element ) =>
						{
							return EditorGUI.GetPropertyHeight( element, true ) + lineHeight * 0.5f;
						} )
						: elementHeightCallback,
						object.Equals( null, drawElementCallback ) ? new ReorderableElementGUIExpanded( ( Rect rect, SerializedProperty list, int index, SerializedProperty element, bool isActive, bool isFocussed ) =>
						{
							Rect bRect = new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, rect.height - lineHeight * 0.5f );

							if( element.hasVisibleChildren )
							{
								EditorGUI.PropertyField( bRect, element, true );
							}
							else
							{
								EditorGUI.PropertyField( bRect, element, new GUIContent() );
							}

						} )
						: drawElementCallback
					);

					reorderableList.onAddCallback = ( ReorderableList _list ) =>
					{
						SerializedProperty list = _list.serializedProperty;

						int arraySize = list.arraySize;

						list.InsertArrayElementAtIndex( arraySize );
						SerializedProperty element = list.GetArrayElementAtIndex( arraySize );

						element.ResetValue( true );

						if( !object.Equals( null, addElementCallback ) )
							addElementCallback.Invoke( element );
					};

					listCache.Add( property.propertyPath, reorderableList );
				}

				try
				{
					listCache[ property.propertyPath ].GetHeight();
				}
				catch( System.NullReferenceException )
				{
					listCache.Remove( property.propertyPath );

					return GetList( property, addElementCallback, elementHeightCallback, drawElementCallback );
				}

				return listCache[ property.propertyPath ];
			}

			////////////////////////////////

			public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
			{
				lineHeight = EditorGUIUtility.singleLineHeight;
				labelWidth = EditorGUIUtility.labelWidth;
				indentLevel = EditorGUI.indentLevel;

				float propertyHeight = 0f;

				switch( labelMode )
				{
					default:
						propertyHeight += CalculatePropertyHeight( ref property );
						break;

					case LabelMode.Header:
						propertyHeight += lineHeight;
						propertyHeight += offsetFoldoutGUI;
						propertyHeight += CalculatePropertyHeight( ref property );
						break;

					case LabelMode.Foldout:
						propertyHeight += lineHeight;
						if( property.isExpanded )
						{
							propertyHeight += offsetFoldoutGUI;
							propertyHeight += CalculatePropertyHeight( ref property );
						}
						break;
				}

				propertyHeight += offsetHeader;
				propertyHeight += offsetFooterPreGUI;
				propertyHeight += offsetFooterGUI;
				propertyHeight += offsetFooterPostGUI;

				return propertyHeight;
			}

			protected void OffsetRect( ref Rect rect, float amount )
			{
				rect.y += amount;
				rect.height -= amount;
			}

			protected void IndentRect( ref Rect rect, float amount )
			{
				rect.x += amount;
				rect.width -= amount;
			}

			public virtual void DrawFoldoutOverlay( Rect totalRect, Rect unusedRect, SerializedProperty property )
			{
				if( GUI.Button( unusedRect, EditorUtils.EmptyContent, GUIStyle.none ) )
					property.isExpanded = !property.isExpanded;
			}

			public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
			{
				lineHeight = EditorGUIUtility.singleLineHeight;
				labelWidth = EditorGUIUtility.labelWidth;
				indentLevel = EditorGUI.indentLevel;

				EditorGUI.indentLevel = 0;

				Rect rect = new Rect( position.x + ( 15f * indentLevel ), position.y, position.width - ( 15f * indentLevel ), lineHeight );

				bool drawGUI = true;

				switch( labelMode )
				{
					case LabelMode.Hidden:
						break;
					case LabelMode.Inline:
						rect = EditorGUI.PrefixLabel( rect, label );
						break;
					case LabelMode.Header:
						EditorGUI.LabelField( rect, label, headerGUIStyle );
						rect = new Rect( rect.x + 15f, rect.y + lineHeight + offsetFoldoutGUI, rect.width - 15f, lineHeight );
						break;
					case LabelMode.Foldout:
						property.isExpanded = EditorUtils.Foldout( rect, property.isExpanded, label, foldoutGUIStyle );
						if( property.isExpanded )
						{
							rect = new Rect( rect.x + 15f, rect.y + lineHeight + offsetFoldoutGUI, rect.width - 15f, lineHeight );
						}
						else
						{
							float labelWidth = EditorStyles.label.CalcSize( label ).x + EditorStyles.label.contentOffset.x + 20f;
							DrawFoldoutOverlay( rect, new Rect( rect.x + labelWidth, rect.y + 1f, rect.width - labelWidth, rect.height ), property );

							drawGUI = false;
						}
						break;
				}

				if( drawGUI )
				{
					rect.y += offsetHeader;

					IndentRect( ref rect, indentPreGUI );
					DrawPreGUI( ref rect, ref property );
					IndentRect( ref rect, -indentPreGUI );

					rect.y += offsetFooterPreGUI;

					IndentRect( ref rect, indentGUI );
					DrawGUI( ref rect, ref property );
					IndentRect( ref rect, -indentGUI );

					rect.y += offsetFooterGUI;

					IndentRect( ref rect, indentPostGUI );
					DrawPostGUI( ref rect, ref property );
					IndentRect( ref rect, -indentPostGUI );

					rect.y += offsetFooterPostGUI;
				}

				EditorGUI.indentLevel = indentLevel;

				ExecutePostGUI( ref property );
			}

		}

		#endregion

		////////////////////////////////
		#region LayoutRects

		public static Rect GetLayoutRect( float lineHeight = -1f )
		{
			return EditorGUILayout.GetControlRect( GUILayout.Height( lineHeight < 0f ? EditorGUIUtility.singleLineHeight : lineHeight ) );
		}

		public static void Space( float lineHeight = -1f )
		{
			GUILayout.Space( lineHeight < 0f ? EditorGUIUtility.singleLineHeight * 0.5f : lineHeight );
		}

		#endregion

		////////////////////////////////
		#region Build Helper

		public static BuildResult BuildHelper( string title, BuildTarget buildTarget, bool debug, string[] sceneAssetPaths, string defaultDirectory = null )
		{
			if( string.IsNullOrEmpty( defaultDirectory ) )
				defaultDirectory = System.IO.Directory.GetParent( Application.dataPath ).FullName;
			
			string path = EditorUtility.SaveFilePanel( "Build Destination", defaultDirectory, title, "exe" );

			if( string.IsNullOrEmpty( path ) )
				return BuildResult.Cancelled;

			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();

			buildPlayerOptions.scenes = sceneAssetPaths;

			buildPlayerOptions.locationPathName = path;
			buildPlayerOptions.target = buildTarget;
			buildPlayerOptions.options = debug ? ( BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ShowBuiltPlayer | BuildOptions.ConnectWithProfiler ) : BuildOptions.ShowBuiltPlayer;

			BuildReport buildReport = BuildPipeline.BuildPlayer( buildPlayerOptions );
			BuildSummary buildSummary = buildReport.summary;
			
			if( buildSummary.result == BuildResult.Succeeded )
				Debug.Log
				(
					$"{title} Build Succeeded!\n" +
					$"Size: {(buildSummary.totalSize * 0.000001):0.00} MB\n" +
					$"Time Elapsed: {buildSummary.totalTime.Hours:00}:{buildSummary.totalTime.Minutes:00}:{buildSummary.totalTime.Seconds:00}\n" +
					$"Warnings: {buildSummary.totalWarnings}\n" +
					$"Path: {buildSummary.outputPath}"
				);
			else if( buildSummary.result == BuildResult.Failed )
				Debug.LogError( "Build Encounter Server Failed!" );

			return buildSummary.result;
		}

		#endregion
		
		////////////////////////////////
		#region Input Manager
		
		public class InputManagerEntry
	    {
	        public enum Kind { KeyOrButton, Mouse, Axis }
	        public enum Axis { X, Y, Third, Fourth, Fifth, Sixth, Seventh, Eigth }
	        public enum Joy { All, First, Second }

	        public string   name = "";
	        public string   desc = "";
	        public string   btnNegative = "";
	        public string   btnPositive = "";
	        public string   altBtnNegative = "";
	        public string   altBtnPositive = "";
	        public float    gravity = 0.0f;
	        public float    deadZone = 0.0f;
	        public float    sensitivity = 0.0f;
	        public bool     snap = false;
	        public bool     invert = false;
	        public Kind     kind = Kind.Axis;
	        public Axis     axis = Axis.X;
	        public Joy      joystick = Joy.All;
	    }

	    public class InputRegistering
	    {
	        static bool InputAlreadyRegistered(string name, InputManagerEntry.Kind kind, SerializedProperty spAxes)
	        {
	            for (var i = 0; i < spAxes.arraySize; ++i)
	            {
	                var spAxis = spAxes.GetArrayElementAtIndex(i);
	                var axisName = spAxis.FindPropertyRelative("m_Name").stringValue;
	                var kindValue = spAxis.FindPropertyRelative("type").intValue;
	                if (axisName == name && (int)kind == kindValue)
	                    return true;
	            }

	            return false;
	        }

			static bool UpdateExistingEntry( string name, InputManagerEntry.Kind kind, SerializedProperty spAxes, InputManagerEntry entry )
			{
				for (var i = 0; i < spAxes.arraySize; ++i)
				{
					var spAxis = spAxes.GetArrayElementAtIndex(i);
					var axisName = spAxis.FindPropertyRelative("m_Name").stringValue;
					var kindValue = spAxis.FindPropertyRelative("type").intValue;
					if( axisName == name && ( int ) kind == kindValue )
					{
						UpdateEntry( spAxis, entry );
						return true;
					}
				}

				return false;
			}

			static void UpdateEntry( SerializedProperty spAxis, InputManagerEntry entry )
			{
				spAxis.FindPropertyRelative("m_Name").stringValue = entry.name;
				spAxis.FindPropertyRelative("descriptiveName").stringValue = entry.desc;
				spAxis.FindPropertyRelative("negativeButton").stringValue = entry.btnNegative;
				spAxis.FindPropertyRelative("altNegativeButton").stringValue = entry.altBtnNegative;
				spAxis.FindPropertyRelative("positiveButton").stringValue = entry.btnPositive;
				spAxis.FindPropertyRelative("altPositiveButton").stringValue = entry.altBtnPositive;
				spAxis.FindPropertyRelative("gravity").floatValue = entry.gravity;
				spAxis.FindPropertyRelative("dead").floatValue = entry.deadZone;
				spAxis.FindPropertyRelative("sensitivity").floatValue = entry.sensitivity;
				spAxis.FindPropertyRelative("snap").boolValue = entry.snap;
				spAxis.FindPropertyRelative("invert").boolValue = entry.invert;
				spAxis.FindPropertyRelative("type").intValue = (int)entry.kind;
				spAxis.FindPropertyRelative("axis").intValue = (int)entry.axis;
				spAxis.FindPropertyRelative("joyNum").intValue = (int)entry.joystick;
			}

	        static void WriteEntry(SerializedProperty spAxes, InputManagerEntry entry)
	        {
	            if (UpdateExistingEntry(entry.name, entry.kind, spAxes,entry))
	                return;

	            spAxes.InsertArrayElementAtIndex(spAxes.arraySize);
				UpdateEntry( spAxes.GetArrayElementAtIndex(spAxes.arraySize - 1), entry );
			}

			private static SerializedObject GetInputManager()
			{
				var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
				// Temporary fix. This happens some time with HDRP init when it's called before asset database is initialized (probably related to package load order).
				if (assets.Length == 0)
					return null;

				var inputManager = assets[0];

				// Wrap in serialized object
				return new SerializedObject(inputManager);
			}

			public static bool HasInput( string name, InputManagerEntry.Kind kind )
			{
				var soInputManager = GetInputManager();

				if( soInputManager == null )
					return false;
				
				return InputAlreadyRegistered( name, kind, soInputManager.FindProperty( "m_Axes" ) );
			}

	        public static void RegisterInputs(List<InputManagerEntry> entries)
	        {
	            // Grab reference to input manager
				var soInputManager = GetInputManager();

				if( soInputManager == null )
					return;
				
	            var spAxes = soInputManager.FindProperty("m_Axes");

	            foreach (InputManagerEntry entry in entries)
	            {
	                WriteEntry(spAxes, entry);
	            }

	            // Commit
	            soInputManager.ApplyModifiedProperties();
	        }
	    }
		
		#endregion
	}
}

#endif
	  