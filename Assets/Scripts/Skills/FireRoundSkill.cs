using Unity.Netcode;
using UnityEngine;
using MonSumo.Core;

public class FireRoundSkill : NetworkBehaviour
{
    private ulong ownerClientId;

    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 15f;

    public void Init(ulong ownerId)
    {
        ownerClientId = ownerId;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Damage chỉ xử lý trên server

        NetworkObject targetNetworkObject =
            other.GetComponent<NetworkObject>();

        if (targetNetworkObject == null)
            return;

        // Bỏ qua chính mình
        if (targetNetworkObject.OwnerClientId == ownerClientId)
            return;

        // Knockback
        Player targetPlayer = other.GetComponentInParent<Player>();
        if (targetPlayer == null)
        {
            targetPlayer = other.GetComponent<Player>();
        }

        if (targetPlayer != null)
        {
            Vector2 dir = (other.transform.position - transform.position).normalized;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = Vector2.up;
            }

            float force = knockbackForce;
            if (targetPlayer.ReceivedPushMultiplier != 1f)
            {
                force *= targetPlayer.ReceivedPushMultiplier;
            }

            targetPlayer.ApplyKnockbackRpc(dir * force);
            Debug.Log($"[Skill] Server: FireRoundSkill pushed Player {targetPlayer.OwnerClientId} with force {force}");
        }
    }
}