using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.Cinematographer
{
	public class CameraAnchor : MonoBehaviour
	{
		////////////////////////////////
		#region Static

		private static Coroutine anchorRoutine = null;

		private static List<CameraAnchor> activeStack = new List<CameraAnchor>();
		private static bool isStackDirty = false;

		public static CameraAnchor activeAnchor { get; private set; } = null;
		public static bool hasActiveAnchor { get; private set; } = false;

		public static float lastCameraSwitch = 0f;
		
		public static event System.Action<CameraAnchor> onAnchorSwitching;

		#endregion

		////////////////////////////////
		#region Attributes

		public bool _isPerspective = false;
		public bool isPerspective = true;

		public bool _orthoSize = false;
		public float orthoSize = 1f;

		public bool _fieldOfView = false;
		public float fieldOfView = 85f;

		public bool _clipMin = false;
		public float clipMin = 0.1f;
		public bool _clipMax = false;
		public float clipMax = 1000f;

		public bool _cullingMask = false;
		public int cullingMask = -1;

		public bool _clearFlags = false;
		public CameraClearFlags clearFlags = CameraClearFlags.SolidColor;

		public bool _backgroundColor = false;
		public Color backgroundColor;

		#endregion

		////////////////////////////////
		#region Mono Methods

		public virtual void OnEnable()
		{
			Register();
		}

		public virtual void OnDisable()
		{
			Drop();
		}

		public virtual void OnDrawGizmosSelected()
		{
			Gizmos.color = new Color( 1f, 1f, 1f, 0.333f );
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawIcon( Vector3.zero, "Camera Gizmo" );
			Gizmos.DrawFrustum( Vector3.zero, fieldOfView, clipMax, clipMin, 1.5f );
		}

		#endregion

		////////////////////////////////
		#region CameraAnchor

		private static IEnumerator IAnchorRoutine()
		{
			WaitForEndOfFrame wait = new WaitForEndOfFrame();
			while( true )
			{
				if( isStackDirty )
				{
					int count = activeStack.Count;
					activeAnchor = count > 0 ? activeStack[ count - 1 ] : null;

					onAnchorSwitching?.Invoke( activeAnchor );

					hasActiveAnchor = Utils.IsObjectAlive( activeAnchor );
					
					lastCameraSwitch = Time.time;

					isStackDirty = false;
				}

				yield return wait;
			}
		}

		private void Register()
		{
			activeStack.Add( this );

			isStackDirty = true;

			if( object.Equals( null, anchorRoutine ) )
			{
				anchorRoutine = Utils.StartCoroutine( IAnchorRoutine() );
			}
		}

		private void Drop()
		{
			for( int i = 0, iC = activeStack.Count; i < iC; i++ )
			{
				if( activeStack[ i ] == this )
				{
					activeStack.RemoveAt( i );

					if( i == iC - 1 )
					{
						isStackDirty = true;
					}

					if( iC == 1 )
					{
						onAnchorSwitching?.Invoke( null );
						hasActiveAnchor = false;
						Utils.StopCoroutine( anchorRoutine );
						anchorRoutine = null;
						activeAnchor = null;
					}

					return;
				}
			}
		}

		public virtual void ApplyCamera( Camera camera )
		{
			if( _isPerspective )
				camera.orthographic = !isPerspective;

			if( _orthoSize )
				camera.orthographicSize = orthoSize;

			if( _fieldOfView )
				camera.fieldOfView = fieldOfView;

			if( _clipMin )
				camera.nearClipPlane = clipMin;

			if( _clipMax )
				camera.farClipPlane = clipMax;

			if( _cullingMask )
				camera.cullingMask = cullingMask;

			if( _clearFlags )
				camera.clearFlags = clearFlags;

			if( _backgroundColor )
				camera.backgroundColor = backgroundColor;
		}

		public virtual void UpdateCamera( Camera camera )
		{
			camera.transform.SetPositionAndRotation( transform.position, transform.rotation );
		}

		public virtual Texture2D CapturePreview( Vector2Int textureSize )
		{
			RenderTexture renderTexture = RenderTexture.GetTemporary( textureSize.x, textureSize.y );
			
			Camera previewCamera = new GameObject( "Camera Anchor Preview", typeof( Camera ) ).GetComponent<Camera>();
			previewCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;

			ApplyCamera( previewCamera );
			UpdateCamera( previewCamera );

			RenderTexture targetTexture = previewCamera.targetTexture;
			previewCamera.targetTexture = renderTexture;
			previewCamera.Render();
			previewCamera.targetTexture = targetTexture;

			Utils.DestroyEditorSafe( previewCamera.gameObject );
			previewCamera = null;

			RenderTexture activeRT = RenderTexture.active;
			RenderTexture.active = renderTexture;

			Texture2D texture = new Texture2D( textureSize.x, textureSize.y );
			texture.ReadPixels( new Rect( 0f, 0f, textureSize.x, textureSize.y ), 0, 0 );

			RenderTexture.active = activeRT;
			RenderTexture.ReleaseTemporary( renderTexture );
			renderTexture = null;

			texture.Apply();

			return texture;
		}

#endregion
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( CameraAnchor ) )]
	public class EditorAnchor : EditorUtils.InheritedEditor
	{
		public override void Cleanup()
		{
			CameraAnchor cameraAnchor = ( CameraAnchor ) target;
			if( Utils.IsObjectAlive( cameraAnchor ) )
			{
				foreach( Camera attachedCamera in ( cameraAnchor ).GetComponents<Camera>() )
				{
					if( attachedCamera.hideFlags == HideFlags.HideAndDontSave )
					{
						if( Application.isPlaying )
							Camera.Destroy( attachedCamera );
						else
							Camera.DestroyImmediate( attachedCamera );
					}
				}
			}

			base.Cleanup();
		}

		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 8.5f + 16f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect dRect, cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "CameraAnchor" ) );
			bRect.y += lineHeight * 1.5f;

			EditorGUIUtility.labelWidth = 115f;

			cRect = new Rect( bRect.x, bRect.y, bRect.width, lineHeight * 1.5f );

			dRect = EditorUtils.BeginDisabledGroupToggle( cRect, this[ "_isPerspective" ], null, true );
			EditorUtils.BetterToggleField( dRect, new GUIContent( "Perspective" ), this[ "isPerspective" ] );
			EditorGUI.EndDisabledGroup();

			bRect.y += lineHeight * 2f;

			if( this[ "isPerspective" ].boolValue )
			{
				cRect = EditorUtils.BeginDisabledGroupToggle( bRect, this[ "_fieldOfView" ] );
				EditorUtils.DrawSnapSlider( cRect, this[ "fieldOfView" ], new GUIContent( "Field Of View" ), 0.1f, 179f );
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				cRect = EditorUtils.BeginDisabledGroupToggle( bRect, this[ "_orthoSize" ] );
				EditorGUI.PropertyField( cRect, this[ "orthoSize" ], new GUIContent( "Orthographic Size" ) );
				EditorGUI.EndDisabledGroup();
			}

			bRect.y += lineHeight + 4f;

			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight );

			this[ "clipMin" ].ClampMaximum( this[ "clipMax" ].floatValue );
			dRect = EditorUtils.BeginDisabledGroupToggle( cRect, this[ "_clipMin" ] );
			EditorGUI.PropertyField( dRect, this[ "clipMin" ], new GUIContent( "Clip Near" ) );
			EditorGUI.EndDisabledGroup();
			this[ "clipMin" ].ClampMinimum( 0.1f );

			cRect.x += cRect.width + 10f;

			this[ "clipMax" ].ClampMinimum( this[ "clipMin" ].floatValue );
			dRect = EditorUtils.BeginDisabledGroupToggle( cRect, this[ "_clipMax" ] );
			EditorGUI.PropertyField( dRect, this[ "clipMax" ], new GUIContent( "Clip Far" ) );
			EditorGUI.EndDisabledGroup();

			bRect.y += lineHeight + 4f;

			dRect = EditorUtils.BeginDisabledGroupToggle( bRect, this[ "_cullingMask" ] );
			EditorUtils.LayerMaskField( dRect, new GUIContent( "Culling Mask" ), this[ "cullingMask" ] );
			EditorGUI.EndDisabledGroup();

			bRect.y += lineHeight + 4f;

			dRect = EditorUtils.BeginDisabledGroupToggle( bRect, this[ "_clearFlags" ] );
			EditorGUI.PropertyField( dRect, this[ "clearFlags" ], new GUIContent( "Clear Flags" ) );
			EditorGUI.EndDisabledGroup();

			bRect.y += lineHeight + 4f;
			
			dRect = EditorUtils.BeginDisabledGroupToggle( bRect, this[ "_backgroundColor" ] );
			EditorGUI.PropertyField( dRect, this[ "backgroundColor" ], new GUIContent( "Background Color" ) );
			EditorGUI.EndDisabledGroup();

			bRect.y += lineHeight;

			EditorGUIUtility.labelWidth = labelWidth;

			rect.y = bRect.y;
		}
	}

	[CustomPreview( typeof( CameraAnchor ) )]
	public class PreviewCameraAnchor : ObjectPreview
	{
		private static Vector2Int lastSize = Vector2Int.zero;
		private static RenderTexture renderTexture = null;

		private static Camera camera = null;

		public override bool HasPreviewGUI() => true;
		
		public override void OnPreviewGUI( Rect rect, GUIStyle backgroundStyle )
		{
			base.OnPreviewGUI( rect, backgroundStyle );

			Vector2Int size = new Vector2Int( Mathf.RoundToInt( rect.width ), Mathf.RoundToInt( rect.height ) );
			
			if( size.x <= 1 && size.y <= 1 )
				return;
			
			CameraAnchor cameraAnchor = ( CameraAnchor ) target;

			if( !Utils.IsObjectAlive( camera ) )
			{
				camera = new GameObject( "Preview Camera", typeof( Camera ) ).GetComponent<Camera>();
				camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
				camera.enabled = !Utils.IsObjectAlive( Camera.main );
			}

			if( Utils.IsObjectAlive( camera ) && camera.enabled )
			{
				InternalEditorUtility.RepaintAllViews();
			}

			if( Utils.IsObjectAlive( renderTexture ) )
				GUI.DrawTexture( rect, renderTexture, ScaleMode.ScaleAndCrop );
			
			if( size != lastSize )
			{
				lastSize = size;

				return;
			}

			if( Utils.IsObjectAlive( renderTexture ) )
			{
				if( renderTexture.width != size.x || renderTexture.height != size.y )
				{
					if( Application.isPlaying )
						RenderTexture.Destroy( renderTexture );
					else
						RenderTexture.DestroyImmediate( renderTexture );

					renderTexture = null;
				}
			}

			if( !Utils.IsObjectAlive( renderTexture ) )
			{
				renderTexture = new RenderTexture( size.x, size.y, 0 );
			}

			cameraAnchor.ApplyCamera( camera );
			cameraAnchor.UpdateCamera( camera );

			camera.targetTexture = renderTexture;
			camera.Render();
			camera.targetTexture = null;

			GUI.DrawTexture( rect, renderTexture );
		}
	}
#endif
}