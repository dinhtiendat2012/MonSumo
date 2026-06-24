using MonSumo.Core.Enums;
using UnityEngine;

namespace MonSumo.Core.State
{
    public sealed class PlayerIdleState : IPlayerState
    {
        public void Enter(PlayerStateMachine stateMachine)
        {
            stateMachine.Controller.SetVelocity(Vector2.zero);
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

            // Handle transition to moving states
            Vector2 input = controller.GetMoveInput();
            if (input.sqrMagnitude > 0.01f)
            {
                if (Input.GetKey(KeyCode.LeftShift) && controller.HasStamina())
                {
                    stateMachine.ChangeState(new PlayerSprintState(), PlayerMovementState.Sprint);
                }
                else
                {
                    stateMachine.ChangeState(new PlayerWalkState(), PlayerMovementState.Walk);
                }
            }
        }

        public void FixedUpdate(PlayerStateMachine stateMachine)
        {
            stateMachine.Controller.SetVelocity(Vector2.zero);
        }

        public void Exit(PlayerStateMachine stateMachine)
        {
        }
    }
}
