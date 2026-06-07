using System.Collections;
using UnityEngine;

public class TimedBomb : MonoBehaviour
{
    private float explosionRadius;
    private float pushForce;
    private LayerMask targetMask;

    // Start the countdown and explode after fuseTime seconds.
    public void Arm(float fuseTime, float radius, float force, LayerMask mask)
    {
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetMask);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D targetBody = hit.attachedRigidbody;
            if (targetBody == null)
            {
                continue;
            }

            PushUtility.ApplyExplosionImpulse(targetBody, transform.position, pushForce);
        }

        Destroy(gameObject);
    }
}
