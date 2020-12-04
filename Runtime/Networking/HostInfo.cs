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
	public class HostInfo
	{
		public string name = string.Empty;
		public string password = string.Empty;
		public int maxPasswordAttempts = 3;
		public bool isPrivate => !string.IsNullOrEmpty( password );
		public string[] tags = new string[ 0 ];
		public string tagsFlat
		{
			get => string.Join( ", ", tags );
			set
			{
				string[] tagsSplitRaw = value.Split( new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries );
				List<string> tagsSplit = new List<string>();
				for( int i = 0, iC = tagsSplitRaw.Length; i < iC; i++ )
				{
					string tag = tagsSplitRaw[ i ].Trim();
					if( !string.IsNullOrEmpty( tag ) )
						tagsSplit.Add( tag );
				}
				tags = tagsSplit.ToArray();
			}
		}
		public int maxPlayers = 32;
		public string description = string.Empty;
		public string adminPassword = string.Empty;
		public int maxAdminAttempts = 1;
		public bool maxAdminAttemptsPerMinute = false;

		public HostInfo() { }

		public HostInfo( HostInfo clone )
		{
			name = clone.name;
			password = clone.password;
			tags = new string[ clone.tags.Length ];
			for( int i = 0, iC = tags.Length; i < iC; i++ )
				tags[ i ] = clone.tags[ i ];
			maxPlayers = clone.maxPlayers;
			description = clone.description;
			adminPassword = clone.adminPassword;
			maxAdminAttempts = clone.maxAdminAttempts;
			maxAdminAttemptsPerMinute = clone.maxAdminAttemptsPerMinute;
		}
	}
	
#if UNITY_EDITOR
	[CustomPropertyDrawer( typeof( HostInfo ) )]
	public class EditorHostInfo : EditorUtils.InheritedPropertyDrawer
	{
		public override LabelMode labelMode => LabelMode.Foldout;

		public override float offsetFoldoutGUI => 4f;

		public override float CalculatePropertyHeight( ref SerializedProperty property )
		{
			float propertyHeight = base.CalculatePropertyHeight( ref property );

			if( property.isExpanded )
			{
				propertyHeight += lineHeight * 9f + 24f;
			}

			return propertyHeight;
		}

		public override void DrawGUI( ref Rect rect, ref SerializedProperty property )
		{
			base.DrawGUI( ref rect, ref property );

			EditorGUIUtility.labelWidth = 140f;

			Rect cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );
			
			SerializedProperty name = property.FindPropertyRelative( "name" );
			EditorGUI.PropertyField( bRect, name );
			name.Truncate( 100 );
			bRect.y += lineHeight + 4f;

			cRect = new Rect( bRect.x, bRect.y, bRect.width - 125f, bRect.height );
			EditorGUI.PropertyField( cRect, property.FindPropertyRelative( "password" ), new GUIContent( "Password" ) );

			cRect = new Rect( cRect.x + cRect.width + 10f, cRect.y, 115f, cRect.height );
			EditorGUIUtility.labelWidth = 85f;
			EditorGUI.PropertyField( cRect, property.FindPropertyRelative( "maxPasswordAttempts" ), new GUIContent( "Max Attempts", "Maximum number of times a client can attempt to join with an invalid password before being kicked." ) );
			EditorGUIUtility.labelWidth = 140f;
			bRect.y += lineHeight + 4f;

			SerializedProperty tags = property.FindPropertyRelative( "tags" );
			string tagsJoined = string.Empty;
			for( int i = 0, iC = tags.arraySize; i < iC; i++ )
				tagsJoined = string.Format( i == 0 ? "{1}" : "{0}, {1}", tagsJoined, tags.GetArrayElementAtIndex( i ).stringValue );

			string tagsEdited = EditorGUI.TextField( bRect, new GUIContent( "Tags" ), tagsJoined );
			if( !string.Equals( tagsEdited, tagsJoined ) )
			{
				string[] tagsSplitRaw = tagsEdited.Split( new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries );
				List<string> tagsSplit = new List<string>();
				for( int i = 0, iC = tagsSplitRaw.Length; i < iC; i++ )
				{
					string tag = tagsSplitRaw[ i ].Trim();
					if( !string.IsNullOrEmpty( tag ) )
						tagsSplit.Add( tag );
				}

				tagsEdited = string.Join( ", ", tagsSplit );
				tagsEdited.Substring( 0, Mathf.Min( tagsEdited.Length, 200 ) );

				tagsSplitRaw = tagsEdited.Split( new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries );
				tagsSplit.Clear();
				for( int i = 0, iC = tagsSplitRaw.Length; i < iC; i++ )
				{
					string tag = tagsSplitRaw[ i ].Trim();
					if( !string.IsNullOrEmpty( tag ) )
						tagsSplit.Add( tag );
				}

				tags.ClearArray();
				tags.arraySize = tagsSplit.Count;
				for( int i = 0, iC = tagsSplit.Count; i < iC; i++ )
				{
					tags.GetArrayElementAtIndex( i ).stringValue = tagsSplit[ i ];
				}
			}
			bRect.y += lineHeight + 4f;
			
			SerializedProperty maxPlayers = property.FindPropertyRelative( "maxPlayers" );
			EditorGUI.PropertyField( bRect, maxPlayers );
			maxPlayers.Clamp( 1, ushort.MaxValue );
			bRect.y += lineHeight + 4f;

			bRect.height = lineHeight * 3f;
			SerializedProperty description = property.FindPropertyRelative( "description" );
			EditorGUI.PropertyField( bRect, description );
			bRect.y += bRect.height + 4f;
			bRect.height = lineHeight;
			description.Truncate( 300 );
			
			EditorGUI.PropertyField( bRect, property.FindPropertyRelative( "adminPassword" ) );
			bRect.y += lineHeight + 4f;
			
			cRect = new Rect( bRect.x, bRect.y, bRect.width - 100f, bRect.height );
			EditorGUI.IntSlider( cRect, property.FindPropertyRelative( "maxAdminAttempts" ), 0, 10, new GUIContent( "Max Admin Attempts", "Maximum number of times a client can attempt to authorize with an invalid password before being kicked." ) );

			cRect = new Rect( cRect.x + cRect.width + 10f, cRect.y, 90f, cRect.height );
			EditorGUIUtility.labelWidth = 70f;
			EditorGUI.PropertyField( cRect, property.FindPropertyRelative( "maxAdminAttemptsPerMinute" ), new GUIContent( "Per Minute", "Authorization attempts can be repeated after a minute." ) );
			EditorGUIUtility.labelWidth = labelWidth;
			bRect.y += lineHeight;

			EditorGUIUtility.labelWidth = labelWidth;

			// Draw Inspector GUI

			rect.y = bRect.y;
		}
	}
#endif
}