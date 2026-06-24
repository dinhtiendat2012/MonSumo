using MonSumo.Core.Enums;
using UnityEngine;

namespace MonSumo.Core.State
{
    public sealed class PlayerDashState : IPlayerState
    {
        private float _dashTimer;
        private Vector2 _dashDirection;

        public void Enter(PlayerStateMachine stateMachine)
        {
            var controller = stateMachine.Controller;

            // Trigger dash logic (consume stamina, reset cooldown)
            controller.TriggerDash();

            // Set direction: input direction if moving, otherwise forward (or local facing direction)
            Vector2 input = controller.GetMoveInput();
            if (input.sqrMagnitude > 0.01f)
            {
                _dashDirection = input.normalized;
            }
            else
            {
                _dashDirection = controller.GetFacingDirection();
            }

            _dashTimer = controller.GetDashDuration();
        }

        public void Update(PlayerStateMachine stateMachine)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
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
            var controller = stateMachine.Controller;
            float dashSpeed = controller.GetBaseSpeed() * controller.GetDashSpeedMultiplier();
            controller.SetVelocity(_dashDirection * dashSpeed);
        }

        public void Exit(PlayerStateMachine stateMachine)
        {
        }
    }
}
