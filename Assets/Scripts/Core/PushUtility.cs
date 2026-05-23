using UnityEngine;

public static class PushUtility
{
    // Push a Rigidbody in the given direction with an impulse force.
    public static void ApplyImpulse(Rigidbody target, Vector3 direction, float force)
    {
        if (target == null)
        {
            return;
        }

        Vector3 pushDirection = direction.normalized;
        if (pushDirection.sqrMagnitude <= 0.0001f)
        {
            pushDirection = Vector3.up;
        }

        target.AddForce(pushDirection * force, ForceMode.Impulse);
    }

    // Push a Rigidbody away from a center point.
    public static void ApplyExplosionImpulse(Rigidbody target, Vector3 center, float force, float upwardBoost)
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.worldCenterOfMass - center;
        direction.y += upwardBoost;
        ApplyImpulse(target, direction, force);
    }
}
