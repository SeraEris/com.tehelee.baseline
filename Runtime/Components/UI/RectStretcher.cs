using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	[RequireComponent( typeof( RectTransform ) )]
	public class RectStretcher : MonoBehaviour
	{
		////////////////////////////////
		#region Static

		private static GroupUpdates<RectStretcher> groupUpdates = new GroupUpdates<RectStretcher>( PerformLayout );

		#endregion

		////////////////////////////////
		#region Attributes

		public bool horizontal = false;
		public bool vertical = false;

		public Vector2 percentage;
		public Vector2 minimum;
		public Vector2 maximum;
		public Vector2 offset;

		#endregion

		////////////////////////////////
		#region Members

		private RectTransform rectParent;
		private RectTransform rectTransform;

		#endregion

		////////////////////////////////
		#region Mono Methods

		protected virtual void Awake()
		{
			rectTransform = ( RectTransform ) transform;
		}

		protected virtual void OnEnable()
		{
#if UNITY_EDITOR
			if( Application.isPlaying )
#endif
			groupUpdates.Register( this );
		}

		protected virtual void OnDisable()
		{
#if UNITY_EDITOR
			if( Application.isPlaying )
#endif
			groupUpdates.Drop( this );
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
		#region RectStretcher

		private static void PerformLayout( RectStretcher rectStretcher ) =>
			rectStretcher.PerformLayout();

		public virtual void PerformLayout()
		{
			if( !Utils.IsObjectAlive( this ) )
				return;

			if( !horizontal && !vertical )
				return;

#if UNITY_EDITOR
			if( !Utils.IsObjectAlive( rectTransform ) )
				rectTransform = ( RectTransform ) transform;
			if( !Utils.IsObjectAlive( rectParent ) )
				rectParent = ( RectTransform ) rectTransform.parent;
#endif

			if( !Utils.IsObjectAlive( rectTransform ) || !Utils.IsObjectAlive( rectParent ) )
				return;

			Rect parentRect = rectParent.rect;
			Vector2 sizeParent = new Vector2( parentRect.width, parentRect.height );
			Vector2 sizeDelta = rectTransform.sizeDelta;

			if( horizontal )
			{
				sizeDelta.x = sizeParent.x * Mathf.Clamp( percentage.x, 0f, 1f );

				bool min = ( minimum.x > 0f );
				bool max = ( maximum.x > 0f );

				if( min && max )
					sizeDelta.x = Mathf.Clamp( sizeDelta.x, minimum.x, maximum.x );
				else if( min )
					sizeDelta.x = Mathf.Max( sizeDelta.x, minimum.x );
				else if( max )
					sizeDelta.x = Mathf.Min( sizeDelta.x, maximum.x );

				sizeDelta.x += offset.x;
			}

			if( vertical )
			{
				sizeDelta.y = sizeParent.y * Mathf.Clamp( percentage.y, 0f, 1f ) + offset.y;

				bool min = ( minimum.y > 0f );
				bool max = ( maximum.y > 0f );

				if( min && max )
					sizeDelta.y = Mathf.Clamp( sizeDelta.y, minimum.y, maximum.y );
				else if( min )
					sizeDelta.y = Mathf.Max( sizeDelta.y, minimum.y );
				else if( max )
					sizeDelta.y = Mathf.Min( sizeDelta.y, maximum.y );

				sizeDelta.y += offset.y;
			}

			rectTransform.sizeDelta = sizeDelta;
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( RectStretcher ) )]
	public class EditorRectStretcher : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 7.5f + 12f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "RectStretcher" ) );
			bRect.y += lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Horizontal" ), this[ "horizontal" ] );
			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Vertical" ), this[ "vertical" ] );
			bRect.y += lineHeight * 2f;

			bool disableX = !this[ "horizontal" ].boolValue;
			bool disableY = !this[ "vertical" ].boolValue;

			EditorUtils.Vector2Field( bRect, new GUIContent( "Percentage" ), this[ "percentage" ], disableX, disableY );
			this[ "percentage" ].Clamp( Vector2.zero, Vector2.one );
			bRect.y += lineHeight + 4f;

			EditorUtils.Vector2Field( bRect, new GUIContent( "Minimum" ), this[ "minimum" ], disableX, disableY );
			bRect.y += lineHeight + 4f;

			EditorUtils.Vector2Field( bRect, new GUIContent( "Maximum" ), this[ "maximum" ], disableX, disableY );
			bRect.y += lineHeight + 4f;

			EditorUtils.Vector2Field( bRect, new GUIContent( "Offset" ), this[ "offset" ], disableX, disableY );
			bRect.y += lineHeight;

			rect.y = bRect.y;
		}
	}
#endif
}