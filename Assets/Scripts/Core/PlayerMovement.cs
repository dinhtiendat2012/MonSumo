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
        private readonly System.Collections.Generic.List<Player> _collidingPlayers = new();

        private readonly NetworkVariable<int> _netState = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<Vector2> _netFacingDirection = new NetworkVariable<Vector2>(Vector2.down, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Public properties for debug and UI
        public float CurrentStamina => _currentStamina;
        public float MaxStamina => _maxStamina;
        public float DashCooldownTimer => _dashCooldownTimer;
        public float DashCooldown => _dashCooldown;
        public float AttackCooldownTimer => _attackCooldownTimer;
        public float AttackCooldown => _attackCooldown;
        public float SkillCooldownTimer => _skillCooldownTimer;
        public float SkillCooldown => _skillCooldown;
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
                _skillCooldown = data.skillCooldown;
                
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

                // Stamina regeneration (only when not sprinting, not dashing, AND not holding the LeftShift key)
                if (_stateMachine.StateEnum != PlayerMovementState.Sprint && 
                    _stateMachine.StateEnum != PlayerMovementState.Dash &&
                    !Input.GetKey(KeyCode.LeftShift))
                {
                    _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _staminaRegenRate * Time.deltaTime);
                }

                // Update State Machine
                _stateMachine.Update();

                // Write to synchronized network variables
                _netState.Value = (int)_stateMachine.StateEnum;
                _netFacingDirection.Value = _facingDirection;

                // Normal Attack (Wired in Phase 4) - Allows dash-canceling to prevent action lockouts and dead-time
                if (Input.GetMouseButtonDown(0))
                {
                    if (CurrentStateEnum == PlayerMovementState.Dash)
                    {
                        _stateMachine.ChangeState(new PlayerIdleState(), PlayerMovementState.Idle);
                        Debug.Log("[Combat] Dash-canceled by Mouse Click!");
                    }
                    RequestAttack();
                }

                // Skill Activation (KeyCode.E) - Allows dash-canceling
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (_skillCooldownTimer <= 0f)
                    {
                        if (CurrentStateEnum == PlayerMovementState.Dash)
                        {
                            _stateMachine.ChangeState(new PlayerIdleState(), PlayerMovementState.Idle);
                            Debug.Log("[Combat] Dash-canceled by Skill Use!");
                        }
                        _skillCooldownTimer = _skillCooldown;
                        RequestUseSkillServerRpc();
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
                // If we are touching another player and trying to move towards them,
                // dampen the tangential velocity (sliding component) to make collisions feel solid and firm!
                if (_collidingPlayers.Count > 0 && velocity.sqrMagnitude > 0.01f)
                {
                    Vector2 combinedNormal = Vector2.zero;
                    int activeCount = 0;
                    foreach (var other in _collidingPlayers)
                    {
                        if (other != null)
                        {
                            Vector2 toOther = (other.transform.position - transform.position).normalized;
                            combinedNormal += toOther;
                            activeCount++;
                        }
                    }

                    if (activeCount > 0)
                    {
                        Vector2 normal = combinedNormal.normalized;
                        float dot = Vector2.Dot(velocity, normal);
                        if (dot > 0f) // Moving towards the other player(s)
                        {
                            // Decompose velocity into normal (pushing) and tangential (sliding) components
                            Vector2 normalProj = normal * dot;
                            Vector2 tangentProj = velocity - normalProj;

                            // Scale down the tangential sliding component completely (0.02) to lock head-on,
                            // and keep 85% of normal pushing power for a firm Sumo wrestling push feel!
                            velocity = normalProj * 0.85f + tangentProj * 0.02f;
                        }
                    }
                }

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

            if (_player != null)
            {
                _player.PlayDashAudio();
            }
            
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
            if (_player != null)
            {
                _player.PlayAttackAudio();
            }

            float baseKnockbackStrength = (_player != null && _player.playerData != null)
                ? _player.playerData.meleePushForce
                : 8f;

            // Apply item/buff multipliers: (Current Push Force / Base Push Force)
            if (_player != null && _player.playerData != null && _player.playerData.basePushForce > 0.01f)
            {
                float buffMultiplier = _player.currentPushForce.Value / _player.playerData.basePushForce;
                baseKnockbackStrength *= buffMultiplier;
                Debug.Log($"[Combat] Attack Buff Multiplier applied: {buffMultiplier}x (New Base: {baseKnockbackStrength})");
            }

            Vector2 origin = (Vector2)transform.position + attackDirection.normalized * _attackRange;
            
            // Combine forward attack circle and point-blank (touching) circle to guarantee point-blank hits!
            System.Collections.Generic.HashSet<Collider2D> uniqueColliders = new System.Collections.Generic.HashSet<Collider2D>();
            
            foreach (var col in Physics2D.OverlapCircleAll(origin, _attackRadius))
            {
                uniqueColliders.Add(col);
            }
            
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, 1.2f))
            {
                uniqueColliders.Add(col);
            }

            foreach (var col in uniqueColliders)
            {
                if (col.gameObject == gameObject) continue;

                var targetPlayer = col.GetComponentInParent<Player>();
                if (targetPlayer == null)
                {
                    targetPlayer = col.GetComponent<Player>();
                }

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
                    _player.PlayHitAudio();
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

                    // Apply item/buff multipliers: (Current Push Force / Base Push Force)
                    if (_player != null && _player.playerData != null && _player.playerData.basePushForce > 0.01f)
                    {
                        float buffMultiplier = _player.currentPushForce.Value / _player.playerData.basePushForce;
                        force *= buffMultiplier;
                        Debug.Log($"[Combat] Dash Buff Multiplier applied: {buffMultiplier}x (New Base: {force})");
                    }

                    float actualKnockbackStrength = force;
                    if (targetPlayer.ReceivedPushMultiplier != 1f)
                    {
                        actualKnockbackStrength *= targetPlayer.ReceivedPushMultiplier;
                    }

                    targetPlayer.ApplyKnockbackRpc(pushDirection.normalized * actualKnockbackStrength);
                    _player.PlayHitAudio();
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

        [Rpc(SendTo.Server)]
        private void RequestUseSkillServerRpc()
        {
            PlaySkillVisualsRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void PlaySkillVisualsRpc()
        {
            if (_skillController != null)
            {
                _skillController.UseSkill();
            }
        }

        private void OnDisable()
        {
            _collidingPlayers.Clear();
        }

        #endregion

        #region Collision Handling

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Player targetPlayer = collision.gameObject.GetComponent<Player>();
            if (targetPlayer != null)
            {
                if (!_collidingPlayers.Contains(targetPlayer))
                {
                    _collidingPlayers.Add(targetPlayer);
                }
            }

            // Only process solid collisions on the owner to prevent duplicate trigger and preserve authority
            if (!IsOwner) return;

            if (targetPlayer != null)
            {
                // If we are currently Dashing, apply heavy knockback and cancel our own dash forward velocity to stop sliding!
                if (CurrentStateEnum == PlayerMovementState.Dash)
                {
                    Vector2 pushDir = (targetPlayer.transform.position - transform.position).normalized;
                    if (pushDir.sqrMagnitude < 0.01f)
                    {
                        pushDir = _facingDirection;
                    }

                    // Trigger server-side heavy dash knockback
                    RequestDashKnockbackServerRpc(targetPlayer.NetworkObjectId, pushDir);

                    // Stop our own dash immediately so we don't slip/slide off their rounded collider sides!
                    _stateMachine.ChangeState(new PlayerIdleState(), PlayerMovementState.Idle);
                    Debug.Log($"[Collision] Owner {OwnerClientId} dashed into {targetPlayer.OwnerClientId}. Instantly canceled dash state to block sliding.");
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Only process solid collisions on the owner to prevent duplicate trigger and preserve authority
            if (!IsOwner) return;

            Player targetPlayer = collision.gameObject.GetComponent<Player>();
            if (targetPlayer != null)
            {
                // If we are currently Dashing while already touching another player, apply heavy knockback and cancel our dash immediately!
                if (CurrentStateEnum == PlayerMovementState.Dash)
                {
                    Vector2 pushDir = (targetPlayer.transform.position - transform.position).normalized;
                    if (pushDir.sqrMagnitude < 0.01f)
                    {
                        pushDir = _facingDirection;
                    }

                    // Trigger server-side heavy dash knockback
                    RequestDashKnockbackServerRpc(targetPlayer.NetworkObjectId, pushDir);

                    // Stop our own dash immediately so we don't slip/slide off their rounded collider sides!
                    _stateMachine.ChangeState(new PlayerIdleState(), PlayerMovementState.Idle);
                    Debug.Log($"[Collision] Owner {OwnerClientId} dashed while already touching {targetPlayer.OwnerClientId}. Instantly canceled dash state and pushed.");
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            Player targetPlayer = collision.gameObject.GetComponent<Player>();
            if (targetPlayer != null)
            {
                _collidingPlayers.Remove(targetPlayer);
            }
        }

        public void PlayDashAudio()
        {
            _player.PlayDashAudio();
        }

        #endregion
    }
}