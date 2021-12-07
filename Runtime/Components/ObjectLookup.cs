using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Text = TMPro.TextMeshProUGUI;
using InputField = TMPro.TMP_InputField;

namespace Tehelee.Baseline.Components
{
	public class ObjectLookup : MonoBehaviour
	{
		static ObjectLookup()
		{
			objectTypes = new Dictionary<string, HashSet<System.Type>>();

			HashSet<System.Type> general = new HashSet<System.Type>();

			general.Add( typeof( Object ) );
			general.Add( typeof( GameObject ) );
			general.Add( typeof( Transform ) );

			objectTypes.Add( "General", general );

			HashSet<System.Type> ui = new HashSet<System.Type>();

			ui.Add( typeof( RectTransform ) );
			ui.Add( typeof( Image ) );
			ui.Add( typeof( Text ) );
			ui.Add( typeof( Slider ) );
			ui.Add( typeof( Button ) );
			ui.Add( typeof( InputField ) );
			ui.Add( typeof( ScrollRect ) );

			objectTypes.Add( "UI", ui );

			HashSet<System.Type> components = new HashSet<System.Type>();

			components.Add( typeof( Animator ) );
			components.Add( typeof( Animation ) );
			components.Add( typeof( Camera ) );

			objectTypes.Add( "Components", components );

			HashSet<System.Type> mesh = new HashSet<System.Type>();

			mesh.Add( typeof( MeshFilter ) );
			mesh.Add( typeof( MeshRenderer ) );
			mesh.Add( typeof( MeshCollider ) );

			objectTypes.Add( "Mesh", mesh );

			HashSet<System.Type> physics = new HashSet<System.Type>();

			physics.Add( typeof( Rigidbody ) );
			physics.Add( typeof( Collider ) );
			physics.Add( typeof( BoxCollider ) );
			physics.Add( typeof( SphereCollider ) );
			physics.Add( typeof( CapsuleCollider ) );

			objectTypes.Add( "Physics", physics );

			HashSet<System.Type> physics2D = new HashSet<System.Type>();

			physics2D.Add( typeof( Rigidbody2D ) );
			physics2D.Add( typeof( Collider2D ) );
			physics2D.Add( typeof( BoxCollider2D ) );
			physics2D.Add( typeof( CircleCollider2D ) );
			physics2D.Add( typeof( CapsuleCollider2D ) );

			objectTypes.Add( "Physics 2D", physics2D );

			HashSet<System.Type> resources = new HashSet<System.Type>();

			resources.Add( typeof( Sprite ) );
			resources.Add( typeof( Texture2D ) );
			resources.Add( typeof( Material ) );
			resources.Add( typeof( Shader ) );
			resources.Add( typeof( Mesh ) );
			resources.Add( typeof( AnimationClip ) );
			resources.Add( typeof( AudioClip ) );

			objectTypes.Add( "Resources", resources );
		}

		public static Dictionary<string, HashSet<System.Type>> objectTypes;

		private static System.Type baseType = typeof( UnityEngine.Object );

		[System.Serializable]
		public class ObjectReference
		{
			public string objectCategory = string.Empty;

			public string castType = string.Empty;

			public string key = string.Empty;

			public UnityEngine.Object reference;

			private static T Convert<T>( ObjectReference objectReference ) where T : Object
			{
				if( object.Equals( null, objectReference ) )
					return objectReference;
				
				if( string.IsNullOrEmpty( objectReference.castType ) || !string.Equals( objectReference.castType, typeof( T ).Name ) )
					return null;

				if( !Utils.IsObjectAlive( objectReference.reference ) )
					return null;
				
				return ( T ) ( Object ) objectReference.reference;
			}

			public static implicit operator Object( ObjectReference objectReference ) => Convert<Object>( objectReference );
			public static implicit operator GameObject( ObjectReference objectReference ) => Convert<GameObject>( objectReference );
			public static implicit operator Transform( ObjectReference objectReference ) => Convert<Transform>( objectReference );

			public static implicit operator RectTransform( ObjectReference objectReference ) => Convert<RectTransform>( objectReference );
			public static implicit operator Image( ObjectReference objectReference ) => Convert<Image>( objectReference );
			public static implicit operator Text( ObjectReference objectReference ) => Convert<Text>( objectReference );
			public static implicit operator Slider( ObjectReference objectReference ) => Convert<Slider>( objectReference );
			public static implicit operator Button( ObjectReference objectReference ) => Convert<Button>( objectReference );
			public static implicit operator InputField( ObjectReference objectReference ) => Convert<InputField>( objectReference );
			public static implicit operator ScrollRect( ObjectReference objectReference ) => Convert<ScrollRect>( objectReference );

			public static implicit operator Animator( ObjectReference objectReference ) => Convert<Animator>( objectReference );
			public static implicit operator Animation( ObjectReference objectReference ) => Convert<Animation>( objectReference );
			public static implicit operator Camera( ObjectReference objectReference ) => Convert<Camera>( objectReference );

			public static implicit operator MeshFilter( ObjectReference objectReference ) => Convert<MeshFilter>( objectReference );
			public static implicit operator MeshRenderer( ObjectReference objectReference ) => Convert<MeshRenderer>( objectReference );
			public static implicit operator MeshCollider( ObjectReference objectReference ) => Convert<MeshCollider>( objectReference );

			public static implicit operator Rigidbody( ObjectReference objectReference ) => Convert<Rigidbody>( objectReference );
			public static implicit operator Collider( ObjectReference objectReference ) => Convert<Collider>( objectReference );
			public static implicit operator BoxCollider( ObjectReference objectReference ) => Convert<BoxCollider>( objectReference );
			public static implicit operator SphereCollider( ObjectReference objectReference ) => Convert<SphereCollider>( objectReference );
			public static implicit operator CapsuleCollider( ObjectReference objectReference ) => Convert<CapsuleCollider>( objectReference );

			public static implicit operator Rigidbody2D( ObjectReference objectReference ) => Convert<Rigidbody2D>( objectReference );
			public static implicit operator Collider2D( ObjectReference objectReference ) => Convert<Collider2D>( objectReference );
			public static implicit operator BoxCollider2D( ObjectReference objectReference ) => Convert<BoxCollider2D>( objectReference );
			public static implicit operator CircleCollider2D( ObjectReference objectReference ) => Convert<CircleCollider2D>( objectReference );
			public static implicit operator CapsuleCollider2D( ObjectReference objectReference ) => Convert<CapsuleCollider2D>( objectReference );

			public static implicit operator Sprite( ObjectReference objectReference ) => Convert<Sprite>( objectReference );
			public static implicit operator Texture2D( ObjectReference objectReference ) => Convert<Texture2D>( objectReference );
			public static implicit operator Material( ObjectReference objectReference ) => Convert<Material>( objectReference );
			public static implicit operator Shader( ObjectReference objectReference ) => Convert<Shader>( objectReference );
			public static implicit operator Mesh( ObjectReference objectReference ) => Convert<Mesh>( objectReference );
			public static implicit operator AnimationClip( ObjectReference objectReference ) => Convert<AnimationClip>( objectReference );
			public static implicit operator AudioClip( ObjectReference objectReference ) => Convert<AudioClip>( objectReference );
		}

#if UNITY_EDITOR
		[CustomPropertyDrawer( typeof( ObjectReference ) )]
		public class ObjectReferencePropertyDrawer : EditorUtils.InheritedPropertyDrawer
		{
			static GUIContent[] categoryKeys;

			static GUIContent[][] categoryTypeNames;

			static System.Type[][] categoryTypes;

			static GUIContent[] categoryEmpty = new GUIContent[] { new GUIContent( " - " ) };

			static ObjectReferencePropertyDrawer()
			{
				List<GUIContent> _categoryKeys = new List<GUIContent>();

				_categoryKeys.Add( categoryEmpty[ 0 ] );

				List<string> __categoryKeys = new List<string>( ObjectLookup.objectTypes.Keys );

				foreach( string key in __categoryKeys )
				{
					_categoryKeys.Add( new GUIContent( key ) );
				}

				categoryKeys = _categoryKeys.ToArray();

				int length = Mathf.Max( 0, categoryKeys.Length - 1 );
				categoryTypeNames = new GUIContent[ length + 1 ][];
				categoryTypes = new System.Type[ length ][];

				for( int i = 1, iC = categoryKeys.Length; i < iC; i++ )
				{
					List<GUIContent> typeNames = new List<GUIContent>();
					List<System.Type> types = new List<System.Type>();

					typeNames.Add( categoryEmpty[ 0 ] );

					HashSet<System.Type> hashTypes = ObjectLookup.objectTypes[ categoryKeys[ i ].text ];

					foreach( System.Type type in hashTypes )
					{
						typeNames.Add( new GUIContent( type.Name ) );
						types.Add( type );
					}

					categoryTypeNames[ i - 1 ] = typeNames.ToArray();
					categoryTypes[ i - 1 ] = types.ToArray();
				}
			}

			public override float CalculatePropertyHeight( ref SerializedProperty property )
			{
				float propertyHeight = base.CalculatePropertyHeight( ref property );

				propertyHeight += lineHeight * 4f;

				return propertyHeight;
			}

			public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
			{
				base.DrawGUI( ref rect, ref property );

				SerializedProperty objectCategory = property.FindPropertyRelative( "objectCategory" );
				SerializedProperty castType = property.FindPropertyRelative( "castType" );
				SerializedProperty key = property.FindPropertyRelative( "key" );
				SerializedProperty reference = property.FindPropertyRelative( "reference" );

				Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

				EditorGUI.PropertyField( bRect, key, new GUIContent() );

				bRect.y += lineHeight * 1.5f;

				bool keyNull = string.IsNullOrEmpty( key.stringValue );

				Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight );

				int categoryIndex = 0;

				string _objectCategory = objectCategory.stringValue;

				if( !string.IsNullOrEmpty( _objectCategory ) )
				{
					for( int i = 1, iC = categoryKeys.Length; i < iC; i++ )
					{
						if( string.Equals( categoryKeys[ i ].text, _objectCategory ) )
						{
							categoryIndex = i;
							break;
						}
					}
				}

				int typeIndex = 0;

				string __castType = castType.stringValue;
				System.Type _castType = null;

				if( ( categoryIndex > 0 ) && !string.IsNullOrEmpty( __castType ) )
				{
					System.Type[] _categoryTypes = categoryTypes[ categoryIndex - 1 ];
					for( int i = 0, iC = _categoryTypes.Length; i < iC; i++ )
					{
						if( string.Equals( _categoryTypes[ i ].Name, __castType ) )
						{
							typeIndex = i + 1;

							_castType = _categoryTypes[ i ];

							break;
						}
					}
				}

				int _categoryIndex = EditorGUI.Popup( cRect, categoryIndex, categoryKeys );

				if( _categoryIndex != categoryIndex )
				{
					categoryIndex = _categoryIndex;

					objectCategory.stringValue = categoryIndex == 0 ? string.Empty : categoryKeys[ categoryIndex ].text;

					typeIndex = 0;

					_castType = null;

					castType.stringValue = string.Empty;

					reference.objectReferenceValue = null;
				}

				cRect.x += cRect.width + 10f;

				bool categoryNull = categoryIndex == 0;

				EditorGUI.BeginDisabledGroup( categoryNull );

				int _typeIndex = EditorGUI.Popup( cRect, categoryNull ? 0 : typeIndex, categoryNull ? categoryEmpty : categoryTypeNames[ categoryIndex - 1 ] );

				EditorGUI.EndDisabledGroup();

				if( _typeIndex != typeIndex )
				{
					typeIndex = _typeIndex;

					_castType = categoryTypes[ categoryIndex - 1 ][ typeIndex - 1 ];

					castType.stringValue = _castType.Name;

					reference.objectReferenceValue = null;
				}

				bRect.y += lineHeight * 1.5f;

				bool typeNull = ( typeIndex == 0 );

				if( categoryNull || typeNull || keyNull )
				{
					EditorGUI.BeginDisabledGroup( true );

					EditorUtils.BetterObjectField( bRect, new GUIContent(), reference, ObjectLookup.baseType, false );

					EditorGUI.EndDisabledGroup();
				}
				else
				{
					EditorUtils.BetterObjectField( bRect, new GUIContent(), reference, _castType, true );
				}

				rect.y += lineHeight * 2.5f;
			}
		}
#endif

		public List<ObjectReference> objectReferences = new List<ObjectReference>();

		private Dictionary<string, ObjectReference> _objectReferenceLookup = null;
		private Dictionary<string, ObjectReference> objectReferenceLookup
		{
			get
			{
				if( object.Equals( null, _objectReferenceLookup ) )
				{
					_objectReferenceLookup = new Dictionary<string, ObjectReference>();

					foreach( ObjectReference objectReference in objectReferences )
					{
						if( string.IsNullOrEmpty( objectReference.key ) )
						{
							if( !object.Equals( null, objectReference.reference ) )
								Debug.LogWarningFormat( this, "An ObjectReference has an empty key with data ( {0} )!", objectReference.reference );

							continue;
						}

						if( _objectReferenceLookup.ContainsKey( objectReference.key ) )
						{
							Debug.LogWarningFormat( this, "An ObjectReference contains multiple instances of '{0}'!", objectReference.key );

							continue;
						}

						_objectReferenceLookup.Add( objectReference.key, objectReference );
					}
				}

				return _objectReferenceLookup;
			}
		}

		public ObjectReference this[ string key ]
		{
			get
			{
				if( string.IsNullOrEmpty( key ) )
					return null;

				if( !objectReferenceLookup.ContainsKey( key ) )
					return null;

				return _objectReferenceLookup[ key ];
			}
		}

		public void Insert( string key, Object value )
		{
			if( object.Equals( null, value ) )
			{
				if( objectReferenceLookup.ContainsKey( key ) )
				{
					_objectReferenceLookup.Remove( key );

					objectReferences = new List<ObjectReference>( _objectReferenceLookup.Values );
				}

				return;
			}

			System.Type type = value.GetType();

			ObjectReference objectReference = null;

			List<string> objectTypeCategories = new List<string>( objectTypes.Keys );
			foreach( string objectTypeCategory in objectTypeCategories )
			{
				HashSet<System.Type> objectTypeMatches = objectTypes[ objectTypeCategory ];
				foreach( System.Type objectTypeMatch in objectTypeMatches )
				{
					if( type.Equals( objectTypeMatch ) )
					{
						objectReference = new ObjectReference();
						objectReference.key = key;
						objectReference.objectCategory = objectTypeCategory;
						objectReference.castType = objectTypeMatch.Name;
						objectReference.reference = value;

						break;
					}
				}
			}
			
			if( !object.Equals( null, objectReference ) )
			{
				if( objectReferenceLookup.ContainsKey( key ) )
				{
					_objectReferenceLookup[ key ] = objectReference;
				}
				else
				{
					_objectReferenceLookup.Add( key, objectReference );
				}

				objectReferences = new List<ObjectReference>( _objectReferenceLookup.Values );
			}
		}

		public void Remove( string key )
		{
			if( objectReferenceLookup.ContainsKey( key ) )
			{
				_objectReferenceLookup.Remove( key );

				objectReferences = new List<ObjectReference>( _objectReferenceLookup.Values );
			}
		}

		private void OnEnable()
		{
			int objectCount = objectReferenceLookup.Count;
		}

		private void OnDisable()
		{
			_objectReferenceLookup = null;
		}
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( ObjectLookup ) )]
	public class EditorObjectLookup : EditorUtils.InheritedEditor
	{
		ReorderableList objectReferences;

		public override void Setup()
		{
			base.Setup();

			objectReferences = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "objectReferences" ),
				( SerializedProperty element ) =>
				{
					return EditorGUI.GetPropertyHeight( element, true ) + lineHeight;
				},
				( Rect rect, SerializedProperty element, int index, bool isActive, bool isFocussed ) =>
				{
					EditorGUI.PropertyField( new Rect( rect.x, rect.y + lineHeight * 0.5f, rect.width, rect.height - lineHeight ), element );
				}
			);

			objectReferences.onAddCallback = ( ReorderableList _list ) =>
			{
				SerializedProperty list = _list.serializedProperty;

				int arraySize = list.arraySize;
				list.InsertArrayElementAtIndex( arraySize );

				SerializedProperty element = list.GetArrayElementAtIndex( arraySize );

				element.FindPropertyRelative( "objectCategory" ).stringValue = string.Empty;
				element.FindPropertyRelative( "castType" ).stringValue = string.Empty;
				element.FindPropertyRelative( "key" ).stringValue = string.Empty;
				element.FindPropertyRelative( "reference" ).objectReferenceValue = null;
			};
		}
		
		public override float GetInspectorHeight() => base.GetInspectorHeight() + objectReferences.GetHeight() + lineHeight * 1.5f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Object Lookup" ) );
			bRect.y += lineHeight * 1.5f;
			
			bRect.height = objectReferences.GetHeight();
			objectReferences.DoList( bRect );

			bRect.y += bRect.height;
			
			rect.y = bRect.y;
		}
	}
#endif
}