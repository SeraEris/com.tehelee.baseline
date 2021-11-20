using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using InputField = TMPro.TMP_InputField;

namespace Tehelee.Baseline
{
	[RequireComponent( typeof( InputField ) )]
	public class InputFocusEvents : MonoBehaviour
	{
		////////////////////////////////
		//	Attributes

		#region Attributes

		[System.Serializable]
		public class InputFocusEvent : UnityEvent<InputField> {}

		public InputFocusEvent onSelect = new InputFocusEvent();
		public InputFocusEvent onDeselect = new InputFocusEvent();

		#endregion

		////////////////////////////////
		//	Members

		#region Members

		private InputField inputField;

		private Coroutine focusCoroutine = null;
		private bool wasFocussed = false;

		#endregion

		////////////////////////////////
		//	Mono Methods

		#region Mono Methods

		private void Awake()
		{
			inputField = GetComponent<InputField>();
		}

		private void OnEnable()
		{
			inputField.onSelect.AddListener( OnComponentSelected );
			inputField.onDeselect.AddListener( OnComponentDeselected );
		}

		private void OnDisable()
		{
			if( !object.Equals( null, focusCoroutine ) )
			{
				StopCoroutine( focusCoroutine );
				focusCoroutine = null;
			}

			inputField.onSelect.RemoveListener( OnComponentSelected );
			inputField.onDeselect.RemoveListener( OnComponentDeselected );
		}

		#endregion

		////////////////////////////////
		//	InputFocusEvents

		#region InputFocusEvents

		private void OnComponentSelected( string val )
		{
			wasFocussed = false;

			if( object.Equals( null, focusCoroutine ) )
				focusCoroutine = StartCoroutine( IFocusWatcher() );
		}

		private void OnComponentDeselected( string val )
		{
			if( !object.Equals( null, focusCoroutine ) )
			{
				StopCoroutine( focusCoroutine );
				focusCoroutine = null;
			}

			if( wasFocussed )
			{
				onDeselect?.Invoke( inputField );

				wasFocussed = false;
			}
		}

		private IEnumerator IFocusWatcher()
		{
			WaitForEndOfFrame wait = new WaitForEndOfFrame();

			while( true )
			{
				yield return wait;

				bool focussed = inputField.isFocused;

				if( wasFocussed != focussed )
				{
					if( focussed )
						onSelect?.Invoke( inputField );
					else
						onDeselect?.Invoke( inputField );

					wasFocussed = focussed;
				}
			}
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( InputFocusEvents ) )]
	public class EditorInputFocusEvents : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 1.5f;

			inspectorHeight += EditorUtils.BetterUnityEventFieldHeight( this[ "onSelect" ] );

			inspectorHeight += lineHeight * 0.5f;

			inspectorHeight += EditorUtils.BetterUnityEventFieldHeight( this[ "onDeselect" ] );

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "InputFocusEvents" ) );
			bRect.y += lineHeight * 1.5f;

			EditorUtils.BetterUnityEventField( ref bRect, this[ "onSelect" ] );

			bRect.y += lineHeight * 0.5f;

			EditorUtils.BetterUnityEventField( ref bRect, this[ "onDeselect" ] );
			
			rect.y = bRect.y;
		}
	}
#endif
}