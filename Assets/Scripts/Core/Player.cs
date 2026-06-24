using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using MonSumo.Data;

namespace MonSumo.Core
{
    public class Player : NetworkBehaviour
    {
        [Header("Yokai Config")]
        public PlayerDataSO playerData;

        [Header("Network Stats")]
        public readonly NetworkVariable<int> currentHP = new(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> currentWeight = new(10f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> currentSpeed = new(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> currentPushForce = new(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Player Name synchronized from lobby
        public readonly NetworkVariable<Unity.Collections.FixedString32Bytes> playerName = new(
            "Player",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("Respawn")]
        public Transform respawnPoint;

        private Rigidbody2D rb;
        private PlayerMovement movement;

        // Multipliers
        private float _speedMultiplier = 1f;
        private float _massMultiplier = 1f;
        private float _forceMultiplier = 1f;
        private float _receivedPushMultiplier = 1f;
        private float _reflectedPushPercent = 0f;

        public float ReceivedPushMultiplier => _receivedPushMultiplier;
        public float ReflectedPushPercent => _reflectedPushPercent;

        private readonly System.Collections.Generic.Dictionary<MonSumo.Core.Enums.ItemType, float> _activeItems = new();
        private readonly System.Collections.Generic.Dictionary<MonSumo.Core.Enums.ItemType, ItemDataSO> _activeItemConfigs = new();

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (playerData != null)
                {
                    currentHP.Value = 3; // base HP
                    currentWeight.Value = playerData.baseMass;
                    currentSpeed.Value = playerData.baseSpeed;
                    currentPushForce.Value = playerData.basePushForce;
                }
                else
                {
                    currentHP.Value = 3;
                    currentWeight.Value = 10f;
                    currentSpeed.Value = 5f;
                    currentPushForce.Value = 5f;
                }
            }
        }

        public void ApplyItemEffect(ItemDataSO itemData)
        {
            if (!IsServer) return;

            _activeItems[itemData.itemType] = itemData.duration;
            _activeItemConfigs[itemData.itemType] = itemData;
            RecalculateStats();
            
            Debug.Log($"[Item] Applied {itemData.itemName} to Player {OwnerClientId}. Duration: {itemData.duration}s");
        }

        private void UpdateActiveItems()
        {
            if (_activeItems.Count == 0) return;

            var keys = new System.Collections.Generic.List<MonSumo.Core.Enums.ItemType>(_activeItems.Keys);
            bool changed = false;

            foreach (var key in keys)
            {
                _activeItems[key] -= Time.deltaTime;
                if (_activeItems[key] <= 0f)
                {
                    _activeItems.Remove(key);
                    _activeItemConfigs.Remove(key);
                    changed = true;
                    Debug.Log($"[Item] Effect of type {key} expired on Player {OwnerClientId}.");
                }
            }

            if (changed)
            {
                RecalculateStats();
            }
        }

        private void RecalculateStats()
        {
            _speedMultiplier = 1f;
            _massMultiplier = 1f;
            _forceMultiplier = 1f;
            _receivedPushMultiplier = 1f;
            _reflectedPushPercent = 0f;

            foreach (var kvp in _activeItemConfigs)
            {
                ItemDataSO config = kvp.Value;
                _speedMultiplier += config.speedModifierPercent;
                _massMultiplier += config.massModifierPercent;
                _forceMultiplier += config.pushForceModifierPercent;
                
                _receivedPushMultiplier *= config.receivedPushMultiplier;
                _reflectedPushPercent = Mathf.Max(_reflectedPushPercent, config.reflectedPushPercent);
            }

            float baseSpeed = playerData != null ? playerData.baseSpeed : 5f;
            float baseMass = playerData != null ? playerData.baseMass : 10f;
            float baseForce = playerData != null ? playerData.basePushForce : 5f;

            currentSpeed.Value = baseSpeed * _speedMultiplier;
            currentWeight.Value = baseMass * _massMultiplier;
            currentPushForce.Value = baseForce * _forceMultiplier;
        }

        // Called on Server when player dies / falls out of map / hit by hazard
        public void TakeDamage()
        {
            if (!IsServer) return;

            currentHP.Value--;
            Debug.Log($"[PLAYER {OwnerClientId}] HP: {currentHP.Value}");

            if (currentHP.Value <= 0)
            {
                GameOverClientRpc();
            }
            else
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            if (!IsServer) return;

            Vector2 spawnPos = respawnPoint != null ? (Vector2)respawnPoint.position : Vector2.zero;
            RespawnClientRpc(spawnPos);
        }

        [Rpc(SendTo.Owner)]
        private void RespawnClientRpc(Vector2 position)
        {
            transform.position = position;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            Debug.Log("Respawned owner client!");
        }

        [Rpc(SendTo.Owner)]
        public void ApplyKnockbackRpc(Vector2 force)
        {
            if (rb != null)
            {
                if (movement != null)
                {
                    movement.StartKnockback();
                }

                rb.AddForce(force, ForceMode2D.Impulse);
                Debug.Log($"[Knockback] Applied force: {force}");
            }
        }

        [Rpc(SendTo.Owner)]
        private void GameOverClientRpc()
        {
            Debug.Log("GAME OVER");
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Update()
        {
            if (IsServer)
            {
                UpdateActiveItems();
            }

            if (!IsOwner) return;

            // Press K to test taking damage
            if (Input.GetKeyDown(KeyCode.K))
            {
                RequestTakeDamageServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestTakeDamageServerRpc()
        {
            TakeDamage();
        }
    }
}