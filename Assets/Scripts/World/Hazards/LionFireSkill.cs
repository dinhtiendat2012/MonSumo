using MonSumo.Core;
using Unity.Netcode;
using UnityEngine;

public class LionFireSkill : NetworkBehaviour
{
    [Header("Lion Fire Settings")]
    [Tooltip("Lực đẩy của lửa áp dụng lên Player")]
    [SerializeField] private float pushForce = 200f;
    [SerializeField] private Transform center;
    private float cooldownTime = 0.5f;

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
              if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.zero;

            float force = pushForce;

            if (targetPlayer.ReceivedPushMultiplier != 1f)
                force *= targetPlayer.ReceivedPushMultiplier;

            targetPlayer.ApplyKnockbackRpc(dir * force);
            Debug.Log($"Player {targetPlayer.playerName.Value} received knockback from Lion Fire with force {force}");
        }
    }
}
