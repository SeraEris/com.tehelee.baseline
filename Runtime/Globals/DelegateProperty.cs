using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tehelee.Baseline
{
	public class DelegateProperty<T,V> where T : class
	{
		public T obj;

		private V _value;
		public V value
		{
			get => _value;
			set
			{
				V last = _value;

				_value = value;

				onChanged?.Invoke( obj, last );
			}
		}

		public delegate void OnChanged( T obj, V last );

		public OnChanged onChanged;

		public DelegateProperty( V value, OnChanged onChanged = null )
		{
			_value = value;

			this.onChanged += onChanged;
		}

		public DelegateProperty( T obj, V value, OnChanged onChanged = null )
		{
			this.obj = obj;
			_value = value;

			this.onChanged += onChanged;
		}

		public static implicit operator V( DelegateProperty<T,V> delegateProperty ) => delegateProperty._value;
	}
}