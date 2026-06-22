using System.Collections;
using UnityEngine;
using Unity.Netcode;
using MonSumo.Core;

[RequireComponent(typeof(NetworkObject))]
public class TimedBomb : NetworkBehaviour
{
    private float explosionRadius;
    private float pushForce;
    private LayerMask targetMask;

    // Start the countdown and explode after fuseTime seconds. Run on Server.
    public void Arm(float fuseTime, float radius, float force, LayerMask mask)
    {
        if (!IsServer) return;

        explosionRadius = radius;
        pushForce = force;
        targetMask = mask;
        StartCoroutine(ExplodeAfterDelay(fuseTime));
    }

    private IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode();
    }

    private void Explode()
    {
        if (!IsServer) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetMask);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D targetBody = hit.attachedRigidbody;
            if (targetBody == null)
            {
                continue;
            }

            // Apply knockback via RPC if player, else apply directly on server
            Player player = targetBody.GetComponent<Player>();
            if (player != null)
            {
                Vector2 diff = targetBody.worldCenterOfMass - (Vector2)transform.position;
                Vector2 pushDirection = diff.normalized;
                if (pushDirection.sqrMagnitude <= 0.0001f)
                {
                    pushDirection = Vector2.up;
                }
                player.ApplyKnockbackRpc(pushDirection * pushForce);
            }
            else
            {
                PushUtility.ApplyExplosionImpulse(targetBody, transform.position, pushForce);
            }
        }

        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
