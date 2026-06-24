using MonSumo.Core.Enums;
using UnityEngine;

namespace MonSumo.Core.State
{
    public sealed class PlayerStateMachine
    {
        public PlayerMovement Controller { get; }
        public IPlayerState CurrentState { get; private set; }
        public PlayerMovementState StateEnum { get; private set; }

        public PlayerStateMachine(PlayerMovement controller)
        {
            Controller = controller;
        }

        public void Initialize(IPlayerState startingState, PlayerMovementState stateEnum)
        {
            CurrentState = startingState;
            StateEnum = stateEnum;
            CurrentState.Enter(this);
        }

        public void ChangeState(IPlayerState newState, PlayerMovementState stateEnum)
        {
            CurrentState?.Exit(this);
            CurrentState = newState;
            StateEnum = stateEnum;
            CurrentState.Enter(this);
        }

        public void Update()
        {
            CurrentState?.Update(this);
        }

        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate(this);
        }
    }
}
