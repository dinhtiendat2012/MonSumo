using System.Collections;
using UnityEngine;

public class TimedBomb : MonoBehaviour
{
    private float explosionRadius;
    private float pushForce;
    private float upwardBoost;
    private LayerMask targetMask;

    // Start the countdown and explode after fuseTime seconds.
    public void Arm(float fuseTime, float radius, float force, float upward, LayerMask mask)
    {
        explosionRadius = radius;
        pushForce = force;
        upwardBoost = upward;
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
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetMask, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            Rigidbody targetBody = hit.attachedRigidbody;
            if (targetBody == null)
            {
                continue;
            }

            PushUtility.ApplyExplosionImpulse(targetBody, transform.position, pushForce, upwardBoost);
        }

        Destroy(gameObject);
    }
}
