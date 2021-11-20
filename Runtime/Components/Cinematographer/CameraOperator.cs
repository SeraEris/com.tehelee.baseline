using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.Cinematographer
{
	#if UNITY_EDITOR
	[ExecuteAlways]
	#endif
	public class CameraOperator : MonoBehaviour
	{
		////////////////////////////////
		//	Static

		#region Static

		public static float fieldOfView = 85f;
		public static float rotationSpeed = 1f;

		public static void LoadStatic()
		{
			fieldOfView = PlayerPrefs.GetFloat( "Cinematographer.Camera.FieldOfView", 85f );
			rotationSpeed = PlayerPrefs.GetFloat( "Cinematographer.Camera.RotationSpeed", 1f );
		}

		public static void SaveStatic()
		{
			PlayerPrefs.SetFloat( "Cinematographer.Camera.FieldOfView", fieldOfView );
			PlayerPrefs.SetFloat( "Cinematographer.Camera.RotationSpeed", rotationSpeed );
		}

		#endregion

		////////////////////////////////
		//	Attributes

		#region Attributes

		public new Camera camera;

		#if UNITY_EDITOR
		[System.NonSerialized]
		public CameraAnchor previewAnchor;
		#endif

		#endregion

		////////////////////////////////
		//	Members

		#region Members
		
		private bool recordAvailable = false;

		private class TransformRecord
		{
			public Transform parent;
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 scale;

			public TransformRecord( Transform transform )
			{
				parent = transform.parent;
				position = transform.position;
				rotation = transform.rotation;
				scale = transform.localScale;
			}

			public void Apply( Transform transform )
			{
				transform.SetParent( parent );
				transform.SetPositionAndRotation( position, rotation );
				transform.localScale = scale;
			}
		}
		private TransformRecord transformRecord = null;

		private class CameraRecord
		{
			public bool useSkybox;
			public Color backgroundColor;
			public bool orthographic;
			public float orthoSize;
			public float fieldOfView;
			public float clipMin;
			public float clipMax;

			public CameraRecord( Camera camera )
			{
				useSkybox = camera.clearFlags == CameraClearFlags.Skybox;
				backgroundColor = camera.backgroundColor;
				orthographic = camera.orthographic;
				orthoSize = camera.orthographicSize;
				fieldOfView = camera.fieldOfView;
				clipMin = camera.nearClipPlane;
				clipMax = camera.farClipPlane;
			}

			public void Apply( Camera camera )
			{
				camera.clearFlags = useSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
				camera.backgroundColor = backgroundColor;
				camera.orthographic = orthographic;
				camera.orthographicSize = orthoSize;
				camera.fieldOfView = fieldOfView;
				camera.nearClipPlane = clipMin;
				camera.farClipPlane = clipMax;
			}
		}
		private CameraRecord cameraRecord = null;
		
		#endregion

		////////////////////////////////
		//	Mono Methods

		#region Mono Methods

		protected virtual void Awake()
		{
			#if UNITY_EDITOR
			if( !Application.isPlaying )
				return;
			#endif

			if( !Utils.IsObjectAlive( camera ) )
			{
				Debug.LogWarning( "CameraOperator needs a camera component assigned.", this );
				gameObject.SetActive( false );
			}

			LoadReturn();
		}

		protected virtual void OnEnable()
		{
			camera.fieldOfView = fieldOfView;

			CameraAnchor.onAnchorSwitching += OnAnchorSwitching;

			LoadStatic();
		}

		protected virtual void OnDisable()
		{
			CameraAnchor.onAnchorSwitching -= OnAnchorSwitching;

			#if UNITY_EDITOR
			if( !Application.isPlaying )
				LoadReturn( true );
			#endif
		}

		protected virtual void Update()
		{
			#if UNITY_EDITOR
			if( !Application.isPlaying )
			{
				if( Utils.IsObjectAlive( previewAnchor ) )
				{
					SaveReturn();

					previewAnchor.ApplyCamera( camera );
					previewAnchor.UpdateCamera( camera );
				}
				else
				{
					LoadReturn( true );
				}

				return;
			}
			#endif

			if( CameraAnchor.hasActiveAnchor )
				CameraAnchor.activeAnchor.UpdateCamera( camera );
		}

		#endregion

		////////////////////////////////
		//	CameraAnchor

		#region CameraOperator
		
		private void SaveReturn()
		{
			if( !recordAvailable )
			{
				transformRecord = new TransformRecord( camera.transform );
				cameraRecord = new CameraRecord( camera );

				recordAvailable = true;

				transform.hideFlags |= HideFlags.NotEditable;
				camera.hideFlags |= HideFlags.NotEditable;
			}
		}

		private void LoadReturn( bool deleteSave = false )
		{
			if( recordAvailable )
			{
				transformRecord.Apply( camera.transform );
				cameraRecord.Apply( camera );

				if( deleteSave )
				{
					transformRecord = null;
					cameraRecord = null;

					recordAvailable = false;
				}

				transform.hideFlags &= ~HideFlags.NotEditable;
				camera.hideFlags &= ~HideFlags.NotEditable;
			}
		}

		protected virtual void OnAnchorSwitching( CameraAnchor cameraAnchor )
		{
			if( !CameraAnchor.hasActiveAnchor )
				SaveReturn();

			LoadReturn();

			cameraAnchor?.ApplyCamera( camera );
		}

		#endregion
	}
	
#if UNITY_EDITOR
	[CustomEditor( typeof( CameraOperator ) )]
	public class EditorCameraOperator : EditorUtils.InheritedEditor
	{
		CameraOperator cameraOperator = null;

		public override void Setup()
		{
			base.Setup();

			cameraOperator = ( CameraOperator ) target;
		}

		public override void Cleanup()
		{
			cameraOperator = null;

			base.Cleanup();
		}

		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 3.5f + 4f;

			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "CameraOperator" ) );
			bRect.y += lineHeight * 1.5f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Camera" ), this[ "camera" ], typeof( Camera ), true );
			bRect.y += lineHeight + 4f;

			EditorGUI.BeginDisabledGroup( Application.isPlaying );
			if( !Application.isPlaying && Utils.IsObjectAlive( cameraOperator ) )
			{
				CameraAnchor anchor = EditorUtils.BetterObjectField<CameraAnchor>( bRect, new GUIContent( "Preview Anchor" ), cameraOperator.previewAnchor, true );
				if( !object.Equals( cameraOperator.previewAnchor, anchor ) )
				{
					cameraOperator.previewAnchor = anchor;
					EditorUtility.SetDirty( cameraOperator );
				}
			}
			else
			{
				EditorUtils.BetterObjectField<CameraAnchor>( bRect, new GUIContent( "Active Anchor" ), CameraAnchor.activeAnchor, true );
			}
			EditorGUI.EndDisabledGroup();
			bRect.y += lineHeight;

			rect.y = bRect.y;
		}
	}
#endif
}