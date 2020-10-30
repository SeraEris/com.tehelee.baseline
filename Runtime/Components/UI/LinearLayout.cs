using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline.Components.UI
{
#if UNITY_EDITOR
	[ExecuteInEditMode]
#endif
	public class LinearLayout : MonoBehaviour
	{
		public float paddingStart = 0f;
		public float paddingMids = 0f;
		public float paddingEnd = 0f;

		public bool layoutHorizontal = false;
		public bool layoutInverse = false;
		public bool resizeBounds = false;

		public bool updateInRuntime = false;

#if UNITY_EDITOR
		public bool updateInEdit = false;
#endif
		protected virtual void Update()
		{
#if UNITY_EDITOR
			if( Application.isPlaying )
			{
				if( !updateInRuntime )
					return;
			}
			else if( !updateInEdit )
			{
				return;
			}
#else
			if( !updateInRuntime )
				return;
#endif
			PerformLayout();
		}

		protected virtual void OnEnable()
		{
			PerformLayout();
		}

		public void PerformLayout()
		{
			RectTransform child, rectTransform = ( RectTransform ) transform;

			float totalSize = paddingStart;
			if( layoutHorizontal )
			{
				if( layoutInverse )
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf )
							continue;

						child.anchorMin = new Vector2( 1f, child.anchorMin.y );
						child.anchorMax = new Vector2( 1f, child.anchorMax.y );
						child.pivot = new Vector2( 1f, child.pivot.y );

						child.anchoredPosition = new Vector2( -totalSize, child.anchoredPosition.y );
						
						totalSize += child.sizeDelta.x + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}
				}
				else
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf )
							continue;

						child.anchorMin = new Vector2( 0f, child.anchorMin.y );
						child.anchorMax = new Vector2( 0f, child.anchorMax.y );
						child.pivot = new Vector2( 0f, child.pivot.y );

						child.anchoredPosition = new Vector2( totalSize, child.anchoredPosition.y );

						totalSize += child.sizeDelta.x + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}
				}

				if( totalSize == paddingStart )
					totalSize = 0f;

				if( resizeBounds )
					rectTransform.sizeDelta = new Vector2( totalSize, rectTransform.sizeDelta.y );
			}
			else
			{
				if( layoutInverse )
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf )
							continue;

						child.anchorMin = new Vector2( child.anchorMin.x, 0f );
						child.anchorMax = new Vector2( child.anchorMax.x, 0f );
						child.pivot = new Vector2( child.pivot.x, 0f );

						child.anchoredPosition = new Vector2( child.anchoredPosition.x, totalSize );

						totalSize += child.sizeDelta.y + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}
				}
				else
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf )
							continue;

						child.anchorMin = new Vector2( child.anchorMin.x, 1f );
						child.anchorMax = new Vector2( child.anchorMax.x, 1f );
						child.pivot = new Vector2( child.pivot.x, 1f );

						child.anchoredPosition = new Vector2( child.anchoredPosition.x, -totalSize );

						totalSize += child.sizeDelta.y + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}
				}

				if( totalSize == paddingStart )
					totalSize = 0f;

				if( resizeBounds )
					rectTransform.sizeDelta = new Vector2( rectTransform.sizeDelta.x, totalSize );
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( LinearLayout ) )]
	public class EditorLinearLayout : EditorUtils.InheritedEditor
	{
		public override float inspectorLeadingOffset => lineHeight * 0.5f;

		public override float GetInspectorHeight()
		{
			float height = base.GetInspectorHeight();

			height += lineHeight * 6f;

			return height;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 20f ) * 0.333f, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Horizontal" ), this[ "layoutHorizontal" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Inverse" ), this[ "layoutInverse" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Resize" ), this[ "resizeBounds" ] );

			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Runtime" ), this[ "updateInRuntime" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Edit" ), this[ "updateInEdit" ] );

			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, bRect.width * 0.333f, lineHeight );

			EditorGUI.LabelField( cRect, new GUIContent( "Start" ), EditorStyles.miniButtonLeft );

			cRect.x += cRect.width;

			EditorGUI.LabelField( cRect, new GUIContent( "Mids" ), EditorStyles.miniButtonMid );

			cRect.x += cRect.width;

			EditorGUI.LabelField( cRect, new GUIContent( "End" ), EditorStyles.miniButtonRight );

			bRect.y += lineHeight;

			cRect = new Rect( bRect.x, bRect.y, bRect.width * 0.333f, lineHeight );
			
			DrawFloatField( cRect, this[ "paddingStart" ] );

			cRect.x += cRect.width;

			DrawFloatField( cRect, this[ "paddingMids" ] );

			cRect.x += cRect.width;

			DrawFloatField( cRect, this[ "paddingEnd" ] );

			bRect.y += lineHeight * 1.5f;

			rect.y = bRect.y;
		}

		private static GUIStyle _floatFieldStyle = null;
		private static GUIStyle floatFieldStyle
		{
			get
			{
				if( object.Equals( null, _floatFieldStyle ) )
				{
					_floatFieldStyle = new GUIStyle( EditorStyles.numberField );
					_floatFieldStyle.alignment = TextAnchor.MiddleCenter;
				}
				return _floatFieldStyle;
			}
		}

		private void DrawFloatField( Rect rect, SerializedProperty property )
		{
			float value = property.floatValue;
			float _value = EditorGUI.FloatField( rect, new GUIContent(), value, floatFieldStyle );
			if( _value != value )
				property.floatValue = value = _value;
		}
	}
#endif
}
