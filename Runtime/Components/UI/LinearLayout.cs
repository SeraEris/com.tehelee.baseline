using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
#if UNITY_EDITOR
	[ExecuteInEditMode]
#endif
	[RequireComponent( typeof( RectTransform ) )]
	public class LinearLayout : MonoBehaviour
	{
		////////////////////////////////
		//	Attributes

		#region Attributes
		
		public float paddingStart = 0f;
		public float paddingMids = 0f;
		public float paddingEnd = 0f;

		public bool layoutHorizontal = false;
		public bool layoutInverse = false;
		public bool resizeBounds = false;

		public bool updateInRuntime = false;
#if UNITY_EDITOR
		public bool updateInEdit = false;
#endif

		public RectTransform rectTransform { get; private set; }

		private RectTransform viewport = null;

		public List<RectTransform> ignoreList = new List<RectTransform>();

		#endregion

		////////////////////////////////
		//	Members

		#region Members

		private HashSet<RectTransform> ignoreHashSet = new HashSet<RectTransform>();

		#endregion

		////////////////////////////////
		//	Mono Methods

		#region Mono Methods
		
		protected virtual void Awake()
		{
			rectTransform = ( RectTransform ) transform;

			CheckScrollRect();
		}

		protected virtual void OnEnable()
		{
			CheckScrollRect();

			PerformLayout();
		}

		protected virtual void OnDisable()
		{
			viewport = null;
		}

		protected virtual void Update()
		{
#if UNITY_EDITOR
			if( Application.isPlaying )
			{
				if( !updateInRuntime )
					return;
			}
			else if( !updateInEdit )
			{
				return;
			}
#else
			if( !updateInRuntime )
				return;
#endif
			PerformLayout();
		}

		#endregion

		////////////////////////////////
		//	LinearLayout

		#region LinearLayout

		public void RebuildIgnoreHashSet() =>
			ignoreHashSet = new HashSet<RectTransform>( ignoreList );

		public void CheckScrollRect()
		{
			UnityEngine.UI.ScrollRect scrollRect = rectTransform.GetComponentInParent<UnityEngine.UI.ScrollRect>();
			if( !object.Equals( null, scrollRect ) && object.Equals( scrollRect.content, rectTransform ) && !object.Equals( null, scrollRect.viewport ) )
				viewport = scrollRect.viewport;
		}

		public void PerformLayout()
		{
			if( !Utils.IsObjectAlive( rectTransform ) )
			{
				rectTransform = ( RectTransform ) transform;
				CheckScrollRect();
			}

			if( ignoreHashSet.Count != ignoreList.Count )
				RebuildIgnoreHashSet();

			RectTransform child;

			float totalSize = paddingStart;
			if( layoutHorizontal )
			{
				if( layoutInverse )
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf || ignoreHashSet.Contains( child ) )
							continue;

						child.anchorMin = new Vector2( 1f, child.anchorMin.y );
						child.anchorMax = new Vector2( 1f, child.anchorMax.y );
						child.pivot = new Vector2( 1f, child.pivot.y );

						child.anchoredPosition = new Vector2( -totalSize, child.anchoredPosition.y );
						
						totalSize += child.sizeDelta.x + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}

					if( Utils.IsObjectAlive( viewport ) )
					{
						totalSize = Mathf.Max( totalSize, viewport.rect.width );
					}
				}
				else
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf || ignoreHashSet.Contains( child ) )
							continue;

						child.anchorMin = new Vector2( 0f, child.anchorMin.y );
						child.anchorMax = new Vector2( 0f, child.anchorMax.y );
						child.pivot = new Vector2( 0f, child.pivot.y );

						child.anchoredPosition = new Vector2( totalSize, child.anchoredPosition.y );

						totalSize += child.sizeDelta.x + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}
				}

				if( totalSize == paddingStart )
					totalSize = 0f;

				if( resizeBounds )
					rectTransform.sizeDelta = new Vector2( totalSize, rectTransform.sizeDelta.y );
			}
			else
			{
				if( layoutInverse )
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf || ignoreHashSet.Contains( child ) )
							continue;

						child.anchorMin = new Vector2( child.anchorMin.x, 0f );
						child.anchorMax = new Vector2( child.anchorMax.x, 0f );
						child.pivot = new Vector2( child.pivot.x, 0f );

						child.anchoredPosition = new Vector2( child.anchoredPosition.x, totalSize );

						totalSize += child.sizeDelta.y + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}
					
					if( Utils.IsObjectAlive( viewport ) )
					{
						totalSize = Mathf.Max( totalSize, viewport.rect.height );
					}
				}
				else
				{
					for( int i = 0, iC = rectTransform.childCount; i < iC; i++ )
					{
						child = ( RectTransform ) rectTransform.GetChild( i );
						if( !child.gameObject.activeSelf || ignoreHashSet.Contains( child ) )
							continue;

						child.anchorMin = new Vector2( child.anchorMin.x, 1f );
						child.anchorMax = new Vector2( child.anchorMax.x, 1f );
						child.pivot = new Vector2( child.pivot.x, 1f );

						child.anchoredPosition = new Vector2( child.anchoredPosition.x, -totalSize );

						totalSize += child.sizeDelta.y + ( ( i < iC - 1 ) ? paddingMids : paddingEnd );
					}
				}

				if( totalSize == paddingStart )
					totalSize = 0f;

				if( resizeBounds )
					rectTransform.sizeDelta = new Vector2( rectTransform.sizeDelta.x, totalSize );
			}
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( LinearLayout ) )]
	public class EditorLinearLayout : EditorUtils.InheritedEditor
	{
		ReorderableList ignoreList;

		private static HashSet<Transform> validParents = new HashSet<Transform>();
		private static HashSet<LinearLayout> targetLayouts = new HashSet<LinearLayout>();

		public override void Setup()
		{
			base.Setup();

			validParents.Clear();

			foreach( Object target in targets )
			{
				LinearLayout linearLayout = ( LinearLayout ) target;
				targetLayouts.Add( linearLayout );
				validParents.Add( linearLayout.transform );
			}

			ignoreList = EditorUtils.CreateReorderableList
			(
				serializedObject.FindProperty( "ignoreList" ),
				( SerializedProperty list, int index, SerializedProperty element ) =>
				{
					return lineHeight * 1.5f;
				},
				( Rect rect, SerializedProperty list, int index, SerializedProperty element, bool isActive, bool isFocussed ) =>
				{
					EditorGUI.BeginChangeCheck();

					EditorUtils.BetterObjectField( new Rect( rect.x, rect.y + lineHeight * 0.25f, rect.width, lineHeight ), new GUIContent(), element, typeof( RectTransform ), true );

					if( EditorGUI.EndChangeCheck() )
					{
						RectTransform rectTransform = ( RectTransform ) element.objectReferenceValue;
						if( Utils.IsObjectAlive( rectTransform ) )
						{
							if( !validParents.Contains( rectTransform.parent ) )
							{
								element.objectReferenceValue = null;
							}
							else
							{
								for( int i = 0, iC = list.arraySize; i < iC; i++ )
								{
									if( index == i )
										continue;

									SerializedProperty elementAtIndex = list.GetArrayElementAtIndex( i );
									RectTransform rectTransformAtIndex = ( RectTransform ) elementAtIndex.objectReferenceValue;
									if( rectTransformAtIndex == rectTransform )
									{
										element.objectReferenceValue = null;
										break;
									}
								}
							}
						}

						serializedObject.ApplyModifiedProperties();

						foreach( LinearLayout linearLayout in targetLayouts )
							linearLayout.RebuildIgnoreHashSet();
					}
				}
			);
		}

		public override float GetInspectorHeight() => base.GetInspectorHeight() + lineHeight * 8f + ignoreList.CalculateCollapsableListHeight();

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Linear Layout", "Provides a more extensive and polished horizontal or vertical layout group." ) );
			bRect.y += lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 20f ) * 0.333f, lineHeight * 1.5f );

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Horizontal" ), this[ "layoutHorizontal" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Inverse" ), this[ "layoutInverse" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Resize" ), this[ "resizeBounds" ] );

			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, lineHeight * 1.5f );
			
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Runtime" ), this[ "updateInRuntime" ] );

			cRect.x += cRect.width + 10f;

			EditorUtils.BetterToggleField( cRect, new GUIContent( "Update In Edit" ), this[ "updateInEdit" ] );

			bRect.y += lineHeight * 2f;

			cRect = new Rect( bRect.x, bRect.y, bRect.width * 0.333f, lineHeight );

			EditorGUI.LabelField( cRect, new GUIContent( "Start" ), EditorStyles.miniButtonLeft );

			cRect.x += cRect.width;

			EditorGUI.LabelField( cRect, new GUIContent( "Mids" ), EditorStyles.miniButtonMid );

			cRect.x += cRect.width;

			EditorGUI.LabelField( cRect, new GUIContent( "End" ), EditorStyles.miniButtonRight );

			bRect.y += lineHeight;

			cRect = new Rect( bRect.x, bRect.y, bRect.width * 0.333f, lineHeight );
			
			DrawFloatField( cRect, this[ "paddingStart" ] );

			cRect.x += cRect.width;

			DrawFloatField( cRect, this[ "paddingMids" ] );

			cRect.x += cRect.width;

			DrawFloatField( cRect, this[ "paddingEnd" ] );

			bRect.y += lineHeight * 1.5f;

			ignoreList.DrawCollapsableList( ref bRect, new GUIContent( "Ignore Child RectTransforms" ) );

			rect.y = bRect.y;
		}

		private static GUIStyle _floatFieldStyle = null;
		private static GUIStyle floatFieldStyle
		{
			get
			{
				if( object.Equals( null, _floatFieldStyle ) )
				{
					_floatFieldStyle = new GUIStyle( EditorStyles.numberField );
					_floatFieldStyle.alignment = TextAnchor.MiddleCenter;
				}
				return _floatFieldStyle;
			}
		}

		private void DrawFloatField( Rect rect, SerializedProperty property )
		{
			float value = property.floatValue;
			float _value = EditorGUI.FloatField( rect, new GUIContent(), value, floatFieldStyle );
			if( _value != value )
				property.floatValue = value = _value;
		}
	}
#endif
}
