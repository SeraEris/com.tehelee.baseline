using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace Tehelee.Baseline.Components.UI
{
#if UNITY_EDITOR
	[ExecuteInEditMode]
#endif
	public class RadioButton : Button
	{
		public Graphic radioGraphic;
		
		[SerializeField]
		private bool _radioActive = false;
		public bool radioActive
		{
			get => _radioActive;
			set
			{
				_radioActive = value;
				radioGraphic.enabled = _radioActive;
				radioChangedEvent?.Invoke( _radioActive );
			}
		}

		[System.Serializable]
		public class RadioChangedEvent : UnityEngine.Events.UnityEvent<bool> { }
		public RadioChangedEvent radioChangedEvent = new RadioChangedEvent();

		public void ToggleRadio()
		{
			radioActive = !radioActive;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			onClick.AddListener( ToggleRadio );
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			onClick.RemoveListener( ToggleRadio );
		}

#if UNITY_EDITOR
		private void Update()
		{
			if( !Application.isPlaying )
			{
				if( Utils.IsObjectAlive( radioGraphic ) && radioGraphic.enabled != radioActive )
				{
					radioGraphic.enabled = radioActive;
				}
			}
		}
#endif
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( RadioButton ) )]
	public class EditorRadioButton : ButtonEditor
	{
		SerializedProperty radioGraphic;
		SerializedProperty radioActive;
		SerializedProperty radioChangedEvent;

		protected override void OnEnable()
		{
			base.OnEnable();
			radioGraphic = serializedObject.FindProperty( "radioGraphic" );
			radioActive = serializedObject.FindProperty( "_radioActive" );
			radioChangedEvent = serializedObject.FindProperty( "radioChangedEvent" );

			Graphic graphic = ( Graphic ) radioGraphic.objectReferenceValue;
			if( Utils.IsObjectAlive( graphic ) )
				graphic.enabled = radioActive.boolValue;
		}

		public override void OnInspectorGUI()
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			float labelWidth = EditorGUIUtility.labelWidth;

			Rect bRect = EditorGUILayout.GetControlRect( GUILayout.Height( lineHeight * 1.5f ) );
			bRect.height = lineHeight;
			EditorUtils.DrawDivider( bRect, new GUIContent( "Unity Button" ) );

			base.OnInspectorGUI();
			
			serializedObject.Update();
			
			bRect = EditorGUILayout.GetControlRect( GUILayout.Height( lineHeight * 1.5f ) );
			bRect.height = lineHeight;
			EditorUtils.DrawDivider( bRect, new GUIContent( "Radio Button", "Provides a toggle element with an optional Graphic representation." ) );

			bRect = EditorGUILayout.GetControlRect( GUILayout.Height( lineHeight * 1.5f ) );

			Rect cRect = new Rect( bRect.x, bRect.y + lineHeight * 0.25f, ( bRect.width - 10f ) * 0.666f, lineHeight );

			EditorGUIUtility.labelWidth = 100f;
			EditorUtils.BetterObjectField( cRect, new GUIContent( "Radio Graphic" ), radioGraphic, typeof( Graphic ), true );
			EditorGUIUtility.labelWidth = labelWidth;

			cRect = new Rect( bRect.x + cRect.width + 10f, bRect.y, ( bRect.width - 10f ) * 0.333f, lineHeight * 1.5f );
			EditorGUI.BeginChangeCheck();
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Radio Active" ), radioActive );
			if( EditorGUI.EndChangeCheck() )
			{
				if( Utils.IsObjectAlive( radioGraphic.objectReferenceValue ) )
					( ( Graphic ) radioGraphic.objectReferenceValue ).enabled = radioActive.boolValue;
			}

			GUILayout.Space( lineHeight * 0.5f );

			bRect = EditorGUILayout.GetControlRect( GUILayout.Height( EditorUtils.BetterUnityEventFieldHeight( radioChangedEvent ) ) );
			EditorUtils.BetterUnityEventField( bRect, radioChangedEvent );

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}