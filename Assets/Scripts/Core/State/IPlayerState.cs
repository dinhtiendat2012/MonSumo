namespace MonSumo.Core.State
{
    public interface IPlayerState
    {
        void Enter(PlayerStateMachine stateMachine);
        void Update(PlayerStateMachine stateMachine);
        void FixedUpdate(PlayerStateMachine stateMachine);
        void Exit(PlayerStateMachine stateMachine);
    }
}
