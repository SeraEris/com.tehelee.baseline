using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline.Components
{
	[ExecuteAlways]
	public class MimicTransform : MonoBehaviour
	{
		////////////////////////////////
		#region Attributes

		[SerializeField]
		private Transform _followTarget;

		public bool executeInEditMode = false;
		public bool executeInRuntime = true;
		public bool executeAsPhysics = false;
		
		public bool mimicPosition = true;
		public bool mimicRotation = true;
		public bool mimicScale = false;

		public Vector3 offsetPosition = Vector3.zero;
		public Vector3 offsetRotation = Vector3.zero;
		public Vector3 multiplyScale = Vector3.one;

		#endregion
		
		////////////////////////////////
		#region Properties
		
		public Transform followTarget => _followTarget;
		public bool hasFollowTarget { get; private set; } = false;

		#endregion
		
		////////////////////////////////
		#region Mono Methods

		protected virtual void OnEnable()
		{
			SetFollowTarget( _followTarget );
		}

		protected virtual void OnDisable()
		{
			if( !object.Equals( null, _IFollow ) )
			{
				StopCoroutine( _IFollow );
				_IFollow = null;
			}
		}

		#if UNITY_EDITOR
		private void Update()
		{
			if( !Application.isPlaying && executeInEditMode )
			{
				hasFollowTarget = Utils.IsObjectAlive( followTarget );
				UpdateFollow();
			}
		}
		#endif
		
		#endregion
		
		////////////////////////////////
		#region MimicTransform
		
		public void UpdateFollow( Rigidbody rigidbody = null )
		{
#if UNITY_EDITOR
			if( !Application.isPlaying )
				hasFollowTarget = Utils.IsObjectAlive( followTarget );
#endif
			
			if( hasFollowTarget )
			{
				Transform t = transform;

				if( !Utils.IsObjectAlive( t ) || !Utils.IsObjectAlive( followTarget ) )
					return;

				Matrix4x4 trs = Utils.IsObjectAlive( rigidbody ) ? Matrix4x4.TRS( rigidbody.position, rigidbody.rotation, t.localScale ) : Matrix4x4.TRS( t.position, t.rotation, t.localScale ); 
				
				if( mimicPosition )
					t.position = trs.MultiplyPoint( offsetPosition );
				if( mimicRotation )
					t.rotation = trs.rotation * Quaternion.Euler( offsetRotation );
				if( mimicScale )
				{
					Transform parent = t.parent;
					Vector3 lossyScale = trs.MultiplyVector( multiplyScale );
					t.localScale = Utils.IsObjectAlive( parent ) ? parent.InverseTransformVector( lossyScale ) : lossyScale;
				}
			}
		}

		public void SetFollowTarget( Transform target )
		{
			if( !object.Equals( null, _IFollow ) )
				StopCoroutine( _IFollow );

			_followTarget = target;
			
			hasFollowTarget = Utils.IsObjectAlive( followTarget );

			if( Application.isPlaying )
			{
				_IFollow = StartCoroutine( IFollow() );
			}
		}
		
		private Coroutine _IFollow = null;
		private IEnumerator IFollow()
		{
			while( true )
			{
				if( executeAsPhysics )
					yield return new WaitForFixedUpdate();
				else
					yield return null;
				
				if( executeInRuntime || executeAsPhysics )
					UpdateFollow();
			}
		}

		#endregion
	}
	
	#if UNITY_EDITOR
	[CustomEditor( typeof( MimicTransform ) )]
	public class EditorMimicTransform : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 10.5f + 8f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );
			
			EditorUtils.DrawDivider( bRect, new GUIContent( "Mimic Transform" ) );
			bRect.y += lineHeight * 1.5f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Follow Target" ), this[ "_followTarget" ], typeof( Transform ), true );
			bRect.y += lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 20f ) / 3f, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Edit Update" ), this[ "executeInEditMode" ] );
			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update" ), this[ "executeInRuntime" ] );
			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Physics" ), this[ "executeAsPhysics" ] );
			cRect.x += cRect.width + 10f;
			
			bRect.y += lineHeight * 2.0f;
			
			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 20f ) / 3f, lineHeight * 1.5f );
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Position" ), this[ "mimicPosition" ] );
			cRect.x += cRect.width + 10f;
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Rotation" ), this[ "mimicRotation" ] );
			cRect.x += cRect.width + 10f;
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Scale" ), this[ "mimicScale" ] );
			
			bRect.y += lineHeight * 2.0f;

			EditorGUI.PropertyField( bRect, this[ "offsetPosition" ], new GUIContent( "Offset Position" ) );
			bRect.y += lineHeight + 4f;
			EditorGUI.PropertyField( bRect, this[ "offsetRotation" ], new GUIContent( "Offset Rotation" ) );
			bRect.y += lineHeight + 4f;
			EditorGUI.PropertyField( bRect, this[ "multiplyScale" ], new GUIContent( "Offset Scale" ) );
			bRect.y += lineHeight * 1.5f;

			rect.y = bRect.y;
		}
	}
	#endif
}