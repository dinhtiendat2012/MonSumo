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

        // Glow effect fields
        private static Sprite _glowSprite;
        private SpriteRenderer _glowRenderer;
        private float _baseGlowAlpha;
        private float _pulseSpeed;
        private float _pulseRange;
        private Vector3 _baseGlowScale;

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

        private void Start()
        {
            AdjustScale();
            InitializeGlow();
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

            // Update glow effect (if any)
            UpdateGlow();

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

        private static Sprite GetOrCreateGlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;

            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2.0f;
            float maxRadius = size / 2.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1.0f - (dist / maxRadius));
                    alpha = Mathf.Pow(alpha, 2f); // soft exponential falloff
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            _glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _glowSprite;
        }

        private void AdjustScale()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Safeguard: Assign sprite from ItemData if currently null
            if (_spriteRenderer != null && _spriteRenderer.sprite == null && _itemData != null && _itemData.icon != null)
            {
                _spriteRenderer.sprite = _itemData.icon;
            }

            if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

            float targetWidth = 5f; // default fallback (player scale 5 * 1 unit sprite)
            
            // Find player in scene to match width dynamically
            var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            if (players != null && players.Length > 0)
            {
                var player = players[0];
                var playerSr = player.GetComponent<SpriteRenderer>();
                if (playerSr != null && playerSr.sprite != null)
                {
                    targetWidth = (playerSr.sprite.rect.width / playerSr.sprite.pixelsPerUnit) * player.transform.localScale.x;
                }
            }

            float itemSpriteLocalWidth = _spriteRenderer.sprite.rect.width / _spriteRenderer.sprite.pixelsPerUnit;
            if (itemSpriteLocalWidth > 0f)
            {
                float requiredScale = (targetWidth / 4f) / itemSpriteLocalWidth;
                transform.localScale = new Vector3(requiredScale, requiredScale, 1f);
            }
        }

        private void InitializeGlow()
        {
            if (_itemData == null) return;

            var rarity = _itemData.Rarity;
            if (rarity == MonSumo.Core.Enums.ItemRarity.Common) return;

            // Create child object for glow
            GameObject glowObj = new GameObject("GlowEffect");
            glowObj.transform.SetParent(transform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localRotation = Quaternion.identity;

            _glowRenderer = glowObj.AddComponent<SpriteRenderer>();
            _glowRenderer.sprite = GetOrCreateGlowSprite();
            _glowRenderer.sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder - 1 : 0;

            if (rarity == MonSumo.Core.Enums.ItemRarity.Rare)
            {
                _glowRenderer.color = new Color(0f, 0.7f, 1f, 0.4f); // Cyan/Blue
                _baseGlowAlpha = 0.4f;
                _pulseSpeed = 2f;
                _pulseRange = 0.1f;
                glowObj.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            }
            else if (rarity == MonSumo.Core.Enums.ItemRarity.Epic)
            {
                _glowRenderer.color = new Color(1f, 0.55f, 0f, 0.6f); // Gold/Orange
                _baseGlowAlpha = 0.6f;
                _pulseSpeed = 4f;
                _pulseRange = 0.15f;
                glowObj.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            }

            _baseGlowScale = glowObj.transform.localScale;
        }

        private void UpdateGlow()
        {
            if (_glowRenderer == null) return;

            // Pulsate scale
            float scaleOffset = Mathf.PingPong(Time.time * _pulseSpeed, _pulseRange);
            _glowRenderer.transform.localScale = _baseGlowScale * (1f + scaleOffset);

            // Pulsate alpha slightly (e.g. within 20% of base alpha)
            float alphaOffset = Mathf.PingPong(Time.time * _pulseSpeed * 0.5f, 0.1f);
            Color gc = _glowRenderer.color;
            float targetAlpha = _baseGlowAlpha - 0.05f + alphaOffset;
            gc.a = _isVisible ? targetAlpha : targetAlpha * 0.2f;
            _glowRenderer.color = gc;
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
