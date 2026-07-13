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

        [Header("Character Registry")]
        [SerializeField] private PlayerDataSO[] availableCharacters;

        [Header("Audio")]
        private AudioClip itemPickupSFX;
        private AudioClip attackSFX;
        private AudioClip dashSFX;
        private AudioClip hitSFX;

        public readonly NetworkVariable<int> selectedCharacterId = new(
            1, // Default is 1 (Tanuki)
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("Network Stats")]
        public readonly NetworkVariable<int> currentHP = new(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> currentWeight = new(10f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> currentSpeed = new(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> currentPushForce = new(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> isDead = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<bool> isWinner = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

        private void OnEnable()
        {
            selectedCharacterId.OnValueChanged += HandleCharacterTypeChanged;
        }

        private void OnDisable()
        {
            selectedCharacterId.OnValueChanged -= HandleCharacterTypeChanged;
        }

        private void HandleCharacterTypeChanged(int oldValue, int newValue)
        {
            ApplyCharacterVisuals(newValue);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Assign server-side stats for the default selected character first
                ApplyCharacterStats(selectedCharacterId.Value);
            }

            // Apply visuals locally
            ApplyCharacterVisuals(selectedCharacterId.Value);

            // Clients send their selection to Server
            if (IsOwner)
            {
                int mySelection = CharacterSelection.SelectionCharacterId;
                // Default to 1 (Tanuki) if None selected
                if (mySelection == 0) mySelection = 1;

                RequestSetCharacterServerRpc(mySelection);
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestSetCharacterServerRpc(int characterId)
        {
            selectedCharacterId.Value = characterId;
            ApplyCharacterStats(characterId);
        }

        private PlayerDataSO GetPlayerData(int characterId)
        {
            if (availableCharacters == null || availableCharacters.Length == 0) return null;

            string targetName = "";
            switch (characterId)
            {
                case 1: targetName = "Tanuki"; break;
                case 2: targetName = "Kappa"; break;
                case 3: targetName = "Oni"; break;
                case 4: targetName = "Tengu"; break;
                case 5: targetName = "Yuki_Onna"; break;
                case 6: targetName = "Nurikabe"; break;
            }

            foreach (var data in availableCharacters)
            {
                if (data != null)
                {
                    bool nameMatch = !string.IsNullOrEmpty(data.yokaiName) && data.yokaiName.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0;
                    bool assetNameMatch = !string.IsNullOrEmpty(data.name) && data.name.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0;

                    if (nameMatch || assetNameMatch)
                    {
                        return data;
                    }
                }
            }

            return availableCharacters[0];
        }

        private void ApplyCharacterStats(int characterId)
        {
            if (!IsServer) return;

            PlayerDataSO data = GetPlayerData(characterId);
            if (data != null)
            {
                playerData = data;

                attackSFX = data.attackSFX;
                dashSFX = data.dashSFX;
                hitSFX = data.hitSFX;
                itemPickupSFX = data.itemPickupSFX;

                currentHP.Value = 3;
                currentWeight.Value = data.baseMass;
                currentSpeed.Value = data.baseSpeed;
                currentPushForce.Value = data.basePushForce;

                if (rb != null)
                {
                    rb.mass = data.baseMass;
                }
            }
        }

        private void ApplyCharacterVisuals(int characterId)
        {
            PlayerDataSO data = GetPlayerData(characterId);
            if (data != null)
            {
                playerData = data;

                attackSFX = data.attackSFX;
                dashSFX = data.dashSFX;
                hitSFX = data.hitSFX;
                itemPickupSFX = data.itemPickupSFX;

                var anim = GetComponent<Animator>();
                if (anim != null && data.animatorController != null)
                {
                    anim.runtimeAnimatorController = data.animatorController;
                }

                if (rb != null)
                {
                    rb.mass = data.baseMass;
                    Debug.Log($"[VisualSync] Applied local rigidbody mass {data.baseMass} for characterId {characterId} on Client {OwnerClientId}");
                }
            }
        }

        public void ApplyItemEffect(ItemDataSO itemData)
        {
            if (!IsServer) return;

            _activeItems[itemData.itemType] = itemData.duration;
            _activeItemConfigs[itemData.itemType] = itemData;
            RecalculateStats();

            PlayAudioClientRpc(PlayerAudioType.ItemPickup);
            
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
            if (isDead.Value) return; // Already dead

            currentHP.Value--;
            Debug.Log($"[PLAYER {OwnerClientId}] HP: {currentHP.Value}");

            if (currentHP.Value <= 0)
            {
                currentHP.Value = 0;
                isDead.Value = true;
                
                // PlayDeathAudio();
                
                // Hide player visual or disable movement
                DisablePlayerPhysicsClientRpc();

                PlayLoseAudio();

                // Check end conditions
                CheckGameEndConditions();
            }
            else
            {
                Respawn();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void DisablePlayerPhysicsClientRpc()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false; // Disable physical movement and collision completely
            }

            // Make sprite transparent or hidden
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }

            // Disable player HUD display inputs if owner
            if (IsOwner && movement != null)
            {
                movement.enabled = false;
            }
        }

        private void CheckGameEndConditions()
        {
            if (!IsServer) return;

            var allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
            
            // Collect all living players
            System.Collections.Generic.List<Player> livingPlayers = new System.Collections.Generic.List<Player>();
            foreach (var p in allPlayers)
            {
                if (p != null && !p.isDead.Value)
                {
                    livingPlayers.Add(p);
                }
            }

            // Check if game end reached
            // In a multiplayer game (usually 2+ players), game ends when there is 1 or 0 living players remaining
            if (livingPlayers.Count == 1)
            {
                Player winner = livingPlayers[0];
                winner.isWinner.Value = true;

                winner.PlayWinAudio();

                foreach (var player in allPlayers)
                {
                    if (player != null && player != winner)
                    {
                        player.PlayLoseAudio();
                    }
                }
                Debug.Log($"[GameEnd] Winner is Player {winner.OwnerClientId}");
            }
            else if (livingPlayers.Count == 0)
            {
                Debug.Log("[GameEnd] Everyone is dead! Draw game.");
            }
        }

        [Rpc(SendTo.Server)]
        public void RequestReturnToLobbyServerRpc()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                Debug.Log("[LobbyReturn] Server is loading Lobby scene...");
                // NetworkManager SceneManager will load Lobby and transition everyone cleanly
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
        }

        private void Respawn()
        {
            if (!IsServer) return;

            Vector2 spawnPos = respawnPoint != null ? (Vector2)respawnPoint.position : Vector2.zero;

            // Try to respawn in the center of the active shrinking/moving safe zone instead of the static initial point
            var zoneController = Object.FindAnyObjectByType<MonSumo.World.Zone.ZoneController>();
            if (zoneController != null)
            {
                spawnPos = zoneController.Center;
                Debug.Log($"[Respawn] Dynamically set respawn position to current Zone Center: {spawnPos}");
            }

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
            if (isDead.Value) return; // Dead players can't be pushed

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

        private void PlayItemPickupSFX()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(itemPickupSFX);
        }

        private void PlayAttackSFX()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(attackSFX);
        }

        [Rpc(SendTo.Owner)]
        private void PlayAudioClientRpc(PlayerAudioType audioType)
        {
            switch (audioType)
            {
                case PlayerAudioType.ItemPickup:
                    PlayItemPickupSFX();
                    break;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void PlayWorldAudioClientRpc(PlayerAudioType audioType)
        {
            switch (audioType)
            {
                case PlayerAudioType.Attack:
                    PlayAttackSFX();
                    break;
                case PlayerAudioType.Dash:
                    PlayDashSFX();
                    break;
                case PlayerAudioType.Hit:
                    PlayHitSFX();
                    break;
                case PlayerAudioType.Death:
                    AudioManager.Instance.PlayDeathSFX();
                    break;             
            }
        }

        [Rpc(SendTo.Owner)]
        private void PlayWinAudioClientRpc()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayWinSFX();
        }

        [Rpc(SendTo.Owner)]
        private void PlayLoseAudioClientRpc()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayLoseSFX();
        }

        public AudioClip GetDashSFX()
        {
            return dashSFX;
        }

        public AudioClip GetHitSFX()
        {
            return hitSFX;
        }

        public AudioClip GetItemPickupSFX()
        {
            return itemPickupSFX;
        }

        private void PlayDashSFX()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(dashSFX);
        }

        private void PlayHitSFX()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(hitSFX);
        }

        public void PlayHitAudio()
        {
            PlayWorldAudioClientRpc(PlayerAudioType.Hit);
        }

        public void PlayDashAudio()
        {
            PlayWorldAudioClientRpc(PlayerAudioType.Dash);
        }
        
        public void PlayAttackAudio()
        {
            PlayWorldAudioClientRpc(PlayerAudioType.Attack);
        }

        public void PlayDeathAudio()
        {
            Debug.Log("[AUDIO] PlayDeathAudio() called");
            PlayWorldAudioClientRpc(PlayerAudioType.Death);
        }

        public void PlayWinAudio()
        {
            PlayWinAudioClientRpc();
        }

        public void PlayLoseAudio()
        {
            PlayLoseAudioClientRpc();
        }
    }
}