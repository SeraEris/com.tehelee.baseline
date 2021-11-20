using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tehelee.Baseline
{
	public class Solo<T> : MonoBehaviour where T : Object
	{
		private static List<Solo<T>> solos = new List<Solo<T>>();
		
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
}