using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tehelee.Baseline.Components
{
	public class GameObjectPool : MonoBehaviour
	{
		////////////////////////////////
		//	Attributes

		#region Attributes

		public bool poolOnAwake = false;

		public int poolSize = 1;

		public GameObject template;

		public new HideFlags hideFlags = HideFlags.NotEditable;

		public bool useQueue = false;

		#endregion

		////////////////////////////////
		//	Members

		#region Members

		private bool initialized = false;

		private Stack<GameObject> stack = new Stack<GameObject>();
		private Queue<GameObject> queue = new Queue<GameObject>();

		private HashSet<GameObject> popped = new HashSet<GameObject>();

		public int poppedCount { get { return popped.Count; } }

		public int poolCount { get { return useQueue ? queue.Count: stack.Count; } }

		private HideFlags templateHideFlags = HideFlags.None;

		private Dictionary<GameObject, Coroutine> popRoutines = new Dictionary<GameObject, Coroutine>();

		#endregion

		////////////////////////////////
		//	MonoMethods

		#region MonoMethods

		private void Awake()
		{
			template.SetActive( false );

			if( poolOnAwake )
			{
				Setup( poolSize );
			}
		}

		public void Setup( int poolSize = -1 )
		{
			if( initialized )
				Cleanup();

			if( poolSize > 0 )
				this.poolSize = poolSize;

			Transform parent = template.transform.parent;

			string name = template.name;

			for( int i = 0; i < this.poolSize; i++ )
			{
				GameObject gameObject = GameObject.Instantiate( template, parent );

				gameObject.name = name;

				gameObject.hideFlags = hideFlags;

				Push( gameObject );
			}

			templateHideFlags = template.hideFlags;
			template.hideFlags = HideFlags.HideInHierarchy;

			initialized = true;
		}

		public void Cleanup()
		{
			if( useQueue )
			{
				while( queue.Count > 0 )
				{
					GameObject pop = queue.Dequeue();

					if( pop )
						GameObject.Destroy( pop );
				}
			}
			else
			{
				while( stack.Count > 0 )
				{
					GameObject pop = stack.Pop();

					if( pop )
						GameObject.Destroy( pop );
				}
			}

			foreach( GameObject pop in popped )
			{
				if( pop )
					GameObject.Destroy( pop );
			}

			popped.Clear();

			queue.Clear();

			stack.Clear();

			template.hideFlags = templateHideFlags;

			initialized = false;
		}

		#endregion

		////////////////////////////////
		//	Pool

		#region Pool

		public GameObject Pop()
		{
			if( ( useQueue ? queue.Count : stack.Count ) == 0 )
				return null;

			GameObject pop = useQueue ? queue.Dequeue() : stack.Pop();

			popped.Add( pop );

			pop.SetActive( true );

			return pop;
		}

		public GameObject Pop( float pushDelay )
		{
			GameObject pop = Pop();

			popRoutines.Add( pop, StartCoroutine( DelayedPush( pop, pushDelay ) ) );

			return pop;
		}

		private IEnumerator DelayedPush( GameObject obj, float pushDelay )
		{
			yield return new WaitForSeconds( pushDelay );

			Push( obj );

			yield break;
		}

		public void Push( GameObject obj )
		{
			if( obj )
			{
				obj.SetActive( false );
				if( popRoutines.ContainsKey( obj ) )
				{
					StopCoroutine( popRoutines[ obj ] );
					popRoutines.Remove( obj );
				}
			}

			if( useQueue )
			{
				queue.Enqueue( obj );
			}
			else
			{
				stack.Push( obj );
			}

			popped.Remove( obj );
		}

		#endregion
	}
}