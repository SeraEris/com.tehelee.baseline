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
	[RequireComponent( typeof( RectTransform ) )]
	public class MimicRectTransform : MonoBehaviour
	{
		////////////////////////////////
		#region Attributes

		public RectTransform target;

		public bool updateInRuntime = false;

#if UNITY_EDITOR
		public bool updateInEdit = false;
#endif
		#endregion

		////////////////////////////////
		#region Properties

		public RectTransform rectTransform { get; private set; }

		#endregion

		////////////////////////////////
		#region Static

		private static GroupUpdates<MimicRectTransform> groupUpdates = new GroupUpdates<MimicRectTransform>( PerformLayout );

		#endregion

		////////////////////////////////
		#region Mono Methods

		private void Awake()
		{
			rectTransform = ( RectTransform ) transform;
		}

		private void OnEnable()
		{
			if( Application.isPlaying && updateInRuntime )
			{
				PerformLayout();

				groupUpdates.Register( this );
			}
		}

		private void OnDisable()
		{
			groupUpdates.Drop( this );
		}

#if UNITY_EDITOR
		protected virtual void Update()
		{
			if( !Application.isPlaying && updateInEdit )
			{
				PerformLayout();
			}
		}
#endif

		#endregion

		////////////////////////////////
		#region Mirror Rect Transform

		public static void PerformLayout( RectTransform target, RectTransform follower, bool matchSize = true )
		{
			if( !Utils.IsObjectAlive( target ) || !Utils.IsObjectAlive( follower ) )
				return;
			
			Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds( follower, target );
			follower.anchoredPosition = bounds.center;
			if( matchSize )
				follower.sizeDelta = bounds.extents * 2f;
		}

		private static void PerformLayout( MimicRectTransform mimicRectTransform ) => mimicRectTransform.PerformLayout();
		public void PerformLayout()
		{
			if( !Utils.IsObjectAlive( rectTransform ) )
				rectTransform = ( RectTransform ) transform;

			PerformLayout( target, rectTransform );
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( MimicRectTransform ) )]
	public class EditormimicRectTransform : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight()
		{
			return base.GetInspectorHeight() + lineHeight * 4.5f;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Mimic Rect Transform", "Matche all attributes from the specified target to the attached RectTransform." ) );
			bRect.y += lineHeight * 1.5f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "RectTransform" ), this[ "target" ], typeof( RectTransform ), true );

			bRect.y += lineHeight * 1.5f;
			bRect.height = lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, bRect.height );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Runtime" ), this[ "updateInRuntime" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Edit" ), this[ "updateInEdit" ] );

			bRect.y += lineHeight * 1.5f;

			rect.y = bRect.y;
		}
	}
#endif
}