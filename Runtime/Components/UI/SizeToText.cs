using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Text = TMPro.TextMeshProUGUI;
using InputField = TMPro.TMP_InputField;

namespace Tehelee.Baseline.Components.UI
{
#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	[RequireComponent( typeof( RectTransform ) )]
	public class SizeToText : MonoBehaviour
	{
		////////////////////////////////
		#region Attributes

		public Text text;

		public bool horizontal;
		public bool vertical;

		public bool clampToParent;

		public bool hasMin = false;
		public bool hasMax = false;

		public Vector2 minSize = Vector2.zero;
		public Vector2 maxSize = Vector2.zero;

		#endregion

		////////////////////////////////
		#region Properties

		private RectTransform _rectTransform = null;
		public RectTransform rectTransform
		{
			get
			{
				if( !Utils.IsObjectAlive( _rectTransform ) )
					_rectTransform = ( RectTransform ) this.transform;
				return _rectTransform;
			}
		}

		#endregion

		////////////////////////////////
		#region Mono Methods

		private void OnEnable()
		{
			PerformLayout();
		}

		private void OnDisable()
		{
			PerformLayout();
		}

#if UNITY_EDITOR
		private void Update()
		{
			if( !Application.isPlaying )
				PerformLayout();
		}
#endif

		#endregion

		////////////////////////////////
		#region SizeToText

		private void PerformLayout( string value ) => PerformLayout();
		public void PerformLayout()
		{
			if( !Utils.IsObjectAlive( text ) )
				return;

			Vector2 size = text.GetPreferredValues();

			if( hasMin )
				size = new Vector2
				(
					Mathf.Max( minSize.x, size.x ),
					Mathf.Max( minSize.y, size.y )
				);
			
			if( clampToParent )
			{
				RectTransform parent = ( RectTransform ) rectTransform.parent;
				if( Utils.IsObjectAlive( parent ) )
				{
					Vector2 anchor = rectTransform.anchoredPosition;
					Vector2 parentSize = parent.rect.size;

					parentSize.x -= Mathf.Abs( anchor.x );
					parentSize.y -= Mathf.Abs( anchor.y );

					if( hasMax )
					{
						parentSize.x += maxSize.x;
						parentSize.y += maxSize.y;
					}

					size = new Vector2
					(
						Mathf.Min( parentSize.x, size.x ),
						Mathf.Min( parentSize.y, size.y )
					);
				}
			}
			else
			{
				if( hasMax )
					size = new Vector2
					(
						Mathf.Min( maxSize.x, size.x ),
						Mathf.Min( maxSize.y, size.y )
					);
			}

			size = new Vector2
				(
					horizontal ? size.x : rectTransform.sizeDelta.x,
					vertical ? size.y : rectTransform.sizeDelta.y
				);

			rectTransform.sizeDelta = size;
		}

		#endregion
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( SizeToText ) )]
	public class EditorSizeToText : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight() => lineHeight * 11f + 4f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Size To Text" ) );
			bRect.y += lineHeight * 1.5f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Text Component" ), this[ "text" ], typeof( Text ), true );
			bRect.y += lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Horizontal" ), this[ "horizontal" ] );
			cRect.x += cRect.width + 10f;
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Vertical" ), this[ "vertical" ] );
			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, bRect.width, lineHeight * 1.5f );
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Clamp To Parent" ), this[ "clampToParent" ] );
			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Min Size" ), this[ "hasMin" ] );
			cRect.x += cRect.width + 10f;
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Max Size" ), this[ "hasMax" ] );
			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight );

			EditorGUIUtility.labelWidth = 20f;

			EditorGUI.BeginDisabledGroup( !this[ "hasMin" ].boolValue );

			Vector2 size = this[ "minSize" ].vector2Value;

			EditorGUI.BeginDisabledGroup( !this[ "horizontal" ].boolValue );
			size.x = EditorGUI.FloatField( cRect, new GUIContent( "X" ), size.x );
			EditorGUI.EndDisabledGroup();

			cRect.y += lineHeight + 4f;

			EditorGUI.BeginDisabledGroup( !this[ "vertical" ].boolValue );
			size.y = EditorGUI.FloatField( cRect, new GUIContent( "Y" ), size.y );
			EditorGUI.EndDisabledGroup();

			cRect.y -= lineHeight + 4f;
			this[ "minSize" ].vector2Value = size;

			EditorGUI.EndDisabledGroup();

			cRect.x += cRect.width + 10f;

			EditorGUI.BeginDisabledGroup( !this[ "hasMax" ].boolValue );

			size = this[ "maxSize" ].vector2Value;

			EditorGUI.BeginDisabledGroup( !this[ "horizontal" ].boolValue );
			size.x = EditorGUI.FloatField( cRect, new GUIContent( "X" ), size.x );
			EditorGUI.EndDisabledGroup();

			cRect.y += lineHeight + 4f;

			EditorGUI.BeginDisabledGroup( !this[ "vertical" ].boolValue );
			size.y = EditorGUI.FloatField( cRect, new GUIContent( "Y" ), size.y );
			EditorGUI.EndDisabledGroup();

			cRect.y -= lineHeight + 4f;
			this[ "maxSize" ].vector2Value = size;

			EditorGUI.EndDisabledGroup();

			EditorGUIUtility.labelWidth = labelWidth;

			bRect.y += lineHeight * 2f + 4f;

			rect.y = bRect.y;
		}
	}
#endif
}