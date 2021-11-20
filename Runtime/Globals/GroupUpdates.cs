using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Tehelee.Baseline
{
	public class GroupUpdates<T> where T : class
	{
		public GroupUpdates( System.Action<T> invoke, float delay = 0f )
		{
			this.invoke = invoke;
			this.fixedUpdate = false;
			this.delay = delay;
		}

		public GroupUpdates( System.Action<T> invoke, bool fixedUpdate, float delay = 0f )
		{
			this.invoke = invoke;
			this.fixedUpdate = fixedUpdate;
			this.delay = delay;
		}

		private bool fixedUpdate = false;
		private float delay = 0f;
		private System.Action<T> invoke = null;

		private HashSet<T> registerQueue = new HashSet<T>();
		private HashSet<T> dropQueue = new HashSet<T>();
		private HashSet<T> updateQueue = new HashSet<T>();

		public void Register( T rollover )
		{
			if( object.Equals( null, rollover ) )
				return;

			registerQueue.Add( rollover );

			if( object.Equals( null, _IUpdate ) )
			{
				_IUpdate = Utils.StartCoroutine( IUpdate() );
			}
		}

		public void Drop( T rollover )
		{
			if( object.Equals( null, rollover ) )
				return;

			dropQueue.Add( rollover );
		}

		private Coroutine _IUpdate = null;
		private IEnumerator IUpdate()
		{
			YieldInstruction yieldInstruction = null;

			if( fixedUpdate )
			{
				yieldInstruction = new WaitForFixedUpdate();
			}
			else
			{
				if( delay < 0f )
					yieldInstruction = new WaitForEndOfFrame();
				else if( delay > 0f )
					yieldInstruction = new WaitForSeconds( delay );
			}

			while( true )
			{
				if( updateQueue.Count > 200 )
				{
					int processed = 0;
					foreach( T target in updateQueue )
					{
						if( ++processed > 100 )
							yield return null;

						invoke( target );
					}
				}
				else
				{
					foreach( T target in updateQueue )
						invoke( target );
				}

				foreach( T target in registerQueue )
				{
					updateQueue.Add( target );
				}
				registerQueue.Clear();

				foreach( T target in dropQueue )
				{
					updateQueue.Remove( target );
				}
				dropQueue.Clear();

				if( updateQueue.Count == 0 )
				{
					Utils.StopCoroutine( _IUpdate );
					_IUpdate = null;
					yield break;
				}
				
				yield return yieldInstruction;
			}
		}
	}
}