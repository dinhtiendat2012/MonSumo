using MonSumo.Core.Enums;
using UnityEngine;

namespace MonSumo.Core.State
{
    public sealed class PlayerKnockbackState : IPlayerState
    {
        private float _knockbackTimer;

        public void Enter(PlayerStateMachine stateMachine)
        {
            _knockbackTimer = stateMachine.Controller.GetKnockbackDuration();
            // Do not override velocity on enter, let the Rigidbody force apply
        }

        public void Update(PlayerStateMachine stateMachine)
        {
            _knockbackTimer -= Time.deltaTime;
            if (_knockbackTimer <= 0f)
            {
                var controller = stateMachine.Controller;
                Vector2 input = controller.GetMoveInput();
                if (input.sqrMagnitude > 0.01f)
                {
                    stateMachine.ChangeState(new PlayerWalkState(), PlayerMovementState.Walk);
                }
                else
                {
                    stateMachine.ChangeState(new PlayerIdleState(), PlayerMovementState.Idle);
                }
            }
        }

        public void FixedUpdate(PlayerStateMachine stateMachine)
        {
            // Do not touch velocity in FixedUpdate, let physics do the knockback motion
        }

        public void Exit(PlayerStateMachine stateMachine)
        {
        }
    }
}
