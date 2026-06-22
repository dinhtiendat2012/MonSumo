using UnityEngine;
using Unity.Netcode;
using MonSumo.Data;
using MonSumo.Core;
using VContainer;

namespace MonSumo.Items
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(NetworkObject))]
    public class ItemPickup : NetworkBehaviour
    {
        [Header("Item Config")]
        [SerializeField] private ItemDataSO _itemData;
        [SerializeField] private SkillDefinition _skill;

        [Header("Visual Settings")]
        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField] private float _blinkStartThreshold = 5f; // Start blinking 5s before expiration
        [SerializeField] private float _blinkInterval = 0.2f;

        private SpriteRenderer _spriteRenderer;
        private float _lifetime = 15f; // Total lifetime 15s
        private float _blinkTimer;
        private bool _isPickedUp;
        private bool _isVisible = true;

        public ItemDataSO ItemData => _itemData;
        public SkillDefinition Skill => _skill;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // Ensure collider is trigger
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            _lifetime = 15f;
            _isPickedUp = false;
            _isVisible = true;
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = 1f;
                _spriteRenderer.color = c;
            }

            // Raise event locally
            var scope = VContainer.Unity.LifetimeScope.Find<MonSumo.Networking.Scopes.GameLifetimeScope>();
            var eventBus = scope?.Container.Resolve<EventBus>();
            eventBus?.RaiseItemSpawned(_itemData.itemType, transform.position);
        }

        private void Update()
        {
            // Visual rotation (all clients)
            transform.Rotate(Vector3.forward * _rotationSpeed * Time.deltaTime);

            // Handle Lifetime and Visual Blinking
            _lifetime -= Time.deltaTime;

            if (_lifetime <= _blinkStartThreshold)
            {
                // Blink effect
                _blinkTimer += Time.deltaTime;
                if (_blinkTimer >= _blinkInterval)
                {
                    _blinkTimer = 0f;
                    _isVisible = !_isVisible;
                    if (_spriteRenderer != null)
                    {
                        Color c = _spriteRenderer.color;
                        c.a = _isVisible ? 1f : 0.2f;
                        _spriteRenderer.color = c;
                    }
                }
            }

            // Server handles despawn
            if (IsServer && _lifetime <= 0f && !_isPickedUp)
            {
                _isPickedUp = true;
                GetComponent<NetworkObject>().Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || _isPickedUp) return;

            Player player = other.GetComponentInParent<Player>();
            if (player != null)
            {
                bool success = false;
                if (_itemData != null)
                {
                    player.ApplyItemEffect(_itemData);

                    // Raise event on EventBus via VContainer resolution
                    var scope = VContainer.Unity.LifetimeScope.Find<MonSumo.Networking.Scopes.GameLifetimeScope>();
                    var eventBus = scope?.Container.Resolve<EventBus>();
                    eventBus?.RaiseItemPickedUp(player.OwnerClientId, _itemData.itemType);
                    success = true;
                }
                else if (_skill != null)
                {
                    PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                    if (inventory != null && inventory.TryPickup(this))
                    {
                        success = true;
                    }
                }

                if (success)
                {
                    _isPickedUp = true;
                    GetComponent<NetworkObject>().Despawn();
                    Debug.Log($"[ItemPickup] Server: Item picked up by client {player.OwnerClientId}");
                }
            }
        }
    }
}
