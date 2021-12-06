using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline
{
	public class SafeObject<T> where T : Object
	{
		public bool hasValue { get; private set; }

		[SerializeField]
		private T obj;
		public T value
		{
			get => obj;
			set
			{
				hasValue = Utils.IsObjectAlive( obj );
				obj = value;
			}
		}

		public static implicit operator T( SafeObject<T> obj ) => obj.value;
	}

	public class SafeObject : SafeObject<Object>{ }
	public class SafeGameObject : SafeObject<GameObject>{ }
	public class SafeTransform : SafeObject<Transform>{ }

	public class SafeRectTransform : SafeObject<RectTransform>{ }
	public class SafeImage : SafeObject<Image>{ }
	public class SafeText : SafeObject<Text>{ }
	public class SafeSlider : SafeObject<Slider>{ }
	public class SafeButton : SafeObject<Button>{ }
	public class SafeInputField : SafeObject<InputField>{ }
	public class SafeScrollRect : SafeObject<ScrollRect>{ }

	public class SafeAnimator : SafeObject<Animator>{ }
	public class SafeAnimation : SafeObject<Animation>{ }
	public class SafeCamera : SafeObject<Camera>{ }

	public class SafeMeshFilter : SafeObject<MeshFilter>{ }
	public class SafeMeshRenderer : SafeObject<MeshRenderer>{ }
	public class SafeMeshCollider : SafeObject<MeshCollider>{ }

	public class SafeRigidbody : SafeObject<Rigidbody>{ }
	public class SafeCollider : SafeObject<Collider>{ }
	public class SafeBoxCollider : SafeObject<BoxCollider>{ }
	public class SafeSphereCollider : SafeObject<SphereCollider>{ }
	public class SafeCapsuleCollider : SafeObject<CapsuleCollider>{ }
	
	public class SafeRigidbody2D : SafeObject<Rigidbody2D>{ }
	public class SafeCollider2D : SafeObject<Collider2D>{ }
	public class SafeBoxCollider2D : SafeObject<BoxCollider2D>{ }
	public class SafeCircleCollider2D : SafeObject<CircleCollider2D>{ }
	public class SafeCapsuleCollider2D : SafeObject<CapsuleCollider2D>{ }

	public class SafeSprite : SafeObject<Sprite>{ }
	public class SafeTexture2D : SafeObject<Texture2D>{ }
	public class SafeMaterial : SafeObject<Material>{ }
	public class SafeShader : SafeObject<Shader>{ }
	public class SafeMesh : SafeObject<Mesh>{ }
	public class SafeAnimationClip : SafeObject<AnimationClip>{ }
	public class SafeAudioClip : SafeObject<AudioClip>{ }

#if UNITY_EDITOR
	public class SafeObjectTPropertyDrawer : EditorUtils.InheritedPropertyDrawer
	{
		public override float CalculatePropertyHeight( ref SerializedProperty property ) =>
			base.CalculatePropertyHeight( ref property ) + EditorGUI.GetPropertyHeight( property.FindPropertyRelative( "obj" ) );

		public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
		{
			base.DrawGUI( ref rect, ref property );

			SerializedProperty obj = property.FindPropertyRelative( "obj" );
			rect.height = EditorGUI.GetPropertyHeight( obj );
			EditorGUI.PropertyField( rect, obj );
			rect.y += rect.height;
			rect.height = lineHeight;
		}
	}
	
	[CustomPropertyDrawer(typeof(SafeObject))]
	public class SafeObjectPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeGameObject))]
	public class SafeGameObjectPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeTransform))]
	public class SafeTransformPropertyDrawer : SafeObjectTPropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeRectTransform))]
	public class SafeRectTransformPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeImage))]
	public class SafeImagePropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeText))]
	public class SafeTextPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeSlider))]
	public class SafeSliderPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeButton))]
	public class SafeButtonPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeInputField))]
	public class SafeInputFieldPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeScrollRect))]
	public class SafeScrollRectPropertyDrawer : SafeObjectTPropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeAnimator))]
	public class SafeAnimatorPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeAnimation))]
	public class SafeAnimationPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCamera))]
	public class SafeCameraPropertyDrawer : SafeObjectTPropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeMeshFilter))]
	public class SafeMeshFilterPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMeshRenderer))]
	public class SafeMeshRendererPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMeshCollider))]
	public class SafeMeshColliderPropertyDrawer : SafeObjectTPropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeRigidbody))]
	public class SafeRigidbodyPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCollider))]
	public class SafeColliderPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeBoxCollider))]
	public class SafeBoxColliderPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeSphereCollider))]
	public class SafeSphereColliderPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCapsuleCollider))]
	public class SafeCapsuleColliderPropertyDrawer : SafeObjectTPropertyDrawer {}
	
	[CustomPropertyDrawer(typeof(SafeRigidbody2D))]
	public class SafeRigidbody2DPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCollider2D))]
	public class SafeCollider2DPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeBoxCollider2D))]
	public class SafeBoxCollider2DPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCircleCollider2D))]
	public class SafeCircleCollider2DPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeCapsuleCollider2D))]
	public class SafeCapsuleCollider2DPropertyDrawer : SafeObjectTPropertyDrawer {}

	[CustomPropertyDrawer(typeof(SafeSprite))]
	public class SafeSpritePropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeTexture2D))]
	public class SafeTexture2DPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMaterial))]
	public class SafeMaterialPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeShader))]
	public class SafeShaderPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeMesh))]
	public class SafeMeshPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeAnimationClip))]
	public class SafeAnimationClipPropertyDrawer : SafeObjectTPropertyDrawer {}
	[CustomPropertyDrawer(typeof(SafeAudioClip))]
	public class SafeAudioClipPropertyDrawer : SafeObjectTPropertyDrawer {}
#endif
}