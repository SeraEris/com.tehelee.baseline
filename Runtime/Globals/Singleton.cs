using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tehelee.Baseline
{
	public class Singleton<T> where T : class
	{
		public Singleton() { }

		private T _instance;
		public T instance
		{
			get => _instance;
			set
			{
				_instance = value;

				if( this )
				{
					onInstance?.Invoke( value );

					onNextInstance?.Invoke( value );
					onNextInstance = null;
				}
			}
		}

		public static implicit operator T( Singleton<T> singleton ) => singleton._instance;
		
		public static implicit operator bool( Singleton<T> singleton ) => !object.Equals( null, singleton._instance );

		public delegate void OnInstance( T instance );

		private event OnInstance onInstance = null;
		private event OnInstance onNextInstance = null;

		public void RegisterListener( OnInstance listener, bool clearOnInvoke = false )
		{
			if( !object.Equals( null, _instance ) )
			{
				listener( _instance );

				if( !clearOnInvoke )
					this.onInstance += listener;
			}
			else
			{
				if( clearOnInvoke )
					this.onNextInstance += listener;
				else
					this.onInstance += listener;
			}
		}

		public void DropListener( OnInstance listener )
		{
			onInstance -= listener;
			onNextInstance -= listener;
		}
	}
}