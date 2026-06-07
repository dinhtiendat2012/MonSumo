using UnityEngine;

public static class PushUtility
{
    // Push a Rigidbody in the given direction with an impulse force.
    public static void ApplyImpulse(Rigidbody2D target, Vector2 direction, float force)
    {
        if (target == null)
        {
            return;
        }

        Vector2 pushDirection = direction.normalized;
        if (pushDirection.sqrMagnitude <= 0.0001f)
        {
            pushDirection = Vector2.up;
        }

        target.AddForce(pushDirection * force, ForceMode2D.Impulse);
    }

    // Push a Rigidbody away from a center point.
    public static void ApplyExplosionImpulse(Rigidbody2D target, Vector2 center, float force)
    {
        if (target == null)
        {
            return;
        }

        Vector2 direction = target.worldCenterOfMass - center;
        ApplyImpulse(target, direction, force);
    }
}
