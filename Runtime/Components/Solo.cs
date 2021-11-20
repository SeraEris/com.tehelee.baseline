using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tehelee.Baseline
{
	public class Solo<T> : MonoBehaviour where T : Object
	{
		private static List<Solo<T>> solos = new List<Solo<T>>();
		public static int GetActiveCount() => solos.Count;
		
#if UNITY_EDITOR
		public static int GetIndex( Solo<T> solo ) =>
			Utils.IsObjectAlive( solo ) ? solos.IndexOf( solo ) : -1;
#endif

		private T _target;
		protected T target
		{
			get
			{
				if( !Utils.IsObjectAlive( _target ) )
					_target = GetComponent<T>();

				return _target;
			}
		}
		
		public bool suppressed
		{
			get => GetSuppressed();
			private set => SetSuppressed( value );
		}

		protected virtual bool GetSuppressed() => false;
		protected virtual void SetSuppressed( bool suppressed ) { }

		public virtual void OnEnable()
		{
			if( solos.Count > 0 )
			{
				Solo<T> last = solos.Last();
				if( Utils.IsObjectAlive( last ) )
					last.suppressed = true;
				
				solos.Add( this );
			}

			suppressed = false;
		}
		
		public virtual void OnDisable()
		{
			suppressed = true;
			
			int index = solos.IndexOf( this );
			if( index > -1 )
			{
				if( index + 1 == solos.Count && solos.Count > 1 )
				{
					Solo<T> next = solos[ index - 1 ];
					if( Utils.IsObjectAlive( next ) )
						next.suppressed = false;
				}
				
				solos.RemoveAt( index );
			}
		}
	}
	
#if UNITY_EDITOR
	public class EditorSolo : EditorUtils.InheritedEditor
	{	
		protected virtual GUIContent label => new GUIContent( "Solo<T>" );
		protected virtual bool suppressed => false;
		protected virtual int count => 0;
		
		public override float GetInspectorHeight() =>
			base.GetInspectorHeight() + lineHeight * 3.5f;

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, label );
			bRect.y += lineHeight * 1.5f;

			Rect cRect = new Rect( bRect.x, bRect.y, bRect.width - 10f * 0.5f, lineHeight * 1.5f );
			EditorGUI.BeginDisabledGroup( true );
			EditorGUI.showMixedValue = targets.Length > 1;
			EditorUtils.BetterToggleField( cRect, new GUIContent( "Suppressed" ), suppressed );
			EditorGUI.EndDisabledGroup();

			cRect.x += cRect.width + 10f;
			EditorUtils.DrawClickCopyLabel( EditorUtils.DrawBetterBackground( cRect, new GUIContent( "Active" ) ), emptyContent, count.ToString() );

			bRect.y = lineHeight * 2f;

			rect.y = bRect.y;
		}
	}
#endif
}