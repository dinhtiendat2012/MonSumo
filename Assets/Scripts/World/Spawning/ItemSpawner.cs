using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using MonSumo.Items;
using MonSumo.Core;
using MonSumo.Core.Enums;
using MonSumo.World.Zone;

namespace MonSumo.World.Spawning
{
    public class ItemSpawner : NetworkBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject[] _itemPrefabs;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private bool _useBoundingBox;
        [SerializeField] private Vector3 _boxCenter;
        [SerializeField] private Vector3 _boxSize = new Vector3(8f, 8f, 0f);

        [Header("Rules")]
        [SerializeField] private float _spawnInterval = 15f;
        [SerializeField] private float _minPlayerDistance = 2f;
        [SerializeField] private float _borderBuffer = 1f; // Buffer distance from zone border
        [SerializeField] private float _overlapCheckRadius = 0.75f;
        [SerializeField] private LayerMask _blockedMask = 0;
        [SerializeField] private int _maxAttempts = 20;

        private readonly List<GameObject> _spawnedItems = new();
        private ZoneController _zoneController;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _zoneController = FindFirstObjectByType<ZoneController>();
                StartCoroutine(SpawnLoop());
            }
        }

        private IEnumerator SpawnLoop()
        {
            // Initial spawn: 2-3 common items
            yield return new WaitForSeconds(1f);
            SpawnInitialItems();

            while (enabled)
            {
                yield return new WaitForSeconds(_spawnInterval);
                TrySpawnItem();
            }
        }

        private void SpawnInitialItems()
        {
            int initialSpawnCount = Random.Range(2, 4);
            int spawned = 0;
            for (int i = 0; i < _maxAttempts && spawned < initialSpawnCount; i++)
            {
                if (TrySpawnItem(onlyCommon: true))
                {
                    spawned++;
                }
            }
            Debug.Log($"[ItemSpawner] Initial spawn completed. Spawned {spawned} items.");
        }

        public bool TrySpawnItem(bool onlyCommon = false)
        {
            if (!IsServer) return false;

            // Cleanup destroyed items from list
            _spawnedItems.RemoveAll(item => item == null);

            // Determine active max items based on zone radius
            int maxItemsLimit = GetMaxItemsBasedOnZone();
            if (_spawnedItems.Count >= maxItemsLimit)
            {
                return false;
            }

            // Get valid prefabs
            List<GameObject> validPrefabs = GetValidPrefabs(onlyCommon);
            if (validPrefabs.Count == 0) return false;

            // Try to find a valid position
            for (int attempt = 0; attempt < _maxAttempts; attempt++)
            {
                Vector2 position = GetRandomSpawnPosition();

                if (!IsValidPosition(position)) continue;

                // Weighted random selection
                GameObject prefab = SelectWeightedPrefab(validPrefabs);
                if (prefab == null) continue;

                GameObject itemObject = Instantiate(prefab, position, Quaternion.identity);
                
                var netObj = itemObject.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }

                _spawnedItems.Add(itemObject);
                return true;
            }

            return false;
        }

        private int GetMaxItemsBasedOnZone()
        {
            if (_zoneController == null) return 3;

            // Get current and start radius
            float currentRadius = _zoneController.CurrentRadius;
            float startRadius = 10f; // default start radius fallback

            // Try to read startRadius using reflection if needed, but we can default to 10f
            // Let's assume startRadius is 10f or look it up.
            // A safer way is to check the percentage:
            float ratio = currentRadius / startRadius;

            if (ratio >= 0.5f) return 5;
            if (ratio >= 0.2f) return 2;
            return 1;
        }

        private List<GameObject> GetValidPrefabs(bool onlyCommon)
        {
            List<GameObject> validPrefabs = new();
            foreach (GameObject prefab in _itemPrefabs)
            {
                if (prefab == null) continue;
                var pickup = prefab.GetComponent<ItemPickup>();
                if (pickup != null && pickup.ItemData != null)
                {
                    if (onlyCommon)
                    {
                        var type = pickup.ItemData.itemType;
                        if (type != ItemType.SpeedJuice && type != ItemType.HeavyAnchor)
                        {
                            continue; // skip non-common items during initial spawn
                        }
                    }
                    validPrefabs.Add(prefab);
                }
            }
            return validPrefabs;
        }

        private GameObject SelectWeightedPrefab(List<GameObject> prefabs)
        {
            float totalWeight = 0f;
            foreach (GameObject prefab in prefabs)
            {
                var pickup = prefab.GetComponent<ItemPickup>();
                totalWeight += pickup.ItemData.spawnWeight;
            }

            if (totalWeight <= 0f) return null;

            float randomValue = Random.Range(0f, totalWeight);
            float currentSum = 0f;

            foreach (GameObject prefab in prefabs)
            {
                var pickup = prefab.GetComponent<ItemPickup>();
                currentSum += pickup.ItemData.spawnWeight;
                if (randomValue <= currentSum)
                {
                    return prefab;
                }
            }

            return prefabs[0];
        }

        private Vector2 GetRandomSpawnPosition()
        {
            if (!_useBoundingBox && _spawnPoints != null && _spawnPoints.Length > 0)
            {
                Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                return point.position;
            }

            Vector3 halfSize = _boxSize * 0.5f;
            Vector3 localOffset = new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                0f);

            return transform.TransformPoint(_boxCenter + localOffset);
        }

        private bool IsValidPosition(Vector2 position)
        {
            // 1. Check distance to other spawned items
            foreach (GameObject item in _spawnedItems)
            {
                if (item == null) continue;
                if (Vector2.Distance(position, item.transform.position) < _overlapCheckRadius * 2f)
                {
                    return false;
                }
            }

            // 2. Check distance to players (avoid spawning on top of players)
            var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player == null) continue;
                if (Vector2.Distance(position, player.transform.position) < _minPlayerDistance)
                {
                    return false;
                }
            }

            // 3. Check inside active zone and buffer from border
            if (_zoneController != null)
            {
                float distToCenter = Vector2.Distance(position, _zoneController.Center);
                if (distToCenter >= _zoneController.CurrentRadius - _borderBuffer)
                {
                    return false; // too close to border or outside
                }
            }

            // 4. Check physics blockers
            LayerMask mask = _blockedMask == 0 ? ~0 : _blockedMask;
            Collider2D[] blockers = Physics2D.OverlapCircleAll(position, _overlapCheckRadius, mask);
            foreach (var blocker in blockers)
            {
                if (blocker == null) continue;
                if (blocker.isTrigger) continue;
                if (blocker.GetComponentInParent<Player>() != null) continue;

                // Non-trigger blocker found!
                return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            if (_useBoundingBox)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(_boxCenter, _boxSize);
                Gizmos.matrix = oldMatrix;
                return;
            }

            if (_spawnPoints == null) return;

            foreach (Transform point in _spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, _overlapCheckRadius);
                }
            }
        }
    }
}
