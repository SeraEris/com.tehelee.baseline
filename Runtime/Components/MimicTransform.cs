using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline.Components
{
	public class MimicTransform : MonoBehaviour
	{
		////////////////////////////////
		//	Attributes
		
		#region Attributes

		public Transform followTarget;

		public bool mimicPosition = true;
		public bool mimicRotation = true;
		public bool mimicScale = false;

		#endregion
		
		////////////////////////////////
		//	Properties
		
		#region Properties

		public bool hasParent { get; private set; } = false;
		public bool hasFollowTarget { get; private set; } = false;
		public bool hasFollowParent { get; private set; } = false;

		#endregion
		
		////////////////////////////////
		//	Members
		
		#region Members

		private Transform parent;
		private Transform followParent;
		
		#endregion
		
		////////////////////////////////
		//	Mono Methods
		
		#region Mono Methods

		protected virtual void OnEnable()
		{
			parent = transform.parent;
			followParent = followTarget.parent;
			
			hasParent = Utils.IsObjectAlive( parent );
			hasFollowTarget = Utils.IsObjectAlive( followTarget );
			hasFollowParent = hasFollowTarget && Utils.IsObjectAlive( followParent );
		}

		protected virtual void OnDisable()
		{
			hasFollowTarget = false;
		}
		
		protected virtual void Update()
		{
			if( hasFollowTarget )
			{
				Transform t = transform;
				if( mimicPosition )
					t.position = followTarget.position;
				if( mimicRotation )
					t.rotation = followTarget.rotation;
				if( mimicScale )
				{
					Vector3 followScale = followTarget.localScale;
					t.localScale = hasParent
						? parent.InverseTransformVector( hasFollowParent ? followParent.TransformVector( followScale ) : followScale )
						: hasFollowParent ? followParent.TransformVector( followScale ) : followScale;
				}
			}
		}
		
		#endregion
	}
	
	#if UNITY_EDITOR
	[CustomEditor( typeof( MimicTransform ) )]
	public class EditorMimicTransform : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 5.0f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );
			
			EditorUtils.DrawDivider( bRect, new GUIContent( "Mimic Transform" ) );
			bRect.y += lineHeight * 1.5f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Follow Target" ), this[ "followTarget" ], typeof( Transform ), true );
			bRect.y += lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 20f ) / 3f, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Position" ), this[ "mimicPosition" ] );
			cRect.x += cRect.width + 10f;
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Rotation" ), this[ "mimicRotation" ] );
			cRect.x += cRect.width + 10f;
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Scale" ), this[ "mimicScale" ] );
			
			bRect.y += lineHeight * 2.0f;

			rect.y = bRect.y;
		}
	}
	#endif
}