using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Type = System.Type;

namespace Tehelee.Baseline.DesignData
{
	public class Lookup : MonoBehaviour
	{
		public List<Data> data = new List<Data>();

		private void OnEnable()
		{
			foreach( Data _data in data )
			{
				Populate( _data );
			}
		}

		private static Dictionary<Type, List<Data>> lookup = new Dictionary<Type, List<Data>>();

		private static Type typeData = typeof( Data ), typeCollection = typeof( Collection );

		public static void Populate( Data data )
		{
			if( object.Equals( null, data ) )
				return;

			Type typeCurrent = data.GetType();

			if( typeCollection.IsAssignableFrom( typeCurrent ) || typeCollection == typeCurrent )
			{
				Collection collection = ( Collection ) data;

				PopulateCollection( ref collection );
			}

			PopulateData( ref data );
		}

		private static void PopulateCollection( ref Collection collection )
		{
			foreach( Data data in collection.data )
			{
				Populate( data );
			}
		}

		private static void PopulateData( ref Data data )
		{
			Type typeCurrent = data.GetType();

			while( typeData.IsAssignableFrom( typeCurrent ) || typeData == typeCurrent )
			{
				if( !lookup.ContainsKey( typeCurrent ) )
					lookup.Add( typeCurrent, new List<Data>() );

				lookup[ typeCurrent ].Add( data );

				typeCurrent = typeCurrent.BaseType;
			}
		}

		
		public static List<T> GetAllData<T>() where T : Data
		{
			Type typeT = typeof( T );

			if( !lookup.ContainsKey( typeT ) )
				return new List<T>();

			List<T> data = new List<T>();
			List<Data> _lookup = lookup[ typeT ];

			foreach( Data _data in _lookup )
			{
				Type typeCurrent = _data.GetType();

				if( typeT.IsAssignableFrom( typeCurrent ) || typeT == typeCurrent )
				{
					data.Add( ( T ) _data );
				}
			}

			return data;
		}

		public static T Find<T>( ushort hash ) where T : Data
		{
			Type typeT = typeof( T );

			if( !lookup.ContainsKey( typeT ) )
				return null;

			List<Data> _lookup = lookup[ typeT ];

			foreach( Data _data in _lookup )
			{
				if( _data.dataHash == hash )
				{
					return ( T ) _data;
				}
			}

			return null;
		}

		public static T Find<T>( string path ) where T : Data
		{
			Type typeT = typeof( T );

			if( !lookup.ContainsKey( typeT ) )
				return null;

			List<Data> _lookup = lookup[ typeT ];

			foreach( Data _data in _lookup )
			{
				if( path == string.Format( "{0}/{1}", _data.path, _data.name ) )
				{
					return ( T ) _data;
				}
			}

			return null;
		}

		public static Data Search( ushort hash )
		{
			return Find<Data>( hash );
		}

		public static Data Search( string path )
		{
			return Find<Data>( path );
		}
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( Lookup ) )]
	public class EditorLookup : EditorUtils.InheritedEditor
	{
		ReorderableList dataList;

		Lookup lookup;
		
		public override void Setup()
		{
			base.Setup();

			dataList = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "data" ),
				( SerializedProperty element ) =>
				{
					return lineHeight * 1.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					EditorUtils.BetterObjectField<Data>( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), EditorUtils.EmptyContent, element );
				}
			);

			lookup = ( Lookup ) target;
		}
	}
#endif
}