using Unity.Netcode;
using UnityEngine;
using MonSumo.Core;

public class FireRoundSkill : NetworkBehaviour
{
    private ulong ownerClientId;
    private Player ownerPlayer;

    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 15f;

    public void Init(Player owner)
    {
        ownerPlayer = owner;
        ownerClientId = owner != null ? owner.OwnerClientId : 9999;
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

            // Get force from Caster Player Data dynamically, fallback to local serializeField config
            float force = (ownerPlayer != null && ownerPlayer.playerData != null)
                ? ownerPlayer.playerData.skillPushForce
                : knockbackForce;

            // Apply item/buff multipliers: (Current Push Force / Base Push Force)
            if (ownerPlayer != null && ownerPlayer.playerData != null && ownerPlayer.playerData.basePushForce > 0.01f)
            {
                float buffMultiplier = ownerPlayer.currentPushForce.Value / ownerPlayer.playerData.basePushForce;
                force *= buffMultiplier;
                Debug.Log($"[Skill] Skill Buff Multiplier applied: {buffMultiplier}x (New Base: {force})");
            }

            if (targetPlayer.ReceivedPushMultiplier != 1f)
            {
                force *= targetPlayer.ReceivedPushMultiplier;
            }

            targetPlayer.ApplyKnockbackRpc(dir * force);
            Debug.Log($"[Skill] Server: FireRoundSkill pushed Player {targetPlayer.OwnerClientId} with force {force}");
        }
    }
}