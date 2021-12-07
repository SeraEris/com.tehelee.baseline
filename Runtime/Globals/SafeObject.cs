using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline
{
	public class SafeObjectBase
	{
		public virtual System.Type GetObjectType() => default;
	}
	
	[System.Serializable]
	public class SafeObject<T> : SafeObjectBase where T : Object
	{
		private int _hasValue = -1;

		public bool hasValue
		{
			get
			{
				if( _hasValue < 0 || _hasValue > 1 )
					_hasValue = Utils.IsObjectAlive( obj ) ? 1 : 0;

				return _hasValue == 1;
			}
		}

		[SerializeField]
		private T obj;
		public T value
		{
			get => obj;
			set
			{
				obj = value;
				_hasValue = Utils.IsObjectAlive( obj ) ? 1 : 0;
			}
		}

		public override System.Type GetObjectType() => typeof( T );

		public static implicit operator T( SafeObject<T> obj ) => obj.value;

		public static implicit operator bool( SafeObject<T> obj ) => obj.hasValue;
	}

	[System.Serializable]
	public class SafeObject : SafeObject<Object>{ }
	[System.Serializable]
	public class SafeGameObject : SafeObject<GameObject>{ }
	[System.Serializable]
	public class SafeTransform : SafeObject<Transform>{ }

	[System.Serializable]
	public class SafeRectTransform : SafeObject<RectTransform>{ }
	[System.Serializable]
	public class SafeImage : SafeObject<Image>{ }
	[System.Serializable]
	public class SafeText : SafeObject<Text>{ }
	[System.Serializable]
	public class SafeSlider : SafeObject<Slider>{ }
	[System.Serializable]
	public class SafeButton : SafeObject<Button>{ }
	[System.Serializable]
	public class SafeInputField : SafeObject<InputField>{ }
	[System.Serializable]
	public class SafeScrollRect : SafeObject<ScrollRect>{ }

	[System.Serializable]
	public class SafeAnimator : SafeObject<Animator>{ }
	[System.Serializable]
	public class SafeAnimation : SafeObject<Animation>{ }
	[System.Serializable]
	public class SafeCamera : SafeObject<Camera>{ }

	[System.Serializable]
	public class SafeMeshFilter : SafeObject<MeshFilter>{ }
	[System.Serializable]
	public class SafeMeshRenderer : SafeObject<MeshRenderer>{ }
	[System.Serializable]
	public class SafeMeshCollider : SafeObject<MeshCollider>{ }

	[System.Serializable]
	public class SafeRigidbody : SafeObject<Rigidbody>{ }
	[System.Serializable]
	public class SafeCollider : SafeObject<Collider>{ }
	[System.Serializable]
	public class SafeBoxCollider : SafeObject<BoxCollider>{ }
	[System.Serializable]
	public class SafeSphereCollider : SafeObject<SphereCollider>{ }
	[System.Serializable]
	public class SafeCapsuleCollider : SafeObject<CapsuleCollider>{ }
	
	[System.Serializable]
	public class SafeRigidbody2D : SafeObject<Rigidbody2D>{ }
	[System.Serializable]
	public class SafeCollider2D : SafeObject<Collider2D>{ }
	[System.Serializable]
	public class SafeBoxCollider2D : SafeObject<BoxCollider2D>{ }
	[System.Serializable]
	public class SafeCircleCollider2D : SafeObject<CircleCollider2D>{ }
	[System.Serializable]
	public class SafeCapsuleCollider2D : SafeObject<CapsuleCollider2D>{ }

	[System.Serializable]
	public class SafeSprite : SafeObject<Sprite>{ }
	[System.Serializable]
	public class SafeTexture2D : SafeObject<Texture2D>{ }
	[System.Serializable]
	public class SafeMaterial : SafeObject<Material>{ }
	[System.Serializable]
	public class SafeShader : SafeObject<Shader>{ }
	[System.Serializable]
	public class SafeMesh : SafeObject<Mesh>{ }
	[System.Serializable]
	public class SafeAnimationClip : SafeObject<AnimationClip>{ }
	[System.Serializable]
	public class SafeAudioClip : SafeObject<AudioClip>{ }

#if UNITY_EDITOR
	public class SafeObjectBasePropertyDrawer : EditorUtils.InheritedPropertyDrawer
	{
		public override float CalculatePropertyHeight( ref SerializedProperty property ) =>
			base.CalculatePropertyHeight( ref property ) + EditorGUI.GetPropertyHeight( property.FindPropertyRelative( "obj" ) );

		public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
		{
			base.DrawGUI( ref rect, ref property );

			SerializedProperty obj = property.FindPropertyRelative( "obj" );
			rect.height = EditorGUI.GetPropertyHeight( obj );
			EditorUtils.BetterObjectField( rect, new GUIContent( property.displayName ), obj, ( ( SafeObjectBase ) property.GetValue() ).GetObjectType(), true );
			rect.y += rect.height;
			rect.height = lineHeight;
		}
	}
	
	[CustomPropertyDrawer(typeof(SafeObject))]
	public class SafeObjectPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeGameObject))]
	public class SafeGameObjectPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeTransform))]
	public class SafeTransformPropertyDrawer : SafeObjectBasePropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeRectTransform))]
	public class SafeRectTransformPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeImage))]
	public class SafeImagePropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeText))]
	public class SafeTextPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeSlider))]
	public class SafeSliderPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeButton))]
	public class SafeButtonPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeInputField))]
	public class SafeInputFieldPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeScrollRect))]
	public class SafeScrollRectPropertyDrawer : SafeObjectBasePropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeAnimator))]
	public class SafeAnimatorPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeAnimation))]
	public class SafeAnimationPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCamera))]
	public class SafeCameraPropertyDrawer : SafeObjectBasePropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeMeshFilter))]
	public class SafeMeshFilterPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMeshRenderer))]
	public class SafeMeshRendererPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMeshCollider))]
	public class SafeMeshColliderPropertyDrawer : SafeObjectBasePropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeRigidbody))]
	public class SafeRigidbodyPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCollider))]
	public class SafeColliderPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeBoxCollider))]
	public class SafeBoxColliderPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeSphereCollider))]
	public class SafeSphereColliderPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCapsuleCollider))]
	public class SafeCapsuleColliderPropertyDrawer : SafeObjectBasePropertyDrawer {}
	
	[CustomPropertyDrawer(typeof(SafeRigidbody2D))]
	public class SafeRigidbody2DPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCollider2D))]
	public class SafeCollider2DPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeBoxCollider2D))]
	public class SafeBoxCollider2DPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCircleCollider2D))]
	public class SafeCircleCollider2DPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCapsuleCollider2D))]
	public class SafeCapsuleCollider2DPropertyDrawer : SafeObjectBasePropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeSprite))]
	public class SafeSpritePropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeTexture2D))]
	public class SafeTexture2DPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMaterial))]
	public class SafeMaterialPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeShader))]
	public class SafeShaderPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMesh))]
	public class SafeMeshPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeAnimationClip))]
	public class SafeAnimationClipPropertyDrawer : SafeObjectBasePropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeAudioClip))]
	public class SafeAudioClipPropertyDrawer : SafeObjectBasePropertyDrawer {}
#endif
}