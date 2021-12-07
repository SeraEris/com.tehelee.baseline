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
	public class ImageFader : MonoBehaviour
	{
		////////////////////////////////
		#region Attributes

		public Image image;
		
		public float fadeTime = 0f;

		#endregion

		////////////////////////////////
		#region Properties
		
		[SerializeField]
		private Sprite _sprite = null;
		public Sprite sprite
		{
			get => _sprite;
			set
			{
				_sprite = value;

				OnSpriteChanged();
			}
		}

		#endregion

		////////////////////////////////
		#region Members

		private Coroutine transitionCoroutine = null;
		private Image transitionImage = null;

		#endregion

		////////////////////////////////
		#region Mono Methods

		protected virtual void OnEnable()
		{
			if( Utils.IsObjectAlive( image ) )
			{
				if( Utils.IsObjectAlive( _sprite ) )
				{
					image.sprite = _sprite;
					image.enabled = true;
					image.CrossFadeAlpha( 1f, 0f, false );
				}
				else
				{
					image.sprite = null;
					image.enabled = false;
					image.CrossFadeAlpha( 0f, 0f, false );
				}
			}
		}

		protected virtual void OnDisable()
		{

		}

		#endregion

		////////////////////////////////
		#region ImageFader

		public void OnSpriteChanged()
		{
			if( !Utils.IsObjectAlive( this ) )
				return;

#if UNITY_EDITOR
			if( !Application.isPlaying )
			{
				if( Utils.IsObjectAlive( _sprite ) )
				{
					image.sprite = _sprite;
					image.enabled = true;
					image.CrossFadeAlpha( 1f, 0f, true );
				}
				else
				{
					image.sprite = null;
					image.enabled = false;
					image.CrossFadeAlpha( 0f, 0f, true );
				}

				return;
			}
#endif
			if( !object.Equals( null, transitionCoroutine ) )
			{
				StopCoroutine( transitionCoroutine );
				transitionCoroutine = null;
			}

			if( Utils.IsObjectAlive( transitionImage ) )
			{
				transitionImage.enabled = false;
				GameObject.Destroy( transitionImage );
				transitionImage = null;
			}

			if( Utils.IsObjectAlive( image ) )
			{
				if( Utils.IsObjectAlive( _sprite ) )
				{

					transitionCoroutine = StartCoroutine( FadeIn( _sprite ) );
				}
				else
				{
					if( Utils.IsObjectAlive( image.sprite ) )
					{

						transitionCoroutine = StartCoroutine( FadeOut() );
					}
					else
					{
						image.CrossFadeAlpha( 0f, 0f, false );
					}
				}
			}
		}

		private IEnumerator FadeIn( Sprite sprite )
		{
			if( !Utils.IsObjectAlive( image ) )
				yield break;

			transitionImage = GameObject.Instantiate( image.gameObject, image.transform ).GetComponent<Image>();
			transitionImage.gameObject.hideFlags = HideFlags.HideAndDontSave;
			
			transitionImage.CrossFadeAlpha( 0f, 0f, false );
			yield return null;

			transitionImage.sprite = sprite;
			transitionImage.enabled = true;
			transitionImage.CrossFadeAlpha( 1f, fadeTime, false );

			yield return new WaitForSeconds( fadeTime );

			image.sprite = sprite;
			image.enabled = true;
			image.CrossFadeAlpha( 1f, 0f, false );

			yield return null;

			GameObject.Destroy( transitionImage.gameObject );
			transitionImage = null;

			transitionCoroutine = null;
			
			yield break;
		}

		private IEnumerator FadeOut()
		{
			if( !Utils.IsObjectAlive( image ) )
				yield break;

			image.CrossFadeAlpha( 1f, 0f, false );
			yield return null;

			image.CrossFadeAlpha( 0f, fadeTime, false );

			yield return new WaitForSeconds( fadeTime );
			yield return null;

			image.sprite = null;
			image.enabled = false;

			yield break;
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( ImageFader ) )]
	public class EditorHideEmptyImage : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 4.5f + 8f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "ImageFader" ) );
			bRect.y += lineHeight * 1.5f;

			EditorGUIUtility.labelWidth = 90f;

			EditorGUI.BeginChangeCheck();

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Image" ), this[ "image" ], typeof( Image ), true );
			bRect.y += lineHeight + 4f;

			bool spriteChanged = false;

			if( EditorGUI.EndChangeCheck() )
			{
				Image image = ( Image ) this[ "image" ].objectReferenceValue;
				if( Utils.IsObjectAlive( image ) && Utils.IsObjectAlive( image.sprite ) )
				{
					this[ "_sprite" ].objectReferenceValue = image.sprite;
					spriteChanged = true;
				}
			}

			EditorGUI.BeginChangeCheck();

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Sprite" ), this[ "_sprite" ], typeof( Sprite ) );
			bRect.y += lineHeight + 4f;

			spriteChanged = EditorGUI.EndChangeCheck() || spriteChanged;

			EditorGUI.PropertyField( bRect, this[ "fadeTime" ], new GUIContent( "Fade Time" ) );
			this[ "fadeTime" ].ClampMinimum( 0f );
			bRect.y += lineHeight;

			EditorGUIUtility.labelWidth = labelWidth;

			rect.y = bRect.y;

			if( spriteChanged )
			{
				serializedObject.ApplyModifiedProperties();
				foreach( Object target in targets )
				{
					ImageFader imageFader = ( ImageFader ) target;
					imageFader.OnSpriteChanged();
				}
			}
		}
	}
#endif
}