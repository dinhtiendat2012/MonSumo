using MonSumo.Core.Enums;
using UnityEngine;

namespace MonSumo.Core.State
{
    public sealed class PlayerSprintState : IPlayerState
    {
        public void Enter(PlayerStateMachine stateMachine)
        {
        }

        public void Update(PlayerStateMachine stateMachine)
        {
            var controller = stateMachine.Controller;

            // Handle Dash Request
            if (Input.GetKeyDown(KeyCode.Q) && controller.CanDash())
            {
                stateMachine.ChangeState(new PlayerDashState(), PlayerMovementState.Dash);
                return;
            }

            Vector2 input = controller.GetMoveInput();
            if (input.sqrMagnitude < 0.01f)
            {
                stateMachine.ChangeState(new PlayerIdleState(), PlayerMovementState.Idle);
                return;
            }

            // Return to Walk if Shift released or out of stamina
            if (!Input.GetKey(KeyCode.LeftShift) || !controller.HasStamina())
            {
                stateMachine.ChangeState(new PlayerWalkState(), PlayerMovementState.Walk);
            }
        }

        public void FixedUpdate(PlayerStateMachine stateMachine)
        {
            var controller = stateMachine.Controller;
            Vector2 input = controller.GetMoveInput();
            float speed = controller.GetBaseSpeed() * 1.5f; // Sprint speed multiplier is 1.5x
            controller.SetVelocity(input.normalized * speed);

            // Consume stamina
            controller.ConsumeSprintStamina(Time.fixedDeltaTime);
        }

        public void Exit(PlayerStateMachine stateMachine)
        {
        }
    }
}
