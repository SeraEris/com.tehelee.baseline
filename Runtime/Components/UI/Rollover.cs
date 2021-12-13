using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
	[RequireComponent( typeof( RectTransform ) )]
	public class Rollover : MonoBehaviour
	{
		////////////////////////////////
		#region Attributes

		public List<GameObject> rollover = new List<GameObject>();
		public List<GameObject> hideOnHover = new List<GameObject>();

		public UnityEvent onShow = new UnityEvent();
		public UnityEvent onHide = new UnityEvent();
		
		#endregion

		////////////////////////////////
		#region Properties

		public RectTransform rectTransform { get; private set; }

		public bool hovering { get; private set; }

		private bool _locked = false;
		public bool locked
		{
			get => _locked;
			set
			{
				if( value )
					ToggleVisible( true );

				_locked = value;

				if( !_locked )
					CheckHover();
			}
		}

		#endregion

		////////////////////////////////
		#region Mono Methods

		private void Awake()
		{
			rectTransform = ( RectTransform ) transform;
		}

		private void OnEnable()
		{
			ToggleVisible( false );
		}

		private void OnDisable()
		{
			locked = false;

			ToggleVisible( false );
		}

		protected virtual void Update()
		{
			CheckHover();
		}

		#endregion

		////////////////////////////////
		#region Rollover

		private void CheckHover()
		{
			bool hovering = enabled && RectTransformUtility.RectangleContainsScreenPoint( rectTransform, Input.mousePosition );
			if( this.hovering != hovering )
				ToggleVisible( hovering );
		}

		private void ToggleVisible( bool hovering )
		{
			if( locked )
				return;

			this.hovering = hovering;
			
			foreach( GameObject gameObject in hideOnHover )
				if( Utils.IsObjectAlive( gameObject ) )
					gameObject.SetActive( !hovering );

			foreach( GameObject gameObject in rollover )
				if( Utils.IsObjectAlive( gameObject ) )
					gameObject.SetActive( hovering );

			if( hovering )
				onShow?.Invoke();
			else
				onHide?.Invoke();
		}

		#endregion
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( Rollover ) )]
	public class EditorRollover : EditorUtils.InheritedEditor
	{
		ReorderableList rollover;
		ReorderableList hideOnHover;

		public override void Setup()
		{
			base.Setup();

			rollover = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "rollover" ),
				( SerializedProperty element ) => lineHeight * 1.5f,
				( Rect rect, SerializedProperty element ) =>
				{
					EditorUtils.BetterObjectField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), new GUIContent(), element, typeof( GameObject ), true );
				}
			);

			hideOnHover = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "hideOnHover" ),
				( SerializedProperty element ) => lineHeight * 1.5f,
				( Rect rect, SerializedProperty element ) =>
				{
					EditorUtils.BetterObjectField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), new GUIContent(), element, typeof( GameObject ), true );
				}
			);
		}


		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 1.5f;

			inspectorHeight += rollover.CalculateCollapsableListHeight();
			inspectorHeight += hideOnHover.CalculateCollapsableListHeight();

			inspectorHeight += EditorUtils.BetterUnityEventFieldHeight( this[ "onShow" ] );
			inspectorHeight += EditorUtils.BetterUnityEventFieldHeight( this[ "onHide" ] );
			
			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Rollover", "Shows and/or hides objects on cursor entry and exit." ) );
			bRect.y += lineHeight * 1.5f;

			rollover.DrawCollapsableList( ref bRect, new GUIContent( "Reveal Objects" ) );
			hideOnHover.DrawCollapsableList( ref bRect, new GUIContent( "Hide Objects" ) );

			EditorUtils.BetterUnityEventField( ref bRect, this[ "onShow" ] );
			EditorUtils.BetterUnityEventField( ref bRect, this[ "onHide" ] );

			rect.y = bRect.y;
		}
	}
#endif
}