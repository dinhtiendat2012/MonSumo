using System.Collections.Generic;
using MonSumo.Core.Enums;
using UnityEngine;

namespace MonSumo.Core.State
{
    public sealed class PlayerDashState : IPlayerState
    {
        private float _dashTimer;
        private Vector2 _dashDirection;
        private readonly HashSet<ulong> _hitClients = new HashSet<ulong>();

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

            // Active dash collision check
            DetectDashCollisions(stateMachine);
        }

        private void DetectDashCollisions(PlayerStateMachine stateMachine)
        {
            var controller = stateMachine.Controller;
            Collider2D myCol = controller.GetComponent<Collider2D>();
            if (myCol == null) return;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;

            Collider2D[] results = new Collider2D[10];
            int count = myCol.Overlap(filter, results);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = results[i];
                if (col == null || col.gameObject == controller.gameObject) continue;

                Player targetPlayer = col.GetComponent<Player>();
                if (targetPlayer != null)
                {
                    ulong targetId = targetPlayer.NetworkObjectId;
                    if (!_hitClients.Contains(targetId))
                    {
                        _hitClients.Add(targetId);

                        Vector2 pushDir = (targetPlayer.transform.position - controller.transform.position).normalized;
                        if (pushDir.sqrMagnitude < 0.01f)
                        {
                            pushDir = _dashDirection;
                        }

                        controller.RequestDashKnockbackServerRpc(targetId, pushDir);
                    }
                }
            }
        }

        public void Exit(PlayerStateMachine stateMachine)
        {
        }
    }
}
