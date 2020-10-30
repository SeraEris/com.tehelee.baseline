using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
	[RequireComponent( typeof( RectTransform ) )]
	public class RectResizer : MonoBehaviour
	{
		[System.Serializable]
		public class Resize
		{
			[System.Serializable]
			public class ToggleFloat
			{
				public float value;
				public bool enabled;

				public static implicit operator bool( ToggleFloat toggleFloat ) => toggleFloat.enabled;
				public static implicit operator float( ToggleFloat toggleFloat ) => toggleFloat.value;
			}
#if UNITY_EDITOR
			[CustomPropertyDrawer( typeof( ToggleFloat ) )]
			public class ToggleFloatPropertyDrawer : EditorUtils.InheritedPropertyDrawer
			{
				public override float CalculatePropertyHeight( ref SerializedProperty property ) => lineHeight;

				public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
				{
					base.DrawGUI( ref rect, ref property );

					Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );
					
					SerializedProperty value = property.FindPropertyRelative( "value" );
					SerializedProperty enabled = property.FindPropertyRelative( "enabled" );

					int vectorSuffix = DetermineVectorPropertySuffix( property.propertyPath );

					if( vectorSuffix != 0 )
					{
						EditorGUI.LabelField( new Rect( bRect.x, bRect.y, 15f, bRect.height ), new GUIContent( vectorSuffix == 1 ? "X" : "Y" ) );

						bRect.x += 15f;
						bRect.width -= 15f;
					}

					EditorGUI.BeginDisabledGroup( !enabled.boolValue );
					float _value = EditorGUI.FloatField( new Rect( bRect.x + 20f, bRect.y, bRect.width - 20f, bRect.height ), new GUIContent( string.Empty, property.displayName ), enabled.boolValue ? value.floatValue : 0f );
					if( _value != value.floatValue )
						value.floatValue = _value;
					EditorGUI.EndDisabledGroup();

					bool _enabled = EditorGUI.Toggle( new Rect( bRect.x, bRect.y, 20f, bRect.height ), new GUIContent(), enabled.boolValue );
					if( _enabled != enabled.boolValue )
						enabled.boolValue = _enabled;
				}

				private static int DetermineVectorPropertySuffix( string path )
				{
					char lastChar = path[ path.Length - 1 ];
					switch( lastChar )
					{
						case 'X':
							return 1;
						case 'Y':
							return 2;

						default:
							return 0;
					}
				}
			}
#endif
			public ToggleFloat anchorMinX = new ToggleFloat();
			public ToggleFloat anchorMinY = new ToggleFloat();
			public ToggleFloat anchorMaxX = new ToggleFloat();
			public ToggleFloat anchorMaxY = new ToggleFloat();
			public ToggleFloat pivotX = new ToggleFloat();
			public ToggleFloat pivotY = new ToggleFloat();

			public ToggleFloat anchoredPositionX = new ToggleFloat();
			public ToggleFloat anchoredPositionY = new ToggleFloat();
			public ToggleFloat sizeDeltaX = new ToggleFloat();
			public ToggleFloat sizeDeltaY = new ToggleFloat();

			[System.Serializable]
			public class OnResize : UnityEngine.Events.UnityEvent { }
			public OnResize onResize = new OnResize();

			public void ApplySize( RectTransform rectTransform )
			{
				rectTransform.anchorMin = new Vector2
				(
					anchorMinX ? anchorMinX : rectTransform.anchorMin.x,
					anchorMinY ? anchorMinY : rectTransform.anchorMin.y
				);
				rectTransform.anchorMax = new Vector2
				(
					anchorMaxX ? anchorMaxX : rectTransform.anchorMax.x,
					anchorMaxY ? anchorMaxY : rectTransform.anchorMax.y
				);
				rectTransform.pivot = new Vector2
				(
					pivotX ? pivotX : rectTransform.pivot.x,
					pivotY ? pivotY : rectTransform.pivot.y
				);

				rectTransform.anchoredPosition = new Vector2
				(
					anchoredPositionX ? anchoredPositionX : rectTransform.anchoredPosition.x,
					anchoredPositionY ? anchoredPositionY : rectTransform.anchoredPosition.y
				);
				rectTransform.sizeDelta = new Vector2
				(
					sizeDeltaX ? sizeDeltaX : rectTransform.sizeDelta.x,
					sizeDeltaY ? sizeDeltaY : rectTransform.sizeDelta.y
				);

				onResize?.Invoke();
			}
		}
#if UNITY_EDITOR
		[CustomPropertyDrawer( typeof( Resize ) )]
		public class ResizePropertyDrawer : EditorUtils.InheritedPropertyDrawer
		{
			public override float CalculatePropertyHeight( ref SerializedProperty property )
			{
				float height = base.CalculatePropertyHeight( ref property );
				height += lineHeight * 6f;
				height += EditorUtils.BetterUnityEventFieldHeight( property.FindPropertyRelative( "onResize" ) );
				return height;
			}

			public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
			{
				base.DrawGUI( ref rect, ref property );

				Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

				Rect cRect = new Rect( bRect.x, bRect.y, EditorGUIUtility.labelWidth, lineHeight );
				EditorGUI.LabelField( cRect, new GUIContent( "Anchor Min" ) );
				cRect.x += cRect.width;
				cRect.width = bRect.width - cRect.width;

				Rect dRect = new Rect( cRect.x, cRect.y, ( cRect.width - 10f ) * 0.5f, cRect.height );

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "anchorMinX" ), true );

				dRect.x += dRect.width + 10f;

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "anchorMinY" ), true );

				bRect.y += lineHeight;

				cRect = new Rect( bRect.x, bRect.y, EditorGUIUtility.labelWidth, lineHeight );
				EditorGUI.LabelField( cRect, new GUIContent( "Anchor Max" ) );
				cRect.x += cRect.width;
				cRect.width = bRect.width - cRect.width;

				dRect = new Rect( cRect.x, cRect.y, ( cRect.width - 10f ) * 0.5f, cRect.height );

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "anchorMaxX" ), true );

				dRect.x += dRect.width + 10f;

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "anchorMaxY" ), true );

				bRect.y += lineHeight;

				cRect = new Rect( bRect.x, bRect.y, EditorGUIUtility.labelWidth, lineHeight );
				EditorGUI.LabelField( cRect, new GUIContent( "Pivot" ) );
				cRect.x += cRect.width;
				cRect.width = bRect.width - cRect.width;

				dRect = new Rect( cRect.x, cRect.y, ( cRect.width - 10f ) * 0.5f, cRect.height );

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "pivotX" ), true );

				dRect.x += dRect.width + 10f;

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "pivotY" ), true );

				bRect.y += lineHeight * 1.5f;

				cRect = new Rect( bRect.x, bRect.y, EditorGUIUtility.labelWidth, lineHeight );
				EditorGUI.LabelField( cRect, new GUIContent( "Anchored Position" ) );
				cRect.x += cRect.width;
				cRect.width = bRect.width - cRect.width;

				dRect = new Rect( cRect.x, cRect.y, ( cRect.width - 10f ) * 0.5f, cRect.height );

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "anchoredPositionX" ), true );

				dRect.x += dRect.width + 10f;

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "anchoredPositionY" ), true );

				bRect.y += lineHeight;

				cRect = new Rect( bRect.x, bRect.y, EditorGUIUtility.labelWidth, lineHeight );
				EditorGUI.LabelField( cRect, new GUIContent( "Size Delta" ) );
				cRect.x += cRect.width;
				cRect.width = bRect.width - cRect.width;

				dRect = new Rect( cRect.x, cRect.y, ( cRect.width - 10f ) * 0.5f, cRect.height );

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "sizeDeltaX" ), true );

				dRect.x += dRect.width + 10f;

				EditorGUI.PropertyField( dRect, property.FindPropertyRelative( "sizeDeltaY" ), true );

				bRect.y += lineHeight * 1.5f;

				SerializedProperty onResize = property.FindPropertyRelative( "onResize" );
				bRect.height = EditorUtils.BetterUnityEventFieldHeight( onResize );

				EditorUtils.BetterUnityEventField( bRect, onResize );

				bRect.y += bRect.height;

				rect.y = bRect.y;
			}
		}
#endif

		protected virtual void OnEnable()
		{
			Select( selectedIndex );
		}

		public int selectedIndex = 0;
		public List<Resize> resizes = new List<Resize>();

		public void Select( int index )
		{
			if( index < 0 && index >= resizes.Count )
				return;

			resizes[ index ].ApplySize( ( RectTransform ) transform );
			selectedIndex = index;
		}

		public void Apply() => Select( 0 );

		public void Toggle() => Toggle( selectedIndex != 1 );

		public void Toggle( bool enabled ) => Select( enabled ? 1 : 0 );
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( RectResizer ) )]
	public class EditorRectResizer : EditorUtils.InheritedEditor
	{
		SerializedProperty selectedIndex;

		ReorderableList resizes;

		public override float inspectorLeadingOffset => lineHeight * 0.5f;

		public override void Setup()
		{
			base.Setup();

			selectedIndex = serializedObject.FindProperty( "selectedIndex" );
			SerializedProperty _resizes = serializedObject.FindProperty( "resizes" );
			if( _resizes.arraySize <= 0 )
			{
				_resizes.InsertArrayElementAtIndex( 0 );
				serializedObject.ApplyModifiedProperties();
			}

			resizes = EditorUtils.CreateReorderableList
			(
				_resizes,
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

		public override float GetInspectorHeight()
		{
			float height = base.GetInspectorHeight();

			SerializedProperty _resizes = resizes.serializedProperty;
			if( _resizes.arraySize > 0 )
			{
				if( _resizes.arraySize < 3 )
					height += lineHeight * 2f;
				else
					height += lineHeight * 1.5f;
			}

			if( _resizes.isExpanded )
				height += resizes.GetHeight();
			else
				height += lineHeight * 1.5f;

			return height;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			SerializedProperty _resizes = resizes.serializedProperty;
			if( _resizes.arraySize > 0 )
			{
				switch( _resizes.arraySize )
				{
					case 1:
						bRect.height = lineHeight * 1.5f;
						if( EditorUtils.BetterButton( bRect, new GUIContent( "Perform Resize" ) ) )
						{
							foreach( Object target in targets )
								( ( RectResizer ) target )?.Apply();
						}
						break;
					case 2:
						bRect.height = lineHeight * 1.5f;
						bool defaultEnabled = selectedIndex.intValue == 1;
						bool _defaultEnabled = EditorUtils.BetterToggleField( bRect, new GUIContent( "Default" ), defaultEnabled );
						if( _defaultEnabled != defaultEnabled )
						{
							defaultEnabled = _defaultEnabled;
							selectedIndex.intValue = defaultEnabled ? 1 : 0;
							foreach( Object target in targets )
								( ( RectResizer ) target )?.Toggle( defaultEnabled );
						}
						break;
					default:
						bRect.height = lineHeight;
						EditorGUI.BeginChangeCheck();
						EditorGUI.IntSlider( bRect, selectedIndex, 0, _resizes.arraySize - 1, new GUIContent( "Default" ) );
						if( EditorGUI.EndChangeCheck() )
						{
							foreach( Object target in targets )
								( ( RectResizer ) target )?.Select( selectedIndex.intValue );
						}
						break;
				}

				bRect.y += bRect.height + lineHeight * 0.5f;
			}

			if( _resizes.isExpanded )
			{
				bRect.height = resizes.GetHeight();

				EditorGUI.BeginChangeCheck();

				resizes.DoList( bRect );

				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					foreach( Object target in targets )
					{
						RectResizer rectResizer = ( RectResizer ) target;
						if( object.Equals( null, rectResizer ) || !rectResizer )
							continue;

						rectResizer.Select( rectResizer.selectedIndex );
					}
				}
			}
			else
			{
				bRect.height = lineHeight;

				EditorUtils.DrawListHeader( bRect, _resizes );
			}

			bRect.y += bRect.height + lineHeight * 0.5f;

			rect.y = bRect.y;
		}
	}
#endif
}
