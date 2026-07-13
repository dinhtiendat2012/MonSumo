using MonSumo.Core.Enums;
using UnityEngine;

namespace MonSumo.Core.State
{
    public sealed class PlayerWalkState : IPlayerState
    {
        public void Enter(PlayerStateMachine stateMachine)
        {
        }

        public void Update(PlayerStateMachine stateMachine)
        {
            var controller = stateMachine.Controller;

            // Handle Dash Request
            if (Input.GetKeyDown(KeyCode.Space) && controller.CanDash())
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

            // Transition to Sprint if holding Shift and has stamina
            if (Input.GetKey(KeyCode.LeftShift) && controller.HasStamina())
            {
                stateMachine.ChangeState(new PlayerSprintState(), PlayerMovementState.Sprint);
            }
        }

        public void FixedUpdate(PlayerStateMachine stateMachine)
        {
            var controller = stateMachine.Controller;
            Vector2 input = controller.GetMoveInput();
            float speed = controller.GetBaseSpeed();
            controller.SetVelocity(input.normalized * speed);
        }

        public void Exit(PlayerStateMachine stateMachine)
        {
        }
    }
}
