using UnityEngine;
using Unity.Netcode;
using MonSumo.Core.State;
using MonSumo.Core.Enums;

namespace MonSumo.Core
{
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("Stamina UI (Local debug)")]
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _staminaRegenRate = 120f;
        [SerializeField] private float _dashStaminaCost = 10f;
        [SerializeField] private float _dashCooldown = 3f;

        private float _currentStamina;
        private float _dashCooldownTimer;

        [Header("Attack Settings")]
        [SerializeField] private float _attackCooldown = 3f;
        [SerializeField] private float _attackRange = 1.0f;
        [SerializeField] private float _attackRadius = 1.0f;

        private float _attackCooldownTimer;
        private float _areaSpeedMultiplier = 1f;
        private Vector2 _moveInput;
        private Vector2 _facingDirection = Vector2.down;

        private Rigidbody2D _rb;
        private Player _player;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        
        private PlayerStateMachine _stateMachine;

        // Public properties for debug and UI
        public float CurrentStamina => _currentStamina;
        public float MaxStamina => _maxStamina;
        public float DashCooldownTimer => _dashCooldownTimer;
        public PlayerMovementState CurrentStateEnum => _stateMachine != null ? _stateMachine.StateEnum : PlayerMovementState.Idle;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _player = GetComponent<Player>();
            _animator = GetComponentInChildren<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _stateMachine = new PlayerStateMachine(this);
        }

        private void Start()
        {
            _currentStamina = _maxStamina;
            _stateMachine.Initialize(new PlayerIdleState(), PlayerMovementState.Idle);
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Handle WASD inputs
            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _facingDirection = _moveInput.normalized;
                
                // Optional Sprite Flip
                if (_spriteRenderer != null)
                {
                    if (_moveInput.x < -0.01f) _spriteRenderer.flipX = true;
                    else if (_moveInput.x > 0.01f) _spriteRenderer.flipX = false;
                }
            }

            // Update Dash Cooldown Timer
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
            }

            // Update Attack Cooldown Timer
            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= Time.deltaTime;
            }

            // Stamina regeneration (only when not sprinting or dashing)
            if (_stateMachine.StateEnum != PlayerMovementState.Sprint && _stateMachine.StateEnum != PlayerMovementState.Dash)
            {
                _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _staminaRegenRate * Time.deltaTime);
            }

            // Update State Machine
            _stateMachine.Update();

            // Sync animation parameters if available
            UpdateAnimator();

            // Normal Attack (Wired in Phase 4)
            if (Input.GetMouseButtonDown(0))
            {
                RequestAttack();
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            _stateMachine.FixedUpdate();
        }

        #region Helper methods for States

        public Vector2 GetMoveInput() => _moveInput;

        public Vector2 GetFacingDirection() => _facingDirection;

        public float GetBaseSpeed()
        {
            float baseSpeed = _player != null ? _player.currentSpeed.Value : 5f;
            return baseSpeed * _areaSpeedMultiplier;
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (_rb != null)
            {
                _rb.linearVelocity = velocity;
            }
        }

        public bool HasStamina() => _currentStamina > 0.01f;

        public bool CanDash()
        {
            return _currentStamina >= _dashStaminaCost && _dashCooldownTimer <= 0.01f;
        }

        public void TriggerDash()
        {
            _currentStamina = Mathf.Max(0f, _currentStamina - _dashStaminaCost);
            _dashCooldownTimer = _dashCooldown;
            
            // Send dash event to server for optional visual sync / log
            NotifyServerDashServerRpc();
        }

        public void ConsumeSprintStamina(float dt)
        {
            // Sprint cost is 2 pow/sec
            _currentStamina = Mathf.Max(0f, _currentStamina - 2f * dt);
        }

        public void StartKnockback()
        {
            if (!IsOwner) return;
            _stateMachine.ChangeState(new PlayerKnockbackState(), PlayerMovementState.Knockback);
        }

        public void SetAreaSpeedMultiplier(float multiplier)
        {
            _areaSpeedMultiplier = multiplier;
        }

        #endregion

        #region Animator Integration

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            float speedMagnitude = _rb != null ? _rb.linearVelocity.magnitude : 0f;
            _animator.SetFloat("Speed", speedMagnitude);
            _animator.SetInteger("State", (int)_stateMachine.StateEnum);
        }

        #endregion

        #region Combat System (Phase 4)

        private void RequestAttack()
        {
            if (_attackCooldownTimer > 0f) return;
            _attackCooldownTimer = _attackCooldown;
            RequestAttackServerRpc(_facingDirection);
        }

        #endregion

        #region Network Rpc Calls

        [Rpc(SendTo.Server)]
        private void NotifyServerDashServerRpc()
        {
            // Server-side logging or validation if needed
            Debug.Log($"[PlayerMovement] Client {OwnerClientId} triggered Dash.");
        }

        [Rpc(SendTo.Server)]
        private void RequestAttackServerRpc(Vector2 attackDirection)
        {
            float attackerMass = _player != null ? _player.currentWeight.Value : 10f;
            float attackerForce = _player != null ? _player.currentPushForce.Value : 5f;
            float attackerSpeed = _rb != null ? _rb.linearVelocity.magnitude : 0f;

            float baseKnockbackStrength = attackerMass + attackerForce + attackerSpeed;

            Vector2 origin = (Vector2)transform.position + attackDirection.normalized * _attackRange;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, _attackRadius);

            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;

                var targetPlayer = col.GetComponent<Player>();
                if (targetPlayer != null)
                {
                    Vector2 pushDirection = (col.transform.position - transform.position).normalized;
                    if (pushDirection.sqrMagnitude < 0.01f)
                    {
                        pushDirection = attackDirection.normalized;
                    }

                    // Apply Thorn Shield received push reduction
                    float actualKnockbackStrength = baseKnockbackStrength;
                    if (targetPlayer.ReceivedPushMultiplier != 1f)
                    {
                        actualKnockbackStrength *= targetPlayer.ReceivedPushMultiplier;
                    }

                    targetPlayer.ApplyKnockbackRpc(pushDirection * actualKnockbackStrength);
                    Debug.Log($"[Combat] Server: Player {OwnerClientId} pushed Player {targetPlayer.OwnerClientId} with force {actualKnockbackStrength}");

                    // Apply Thorn Shield force reflection back to attacker
                    if (targetPlayer.ReflectedPushPercent > 0.01f)
                    {
                        float reflectedForce = baseKnockbackStrength * targetPlayer.ReflectedPushPercent;
                        Vector2 reflectDirection = -pushDirection; // Push back to attacker
                        if (_player != null)
                        {
                            _player.ApplyKnockbackRpc(reflectDirection * reflectedForce);
                            Debug.Log($"[Combat] Server: Player {targetPlayer.OwnerClientId} reflected {reflectedForce} force back to attacker {OwnerClientId}!");
                        }
                    }
                }
            }
        }

        #endregion
    }
}