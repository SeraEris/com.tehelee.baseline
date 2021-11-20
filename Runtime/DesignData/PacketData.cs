using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.Reflection;
#endif

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.DesignData
{
	[CreateAssetMenu( fileName = "PacketData", menuName = "Design Data/Packets", order = 200 )]
	public class PacketData : Data
	{
		[System.Serializable]
		public class PacketDefinition
		{
			public string typeFullName = null;

			public bool validated = false;

			public string[] subTypes = new string[ 0 ];
		}

#if UNITY_EDITOR
		[CustomPropertyDrawer( typeof( PacketDefinition ) )]
		public class PacketDefinitionPropertyDrawer : EditorUtils.InheritedPropertyDrawer
		{
			private static GUIStyle _warningStyle = null;
			private static GUIStyle warningStyle
			{
				get
				{
					if( object.Equals( null, _warningStyle ) )
					{
						_warningStyle = new GUIStyle( "CN EntryWarnIconSmall" );
					}

					return _warningStyle;
				}
			}

			public static void PopulateSubTypes( SerializedProperty property )
			{
				SerializedProperty subTypes = property.FindPropertyRelative( "subTypes" );
				SerializedProperty typeFullName = property.FindPropertyRelative( "typeFullName" );
				SerializedProperty validated = property.FindPropertyRelative( "validated" );

				validated.boolValue = false;
				subTypes.ClearArray();

				string wildcardFilter = typeFullName.stringValue;
				if( string.IsNullOrWhiteSpace( wildcardFilter ) )
					return;

				wildcardFilter = wildcardFilter.Substring( 0, wildcardFilter.Length - 1 );

				List<System.Type> types = Utils.FindSubTypes<Networking.Packet>( wildcardFilter );

				foreach( System.Type type in types )
				{
					int index = subTypes.arraySize;
					subTypes.InsertArrayElementAtIndex( index );
					subTypes.GetArrayElementAtIndex( index ).stringValue = type.FullName;
				}

				validated.boolValue = ( subTypes.arraySize > 0 );
			}

			public override float CalculatePropertyHeight( ref SerializedProperty property )
			{
				float propertyHeight = base.CalculatePropertyHeight( ref property );

				propertyHeight += lineHeight * 1f;

				SerializedProperty subTypes = property.FindPropertyRelative( "subTypes" );

				if( subTypes.arraySize > 0 )
				{
					propertyHeight += lineHeight * 1.5f * subTypes.arraySize;
				}

				return propertyHeight;
			}

			public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
			{
				base.DrawGUI( ref rect, ref property );

				SerializedProperty validated = property.FindPropertyRelative( "validated" );
				SerializedProperty typeFullName = property.FindPropertyRelative( "typeFullName" );
				SerializedProperty subTypes = property.FindPropertyRelative( "subTypes" );

				Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

				Color backgroundColor = GUI.backgroundColor;
				Color contentColor = GUI.contentColor;

				if( !validated.boolValue )
				{
					bRect = new Rect( bRect.x + 25f, bRect.y, bRect.width - 25f, lineHeight );

					GUI.contentColor = new Color( 1f, 0.75f, 0.25f, 1f );
				}

				EditorGUI.BeginChangeCheck();

				EditorGUI.PropertyField( bRect, typeFullName, new GUIContent() );
				
				if( EditorGUI.EndChangeCheck() )
				{
					if( string.IsNullOrWhiteSpace( typeFullName.stringValue ) )
					{
						subTypes.ClearArray();

						validated.boolValue = false;
					}
					else
					{
						string _typeFullName = typeFullName.stringValue.Trim();

						if( _typeFullName.EndsWith( ".*" ) )
						{
							PopulateSubTypes( property );
						}
						else
						{
							subTypes.ClearArray();

							System.Type type = System.Type.GetType( _typeFullName );

							validated.boolValue = ( !object.Equals( null, type ) && typeof( Networking.Packet ).IsAssignableFrom( type ) );
						}
					}
				}

				if( !validated.boolValue )
				{
					GUI.contentColor = contentColor;

					Rect cRect = new Rect( rect.x, rect.y + 2f, 15f, lineHeight );

					EditorGUI.LabelField( cRect, new GUIContent(), warningStyle );
				}

				GUI.backgroundColor = Color.clear;
				GUI.contentColor = Color.clear;

				MonoScript monoScript = EditorUtils.BetterObjectField<MonoScript>( bRect, new GUIContent(), ( MonoScript ) null );

				if( !object.Equals( null, monoScript ) )
				{
					System.Type monoType = monoScript.GetClass();

					if( typeof( Networking.Packet ).IsAssignableFrom( monoType ) )
					{
						typeFullName.stringValue = monoType.FullName;
						validated.boolValue = true;
					}
				}

				GUI.backgroundColor = backgroundColor;
				GUI.contentColor = contentColor;

				

				rect.y += lineHeight;

				if( subTypes.arraySize > 0 )
				{
					EditorGUI.BeginDisabledGroup( true );

					Rect cRect = new Rect( rect.x + 15f, rect.y + lineHeight * 0.5f, rect.width - 15f, lineHeight );

					for( int i = 0, iC = subTypes.arraySize; i < iC; i++ )
					{
						EditorGUI.TextField( cRect, subTypes.GetArrayElementAtIndex( i ).stringValue );

						cRect.y += lineHeight * 1.5f;
					}

					rect.y = cRect.y - lineHeight * 0.5f;

					EditorGUI.EndDisabledGroup();
				}
			}
		}
#endif

		public List<PacketDefinition> packetDefinitions = new List<PacketDefinition>();

		public string[] packetTypeNames
		{
			get
			{
				List<string> typeNames = new List<string>();

				foreach( PacketDefinition packetDefinition in packetDefinitions )
				{
					if( !packetDefinition.validated )
						continue;

					if( packetDefinition.typeFullName.EndsWith( ".*" ) )
					{
						if( packetDefinition.subTypes.Length > 0 )
							typeNames.AddRange( packetDefinition.subTypes );
					}
					else
					{
						typeNames.Add( packetDefinition.typeFullName );
					}
				}

				return typeNames.ToArray();
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( PacketData ) )]
	public class EditorPacketData : EditorData
	{
		ReorderableList packetDefinitions;
		
		public override void Setup()
		{
			base.Setup();

			packetDefinitions = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "packetDefinitions" ),
				( SerializedProperty element ) =>
				{
					return EditorGUI.GetPropertyHeight( element, true ) + lineHeight * 0.5f;
				},
				( Rect rect, SerializedProperty element ) =>
				{
					Rect bRect = new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, rect.height - lineHeight * 0.5f );

					EditorGUI.PropertyField( bRect, element, true );
				},
				( SerializedProperty list, SerializedProperty element ) =>
				{
					element.FindPropertyRelative( "typeFullName" ).stringValue = string.Empty;
					element.FindPropertyRelative( "validated" ).boolValue = false;
					element.FindPropertyRelative( "subTypes" ).ClearArray();
				}
			);
		}

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 1.5f;

			inspectorHeight += packetDefinitions.CalculateCollapsableListHeight();

			inspectorHeight += lineHeight * 2f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Packet Data" ) );
			bRect.y += lineHeight * 1.5f;

			packetDefinitions.DrawCollapsableList( ref bRect );
			
			bRect.height = lineHeight * 1.5f;

			if( EditorUtils.BetterButton( bRect, new GUIContent( "Repopulate Packets" ) ) )
			{
				Utils.ClearTypeCache();

				SerializedProperty _packetDefinitions = packetDefinitions.serializedProperty;
				for( int i = 0, iC = _packetDefinitions.arraySize; i < iC; i++ )
				{
					PacketData.PacketDefinitionPropertyDrawer.PopulateSubTypes( _packetDefinitions.GetArrayElementAtIndex( i ) );
				}
			}

			bRect.y += bRect.height;

			rect.y = bRect.y;
		}
	}
#endif
}