using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tehelee.Baseline
{
	public class Singleton<T> where T : class
	{
		public Singleton() { }

		public Singleton( bool clearOnInvoke )
		{
			this.clearOnInvoke = clearOnInvoke;
		}

		public bool clearOnInvoke = false;

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

					if( clearOnInvoke )
						onInstance = null;
				}
			}
		}

		public static implicit operator T( Singleton<T> singleton ) => singleton._instance;
		
		public static implicit operator bool( Singleton<T> singleton ) => !object.Equals( null, singleton._instance );

		public delegate void OnInstance( T instance );

		public event OnInstance onInstance;

		public void ListenForInstance( OnInstance onInstance )
		{
			if( object.Equals( null, onInstance ) )
				return;

			this.onInstance += onInstance;

			if( this )
				onInstance( _instance );
		}
	}
}