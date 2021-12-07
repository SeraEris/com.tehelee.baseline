using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Tehelee.Baseline.Components.UI
{
	public class PanelBase : MonoBehaviour
	{
		////////////////////////////////
		#region Properties

		public RectTransform rectTransform { get; private set; }

		public bool isOpen =>
			gameObject.activeInHierarchy && enabled;

		#endregion

		////////////////////////////////
		#region Events

		public event System.Action onOpen;
		public event System.Action onClose;

		#endregion

		////////////////////////////////
		#region Members
			
		private bool closed = false;

		#endregion

		////////////////////////////////
		#region Mono Methods

		protected virtual void Awake()
		{
			rectTransform = ( RectTransform ) transform;
		}

		private void OnEnable()
		{
			OnOpen();
			
			onOpen?.Invoke();
		}

		private void OnDisable()
		{
			OnClose();
			
			if( closed )
			{
				onClose?.Invoke();
				closed = false;
			}
		}

		#endregion

		////////////////////////////////
		#region PanelBase
		
		public void Open() =>
			gameObject.SetActive( true );

		public void Close()
		{
			closed = true;
			gameObject.SetActive( false );
		}

		protected virtual void OnOpen() { }

		protected virtual void OnClose() { }

		public void Submit() =>
			TrySubmit();

		public bool TrySubmit()
		{
			if( !isOpen )
				return false;

			if( OnSubmit() )
			{
				Close();
				return true;
			}

			return false;
		}

		public void Cancel() =>
			TryCancel();

		public bool TryCancel()
		{
			if( !isOpen )
				return false;

			if( OnCancel() )
			{
				Close();
				return true;
			}

			return false;
		}

		protected virtual bool OnSubmit() => true;

		protected virtual bool OnCancel() => true;

		#endregion
	}
}