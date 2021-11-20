using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
	public class FadeGraphics : MonoBehaviour
	{
		////////////////////////////////
		//	Attributes

		#region Attributes

		public float fadeTime = 1f;

		public List<Graphic> ignoreGraphics = new List<Graphic>();

		#endregion

		////////////////////////////////
		//	Properties

		#region Properties

		[SerializeField]
		private bool _visible = false;
		public bool visible
		{
			get => _visible;
			set
			{
				_visible = value;
				OnVisibilityChanged();
			}
		}

		public void SetVisibility( bool visible ) =>
			this.visible = visible;

		#endregion

		////////////////////////////////
		//	Members

		#region Members

		private Coroutine fadeCoroutine = null;

		#endregion

		////////////////////////////////
		//	Mono Methods

		#region Mono Methods

		private void OnEnable()
		{
			FlushVisibility();
		}

		private void OnDisable()
		{
			if( !object.Equals( null, fadeCoroutine ) )
			{
				StopCoroutine( fadeCoroutine );
				fadeCoroutine = null;
			}

			FlushVisibility();
		}

		#endregion

		////////////////////////////////
		//	FadeGraphics

		#region FadeGraphics

		private void FlushVisibility( HashSet<Graphic> graphics = null ) =>
			ForceVisibility( _visible, graphics );
		private void ForceVisibility( bool visible, HashSet<Graphic> graphics = null)
		{
			if( object.Equals( null, graphics ) )
				graphics = new HashSet<Graphic>( GetComponentsInChildren<Graphic>() );

			foreach( Graphic graphic in graphics )
			{
				graphic.enabled = visible;
				graphic.CrossFadeAlpha( visible ? 1f : 0f, 0f, false );
			}
		}

		public void OnVisibilityChanged()
		{
			if( !Utils.IsObjectAlive( this ) )
				return;

			HashSet<Graphic> graphics = new HashSet<Graphic>( GetComponentsInChildren<Graphic>( true ) );

			foreach( Graphic ignoreGraphic in ignoreGraphics )
				if( Utils.IsObjectAlive( ignoreGraphic ) )
					graphics.Remove( ignoreGraphic );

#if UNITY_EDITOR
			if( !Application.isPlaying )
			{
				FlushVisibility( graphics );

				return;
			}
#endif

			if( !object.Equals( null, fadeCoroutine ) )
			{
				StopCoroutine( fadeCoroutine );
				fadeCoroutine = null;
			}

			if( fadeTime <= 0f )
			{
				FlushVisibility( graphics );
			}
			else
			{
				fadeCoroutine = _visible ? StartCoroutine( IFadeIn( graphics ) ) : StartCoroutine( IFadeOut( graphics ) );
			}
		}

		private IEnumerator IFadeOut( HashSet<Graphic> graphics )
		{
			foreach( Graphic graphic in graphics )
			{
				graphic.enabled = true;
				graphic.CrossFadeAlpha( 1f, 0f, false );
			}

			yield return null;

			foreach( Graphic graphic in graphics )
				graphic.CrossFadeAlpha( 0f, fadeTime, false );

			yield return new WaitForSeconds( fadeTime );

			yield return null;

			foreach( Graphic graphic in graphics )
				graphic.enabled = false;

			fadeCoroutine = null;

			yield break;
		}

		private IEnumerator IFadeIn( HashSet<Graphic> graphics )
		{
			foreach( Graphic graphic in graphics )
			{
				graphic.enabled = true;
				graphic.CrossFadeAlpha( 0f, 0f, false );
			}

			yield return null;

			foreach( Graphic graphic in graphics )
				graphic.CrossFadeAlpha( 1f, fadeTime, false );

			yield return new WaitForSeconds( fadeTime );

			fadeCoroutine = null;

			yield break;
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( FadeGraphics ) )]
	public class EditorFadeGraphics : EditorUtils.InheritedEditor
	{
		ReorderableList ignoreGraphics;

		private static HashSet<FadeGraphics> validParents = new HashSet<FadeGraphics>();

		public override void Setup()
		{
			base.Setup();

			validParents.Clear();

			foreach( Object target in targets )
			{
				FadeGraphics fadeGraphics = ( FadeGraphics ) target;

				if( Utils.IsObjectAlive( fadeGraphics ) )
				{
					validParents.Add( fadeGraphics );
				}
			}

			if( !Application.isPlaying )
				foreach( FadeGraphics fadeGraphics in validParents )
					fadeGraphics.OnVisibilityChanged();

			ignoreGraphics = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "ignoreGraphics" ),
				( SerializedProperty list, int index, SerializedProperty element ) =>
				{
					return lineHeight * 1.5f;
				},
				( Rect rect, SerializedProperty list, int index, SerializedProperty element, bool isActive, bool isFocussed ) =>
				{
					EditorGUI.BeginChangeCheck();

					EditorUtils.BetterObjectField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), new GUIContent(), element, typeof( Graphic ), true );

					if( EditorGUI.EndChangeCheck() )
					{
						Graphic graphic = ( Graphic ) element.objectReferenceValue;
						if( Utils.IsObjectAlive( graphic ) )
						{
							FadeGraphics fadeGraphics = graphic.GetComponentInParent<FadeGraphics>();
							if( Utils.IsObjectAlive( fadeGraphics ) )
							{
								if( !validParents.Contains( fadeGraphics ) )
								{
									element.objectReferenceValue = null;
								}
								else
								{
									for( int i = 0, iC = list.arraySize; i < iC; i++ )
									{
										if( i == index )
											continue;

										SerializedProperty indexedElement = list.GetArrayElementAtIndex( i );
										Graphic indexedGraphic = ( Graphic ) indexedElement.objectReferenceValue;

										if( Utils.IsObjectAlive( indexedGraphic ) && indexedGraphic == graphic )
										{
											element.objectReferenceValue = null;
											break;
										}
									}
								}
							}
							else
							{
								element.objectReferenceValue = null;
							}
						}

						serializedObject.ApplyModifiedProperties();

						foreach( FadeGraphics fadeGraphics in validParents )
							fadeGraphics.OnVisibilityChanged();
					}
				}
			);
		}

		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 3.5f + ignoreGraphics.CalculateCollapsableListHeight();

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "FadeGraphics" ) );
			bRect.y += lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );

			EditorGUI.BeginChangeCheck();
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Visible" ), this[ "_visible" ] );
			cRect.x += cRect.width + 10f;
			cRect.y += lineHeight * 0.25f;
			cRect.height = lineHeight;

			bool visiblityChanged = EditorGUI.EndChangeCheck();

			EditorGUIUtility.labelWidth = 70f;

			EditorGUI.PropertyField( cRect, this[ "fadeTime" ], new GUIContent( "Fade Time" ) );
			this[ "fadeTime" ].ClampMinimum( 0f );
			bRect.y += lineHeight * 2f;

			EditorGUIUtility.labelWidth = labelWidth;

			ignoreGraphics.DrawCollapsableList( ref bRect, new GUIContent( "Ignore Graphics" ) );

			rect.y = bRect.y;
			
			if( visiblityChanged )
			{
				serializedObject.ApplyModifiedProperties();

				foreach( FadeGraphics fadeGraphics in validParents )
					fadeGraphics.OnVisibilityChanged();
			}
		}
	}
#endif
}