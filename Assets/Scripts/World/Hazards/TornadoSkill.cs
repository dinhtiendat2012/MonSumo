using MonSumo.Core;
using Unity.Netcode;
using UnityEngine;

public class TornadoSkill : NetworkBehaviour
{
    [Header("Tornado Settings")]
    [Tooltip("Lực đẩy của vòi rồng áp dụng lên Player")]
    [SerializeField] private float pushForce = 300f;
    [SerializeField] private Transform center;

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
        }
    }

}
