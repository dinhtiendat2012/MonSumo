using Unity.Netcode;
using UnityEngine;

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
        Rigidbody2D rb = other.attachedRigidbody;

        if (rb != null)
        {
            Vector2 dir =
                (other.transform.position -
                 transform.position).normalized;


            rb.AddForce(dir * knockbackForce,
                        ForceMode2D.Impulse);
        }
    }
}