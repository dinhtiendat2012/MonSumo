using UnityEngine;
using MonSumo.Core;

[CreateAssetMenu(menuName = "MonSumo/Skills/Melee Push Skill")]
public class MeleePushSkill : SkillDefinition
{
    [SerializeField] private float pushForce = 14f;
    [SerializeField] private float radius = 2.25f;
    [SerializeField] private float forwardOffset = 1.4f;
    [SerializeField] private LayerMask targetMask = ~0;

    public override void Activate(PlayerInventory owner)
    {
        Transform origin = owner.SkillOrigin;
        Vector2 direction = GetActivationDirection(origin);
        Vector2 center = (Vector2)origin.position + direction * forwardOffset;

        // Scan the area in front of the player and push Rigidbody2D targets away from the activation center.
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, targetMask);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D targetBody = hit.attachedRigidbody;
            if (targetBody == null || targetBody.transform == owner.transform)
            {
                continue;
            }

            Player player = targetBody.GetComponent<Player>();
            if (player != null)
            {
                Vector2 diff = targetBody.worldCenterOfMass - center;
                Vector2 pushDirection = diff.normalized;
                if (pushDirection.sqrMagnitude <= 0.0001f)
                {
                    pushDirection = Vector2.up;
                }
                player.ApplyKnockbackRpc(pushDirection * pushForce);
            }
            else
            {
                PushUtility.ApplyExplosionImpulse(targetBody, center, pushForce);
            }
        }
    }

    private Vector2 GetActivationDirection(Transform origin)
    {
        Vector2 direction = origin.right;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = origin.up;
        }

        return direction.normalized;
    }
}
