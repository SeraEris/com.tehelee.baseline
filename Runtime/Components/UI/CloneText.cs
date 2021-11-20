using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Text = TMPro.TextMeshProUGUI;

namespace Tehelee.Baseline
{
	[RequireComponent( typeof( Text ) )]
	public class CloneText : MonoBehaviour
	{
		////////////////////////////////
		//	Static

		#region Static

		private static void Register( Text textToCopy, Text destinationText )
		{
			if( !Utils.IsObjectAlive( textToCopy ) || !Utils.IsObjectAlive( destinationText ) )
				return;

			// No copying yourself.
			if( textToCopy == destinationText )
				return;

			destinationText.text = textToCopy.text;

			if( cloneSetsByText.ContainsKey( textToCopy ) )
				cloneSetsByText[ textToCopy ].destinationTexts.Add( destinationText );
			else
				cloneSetsByText.Add( textToCopy, new CloneSet( destinationText ) );

			if( object.Equals( null, _IUpdateTexts ) )
				_IUpdateTexts = Utils.StartCoroutine( IUpdateTexts() );
		}

		private static void Drop( Text textToCopy, Text destinationText )
		{
			// Using explicit null check here instead of IsObjectAlive because we want to remove them even if they're dead.
			if( object.Equals( null, textToCopy ) || object.Equals( null, destinationText ) )
				return;

			// If the destination text is alive, clear duplicate string
			if( destinationText )
				destinationText.text = string.Empty;

			if( cloneSetsByText.ContainsKey( textToCopy ) )
			{
				CloneSet cloneSet = cloneSetsByText[ textToCopy ];

				if( cloneSet.destinationTexts.Contains( destinationText ) )
					cloneSet.destinationTexts.Remove( destinationText );

				if( cloneSet.destinationTexts.Count == 0 )
					cloneSetsByText.Remove( textToCopy );
				else
					cloneSetsByText[ textToCopy ] = cloneSet;
			}

			if( cloneSetsByText.Count == 0 )
			{
				Utils.StopCoroutine( _IUpdateTexts );
				_IUpdateTexts = null;
			}
		}

		private class CloneSet
		{
			public string lastString = null;
			public HashSet<Text> destinationTexts = new HashSet<Text>();

			public CloneSet() { }
			public CloneSet( Text destinationText )
			{
				destinationTexts.Add( destinationText );
			}
		}

		private static Dictionary<Text, CloneSet> cloneSetsByText = new Dictionary<Text, CloneSet>();

		private static Coroutine _IUpdateTexts = null;
		private static IEnumerator IUpdateTexts()
		{
			while( true )
			{
				List<Text> keys = new List<Text>( cloneSetsByText.Keys );
				foreach( Text text in keys )
				{
					if( !Utils.IsObjectAlive( text ) )
						continue;

					CloneSet cloneSet = cloneSetsByText[ text ];
					
					if( object.Equals( null, cloneSet.lastString ) || !string.Equals( text.text, cloneSet.lastString ) )
					{
						cloneSet.lastString = text.text;

						foreach( Text destinationText in cloneSet.destinationTexts )
							if( Utils.IsObjectAlive( destinationText ) )
								destinationText.text = cloneSet.lastString;

						cloneSetsByText[ text ] = cloneSet;
					}
				}

				yield return null;
			}
		}

		#endregion

		////////////////////////////////
		//	Attributes

		#region Attributes

		public Text textToCopy;

		#endregion

		////////////////////////////////
		//	Properties

		#region Properties

		public Text text { get; private set; }

		#endregion

		////////////////////////////////
		//	Mono Methods

		#region Mono Methods

		protected virtual void Awake()
		{
			text = GetComponent<Text>();
		}

		protected virtual void OnEnable()
		{
			Register( textToCopy, text );
		}

		protected virtual void OnDisable()
		{
			Drop( textToCopy, text );
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( CloneText ) )]
	public class EditorCloneText : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 1.5f;

			inspectorHeight += lineHeight * 1.5f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Clone Text" ) );
			bRect.y += lineHeight * 1.5f;

			EditorGUIUtility.labelWidth = 120f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Text To Copy" ), this[ "textToCopy" ], typeof( Text ), true );
			bRect.y += lineHeight * 1.5f;

			EditorGUIUtility.labelWidth = labelWidth;

			rect.y = bRect.y;
		}
	}
#endif
}