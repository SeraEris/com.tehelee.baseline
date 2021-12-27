using UnityEngine;

namespace Tehelee.Baseline
{
	public interface AnimProperty
	{
		void Apply( in Animator animator );
		void Apply( in Animator animator, float dampTime, float deltaTime );
	}
	
	public struct AnimBool : AnimProperty
	{
		public readonly bool useId;
		public readonly int id;
		public readonly string parameter;
		public bool value;

		public static implicit operator AnimBool( string parameter ) => new AnimBool( parameter );
		public static implicit operator AnimBool( int id ) => new AnimBool( id );

		public AnimBool( string parameter, bool value = false )
		{
			this.id = 0;
			this.useId = false;
			this.parameter = parameter;
			this.value = value;
		}
			
		public AnimBool( int id, bool value = false )
		{
			this.id = id;
			this.useId = true;
			this.parameter = null;
			this.value = value;
		}

		public void Apply( in Animator animator )
		{
			if( useId )
				animator.SetBool( id, value );
			else
				animator.SetBool( parameter, value );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( animator );
	}
	
	public struct AnimFloat : AnimProperty
	{
		public readonly bool useId;
		public readonly int id;
		public readonly string parameter;
		public float value;
		
		public static implicit operator AnimFloat( string parameter ) => new AnimFloat( parameter );
		public static implicit operator AnimFloat( int id ) => new AnimFloat( id );

		public AnimFloat( string parameter, float value = 0f )
		{
			this.id = 0;
			this.useId = false;
			this.parameter = parameter;
			this.value = value;
		}
			
		public AnimFloat( int id, float value = 0f )
		{
			this.id = id;
			this.useId = true;
			this.parameter = null;
			this.value = value;
		}

		public void Apply( in Animator animator )
		{
			if( useId )
				animator.SetFloat( id, value );
			else
				animator.SetFloat( parameter, value );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime )
		{
			if( useId )
				animator.SetFloat( id, value, dampTime, deltaTime );
			else
				animator.SetFloat( parameter, value, dampTime, deltaTime );
		}	
	}
	
	public struct AnimInteger : AnimProperty
	{
		public readonly bool useId;
		public readonly int id;
		public readonly string parameter;
		public int value;
		
		public static implicit operator AnimInteger( string parameter ) => new AnimInteger( parameter );
		public static implicit operator AnimInteger( int id ) => new AnimInteger( id );

		public AnimInteger( string parameter, int value = 0 )
		{
			this.id = 0;
			this.useId = false;
			this.parameter = parameter;
			this.value = value;
		}
			
		public AnimInteger( int id, int value = 0 )
		{
			this.id = id;
			this.useId = true;
			this.parameter = null;
			this.value = value;
		}

		public void Apply( in Animator animator )
		{
			if( useId )
				animator.SetInteger( id, value );
			else
				animator.SetInteger( parameter, value );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( animator );
	}
	
	public struct AnimTarget : AnimProperty
	{
		public readonly AvatarTarget avatarTarget;
		public float value;
		
		public static implicit operator AnimTarget( AvatarTarget avatarTarget ) => new AnimTarget( avatarTarget );

		public AnimTarget( AvatarTarget avatarTarget, float value = 0 )
		{
			this.avatarTarget = avatarTarget;
			this.value = value;
		}
		
		public void Apply( in Animator animator )
		{
			animator.SetTarget( avatarTarget, value );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
	
	public struct AnimTrigger : AnimProperty
	{
		public readonly bool useId;
		public readonly int id;
		public readonly string parameter;
		
		public static implicit operator AnimTrigger( string parameter ) => new AnimTrigger( parameter );
		public static implicit operator AnimTrigger( int id ) => new AnimTrigger( id );

		public AnimTrigger( string parameter )
		{
			this.id = 0;
			this.useId = false;
			this.parameter = parameter;
		}
			
		public AnimTrigger( int id )
		{
			this.id = id;
			this.useId = true;
			this.parameter = null;
		}

		public void Apply( in Animator animator )
		{
			if( useId )
				animator.SetTrigger( id );
			else
				animator.SetTrigger( parameter );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
	
	public struct AnimLayer : AnimProperty
	{
		public readonly int layer;
		public float weight;
		
		public static implicit operator AnimLayer( int layer ) => new AnimLayer( layer );
			
		public AnimLayer( int layer, float weight = 0f )
		{
			this.layer = layer;
			this.weight = weight;
		}

		public void Apply( in Animator animator ) =>
			animator.SetLayerWeight( layer, weight );

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
	
	public struct AnimBone : AnimProperty
	{
		public readonly HumanBodyBones humanBodyBone;
		public Quaternion goalRot;
		public float weight;
		
		public static implicit operator AnimBone( HumanBodyBones humanBodyBone ) => new AnimBone( humanBodyBone );

		public AnimBone( HumanBodyBones humanBodyBone, Quaternion goalRot = default, float weight = 0 )
		{
			this.humanBodyBone = humanBodyBone;
			this.goalRot = goalRot == default ? Quaternion.identity : goalRot;
			this.weight = weight;
		}
		
		public void Apply( in Animator animator )
		{
			animator.SetBoneLocalRotation( humanBodyBone, goalRot );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
	
	public struct AnimIKPos : AnimProperty
	{
		public readonly AvatarIKGoal avatarIKGoal;
		public Vector3 goalPos;
		public float weight;
		
		public static implicit operator AnimIKPos( AvatarIKGoal avatarIKGoal ) => new AnimIKPos( avatarIKGoal );

		public AnimIKPos( AvatarIKGoal avatarIKGoal, Vector3 goalPos = default, float weight = 0 )
		{
			this.avatarIKGoal = avatarIKGoal;
			this.goalPos = goalPos;
			this.weight = weight;
		}
		
		public void Apply( in Animator animator )
		{
			animator.SetIKPosition( avatarIKGoal, goalPos );
			animator.SetIKPositionWeight( avatarIKGoal, weight );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
	
	public struct AnimIKRot : AnimProperty
	{
		public readonly AvatarIKGoal avatarIKGoal;
		public Quaternion goalRot;
		public float weight;
		
		public static implicit operator AnimIKRot( AvatarIKGoal avatarIKGoal ) => new AnimIKRot( avatarIKGoal );

		public AnimIKRot( AvatarIKGoal avatarIKGoal, Quaternion goalRot = default, float weight = 0 )
		{
			this.avatarIKGoal = avatarIKGoal;
			this.goalRot = goalRot == default ? Quaternion.identity : goalRot;
			this.weight = weight;
		}
		
		public void Apply( in Animator animator )
		{
			animator.SetIKRotation( avatarIKGoal, goalRot );
			animator.SetIKRotationWeight( avatarIKGoal, weight );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
	
	public struct AnimIKHint  : AnimProperty
	{
		public readonly AvatarIKHint avatarIKHint;
		public Vector3 goalHint;
		public float weight;
		
		public static implicit operator AnimIKHint( AvatarIKHint avatarIKHint ) => new AnimIKHint( avatarIKHint );

		public AnimIKHint( AvatarIKHint avatarIKHint, Vector3 goalHint = default, float weight = 0 )
		{
			this.avatarIKHint = avatarIKHint;
			this.goalHint = goalHint;
			this.weight = weight;
		}
		
		public void Apply( in Animator animator )
		{
			animator.SetIKHintPosition( avatarIKHint, goalHint );
			animator.SetIKHintPositionWeight( avatarIKHint, weight );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
	
	public struct AnimLookAt  : AnimProperty
	{
		public Vector3 lookAtPos;
		public float weight;
		
		public static implicit operator AnimLookAt( Vector3 lookAtPos ) => new AnimLookAt( lookAtPos );
		public static implicit operator AnimLookAt( float weight ) => new AnimLookAt( Vector3.zero, weight );

		public AnimLookAt( Vector3 lookAtPos = default, float weight = 0 )
		{
			this.lookAtPos = lookAtPos;
			this.weight = weight;
		}
		
		public void Apply( in Animator animator )
		{
			animator.SetLookAtPosition( lookAtPos );
			animator.SetLookAtWeight( weight );
		}

		public void Apply( in Animator animator, float dampTime, float deltaTime ) =>
			Apply( in animator );
	}
}