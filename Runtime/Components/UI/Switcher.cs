using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
	public class Switcher : MonoBehaviour
	{
		[System.Serializable]
		public class SwitchObject
		{
			public GameObject gameObject;

			[Space( 5f )]
			public UnityEvent onEnable = new UnityEvent();

			[Space( 5f )]
			public UnityEvent onDisable = new UnityEvent();
		}
#if UNITY_EDITOR
		[CustomPropertyDrawer( typeof( SwitchObject ) )]
		public class SwitchObjectPropertyDrawer : EditorUtils.InheritedPropertyDrawer
		{
			public override float CalculatePropertyHeight( ref SerializedProperty property )
			{
				float height = base.CalculatePropertyHeight( ref property );

				height += lineHeight * 1.5f;

				height += EditorUtils.BetterUnityEventFieldHeight( property.FindPropertyRelative( "onEnable" ) ) + lineHeight * 0.5f;
				height += EditorUtils.BetterUnityEventFieldHeight( property.FindPropertyRelative( "onDisable" ) );

				return height;
			}

			public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
			{
				base.DrawGUI( ref rect, ref property );

				Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

				string gameObjectLabel = "Game Object";
				string propertyPath = property.propertyPath;
				if( propertyPath.EndsWith( "]" ) )
				{
					int lastOpenBracket = propertyPath.LastIndexOf( '[' );
					int propertyIndex = -1;
					if( int.TryParse( propertyPath.Substring( lastOpenBracket + 1, propertyPath.Length - ( lastOpenBracket + 2 ) ), out propertyIndex ) )
					{
						gameObjectLabel = string.Format( "[ {0} ]: {1}", propertyIndex, gameObjectLabel );
					}
				}

				EditorUtils.BetterObjectField( bRect, new GUIContent( gameObjectLabel ), property.FindPropertyRelative( "gameObject" ), typeof( GameObject ), true );

				bRect.y += lineHeight * 1.5f;

				SerializedProperty onEnable = property.FindPropertyRelative( "onEnable" );
				SerializedProperty onDisable = property.FindPropertyRelative( "onDisable" );

				bRect.height = EditorUtils.BetterUnityEventFieldHeight( onEnable );
				EditorUtils.BetterUnityEventField( bRect, onEnable );

				bRect.y += bRect.height + lineHeight * 0.5f;

				bRect.height = EditorUtils.BetterUnityEventFieldHeight( onDisable );
				EditorUtils.BetterUnityEventField( bRect, onDisable );

				bRect.y += bRect.height;

				rect.y = bRect.y;
			}
		}
#endif

		private int selectedIndex = -1;

		public bool allowNone = true;
		public int defaultIndex = -1;

		public bool toggleOnReselect = false;

		public List<SwitchObject> switchObjects = new List<SwitchObject>();

		private void OnEnable()
		{
			selectedIndex = Mathf.Clamp( defaultIndex, allowNone ? -1 : 0, Mathf.Max( 0, switchObjects.Count -1 ) );

			for( int i = 0, iC = switchObjects.Count; i < iC; i++ )
			{
				if( switchObjects[ i ].gameObject )
					switchObjects[ i ].gameObject.SetActive( i == selectedIndex );
			}
		}

		public void Select( int switchIndex )
		{
			switchIndex = Mathf.Clamp( switchIndex, allowNone ? -1 : 0, Mathf.Max( 0, switchObjects.Count - 1 ) );

			if( selectedIndex == switchIndex )
			{
				if( toggleOnReselect )
					switchIndex = -1;
				else
					return;
			}

			if( selectedIndex > -1 && selectedIndex < switchObjects.Count )
			{
				switchObjects[ selectedIndex ].onDisable.Invoke();
				if( switchObjects[ selectedIndex ].gameObject )
					switchObjects[ selectedIndex ].gameObject.SetActive( false );
			}

			selectedIndex = switchIndex;

			if( selectedIndex > -1 && selectedIndex < switchObjects.Count )
			{
				switchObjects[ selectedIndex ].onEnable.Invoke();
				if( switchObjects[ selectedIndex ].gameObject )
					switchObjects[ selectedIndex ].gameObject.SetActive( true );
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( Switcher ) )]
	public class EditorSwitcher : EditorUtils.InheritedEditor
	{
		SerializedProperty allowNone;
		SerializedProperty defaultIndex;
		SerializedProperty toggleOnReselect;

		ReorderableList switchObjects;

		public override void Setup()
		{
			base.Setup();

			allowNone = this[ "allowNone" ];
			defaultIndex = this[ "defaultIndex" ];
			toggleOnReselect = this[ "toggleOnReselect" ];

			switchObjects = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "switchObjects" ),
				( SerializedProperty element ) =>
				{
					return EditorGUI.GetPropertyHeight( element, true ) + lineHeight * 0.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					Rect bRect = new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, rect.height - lineHeight * 0.5f );
					EditorGUI.PropertyField( bRect, element, true );
				}
			);
		}

		public override float inspectorLeadingOffset => lineHeight * 0.5f;

		public override float GetInspectorHeight()
		{
			float height = base.GetInspectorHeight();

			height += lineHeight * 3.5f;

			if( switchObjects.serializedProperty.isExpanded )
				height += switchObjects.GetHeight();
			else
				height += lineHeight * 1.5f;


			return height;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );

			EditorGUI.BeginChangeCheck();

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Allow None" ), allowNone );

			if( EditorGUI.EndChangeCheck() )
			{
				if( allowNone.boolValue )
				{
					if( defaultIndex.intValue == 0 )
						defaultIndex.intValue = -1;
				}
				else
				{
					if( defaultIndex.intValue < 0 )
						defaultIndex.intValue = 0;
				}
			}

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Toggle On Reselection" ), toggleOnReselect );

			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, bRect.width, lineHeight );

			int min = allowNone.boolValue ? -1 : 0;
			int max = switchObjects.serializedProperty.arraySize - 1;

			if( max - min <= 0 )
			{
				EditorGUI.BeginDisabledGroup( true );

				EditorGUI.IntSlider( cRect, new GUIContent( string.Empty, "Default Index" ), 0, 0, 1 );

				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUI.IntSlider( cRect, defaultIndex, min, max, new GUIContent( string.Empty, "Default Index" ) );
			}

			bRect.y += lineHeight * 1.5f;

			if( switchObjects.serializedProperty.isExpanded )
			{
				bRect.height = switchObjects.GetHeight();

				switchObjects.DoList( bRect );

				bRect.y += bRect.height;
			}
			else
			{
				bRect.height = lineHeight;

				EditorUtils.DrawListHeader( bRect, switchObjects.serializedProperty );

				bRect.y += lineHeight * 1.5f;
			}

			rect.y = bRect.y;
		}
	}
#endif
}