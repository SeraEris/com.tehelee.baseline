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
	}

	public class SafeObject : SafeObject<Object>
	{
		public static implicit operator Object( SafeObject obj ) => obj.value;
	}
	public class SafeGameObject : SafeObject<GameObject>
	{
		public static implicit operator GameObject( SafeGameObject obj ) => obj.value;
	}
	public class SafeTransform : SafeObject<Transform>
	{
		public static implicit operator Transform( SafeTransform obj ) => obj.value;
	}

	public class SafeRectTransform : SafeObject<RectTransform>
	{
		public static implicit operator RectTransform( SafeRectTransform obj ) => obj.value;
	}
	public class SafeImage : SafeObject<Image>
	{
		public static implicit operator Image( SafeImage obj ) => obj.value;
	}
	public class SafeText : SafeObject<Text>
	{
		public static implicit operator Text( SafeText obj ) => obj.value;
	}
	public class SafeSlider : SafeObject<Slider>
	{
		public static implicit operator Slider( SafeSlider obj ) => obj.value;
	}
	public class SafeButton : SafeObject<Button>
	{
		public static implicit operator Button( SafeButton obj ) => obj.value;
	}
	public class SafeInputField : SafeObject<InputField>
	{
		public static implicit operator InputField( SafeInputField obj ) => obj.value;
	}
	public class SafeScrollRect : SafeObject<ScrollRect>
	{
		public static implicit operator ScrollRect( SafeScrollRect obj ) => obj.value;
	}

	public class SafeAnimator : SafeObject<Animator>
	{
		public static implicit operator Animator( SafeAnimator obj ) => obj.value;
	}
	public class SafeAnimation : SafeObject<Animation>
	{
		public static implicit operator Animation( SafeAnimation obj ) => obj.value;
	}
	public class SafeCamera : SafeObject<Camera>
	{
		public static implicit operator Camera( SafeCamera obj ) => obj.value;
	}

	public class SafeMeshFilter : SafeObject<MeshFilter>
	{
		public static implicit operator MeshFilter( SafeMeshFilter obj ) => obj.value;
	}
	public class SafeMeshRenderer : SafeObject<MeshRenderer>
	{
		public static implicit operator MeshRenderer( SafeMeshRenderer obj ) => obj.value;
	}
	public class SafeMeshCollider : SafeObject<MeshCollider>
	{
		public static implicit operator MeshCollider( SafeMeshCollider obj ) => obj.value;
	}

	public class SafeRigidbody : SafeObject<Rigidbody>
	{
		public static implicit operator Rigidbody( SafeRigidbody obj ) => obj.value;
	}
	public class SafeCollider : SafeObject<Collider>
	{
		public static implicit operator Collider( SafeCollider obj ) => obj.value;
	}
	public class SafeBoxCollider : SafeObject<BoxCollider>
	{
		public static implicit operator BoxCollider( SafeBoxCollider obj ) => obj.value;
	}
	public class SafeSphereCollider : SafeObject<SphereCollider>
	{
		public static implicit operator SphereCollider( SafeSphereCollider obj ) => obj.value;
	}
	public class SafeCapsuleCollider : SafeObject<CapsuleCollider>
	{
		public static implicit operator CapsuleCollider( SafeCapsuleCollider obj ) => obj.value;
	}
	
	public class SafeRigidbody2D : SafeObject<Rigidbody2D>
	{
		public static implicit operator Rigidbody2D( SafeRigidbody2D obj ) => obj.value;
	}
	public class SafeCollider2D : SafeObject<Collider2D>
	{
		public static implicit operator Collider2D( SafeCollider2D obj ) => obj.value;
	}
	public class SafeBoxCollider2D : SafeObject<BoxCollider2D>
	{
		public static implicit operator BoxCollider2D( SafeBoxCollider2D obj ) => obj.value;
	}
	public class SafeCircleCollider2D : SafeObject<CircleCollider2D>
	{
		public static implicit operator CircleCollider2D( SafeCircleCollider2D obj ) => obj.value;
	}
	public class SafeCapsuleCollider2D : SafeObject<CapsuleCollider2D>
	{
		public static implicit operator CapsuleCollider2D( SafeCapsuleCollider2D obj ) => obj.value;
	}

	public class SafeSprite : SafeObject<Sprite>
	{
		public static implicit operator Sprite( SafeSprite obj ) => obj.value;
	}
	public class SafeTexture2D : SafeObject<Texture2D>
	{
		public static implicit operator Texture2D( SafeTexture2D obj ) => obj.value;
	}
	public class SafeMaterial : SafeObject<Material>
	{
		public static implicit operator Material( SafeMaterial obj ) => obj.value;
	}
	public class SafeShader : SafeObject<Shader>
	{
		public static implicit operator Shader( SafeShader obj ) => obj.value;
	}
	public class SafeMesh : SafeObject<Mesh>
	{
		public static implicit operator Mesh( SafeMesh obj ) => obj.value;
	}
	public class SafeAnimationClip : SafeObject<AnimationClip>
	{
		public static implicit operator AnimationClip( SafeAnimationClip obj ) => obj.value;
	}
	public class SafeAudioClip : SafeObject<AudioClip>
	{
		public static implicit operator AudioClip( SafeAudioClip obj ) => obj.value;
	}

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