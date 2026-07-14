using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using MonSumo.Core;

[RequireComponent(typeof(CapsuleCollider2D))]
public class Wave : MonoBehaviour
{
    [SerializeField] private float pushForce = 20f;

    // Track players pushed during this activation cycle to prevent double pushes
    private readonly HashSet<ulong> _pushedPlayerIds = new HashSet<ulong>();

    private void Awake()
    {
        // Ensure there is a Kinematic Rigidbody2D so that OnTriggerEnter2D fires reliably in Netcode (server-side)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }
    }

    private void OnEnable()
    {
        _pushedPlayerIds.Clear();
    }

    private void FixedUpdate()
    {
        // Only the server should handle the collision detection and apply knockback
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        PushOverlappingPlayers();
    }

    private void OnDisable()
    {
        _pushedPlayerIds.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only the server should handle the collision detection and apply knockback
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            Debug.Log($"[Wave] OnTriggerEnter2D detected player: OwnerClientId={player.OwnerClientId}");
            TryPushPlayer(player);
        }
    }

    private void PushOverlappingPlayers()
    {
        CapsuleCollider2D capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (capsuleCollider == null)
        {
            Debug.LogError("[Wave] CapsuleCollider2D is null in PushOverlappingPlayers!");
            return;
        }

        // Query the physics world directly to bypass any OnEnable broadphase delay
        Vector2 center = transform.TransformPoint(capsuleCollider.offset);
        Vector2 size = new Vector2(capsuleCollider.size.x * Mathf.Abs(transform.lossyScale.x), capsuleCollider.size.y * Mathf.Abs(transform.lossyScale.y));
        CapsuleDirection2D direction = capsuleCollider.direction;
        float angle = transform.eulerAngles.z;

        Collider2D[] hits = Physics2D.OverlapCapsuleAll(center, size, direction, angle);

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            Player player = hit.GetComponentInParent<Player>();
            if (player != null)
            {
                TryPushPlayer(player);
            }
        }
    }

    private void TryPushPlayer(Player player)
    {
        ulong playerId = player.NetworkObjectId;
        if (_pushedPlayerIds.Contains(playerId))
        {
            return;
        }

        _pushedPlayerIds.Add(playerId);

        Vector2 dir = (player.transform.position - transform.position).normalized;
        if (dir.sqrMagnitude <= 0.0001f)
        {
            dir = Vector2.up;
        }

        player.ApplyKnockbackRpc(dir * pushForce);
        Debug.Log($"[Wave] Successfully called ApplyKnockbackRpc on player {player.OwnerClientId} (NetworkID: {playerId}) with force {pushForce} in direction {dir}");
    }
}

