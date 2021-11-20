using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline
{
	[System.Serializable]
	public class EnumStateSwitcher
	{
		[SerializeField]
		protected int selected = 0;
		
		public virtual System.Enum GetSelected() => null;

		public T GetSelected<T>()
			where T : System.Enum
		{
			return ( T ) System.Convert.ChangeType( selected, typeof( T ).GetEnumUnderlyingType() );
		}

		public virtual void SetSelected( System.Enum select ) { }

		public void SetSelected<T>( T select )
			where T : System.Enum
		{
			int _select = ( int ) System.Convert.ChangeType( select, typeof( int ) );
			if( selected != _select )
			{
				int _selected = selected;
				selected = _select;

				OnChanged( _selected );
			}
		}

		public bool toggleGameObjects = false;

		public List<Object> objects = new List<Object>();

		public Object GetStateObject( int state ) =>
			state < 0 || state >= objects.Count ? null : objects[ state ];

		public Object this[ int state ] =>
			GetStateObject( state );

		public Object currentObject =>
			GetCurrentObject();

		public Object GetCurrentObject() =>
			GetStateObject( selected );

		public X GetCurrentObject<X>() where X : Object =>
			( X ) GetCurrentObject();

		public virtual System.Type GetEnumType() => null;
		public virtual System.Type GetObjectType() => typeof( Object );

		public virtual int Count => 0;

		public virtual Dictionary<string, int> GetEnumDictionary() =>
			new Dictionary<string, int>();

		public virtual bool IsIndexSelected( int index ) => false;

		[System.Serializable]
		public class IntToggleEvent : UnityEngine.Events.UnityEvent<int> { }

		public IntToggleEvent onToggleIntOn = new IntToggleEvent();
		public IntToggleEvent onToggleIntOff = new IntToggleEvent();

		protected virtual void OnChanged( int previous )
		{
			onToggleIntOff?.Invoke( previous );
			onToggleIntOn?.Invoke( selected );
		}

		public virtual void ActivateSelected() { }
		public virtual void DeactivateSelected() { }
	}

	public class EnumStateSwitcher<T,V> : EnumStateSwitcher
		where T : System.Enum
		where V : Object
	{
		public override System.Type GetEnumType() => typeof( T );
		public override System.Type GetObjectType() => typeof( V );

		public static bool ObjectIsGameObject = typeof( V ) == typeof( GameObject );

		private static int EnumLength = System.Enum.GetValues( typeof( T ) ).Length;
		public override int Count => EnumLength;
		
		private static Dictionary<T, int> EnumIndex;
		private static int[] EnumValues;

		private static Dictionary<string, int> EnumDictionary;
		
		static EnumStateSwitcher()
		{
			System.Array valuesArray = System.Enum.GetValues( typeof( T ) );

			EnumIndex = new Dictionary<T, int>();
			for( int i = 0, iC = valuesArray.Length; i < iC; i++ )
				EnumIndex.Add( ( T ) valuesArray.GetValue( i ), i );

			System.Type underlyingType = typeof( T ).GetEnumUnderlyingType();
			EnumValues = new int[ valuesArray.Length ];
			if( underlyingType == typeof( int ) )
			{
				EnumValues = valuesArray as int[];
			}
			else if( underlyingType == typeof( byte ) )
			{
				byte[] byteArray = valuesArray as byte[];
				for( int i = 0, iC = EnumValues.Length; i < iC; i++ )
					EnumValues[ i ] = byteArray[ i ];
			}
			else if( underlyingType == typeof( sbyte ) )
			{
				sbyte[] sbyteArray = valuesArray as sbyte[];
				for( int i = 0, iC = EnumValues.Length; i < iC; i++ )
					EnumValues[ i ] = sbyteArray[ i ];
			}
			else if( underlyingType == typeof( short ) )
			{
				short[] shortArray = valuesArray as short[];
				for( int i = 0, iC = EnumValues.Length; i < iC; i++ )
					EnumValues[ i ] = shortArray[ i ];
			}
			else if( underlyingType == typeof( ushort ) )
			{
				ushort[] ushortArray = valuesArray as ushort[];
				for( int i = 0, iC = EnumValues.Length; i < iC; i++ )
					EnumValues[ i ] = ushortArray[ i ];
			}
			else if( underlyingType == typeof( uint ) )
			{
				uint[] uintArray = valuesArray as uint[];
				for( int i = 0, iC = EnumValues.Length; i < iC; i++ )
					EnumValues[ i ] = ( int ) uintArray[ i ];
			}
			else
			{
				EnumValues = new int[ 0 ];
			}

			EnumDictionary = new Dictionary<string, int>();

			foreach( int value in EnumValues )
				EnumDictionary.Add( System.Enum.GetName( typeof( T ), value ), value );
		}

		public override System.Enum GetSelected()
		{
			System.Enum value = GetSelected<T>();
			return value;
		}

		public override void SetSelected( System.Enum select )
		{
			SetSelected<T>( ( T ) select );
		}

		public new T selected
		{
			get => GetSelected<T>();
			set => SetSelected<T>( value );
		}

		public V GetStateObject( T state ) =>
			( V ) base.GetStateObject( EnumIndex[ state ] );

		public X GetStateObject<X>( T state ) where X : V =>
			( X ) GetStateObject( state );
		
		public V this[ T state ] =>
			GetStateObject( state );

		public new V currentObject =>
			GetCurrentObject();

		public new V GetCurrentObject() =>
			( V ) base.GetStateObject( EnumIndex[ selected ] );

		public new X GetCurrentObject<X>() where X : V =>
			( X ) GetCurrentObject();

		public override Dictionary<string, int> GetEnumDictionary() =>
			new Dictionary<string, int>( EnumDictionary );

		public override bool IsIndexSelected( int index ) => base.selected == EnumValues[ index ];

		[System.Serializable]
		public class EnumToggleEvent : UnityEngine.Events.UnityEvent<T> { }

		public EnumToggleEvent onToggleEnumOn = new EnumToggleEvent();
		public EnumToggleEvent onToggleEnumOff = new EnumToggleEvent();

		protected override void OnChanged( int previous )
		{
			base.OnChanged( previous );

			T _previous = ( T ) System.Convert.ChangeType( previous, typeof( T ).GetEnumUnderlyingType() );
			T _selected = ( T ) System.Convert.ChangeType( selected, typeof( T ).GetEnumUnderlyingType() );

			onToggleEnumOff?.Invoke( _previous );

			Object objPrevious = EnumIndex.ContainsKey( _previous ) ? objects[ EnumIndex[ _previous ] ] : null;
			Object objSelected = EnumIndex.ContainsKey( _selected ) ? objects[ EnumIndex[ _selected ] ] : null;

			if( toggleGameObjects || ObjectIsGameObject )
			{
				if( Utils.IsObjectAlive( objPrevious ) )
				{
					if( objPrevious is GameObject )
					{
						GameObject gameObject = ( GameObject ) objPrevious;
						gameObject.SetActive( false );
					}
					else if( objPrevious is MonoBehaviour )
					{
						MonoBehaviour component = ( MonoBehaviour ) objPrevious;
						component.gameObject.SetActive( false );
					}
				}
				if( Utils.IsObjectAlive( objSelected ) )
				{
					if( objSelected is GameObject )
					{
						GameObject gameObject = ( GameObject ) objSelected;
						gameObject.SetActive( true );
					}
					else if( objSelected is MonoBehaviour )
					{
						MonoBehaviour component = ( MonoBehaviour ) objSelected;
						component.gameObject.SetActive( true );
					}
				}
			}
			else
			{
				if( Utils.IsObjectAlive( objPrevious ) && objPrevious is MonoBehaviour )
				{
					MonoBehaviour component = ( MonoBehaviour ) objPrevious;
					component.enabled = false;
				}

				if( Utils.IsObjectAlive( objSelected ) && objSelected is MonoBehaviour )
				{
					MonoBehaviour component = ( MonoBehaviour ) objSelected;
					component.enabled = true;
				}
			}

			onToggleEnumOn?.Invoke( _selected );
		}

		public override void ActivateSelected()
		{
			base.ActivateSelected();

			T _selected = ( T ) System.Convert.ChangeType( selected, typeof( T ).GetEnumUnderlyingType() );
			Object objSelected = objects[ EnumIndex[ _selected ] ];

			if( ObjectIsGameObject )
			{
				foreach( Object obj in objects )
				{
					if( Utils.IsObjectAlive( obj ) && !object.Equals( obj, objSelected ) )
					{
						GameObject gameObject = ( GameObject ) obj;
						gameObject.SetActive( false );
					}
				}
			}
			else
			{
				foreach( Object obj in objects )
				{
					if( Utils.IsObjectAlive( obj ) && !object.Equals( obj, objSelected ) && ( obj is MonoBehaviour ) )
					{
						MonoBehaviour component = ( MonoBehaviour ) obj;
						if( toggleGameObjects )
							component.gameObject.SetActive( false );
						else
							component.enabled = false;
					}
				}
			}
			
			if( Utils.IsObjectAlive( objSelected ) )
			{
				if( ObjectIsGameObject )
				{
					GameObject gameObject = ( GameObject ) objSelected;
					if( !gameObject.activeSelf )
						gameObject.SetActive( true );
				}
				else if( objSelected is MonoBehaviour )
				{
					MonoBehaviour component = ( MonoBehaviour ) objSelected;
					if( toggleGameObjects )
					{
						if( !component.gameObject.activeSelf )
							component.gameObject.SetActive( true );
					}
					else
					{
						if( !component.enabled )
							component.enabled = true;
					}
				}
			}

			onToggleEnumOn?.Invoke( _selected );
		}

		public override void DeactivateSelected()
		{
			base.DeactivateSelected();

			T _selected = ( T ) System.Convert.ChangeType( selected, typeof( T ).GetEnumUnderlyingType() );

			onToggleEnumOff?.Invoke( _selected );

			Object objSelected = objects[ EnumIndex[ _selected ] ];

			if( toggleGameObjects || typeof( V ) == typeof( GameObject ) )
			{
				if( Utils.IsObjectAlive( objSelected ) )
				{
					if( objSelected is GameObject )
					{
						GameObject gameObject = ( GameObject ) objSelected;
						gameObject.SetActive( false );
					}
					else if( objSelected is MonoBehaviour )
					{
						MonoBehaviour component = ( MonoBehaviour ) objSelected;
						component.gameObject.SetActive( false );
					}
				}
			}
			else
			{
				if( Utils.IsObjectAlive( objSelected ) && objSelected is MonoBehaviour )
				{
					MonoBehaviour component = ( MonoBehaviour ) objSelected;
					component.enabled = false;
				}
			}
		}
	}
	
#if UNITY_EDITOR
	[CustomPropertyDrawer( typeof( EnumStateSwitcher ) )]
	public class EditorEnumStateSwitcher : EditorUtils.InheritedPropertyDrawer
	{
		public override LabelMode labelMode => LabelMode.Foldout;

		public override float CalculatePropertyHeight( ref SerializedProperty property )
		{
			EnumStateSwitcher enumStateSwitcher = ( EnumStateSwitcher ) property.GetValue();

			if( object.Equals( null, enumStateSwitcher ) || object.Equals( null, enumStateSwitcher.GetEnumType() ) )
				return base.CalculatePropertyHeight( ref property );

			float inspectorHeight = base.CalculatePropertyHeight( ref property );

			inspectorHeight += lineHeight * 1.5f;

			if( enumStateSwitcher.GetObjectType() != typeof( GameObject ) )
				inspectorHeight += lineHeight * 2f;

			int enumCount = enumStateSwitcher.Count;

			inspectorHeight += lineHeight * enumCount + 4f * ( enumCount - 1 );

			return inspectorHeight;
		}
		
		public override void DrawFoldoutOverlay( Rect rect, Rect unusedRect, SerializedProperty property )
		{
			EnumStateSwitcher enumStateSwitcher = ( EnumStateSwitcher ) property.GetValue();

			if( object.Equals( null, enumStateSwitcher ) || object.Equals( null, enumStateSwitcher.GetEnumType() ) )
				return;

			System.Enum selected = enumStateSwitcher.GetSelected();
			System.Enum _selected = EditorGUI.EnumPopup( unusedRect, EditorUtils.EmptyContent, selected );

			if( _selected != selected )
			{
				selected = _selected;
				enumStateSwitcher.SetSelected( selected );
				property.serializedObject.Update();
			}
		}

		public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
		{
			EnumStateSwitcher enumStateSwitcher = ( EnumStateSwitcher ) property.GetValue();

			if( object.Equals( null, enumStateSwitcher ) || object.Equals( null, enumStateSwitcher.GetEnumType() ) )
			{
				base.DrawGUI( ref rect, ref property );
				return;
			}

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorGUIUtility.labelWidth = 70f;

			System.Enum selected = enumStateSwitcher.GetSelected();
			System.Enum _selected = EditorGUI.EnumPopup( bRect, new GUIContent( "Selected" ), selected );
			bRect.y += lineHeight * 1.5f;
			
			EditorGUIUtility.labelWidth = labelWidth;

			if( _selected != selected )
			{
				selected = _selected;
				enumStateSwitcher.SetSelected( selected );
				property.serializedObject.Update();
			}

			System.Type objectType = enumStateSwitcher.GetObjectType();

			if( objectType != typeof( GameObject ) )
			{
				bRect.height = lineHeight * 1.5f;
				EditorUtils.BetterToggleField( bRect, new GUIContent( "Toggle GameObjects" ), property.FindPropertyRelative( "toggleGameObjects" ) );
				bRect.height = lineHeight;
				bRect.y += lineHeight * 2f;
			}

			int enumCount = enumStateSwitcher.Count;

			bRect.x += 15f;
			bRect.width -= 15f;

			SerializedProperty objects = property.FindPropertyRelative( "objects" );

			while( objects.arraySize > enumCount )
				objects.DeleteArrayElementAtIndex( objects.arraySize - 1 );

			while( objects.arraySize < enumCount )
				objects.InsertArrayElementAtIndex( objects.arraySize );

			string[] enumNames = System.Enum.GetNames( enumStateSwitcher.GetEnumType() );

			float maxLabelWidth = 0f;
			foreach( string enumName in enumNames )
			{
				maxLabelWidth = Mathf.Max( maxLabelWidth, EditorStyles.label.CalcSize( new GUIContent( enumName ) ).x );
			}

			maxLabelWidth += 10f;

			for( int i = 0; i < enumCount; i++ )
			{
				bool isIndexSelected = enumStateSwitcher.IsIndexSelected( i );

				if( isIndexSelected )
					GUI.color = Color.yellow;

				EditorGUI.LabelField( new Rect( bRect.x, bRect.y, maxLabelWidth, bRect.height ), new GUIContent( enumNames[ i ] ) );
				Rect cRect = new Rect( bRect.x + maxLabelWidth, bRect.y, bRect.width - maxLabelWidth, bRect.height );

				if( isIndexSelected )
					GUI.color = Color.white;

				SerializedProperty element = objects.GetArrayElementAtIndex( i );

				bool isObjectMissing = !Utils.IsObjectAlive( element.objectReferenceValue );

				if( isObjectMissing )
					GUI.contentColor = new Color( 1f, 0.5f, 0.5f );

				EditorUtils.BetterObjectField( cRect, EditorUtils.EmptyContent, element, objectType, true );

				if( isObjectMissing )
					GUI.contentColor = Color.white;

				bRect.y += lineHeight;

				if( i < enumCount - 1 )
					bRect.y += 4f;
			}

			bRect.x -= 15f;
			bRect.width += 15f;

			rect.y = bRect.y;
		}
	}
#endif
}