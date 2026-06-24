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

        [Header("Skill Settings")]
        [SerializeField] private float _skillCooldown = 10f;

        private float _skillCooldownTimer;
        private float _attackCooldownTimer;
        private float _areaSpeedMultiplier = 1f;
        private Vector2 _moveInput;
        private Vector2 _facingDirection = Vector2.down;

        private Rigidbody2D _rb;
        private Player _player;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private SkillController _skillController;
        private PlayerStateMachine _stateMachine;

        private readonly NetworkVariable<int> _netState = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<Vector2> _netFacingDirection = new NetworkVariable<Vector2>(Vector2.down, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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
            _skillController = GetComponentInChildren<SkillController>();
        }

        private void Start()
        {
            if (_player != null && _player.playerData != null)
            {
                var data = _player.playerData;
                _maxStamina = data.maxStamina;
                _staminaRegenRate = data.staminaRegenPerSecond;
                _dashStaminaCost = data.dashStaminaCost;
                _dashCooldown = data.dashCooldown;
                _attackCooldown = data.pushCooldown;
                
                if (_animator != null && data.animatorController != null)
                {
                    _animator.runtimeAnimatorController = data.animatorController;
                }
            }

            _currentStamina = _maxStamina;
            _stateMachine.Initialize(new PlayerIdleState(), PlayerMovementState.Idle);
        }

        private void Update()
        {
            // Dynamically ignore solid collisions between players to prevent passive pushing
            IgnoreOtherPlayersCollisions();

            if (IsOwner)
            {
                // Handle WASD inputs
                _moveInput.x = Input.GetAxisRaw("Horizontal");
                _moveInput.y = Input.GetAxisRaw("Vertical");

                if (_moveInput.sqrMagnitude > 0.01f)
                {
                    _facingDirection = _moveInput.normalized;
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

                // Update Skill Cooldown Timer
                if (_skillCooldownTimer > 0f)
                {
                    _skillCooldownTimer -= Time.deltaTime;
                }

                // Stamina regeneration (only when not sprinting or dashing)
                if (_stateMachine.StateEnum != PlayerMovementState.Sprint && _stateMachine.StateEnum != PlayerMovementState.Dash)
                {
                    _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _staminaRegenRate * Time.deltaTime);
                }

                // Update State Machine
                _stateMachine.Update();

                // Write to synchronized network variables
                _netState.Value = (int)_stateMachine.StateEnum;
                _netFacingDirection.Value = _facingDirection;

                // Normal Attack (Wired in Phase 4)
                if (Input.GetMouseButtonDown(0))
                {
                    RequestAttack();
                }

                // Skill Activation (KeyCode.E)
                if (Input.GetKey(KeyCode.E))
                {
                    if (_skillCooldownTimer <= 0f)
                    {
                        _skillCooldownTimer = _skillCooldown;
                        _skillController.UseSkill();
                    }
                }
            }

            // Sync animation parameters if available
            UpdateAnimator();
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
            float cost = (_player != null && _player.playerData != null)
                ? _player.playerData.sprintStaminaCostPerSecond
                : 2f;
            _currentStamina = Mathf.Max(0f, _currentStamina - cost * dt);
        }

        public float GetDashDuration()
        {
            return (_player != null && _player.playerData != null)
                ? _player.playerData.dashDuration
                : 0.2f;
        }

        public float GetDashSpeedMultiplier()
        {
            return (_player != null && _player.playerData != null)
                ? _player.playerData.dashSpeedMultiplier
                : 4f;
        }

        public float GetSprintSpeedMultiplier()
        {
            return (_player != null && _player.playerData != null)
                ? _player.playerData.sprintSpeedMultiplier
                : 1.5f;
        }

        public float GetKnockbackDuration()
        {
            return (_player != null && _player.playerData != null)
                ? _player.playerData.knockbackDuration
                : 0.3f;
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

            // Use networked variables to synchronize animations for remote clones
            int stateVal = _netState.Value;
            Vector2 facingDir = _netFacingDirection.Value;

            float speedMagnitude = _rb != null ? _rb.linearVelocity.magnitude : 0f;
            _animator.SetFloat("Speed", speedMagnitude);
            _animator.SetInteger("State", stateVal);
            _animator.SetFloat("DirX", facingDir.x);
            _animator.SetFloat("DirY", facingDir.y);

            // Synchronize Sprite flipping based on the synchronized facing direction
            if (_spriteRenderer != null)
            {
                if (facingDir.x < -0.01f) _spriteRenderer.flipX = true;
                else if (facingDir.x > 0.01f) _spriteRenderer.flipX = false;
            }
        }

        private void IgnoreOtherPlayersCollisions()
        {
            Collider2D myCol = GetComponent<Collider2D>();
            if (myCol == null) return;

            var allMovements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var other in allMovements)
            {
                if (other == this) continue;
                Collider2D otherCol = other.GetComponent<Collider2D>();
                if (otherCol != null)
                {
                    Physics2D.IgnoreCollision(myCol, otherCol, true);
                }
            }
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
            float baseKnockbackStrength = (_player != null && _player.playerData != null)
                ? _player.playerData.meleePushForce
                : 8f;

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

        [Rpc(SendTo.Server)]
        public void RequestDashKnockbackServerRpc(ulong targetObjectId, Vector2 pushDirection)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out var networkObject))
            {
                Player targetPlayer = networkObject.GetComponent<Player>();
                if (targetPlayer != null)
                {
                    float force = (_player != null && _player.playerData != null)
                        ? _player.playerData.dashPushForce
                        : 18f;

                    float actualKnockbackStrength = force;
                    if (targetPlayer.ReceivedPushMultiplier != 1f)
                    {
                        actualKnockbackStrength *= targetPlayer.ReceivedPushMultiplier;
                    }

                    targetPlayer.ApplyKnockbackRpc(pushDirection.normalized * actualKnockbackStrength);
                    Debug.Log($"[Combat] Server: Player {OwnerClientId} dashed into Player {targetPlayer.OwnerClientId} with force {actualKnockbackStrength}");

                    // Apply Thorn Shield force reflection back to attacker
                    if (targetPlayer.ReflectedPushPercent > 0.01f)
                    {
                        float reflectedForce = force * targetPlayer.ReflectedPushPercent;
                        Vector2 reflectDirection = -pushDirection.normalized;
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