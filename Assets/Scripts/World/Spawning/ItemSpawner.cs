using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] itemPrefabs;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool useBoundingBox;
    [SerializeField] private Vector3 boxCenter;
    [SerializeField] private Vector3 boxSize = new Vector3(8f, 0f, 8f);

    [Header("Rules")]
    [SerializeField] private float spawnCooldown = 5f;
    [SerializeField] private int maxItems = 3;
    [SerializeField] private float overlapCheckRadius = 0.75f;
    [SerializeField] private LayerMask blockedMask = 0;
    [SerializeField] private int maxAttempts = 12;

    private readonly List<ItemPickup> spawnedItems = new List<ItemPickup>();

    private void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(spawnCooldown);
            TrySpawnItem();
        }
    }

    // Try to spawn an item if the current item count is below the limit.
    public bool TrySpawnItem()
    {
        spawnedItems.RemoveAll(item => item == null);

        List<GameObject> validPrefabs = GetValidPrefabs();

        if (spawnedItems.Count >= maxItems || validPrefabs.Count == 0)
        {
            return false;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 position = GetRandomSpawnPosition();
            if (!IsValidPosition(position))
            {
                continue;
            }

            GameObject prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
            GameObject itemObject = Instantiate(prefab, position, Quaternion.identity);
            spawnedItems.Add(itemObject.GetComponent<ItemPickup>());
            return true;
        }

        return false;
    }

    private List<GameObject> GetValidPrefabs()
    {
        List<GameObject> validPrefabs = new List<GameObject>();

        if (itemPrefabs == null)
        {
            return validPrefabs;
        }

        foreach (GameObject prefab in itemPrefabs)
        {
            if (prefab != null && prefab.GetComponent<ItemPickup>() != null)
            {
                validPrefabs.Add(prefab);
            }
        }

        return validPrefabs;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (!useBoundingBox && spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return point.position;
        }

        Vector3 halfSize = boxSize * 0.5f;
        Vector3 localOffset = new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            Random.Range(-halfSize.y, halfSize.y),
            Random.Range(-halfSize.z, halfSize.z));

        return transform.TransformPoint(boxCenter + localOffset);
    }

    private bool IsValidPosition(Vector3 position)
    {
        foreach (ItemPickup item in spawnedItems)
        {
            if (item == null)
            {
                continue;
            }

            if (Vector3.Distance(position, item.transform.position) < overlapCheckRadius * 2f)
            {
                return false;
            }
        }

        Collider[] blockers = Physics.OverlapSphere(position, overlapCheckRadius, blockedMask, QueryTriggerInteraction.Collide);
        return blockers.Length == 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (useBoundingBox)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCenter, boxSize);
            Gizmos.matrix = oldMatrix;
            return;
        }

        if (spawnPoints == null)
        {
            return;
        }

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, overlapCheckRadius);
            }
        }
    }
}
