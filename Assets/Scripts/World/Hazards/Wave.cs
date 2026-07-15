using MonSumo.Core;

using Unity.Netcode;
using UnityEngine;

public class Wave : NetworkBehaviour
{
    [Header("Sóng Settings")]
    [Tooltip("Lực đẩy của sóng âm áp dụng lên Player")]
    [SerializeField] private float pushForce = 100f;
    [SerializeField] private Transform center;

    private CapsuleCollider2D capsule;

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider2D>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        if (other.gameObject.CompareTag("Player"))
        {
            Player targetPlayer = other.GetComponent<Player>();

            if (targetPlayer == null)
                return;

            Vector2 dir =
                ((Vector2)targetPlayer.transform.position -
                 (Vector2)center.position).normalized;

            // Khoảng cách từ tâm đến Player
            float distance = Vector2.Distance(targetPlayer.transform.position, center.position);

            // 0 = ở tâm, 1 = ở mép
            float radius = Mathf.Max(capsule.bounds.extents.x, capsule.bounds.extents.y);
            float t = Mathf.Clamp01(distance / radius);

            // Càng gần tâm càng mạnh
            float force = pushForce * (1f - t);

            if (targetPlayer.ReceivedPushMultiplier != 1f)
                force *= targetPlayer.ReceivedPushMultiplier;
            
            targetPlayer.ApplyKnockbackRpc(dir * force);
            Debug.Log($"Player {targetPlayer.playerName.Value} received knockback from Wave with force {force}");
        }
    }
}

