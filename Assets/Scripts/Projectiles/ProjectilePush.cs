using UnityEngine;
using Unity.Netcode;
using MonSumo.Core;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(NetworkObject))]
public class ProjectilePush : NetworkBehaviour
{
    private Rigidbody2D body;
    private GameObject owner;
    private Vector2 moveDirection;
    private float pushForce;
    private LayerMask targetMask;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Initialize the projectile after it is spawned by a skill. Run on Server.
    public void Launch(Vector2 direction, float speed, float force, float lifeTime, GameObject ownerObject, LayerMask mask)
    {
        if (!IsServer) return;

        owner = ownerObject;
        moveDirection = direction.normalized;
        pushForce = force;
        targetMask = mask;
        body.linearVelocity = moveDirection * speed;
        
        // Setup network auto-destruction
        StartCoroutine(DestroyAfterDelay(lifeTime));
    }

    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Trigger detection runs strictly on Server
        if (!IsServer) return;

        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        if ((targetMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        Rigidbody2D targetBody = other.attachedRigidbody;
        if (targetBody == null)
        {
            return;
        }

        // Apply knockback via Rpc if player, else apply directly on server
        Player player = targetBody.GetComponent<Player>();
        if (player != null)
        {
            Vector2 forceVector = moveDirection.normalized * pushForce;
            player.ApplyKnockbackRpc(forceVector);
        }
        else
        {
            PushUtility.ApplyImpulse(targetBody, moveDirection, pushForce);
        }

        GetComponent<NetworkObject>().Despawn();
    }
}
