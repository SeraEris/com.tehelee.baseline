namespace Tehelee.Baseline
{
	// Glorified boolean, but ensures conformity through casting and plaintext legibility
	public struct DelegateState : System.IEquatable<DelegateState>
	{
		private bool success;
		public static readonly DelegateState Fail = new DelegateState() { success = false };
		public static readonly DelegateState Pass = new DelegateState() { success = true };

		public static implicit operator bool( DelegateState delegateState ) => delegateState.success;
		public static implicit operator DelegateState( bool success ) => success ? Pass : Fail;

		public static bool operator ==( DelegateState a, DelegateState b )
		{
			return Equals( a, b );
		}

		public static bool operator !=( DelegateState a, DelegateState b )
		{
			return !( a == b );
		}

		public bool Equals( DelegateState delegateState )
		{
			return success == delegateState.success;
		}

		public override int GetHashCode()
		{
			return -468326000 + success.GetHashCode();
		}

		public override bool Equals( object obj )
		{
			if( !( obj is DelegateState ) )
				return false;

			return Equals( ( DelegateState ) obj );
		}

		public override string ToString()
		{
			return string.Format( "DelegateState.{0}", success ? "Pass" : "Fail" );
		}
	}
}